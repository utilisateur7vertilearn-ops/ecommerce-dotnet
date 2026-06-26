using System.Net;
using System.Net.Http.Json;
using ECommerce.Catalog.Api.Data;
using ECommerce.Catalog.Api.Endpoints;
using ECommerce.Catalog.Api.Models;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace ECommerce.Catalog.Tests;

public class CatalogApiFactory : WebApplicationFactory<Program>
{
    // One fixed name per factory instance — shared across all request scopes.
    private readonly string _dbName = Guid.NewGuid().ToString();

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureServices(services =>
        {
            var descriptor = services.SingleOrDefault(
                d => d.ServiceType == typeof(DbContextOptions<CatalogDbContext>));
            if (descriptor is not null)
                services.Remove(descriptor);

            services.AddDbContext<CatalogDbContext>(options =>
                options.UseInMemoryDatabase(_dbName));
        });
    }
}

public class GetProductsTests(CatalogApiFactory factory) : IClassFixture<CatalogApiFactory>
{
    private readonly HttpClient _client = factory.CreateClient();

    [Fact]
    public async Task GetProducts_ReturnsOk_WithFourSeededProducts()
    {
        var response = await _client.GetAsync("/api/products");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var products = await response.Content.ReadFromJsonAsync<List<Product>>();
        Assert.NotNull(products);
        Assert.Equal(4, products.Count);
    }

    [Fact]
    public async Task GetProductById_ExistingId_ReturnsOkWithCorrectProduct()
    {
        var response = await _client.GetAsync("/api/products/1");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var product = await response.Content.ReadFromJsonAsync<Product>();
        Assert.NotNull(product);
        Assert.Equal(1, product.Id);
        Assert.Equal("Mechanical Keyboard", product.Name);
        Assert.Equal(119.99m, product.Price);
    }

    [Fact]
    public async Task GetProductById_NonExistentId_ReturnsNotFound()
    {
        var response = await _client.GetAsync("/api/products/999");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }
}

public class CreateProductTests(CatalogApiFactory factory) : IClassFixture<CatalogApiFactory>
{
    private readonly HttpClient _client = factory.CreateClient();

    [Fact]
    public async Task CreateProduct_ValidRequest_ReturnsCreatedWithLocationHeader()
    {
        var request = new CreateProductRequest("Test Widget", "A test product", 9.99m, 10);

        var response = await _client.PostAsJsonAsync("/api/products", request);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        Assert.NotNull(response.Headers.Location);
        var created = await response.Content.ReadFromJsonAsync<Product>();
        Assert.NotNull(created);
        Assert.True(created.Id > 0);
        Assert.Equal("Test Widget", created.Name);
        Assert.Equal(9.99m, created.Price);
        Assert.Equal(10, created.AvailableStock);
    }

    [Fact]
    public async Task CreateProduct_NullDescription_ReturnsCreatedWithNullDescription()
    {
        var request = new CreateProductRequest("No-Desc Product", null, 1.00m, 5);

        var response = await _client.PostAsJsonAsync("/api/products", request);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var created = await response.Content.ReadFromJsonAsync<Product>();
        Assert.NotNull(created);
        Assert.Null(created.Description);
    }

    [Fact]
    public async Task CreateProduct_ZeroStock_ReturnsCreatedWithZeroStock()
    {
        var request = new CreateProductRequest("Out-of-stock Item", "No units left", 49.99m, 0);

        var response = await _client.PostAsJsonAsync("/api/products", request);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var created = await response.Content.ReadFromJsonAsync<Product>();
        Assert.NotNull(created);
        Assert.Equal(0, created.AvailableStock);
    }

    [Fact]
    public async Task CreateProduct_ThenGetById_ReturnsSameProduct()
    {
        var request = new CreateProductRequest("Round-trip Widget", "Check GET after POST", 19.99m, 3);
        var createResponse = await _client.PostAsJsonAsync("/api/products", request);
        var created = await createResponse.Content.ReadFromJsonAsync<Product>();
        Assert.NotNull(created);

        var getResponse = await _client.GetAsync($"/api/products/{created.Id}");

        Assert.Equal(HttpStatusCode.OK, getResponse.StatusCode);
        var fetched = await getResponse.Content.ReadFromJsonAsync<Product>();
        Assert.NotNull(fetched);
        Assert.Equal(created.Id, fetched.Id);
        Assert.Equal("Round-trip Widget", fetched.Name);
        Assert.Equal(19.99m, fetched.Price);
    }

    [Theory]
    [InlineData("Laptop", "High-end machine", 1299.99, 15)]
    [InlineData("USB Cable", null, 4.99, 500)]
    [InlineData("Monitor Stand", "Adjustable arm", 0.00, 30)]
    public async Task CreateProduct_VariousValidInputs_ReturnsCreated(
        string name, string? description, double price, int stock)
    {
        var request = new CreateProductRequest(name, description, (decimal)price, stock);

        var response = await _client.PostAsJsonAsync("/api/products", request);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
    }
}
