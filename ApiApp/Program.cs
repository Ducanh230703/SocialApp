using ApiApp.Fillter;
using Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.FileProviders;
using ApiApp.Hubs; // Đảm bảo có using này

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
connectDB.conStr = builder.Configuration.GetConnectionString("DefaultConnection");
UserSerivce.apiHost = builder.Configuration.GetConnectionString("Defaultapihost");
PostService.apiAvatar= builder.Configuration.GetConnectionString("Defaultapihost");
UserSerivce.apiAvatar = builder.Configuration.GetConnectionString("Defaultapihost");
NotificationService.apiAvatar = builder.Configuration.GetConnectionString("Defaultapihost");
FriendRequestService.apiAvatar = builder.Configuration.GetConnectionString("Defaultapihost");
MessageService.apiAvatar = builder.Configuration.GetConnectionString("Defaultapihost");
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
        builder => builder.WithOrigins("https://localhost:7024", "https://localhost:7080")
                            .AllowAnyHeader()
                            .AllowAnyMethod()
                            .AllowCredentials());
});
var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}
app.UseCors("AllowSpecificOrigins");
app.UseHttpsRedirection();

app.UseAuthentication();
app.UseAuthorization();
app.UseMiddleware<Middleware>();


// Hoặc nếu bạn dùng default route cho tất cả MVC controllers:
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}"
);
app.MapHub<ChatHub>("chathub");
app.Run();