using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using CompanyManagementSystem.Data;
using CompanyManagementSystem.Models;
using CompanyManagementSystem.Services;
using System.Collections.Generic;
using Microsoft.AspNetCore.Identity;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseInMemoryDatabase("CompanyManagementSystem"));

// Add email services
builder.Services.AddScoped<IEmailService, EmailService>();
builder.Services.AddHostedService<PurchaseOrderNotificationService>();

// Add JWT Authentication
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = builder.Configuration["Jwt:Issuer"],
            ValidAudience = builder.Configuration["Jwt:Audience"],
            IssuerSigningKey = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(builder.Configuration["Jwt:Key"] ?? "Your_Actual_Secret_Key_Here_With_Minimum_32_Bytes_Length_For_HS256_Algorithm"))
        };
    });

builder.Services.AddControllers();
builder.Services.AddAuthorization();

// Add CORS services
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll",
        builder =>
        {
            builder.AllowAnyOrigin()
                   .AllowAnyMethod()
                   .AllowAnyHeader();
        });
});

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

//app.UseHttpsRedirection();

// Enable CORS
app.UseCors("AllowAll");

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    var context = services.GetRequiredService<AppDbContext>();

    // Create the database if it doesn't exist
    context.Database.EnsureCreated();

    // Always recreate the admin user to ensure consistent credentials
    // First remove any existing admin user
    var existingAdmin = context.Users.FirstOrDefault(u => u.Username == "admin");
    if (existingAdmin != null)
    {
        context.Users.Remove(existingAdmin);
        context.SaveChanges();
    }

    // Create a new admin user with known credentials
    var adminUser = new User
    {
        Username = "admin",
        PasswordHash = BCrypt.Net.BCrypt.HashPassword("password"), // Hash the password
        PurchaseOrders = new List<PurchaseOrder>() // Initialize PurchaseOrders
    };

    // Add the admin user to the database
    context.Users.Add(adminUser);
    context.SaveChanges();

    Console.WriteLine("Admin user created with username: 'admin' and password: 'password'");
}

app.Run();
