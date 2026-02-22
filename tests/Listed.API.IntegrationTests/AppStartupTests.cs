namespace Listed.API.IntegrationTests;

[Trait("Category", "Integration")]
public sealed class AppStartupTests : IClassFixture<ApiWebApplicationFactory>
{
    private readonly ApiWebApplicationFactory _factory;

    public AppStartupTests(ApiWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task GetRoot_ReturnsNotFound()
    {
        using var client = _factory.CreateClient();

        var response = await client.GetAsync("/");

        Assert.Equal(System.Net.HttpStatusCode.NotFound, response.StatusCode);
    }
}
