using ZellburyQueueSim.Components;
using ZellburyQueueSim.Services;

var builder = WebApplication.CreateBuilder(args);

// Add Razor Components with interactive server-side rendering (Blazor Server model).
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

// Application services (registered as scoped: one instance per user circuit,
// which is what we want since each user's simulation run/config is independent).
builder.Services.AddScoped<RandomGenerator>();
builder.Services.AddScoped<StatisticsService>();
builder.Services.AddSingleton<ZellburyDataService>(); // static reference data, safe to share

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseAntiforgery();

app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();
