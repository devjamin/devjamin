using Microsoft.EntityFrameworkCore;
using devjamin.Models;
using devjamin.Services;
using AspNetCoreRateLimit;
using devjamin.Controllers;
using Microsoft.AspNetCore.ResponseCompression;
using WebMarkupMin.AspNetCore6;
using Microsoft.Net.Http.Headers;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllersWithViews();

// Add database context

builder.Services.AddDbContext<BlogDbContext>(options =>
    options.UseMySql(builder.Configuration.GetConnectionString("DefaultConnection"),
    ServerVersion.AutoDetect(builder.Configuration.GetConnectionString("DefaultConnection"))));

// Configure identity services to use Entity Framework and DbContext
builder.Services.AddDefaultIdentity<ApplicationUser>(options =>
{
    options.User.RequireUniqueEmail = true;
    options.SignIn.RequireConfirmedEmail = false;
})
.AddEntityFrameworkStores<BlogDbContext>();

// Create admin user
builder.Services.AddHostedService<SystemInitController>();

// Add AWS S3 service
builder.Services.AddAWSService<Amazon.S3.IAmazonS3>(new Amazon.Extensions.NETCore.Setup.AWSOptions
{
    Credentials = new Amazon.Runtime.BasicAWSCredentials(
        builder.Configuration["AWS:AccessKey"],
        builder.Configuration["AWS:SecretKey"]),
    Region = Amazon.RegionEndpoint.GetBySystemName(builder.Configuration["AWS:Region"]),
    DefaultClientConfig = { ServiceURL = builder.Configuration["AWS:ServiceURL"] }
});

// Cleanup service for draft posts
builder.Services.AddHostedService<DraftCleanupService>();
// Email service and settings
builder.Services.Configure<EmailService.EmailSettings>(builder.Configuration.GetSection("EmailSettings"));
builder.Services.AddSingleton<IEmailService, EmailService>();

// Configure CORS policy
builder.Services.AddCors(options =>
{
    options.AddPolicy("SameOriginPolicy",
        builder =>
        {
            builder.WithOrigins("https://www.devjamin.com")
                   .AllowAnyHeader()
                   .AllowAnyMethod();
        });
});

builder.Services.AddMemoryCache();

// Rate limiting services
builder.Services.AddMemoryCache();
builder.Services.Configure<IpRateLimitOptions>(builder.Configuration.GetSection("IpRateLimiting"));
builder.Services.AddInMemoryRateLimiting();
builder.Services.AddSingleton<IRateLimitConfiguration, RateLimitConfiguration>();

// Add compression services
if (!builder.Environment.IsDevelopment())
{
    builder.Services.AddResponseCompression(options =>
    {
        options.EnableForHttps = true;
        options.Providers.Add<BrotliCompressionProvider>();
        options.Providers.Add<GzipCompressionProvider>();
    });

    // Configure compression level
    builder.Services.Configure<BrotliCompressionProviderOptions>(options =>
    {
        options.Level = System.IO.Compression.CompressionLevel.Optimal;
    });

    builder.Services.Configure<GzipCompressionProviderOptions>(options =>
    {
        options.Level = System.IO.Compression.CompressionLevel.Optimal;
    });
}

// Add WebMarkupMin services
if (!builder.Environment.IsDevelopment())
{
    builder.Services.AddWebMarkupMin(options =>
    {
        options.AllowMinificationInDevelopmentEnvironment = true;
        options.AllowCompressionInDevelopmentEnvironment = true;
    })
    .AddHtmlMinification(options =>
    {
        options.MinificationSettings.RemoveRedundantAttributes = true;
        options.MinificationSettings.RemoveHttpProtocolFromAttributes = true;
        options.MinificationSettings.RemoveHttpsProtocolFromAttributes = true;
        options.MinificationSettings.MinifyEmbeddedCssCode = true;
        options.MinificationSettings.MinifyInlineCssCode = true;
        options.MinificationSettings.MinifyEmbeddedJsCode = true;
        options.MinificationSettings.MinifyInlineJsCode = true;
        options.MinificationSettings.RemoveOptionalEndTags = false;
    });
}

var app = builder.Build();

// Add compression middleware
if (!app.Environment.IsDevelopment())
{
    app.UseResponseCompression();
}

app.UseResponseCaching();

// Use CORS policy
app.UseCors("SameOriginPolicy");

// Use rate limiting middleware
app.UseIpRateLimiting();

// Ensure database creation (use migrations in production)
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    var dbContext = services.GetRequiredService<BlogDbContext>();
}

// Configure HTTP request pipeline
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles(new StaticFileOptions
{
    OnPrepareResponse = ctx =>
    {
        const int durationInSeconds = 60 * 60 * 24 * 365; // 1 year
        ctx.Context.Response.Headers[HeaderNames.CacheControl] =
            $"public, max-age={durationInSeconds}";
    }
});

app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

// Add WebMarkupMin middleware (add this after UseRouting but before UseEndpoints)
if (!app.Environment.IsDevelopment())
{
    app.UseWebMarkupMin();
}

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();
