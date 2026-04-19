using System.Text.RegularExpressions;
using Microsoft.Playwright;

namespace CloudSoft.Auth.Web.PlaywrightTests;

[Parallelizable(ParallelScope.Self)]
public class Exercise43Tests : TestBase
{
    [Test]
    public async Task Admin_Sees_Department_Claim_In_Claims_Table()
    {
        await TestHelpers.LoginAsAdminAsync(Page);
        await Page.GotoAsync("/WhoAmI");

        var table = Page.GetByTestId("whoami-claims-table");
        await Expect(table).ToContainTextAsync("Department");
        await Expect(table).ToContainTextAsync("Engineering");
    }

    [Test]
    public async Task Candidate_Sees_Sales_Department_In_Claims_Table()
    {
        await TestHelpers.LoginAsCandidateAsync(Page);
        await Page.GotoAsync("/WhoAmI");

        var table = Page.GetByTestId("whoami-claims-table");
        await Expect(table).ToContainTextAsync("Sales");
    }

    [Test]
    public async Task Admin_With_Engineering_Claim_Can_Access_EngineeringPage()
    {
        await TestHelpers.LoginAsAdminAsync(Page);
        var response = await Page.GotoAsync("/Engineering");

        Assert.That(response?.Status, Is.EqualTo(200));
        await Expect(Page.GetByTestId("engineering-heading")).ToBeVisibleAsync();
    }

    [Test]
    public async Task Candidate_Without_Engineering_Claim_Gets_AccessDenied()
    {
        await TestHelpers.LoginAsCandidateAsync(Page);
        await Page.GotoAsync("/Engineering");

        await Expect(Page).ToHaveURLAsync(new Regex(@"/Account/AccessDenied"));
    }

    [Test]
    public async Task Antiforgery_Form_Succeeds_With_Token()
    {
        await TestHelpers.LoginAsAdminAsync(Page);
        await Page.GotoAsync("/WhoAmI");

        await Page.GetByTestId("csrf-message").FillAsync("playwright-hello");
        await Page.GetByTestId("csrf-submit").ClickAsync();

        await Expect(Page.GetByTestId("csrf-result"))
            .ToContainTextAsync("Received: playwright-hello");
    }

    [Test]
    public async Task Antiforgery_Post_Without_Token_Returns_400()
    {
        await TestHelpers.LoginAsAdminAsync(Page);

        // Make a raw POST to the TestPost action without the antiforgery token field.
        // The auth cookie is automatically sent because we use the Page's context.
        var form = Context.APIRequest.CreateFormData();
        form.Set("message", "sneaky");

        var response = await Context.APIRequest.PostAsync("/WhoAmI/TestPost", new APIRequestContextOptions
        {
            Form = form,
        });

        Assert.That(response.Status, Is.EqualTo(400),
            $"expected HTTP 400 when antiforgery token is missing, got {response.Status}");
    }
}
