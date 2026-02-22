using CloudSoft.Models;
using CloudSoft.Services;

namespace CloudSoft.Services.UnitTests;

public class NewsletterServiceTests
{
    private readonly INewsletterService _sut;

    public NewsletterServiceTests()
    {
        _sut = new NewsletterService();
    }

    [Fact]
    public async Task SignUpForNewsletterAsync_WithValidSubscriber_ReturnsSuccess()
    {
        // Arrange
        var subscriber = new Subscriber { Name = "Test User", Email = "user@example.com" };

        // Act
        var result = await _sut.SignUpForNewsletterAsync(subscriber);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Contains("Welcome to our newsletter", result.Message);
    }

    [Fact]
    public async Task SignUpForNewsletterAsync_WithDuplicateEmail_ReturnsFailure()
    {
        // Arrange
        var subscriber1 = new Subscriber { Name = "Test User 1", Email = "duplicate@example.com" };
        var subscriber2 = new Subscriber { Name = "Test User 2", Email = "duplicate@example.com" };
        await _sut.SignUpForNewsletterAsync(subscriber1);

        // Act
        var result = await _sut.SignUpForNewsletterAsync(subscriber2);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Contains("already subscribed", result.Message);
    }

    [Fact]
    public async Task OptOutFromNewsletterAsync_WithExistingEmail_ReturnsSuccess()
    {
        // Arrange
        var subscriber = new Subscriber { Name = "Test User", Email = "optoutuser@example.com" };
        await _sut.SignUpForNewsletterAsync(subscriber);

        // Act
        var result = await _sut.OptOutFromNewsletterAsync("optoutuser@example.com");

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Contains("successfully removed", result.Message);
    }

    [Fact]
    public async Task OptOutFromNewsletterAsync_WithNonexistentEmail_ReturnsFailure()
    {
        // Act
        var result = await _sut.OptOutFromNewsletterAsync("nonexistent@example.com");

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Contains("couldn't find your subscription", result.Message);
    }

    [Fact]
    public async Task GetActiveSubscribersAsync_ReturnsAllSubscribers()
    {
        // Arrange
        var subscriber1 = new Subscriber { Name = "Test User 1", Email = "test1@example.com" };
        var subscriber2 = new Subscriber { Name = "Test User 2", Email = "test2@example.com" };
        await _sut.SignUpForNewsletterAsync(subscriber1);
        await _sut.SignUpForNewsletterAsync(subscriber2);

        // Act
        var subscribers = await _sut.GetActiveSubscribersAsync();

        // Assert
        Assert.True(subscribers.Count() >= 2); // At least 2 subscribers. Other tests add subscribers.
        Assert.Contains(subscribers, s => s.Email == "test1@example.com");
        Assert.Contains(subscribers, s => s.Email == "test2@example.com");
    }
}
