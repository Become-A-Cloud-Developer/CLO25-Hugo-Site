using System.Net;
using System.Net.Http.Headers;
using System.Text.RegularExpressions;

namespace CloudSoft.Web.Tests.IntegrationTests;

public class CvUploadTests : IDisposable
{
    private readonly CloudSoftWebApplicationFactory _factory;

    public CvUploadTests()
    {
        _factory = new CloudSoftWebApplicationFactory();
    }

    public void Dispose()
    {
        _factory.Dispose();
    }

    private HttpClient CreateClient() =>
        _factory.CreateClient(new Microsoft.AspNetCore.Mvc.Testing.WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        });

    private async Task<string> CreateJobAsAdmin(HttpClient client)
    {
        await client.LoginAsync("admin@cloudsoft.com", "Admin123!");

        var (createToken, _) = await client.GetAntiforgeryTokensAsync("/Job/Create");

        var jobData = new Dictionary<string, string>
        {
            ["Title"] = "CV Upload Test Job",
            ["Description"] = "Testing CV upload functionality",
            ["Location"] = "Oslo",
            ["Deadline"] = DateTime.UtcNow.AddDays(30).ToString("yyyy-MM-dd"),
            ["__RequestVerificationToken"] = createToken
        };

        await client.PostAsync("/Job/Create", new FormUrlEncodedContent(jobData));

        // Get job ID from listing
        var listHtml = await (await client.GetAsync("/Job")).Content.ReadAsStringAsync();
        var jobIdMatches = Regex.Matches(listHtml, @"/Job/Details/([^""]+)");
        Assert.True(jobIdMatches.Count > 0, "No job links found");
        return jobIdMatches[^1].Groups[1].Value;
    }

    private async Task SwitchToCandidate(HttpClient client)
    {
        // Logout admin
        var (logoutToken, _) = await client.GetAntiforgeryTokensAsync("/Job");
        await client.PostAsync("/Account/Logout",
            new FormUrlEncodedContent(new Dictionary<string, string>
            {
                ["__RequestVerificationToken"] = logoutToken
            }));

        // Login as candidate
        await client.LoginAsync("candidate@test.com", "Candidate123!");
    }

    [Fact]
    public async Task Apply_WithPdfUpload_Succeeds()
    {
        var client = CreateClient();

        var jobId = await CreateJobAsAdmin(client);
        await SwitchToCandidate(client);

        // Get antiforgery token from Apply page
        var (applyToken, _) = await client.GetAntiforgeryTokensAsync($"/Application/Apply/{jobId}");

        // Create a minimal PDF byte array
        var pdfBytes = "%PDF-1.0 minimal test file"u8.ToArray();

        using var formContent = new MultipartFormDataContent();
        formContent.Add(new StringContent(jobId), "JobId");
        formContent.Add(new StringContent("I am passionate about this role."), "CoverLetter");
        formContent.Add(new StringContent(applyToken), "__RequestVerificationToken");

        var fileContent = new ByteArrayContent(pdfBytes);
        fileContent.Headers.ContentType = new MediaTypeHeaderValue("application/pdf");
        formContent.Add(fileContent, "cvFile", "resume.pdf");

        var response = await client.PostAsync($"/Application/Apply/{jobId}", formContent);

        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        var redirectLocation = response.Headers.Location?.ToString() ?? "";
        Assert.Contains("MyApplications", redirectLocation);
    }

    [Fact]
    public async Task Apply_DuplicateApplication_ShowsError()
    {
        var client = CreateClient();

        var jobId = await CreateJobAsAdmin(client);
        await SwitchToCandidate(client);

        // First application
        var (applyToken1, _) = await client.GetAntiforgeryTokensAsync($"/Application/Apply/{jobId}");
        var formContent1 = new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["JobId"] = jobId,
            ["CoverLetter"] = "First application",
            ["__RequestVerificationToken"] = applyToken1
        });
        var response1 = await client.PostAsync($"/Application/Apply/{jobId}", formContent1);
        Assert.Equal(HttpStatusCode.Redirect, response1.StatusCode);

        // Second application to same job — should fail
        var (applyToken2, _) = await client.GetAntiforgeryTokensAsync($"/Application/Apply/{jobId}");
        var formContent2 = new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["JobId"] = jobId,
            ["CoverLetter"] = "Second application attempt",
            ["__RequestVerificationToken"] = applyToken2
        });
        var response2 = await client.PostAsync($"/Application/Apply/{jobId}", formContent2);

        Assert.Equal(HttpStatusCode.OK, response2.StatusCode);
        var html = await response2.Content.ReadAsStringAsync();
        Assert.Contains("already applied", html);
    }

    [Fact]
    public async Task DownloadCv_AsAdmin_ReturnsFile()
    {
        var client = CreateClient();

        var jobId = await CreateJobAsAdmin(client);
        await SwitchToCandidate(client);

        // Upload a CV
        var (applyToken, _) = await client.GetAntiforgeryTokensAsync($"/Application/Apply/{jobId}");
        var pdfBytes = "%PDF-1.0 test cv content for download"u8.ToArray();

        using var formContent = new MultipartFormDataContent();
        formContent.Add(new StringContent(jobId), "JobId");
        formContent.Add(new StringContent("Cover letter for download test"), "CoverLetter");
        formContent.Add(new StringContent(applyToken), "__RequestVerificationToken");

        var fileContent = new ByteArrayContent(pdfBytes);
        fileContent.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("application/pdf");
        formContent.Add(fileContent, "cvFile", "test-cv.pdf");

        await client.PostAsync($"/Application/Apply/{jobId}", formContent);

        // Switch back to admin
        var (logoutToken, _) = await client.GetAntiforgeryTokensAsync("/Application/MyApplications");
        await client.PostAsync("/Account/Logout",
            new FormUrlEncodedContent(new Dictionary<string, string>
            {
                ["__RequestVerificationToken"] = logoutToken
            }));
        await client.LoginAsync("admin@cloudsoft.com", "Admin123!");

        // Get the CV filename from the ForJob page
        var forJobResponse = await client.GetAsync($"/Application/ForJob/{jobId}");
        var forJobHtml = await forJobResponse.Content.ReadAsStringAsync();
        var cvMatch = System.Text.RegularExpressions.Regex.Match(forJobHtml, @"DownloadCv[?/](?:fileName=)?([^""&]+)");
        Assert.True(cvMatch.Success, "CV download link not found in ForJob page. HTML: " + forJobHtml[..Math.Min(500, forJobHtml.Length)]);

        var cvFileName = cvMatch.Groups[1].Value;

        // Download the CV (try both route formats)
        var downloadResponse = await client.GetAsync($"/Application/DownloadCv?fileName={cvFileName}");
        if (downloadResponse.StatusCode == HttpStatusCode.NotFound)
            downloadResponse = await client.GetAsync($"/Application/DownloadCv/{cvFileName}");
        Assert.Equal(HttpStatusCode.OK, downloadResponse.StatusCode);
        Assert.Equal("application/pdf", downloadResponse.Content.Headers.ContentType?.MediaType);

        var downloadedBytes = await downloadResponse.Content.ReadAsByteArrayAsync();
        Assert.True(downloadedBytes.Length > 0);
    }

    [Fact]
    public async Task Apply_WithOversizedFile_ReturnsError()
    {
        var client = CreateClient();

        var jobId = await CreateJobAsAdmin(client);
        await SwitchToCandidate(client);

        var (applyToken, _) = await client.GetAntiforgeryTokensAsync($"/Application/Apply/{jobId}");

        // Create a file >5MB (5 * 1024 * 1024 + 1 bytes)
        var oversizedBytes = new byte[5 * 1024 * 1024 + 1];
        // Write PDF header so it passes other checks
        var header = "%PDF-"u8;
        header.CopyTo(oversizedBytes);

        using var formContent = new MultipartFormDataContent();
        formContent.Add(new StringContent(jobId), "JobId");
        formContent.Add(new StringContent("Cover letter text."), "CoverLetter");
        formContent.Add(new StringContent(applyToken), "__RequestVerificationToken");

        var fileContent = new ByteArrayContent(oversizedBytes);
        fileContent.Headers.ContentType = new MediaTypeHeaderValue("application/pdf");
        formContent.Add(fileContent, "cvFile", "large-resume.pdf");

        var response = await client.PostAsync($"/Application/Apply/{jobId}", formContent);

        // Should return the form with validation error (200), not redirect
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var html = await response.Content.ReadAsStringAsync();
        Assert.Contains("5 MB", html);
    }

    [Fact]
    public async Task Apply_WithNonPdfFile_ReturnsError()
    {
        var client = CreateClient();

        var jobId = await CreateJobAsAdmin(client);
        await SwitchToCandidate(client);

        var (applyToken, _) = await client.GetAntiforgeryTokensAsync($"/Application/Apply/{jobId}");

        // Create a plain text file
        var textBytes = "This is not a PDF file"u8.ToArray();

        using var formContent = new MultipartFormDataContent();
        formContent.Add(new StringContent(jobId), "JobId");
        formContent.Add(new StringContent("Cover letter text."), "CoverLetter");
        formContent.Add(new StringContent(applyToken), "__RequestVerificationToken");

        var fileContent = new ByteArrayContent(textBytes);
        fileContent.Headers.ContentType = new MediaTypeHeaderValue("text/plain");
        formContent.Add(fileContent, "cvFile", "resume.txt");

        var response = await client.PostAsync($"/Application/Apply/{jobId}", formContent);

        // Should return the form with validation error (200), not redirect
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var html = await response.Content.ReadAsStringAsync();
        Assert.Contains("PDF", html);
    }
}
