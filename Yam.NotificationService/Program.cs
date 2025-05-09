using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Neo4jClient;
using System.Text;
using Yam.NotificationService.Configurations;
using Yam.NotificationService.Core.Interfaces;
using Yam.NotificationService.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllers();
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();
builder.Services.Configure<Neo4jConfig>(
    builder.Configuration.GetSection("Neo4jSettings"));
builder.Services.AddSingleton<IGraphClient>(provider =>
{
    var settings = provider.GetRequiredService<IOptions<Neo4jConfig>>().Value;
    var client = new GraphClient(new Uri(settings.Uri), settings.Username, settings.Password);
    client.ConnectAsync().Wait();
    return client;
});

builder.Services.AddAuthentication()
    .AddJwtBearer(JwtBearerDefaults.AuthenticationScheme, options =>
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
builder.Services.AddScoped(typeof(IMailService), typeof(EmailService));
builder.Services.AddScoped< ILogger, Logger<string>> ();
var app = builder.Build();
app.UseCors(builder =>
{
    builder.AllowAnyOrigin()
           .AllowAnyMethod()
           .AllowAnyHeader();
});
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
