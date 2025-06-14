using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Neo4jClient;
using System.Text;
using Yam.Core.sql.Entities;
using Yam.Core.sql;
using StackExchange.Redis;
using PostService.Core.Interfaces;
using PostService.Infrastructure;
using PostService.Serivces;
using Azure.Storage.Blobs;
using Yam.Core.SharedServices;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();
builder.Services.AddScoped(typeof(IPostRepository), typeof(PostRepository));
builder.Services.AddScoped(typeof(IFileService), typeof(FileService));
builder.Services.AddScoped(typeof(IPostService), typeof(PostServices));
builder.Services.AddScoped(typeof(ICommentService), typeof(CommentService));
builder.Services.AddScoped(typeof(ICommentRepository), typeof(CommentRepository));
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi

// 1. Add IHttpClientFactory to the service collection.

// 2. Register your typed client (DataService).
builder.Services.AddHttpClient("TextValidationService", client =>
{
    client.BaseAddress = new Uri("http://20.215.192.90:5000/");
});

builder.Services.AddHttpClient("ImageValidationService", client =>
{
    client.BaseAddress = new Uri("http://20.215.192.90:5000/");
});

builder.Services.AddHttpClient("CommunityValidationService", client =>
{
    client.BaseAddress = new Uri("https://20.215.192.90:5002/api/Commuity/");
})
.ConfigurePrimaryHttpMessageHandler(() =>
{
    // DANGER: This handler bypasses SSL certificate validation.
    // ONLY use this for local development and testing.
    return new HttpClientHandler
    {
        ServerCertificateCustomValidationCallback =
            (message, cert, chain, errors) => true
    };
});

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        ValidIssuer = builder.Configuration["jwt:issuer"],
        ValidAudience = builder.Configuration["jwt:audience"],
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(builder.Configuration["jwt:secretKey"])),
        ClockSkew = TimeSpan.Zero // Remove delay of token when expire
    });
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("sqlConnection")));

builder.Services.AddIdentity<ApplicationUser, ApplicationRole>()
            .AddEntityFrameworkStores<ApplicationDbContext>();

builder.Services.AddSingleton(x => new BlobServiceClient(
    builder.Configuration.GetConnectionString("azureFileStore")));

builder.Services.AddSingleton<IGraphClient>(provider =>
{
    var config = builder.Configuration;
    var client = new BoltGraphClient(new Uri("bolt+s://224f2923.databases.neo4j.io:7687"), config["ConnectionStrings:neo4j:Username"], config["ConnectionStrings:neo4j:Password"]);
    client.ConnectAsync().Wait();
    return client;
});

try
{
    IServiceCollection serviceCollection = builder.Services.AddScoped<IConnectionMultiplexer>((ServiceProvider) =>
    {
        var connection = builder.Configuration.GetConnectionString("redis");
        return ConnectionMultiplexer.Connect(connection);
    });

}
catch (Exception ex)
{
    Console.WriteLine(ex.Message);
}




builder.Services.AddOpenApi();



var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.UseSwaggerUI(options =>
        options.SwaggerEndpoint("/openapi/v1.json", "v1")
    );
}

app.UseHttpsRedirection();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();
