using Microsoft.AspNetCore.SignalR;
using MoozicOrb.Api.Services;
using MoozicOrb.Api.Services.Interfaces;
using MoozicOrb.Hubs;
using MoozicOrb.Infrastructure;
using MoozicOrb.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllersWithViews();
builder.Services.AddSignalR();

builder.Services.AddScoped<IGroupMessageService, GroupMessageService>();
builder.Services.AddScoped<IDirectMessageService, DirectMessageService>();
builder.Services.AddScoped<IUserService, UserService>();

builder.Services.AddScoped<IGroupMessageApiService, GroupMessageApiService>();
builder.Services.AddScoped<IDirectMessageApiService, DirectMessageApiService>();

// SignalR services remain untouched


builder.Services.AddSingleton<IUserIdProvider, SignalRUserIdProvider>();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseAuthorization();

app.MapHub<SyncHub>("SyncHub");
app.MapHub<MessageHub>("MessageHub");

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();
