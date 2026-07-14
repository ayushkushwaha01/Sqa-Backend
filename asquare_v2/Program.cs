using System.Text;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using sqa_core.Data;
using sqa_core.Services;
using Resend;

var builder = WebApplication.CreateBuilder(args);

// 🟢 1️⃣ DATABASE
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// 🟢 2️⃣ JWT AUTHENTICATION 
var jwtKey = builder.Configuration["Jwt:Key"];
var key = Encoding.ASCII.GetBytes(jwtKey);

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.RequireHttpsMetadata = false;
    options.SaveToken = true;
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = false,
        ValidateAudience = false,
        ValidateIssuerSigningKey = true,
        IssuerSigningKey = new SymmetricSecurityKey(key),
        ClockSkew = TimeSpan.Zero
    };
});

// 🟢 3️⃣ CUSTOM SERVICES & RESEND (🔥 THIS IS WHAT FIXED THE ERROR)
builder.Services.AddResend(builder.Configuration["Resend:ApiKey"]);
builder.Services.AddScoped<IEmailService, EmailService>();

// 🟢 4️⃣ CONTROLLERS
builder.Services.AddControllers();

// 🟢 5️⃣ SWAGGER
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// 🟢 6️⃣ CORS
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAngular", policy =>
    {
        policy.WithOrigins(
            "http://localhost:4200",
            "https://localhost:4200"
        )
        .AllowAnyHeader()
        .AllowAnyMethod()
        .AllowCredentials();
    });
});

// 🟢 7️⃣ AWS S3 CONFIGURATION
var awsOptions = builder.Configuration.GetAWSOptions();
awsOptions.Credentials = new Amazon.Runtime.BasicAWSCredentials(
    builder.Configuration["AWS:AccessKey"],
    builder.Configuration["AWS:SecretKey"]
);

var regionName = builder.Configuration["AWS:Region"];
awsOptions.Region = Amazon.RegionEndpoint.GetBySystemName(regionName);

builder.Services.AddDefaultAWSOptions(awsOptions);
builder.Services.AddAWSService<Amazon.S3.IAmazonS3>();

var app = builder.Build();

// 🟢 8️⃣ MIDDLEWARE ORDER (VERY IMPORTANT)
app.UseHttpsRedirection();
app.UseCors("AllowAngular");
app.UseAuthentication();
app.UseAuthorization();

// 🟢 9️⃣ SWAGGER ENABLE
app.UseSwagger();
app.UseSwaggerUI();

// 🟢 🔟 MAP CONTROLLERS
app.MapControllers();

app.Run();