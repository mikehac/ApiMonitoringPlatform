using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.IdentityModel.Tokens;

var builder = WebApplication.CreateBuilder(args);

var jwtConfig = builder.Configuration.GetSection("Jwt");

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = jwtConfig["Issuer"],
            ValidAudience = jwtConfig["Audience"],
            IssuerSigningKey = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(jwtConfig["Key"]!))
        };
    });

builder.Services.AddAuthorization();

var app = builder.Build();

app.UseAuthentication();
app.UseAuthorization();

// Simulation middleware — applies global DelayMs and FailureRate to every request
app.Use(async (context, next) =>
{
    var sim = context.RequestServices.GetRequiredService<IConfiguration>().GetSection("Simulation");
    var delayMs = sim.GetValue<int>("DelayMs");
    var failureRate = sim.GetValue<double>("FailureRate");

    if (delayMs > 0)
        await Task.Delay(delayMs);

    if (failureRate > 0 && Random.Shared.NextDouble() < failureRate)
    {
        context.Response.StatusCode = 500;
        await context.Response.WriteAsJsonAsync(new { error = "Simulated failure" });
        return;
    }

    await next(context);
});

// In-memory product store
var products = new List<Product>
{
    new(1, "Widget A", 9.99m),
    new(2, "Widget B", 19.99m),
};
var nextId = 3;

// POST /auth/token — issues a JWT for testing /orders
app.MapPost("/auth/token", (LoginRequest req, IConfiguration config) =>
{
    var jwt = config.GetSection("Jwt");
    if (req.Username != jwt["TestUser"] || req.Password != jwt["TestPassword"])
        return Results.Unauthorized();

    var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwt["Key"]!));
    var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
    var token = new JwtSecurityToken(
        issuer: jwt["Issuer"],
        audience: jwt["Audience"],
        claims: [new Claim(ClaimTypes.Name, req.Username)],
        expires: DateTime.UtcNow.AddHours(1),
        signingCredentials: creds);

    return Results.Ok(new { token = new JwtSecurityTokenHandler().WriteToken(token) });
});

// GET /health
app.MapGet("/health", () => Results.Ok(new { status = "Healthy", timestamp = DateTime.UtcNow }));

// GET /products
app.MapGet("/products", () => Results.Ok(products));

// POST /products
app.MapPost("/products", (ProductRequest req) =>
{
    var product = new Product(nextId++, req.Name, req.Price);
    products.Add(product);
    return Results.Created($"/products/{product.Id}", product);
});

// PUT /products/{id}
app.MapPut("/products/{id}", (int id, ProductRequest req) =>
{
    var index = products.FindIndex(p => p.Id == id);
    if (index < 0) return Results.NotFound();
    products[index] = new Product(id, req.Name, req.Price);
    return Results.Ok(products[index]);
});

// DELETE /products/{id}
app.MapDelete("/products/{id}", (int id) =>
{
    var removed = products.RemoveAll(p => p.Id == id);
    return removed > 0 ? Results.NoContent() : Results.NotFound();
});

// GET /orders — JWT protected
app.MapGet("/orders", [Authorize] () =>
{
    var orders = new[]
    {
        new { Id = 1, Product = "Widget A", Quantity = 3, Total = 29.97m },
        new { Id = 2, Product = "Widget B", Quantity = 1, Total = 19.99m },
    };
    return Results.Ok(orders);
});

// GET /reports — simulates a slow computation
app.MapGet("/reports", async (IConfiguration config) =>
{
    var extraDelay = config.GetSection("Simulation").GetValue<int>("ReportsExtraDelayMs");
    if (extraDelay > 0)
        await Task.Delay(extraDelay);

    return Results.Ok(new
    {
        generated = DateTime.UtcNow,
        rows = 10_000,
        summary = "Monthly sales report",
    });
});

// GET /external-data — simulates a flaky external dependency
app.MapGet("/external-data", (IConfiguration config) =>
{
    var rate = config.GetSection("Simulation").GetValue<double>("ExternalFailureRate");
    if (rate > 0 && Random.Shared.NextDouble() < rate)
    {
        return Results.Problem(
            title: "External Service Unavailable",
            statusCode: 503,
            detail: "The upstream service did not respond.");
    }

    return Results.Ok(new { source = "external-service", data = "sample payload", fetchedAt = DateTime.UtcNow });
});

app.Run();

record Product(int Id, string Name, decimal Price);
record ProductRequest(string Name, decimal Price);
record LoginRequest(string Username, string Password);
