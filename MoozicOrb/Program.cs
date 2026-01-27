using Microsoft.AspNetCore.SignalR;
using MoozicOrb.Api.Services;
using MoozicOrb.Api.Services.Interfaces;
using MoozicOrb.Hubs;
using MoozicOrb.Infrastructure;
using MoozicOrb.Services;
using MoozicOrb.Services.Interfaces;
using MoozicOrb.Services.Radio;
using StackExchange.Redis;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllersWithViews();
builder.Services.AddSignalR();

builder.Services.AddScoped<IGroupMessageService, GroupMessageService>();
builder.Services.AddScoped<IDirectMessageService, DirectMessageService>();
builder.Services.AddScoped<IUserService, UserService>();

builder.Services.AddScoped<IUserAuthService, UserAuthService>();
builder.Services.AddScoped<ISessionStore, InMemorySessionStore>();

builder.Services.AddScoped<IGroupMessageApiService, GroupMessageApiService>();
builder.Services.AddScoped<IDirectMessageApiService, DirectMessageApiService>();

builder.Services.AddScoped<ILoginService, LoginService>();

// 2. Register the Broadcaster (The Sink)
// When you switch to WebRTC later, you only change THIS line.
builder.Services.AddSingleton<IAudioBroadcaster, SignalRAudioBroadcaster>();

// 3. Register the Radio Station (The DJ)
builder.Services.AddHostedService<RadioStationService>();

// SignalR services remain untouched

builder.Services.AddSingleton<UserConnectionManager>();

builder.Services.AddSingleton<IUserIdProvider, SignalRUserIdProvider>();
builder.Services.AddHttpContextAccessor();

// ---------------- REDIS ----------------
//builder.Services.AddSingleton<IConnectionMultiplexer>(sp =>
//{
//    var configuration = ConfigurationOptions.Parse(
//        builder.Configuration.GetConnectionString("Redis"),
//        true
//    );

//    configuration.AbortOnConnectFail = false;

//    return ConnectionMultiplexer.Connect(configuration);
//});

//// ---------------- STREAM SERVICES ----------------
//builder.Services.AddSingleton<IRedisStreamStateService, RedisStreamStateService>();
//builder.Services.AddSingleton<IStreamSessionService, StreamSessionService>();


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

app.MapHub<GroupHub>("/GroupHub");
app.MapHub<MessageHub>("/MessageHub");
app.MapHub<TestStreamHub>("/hubs/teststream");

app.MapHub<StreamHub>("/StreamHub");
app.MapHub<CallHub>("/CallHub");

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();
