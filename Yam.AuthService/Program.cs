using Yam.Core.sql.Entities;
using Yam.Core.sql;
using Microsoft.Extensions.Configuration;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using Yam.AuthService.Helper;
using Microsoft.AspNetCore.Identity;
using Yam.AuthService.Core.Interfaces;
using Yam.AuthService.Services;
using StackExchange.Redis;
using Microsoft.Extensions.Options;
using Neo4jClient;
using Yam.NotificationService.Configurations;
using Microsoft.Extensions.DependencyInjection;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();
builder.Services.AddAutoMapper(m => m.AddProfile(new MapperProfile()));
builder.Services.AddScoped(typeof(IAuthService), typeof(AuthServices));
builder.Services.AddScoped(typeof(ILogger), typeof(Logger<string>));

builder.Services.Configure<Neo4jConfig>(
    builder.Configuration.GetSection("Neo4jSettings"));
//neo4j config
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
builder.Services.AddDbContext<ApplicationDbContext>(options =>
options.UseSqlServer(
            builder.Configuration.GetConnectionString("sqlConnection")),ServiceLifetime.Transient);

builder.Services.AddIdentity<ApplicationUser, ApplicationRole>()
            .AddEntityFrameworkStores<ApplicationDbContext>()
            .AddDefaultTokenProviders();
builder.Services.AddTransient<UserManager<ApplicationUser>>();

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
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
        };
    });
var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.UseSwaggerUI(options =>
        options.SwaggerEndpoint("/openapi/v1.json", "v1")
    );
    var role = new ApplicationRole()
    {
        Name = "admin"
    };
    var _roleManager = app.Services.CreateScope().ServiceProvider.GetRequiredService<RoleManager<ApplicationRole>>();
    if(await _roleManager.GetRoleIdAsync(role) is null)
        await _roleManager.CreateAsync(role);
}

app.UseHttpsRedirection();
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
