using CommunityService.Core.Interfaces;
using CommunityService.Infrastructure;
using CommunityService.Infrastructure.Repositories;
using Microsoft.EntityFrameworkCore;
using Yam.Core.sql.Entities;
using Yam.Core.sql;
using Neo4jClient;
using StackExchange.Redis;
using CommunityService.Services;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;

using Yam.Core.SharedServices;
using Azure.Storage.Blobs;


var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();
builder.Services.AddScoped(typeof(ICommunityRepo), typeof(CommunityRepo));
builder.Services.AddScoped(typeof(ICommunityService), typeof(CommunityServices));
builder.Services.AddScoped(typeof(IFileService), typeof(FileService));

//builder.Services.AddDbContext<CommunityDbContext>(options =>
//    options.UseSqlServer(
//        builder.Configuration.GetConnectionString("sqlConnection")), ServiceLifetime.Transient);


builder.Services.AddSingleton(x => new BlobServiceClient(
    builder.Configuration.GetConnectionString("azureFileStore")));


builder.Services.AddHttpClient("TextValidationService", client =>
{
    client.BaseAddress = new Uri("http://localhost:5000/");
});

builder.Services.AddHttpClient("ImageValidationService", client =>
{
    client.BaseAddress = new Uri("http://localhost:5000/");
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

builder.Services.AddSingleton<IGraphClient>(provider =>
{
    var config = builder.Configuration;
    var client = new BoltGraphClient(new Uri("bolt+s://224f2923.databases.neo4j.io:7687"), config["ConnectionStrings:neo4j:Username"], config["ConnectionStrings:neo4j:Password"]);
    client.ConnectAsync().Wait();
    return client;
});

try
{
    builder.Services.AddScoped<IConnectionMultiplexer>((ServiceProvider) =>
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

app.UseRouting();
app.UseCors(builder =>
{
    builder.AllowAnyOrigin()
    .AllowAnyMethod()
    .AllowAnyHeader();
});

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();
