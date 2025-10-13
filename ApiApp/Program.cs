using ApiApp.Fillter;
using ApiApp.Hubs; // Đảm bảo có using này
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.FileProviders;
using Models;
using Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
connectDB.conStr = builder.Configuration.GetConnectionString("DefaultConnection");
UserSerivce.apiAvatar = builder.Configuration.GetConnectionString("Defaultapihost");
PostService.apiAvatar= builder.Configuration.GetConnectionString("Defaultapihost");
UserSerivce.apiAvatar = builder.Configuration.GetConnectionString("Defaultapihost");
GroupMemberService.apiAvatar = builder.Configuration.GetConnectionString("Defaultapihost");
NotificationService.apiAvatar = builder.Configuration.GetConnectionString("Defaultapihost");
FriendRequestService.apiAvatar = builder.Configuration.GetConnectionString("Defaultapihost");   
MessageService.apiAvatar = builder.Configuration.GetConnectionString("Defaultapihost");


builder.Services.Configure<SmtpSettings>(builder.Configuration.GetSection("SmtpSettings"));
builder.Services.AddTransient<EmailService>();


builder.Services.AddAuthentication(options =>
{
    options.DefaultScheme = Microsoft.AspNetCore.Authentication.Cookies.CookieAuthenticationDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = "Google";
})
.AddCookie()
.AddGoogle(options =>
{
    options.ClientId = builder.Configuration["Authentication:Google:ClientId"] ?? "";
    options.ClientSecret = builder.Configuration["Authentication:Google:ClientSecret"] ?? "";
    options.CallbackPath = "/api/User/GoogleCallback";
});
builder.Services.AddSignalR();  

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowSpecificOrigins",
        builder => builder.WithOrigins("https://localhost:7024", "https://localhost:7080", "https://socialmedia20250930142855-gegwd5esgrcvczdz.canadacentral-01.azurewebsites.net")
                            .AllowAnyHeader()
                            .AllowAnyMethod()
                            .AllowCredentials());
});
var app = builder.Build();

// Configure the HTTP request pipeline.
//if (app.Environment.IsDevelopment())
//{
//    app.UseSwagger();
//    app.UseSwaggerUI();
//}
var imageRootPath = Path.Combine(app.Environment.ContentRootPath, "Image");

app.UseStaticFiles(new StaticFileOptions
{
    FileProvider = new PhysicalFileProvider(imageRootPath),

    RequestPath = "/Image"
});

app.UseCors("AllowSpecificOrigins");
app.UseHttpsRedirection();

app.UseAuthentication();
app.UseAuthorization();
app.UseMiddleware<Middleware>();


app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}"
);
app.MapHub<ChatHub>("/chathub");
app.Run();