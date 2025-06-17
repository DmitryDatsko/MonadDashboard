using MonadDashboard.Configuration;
using MonadDashboard.Hubs;
using MonadDashboard.Services;
using MonadDashboard.Services.Hub;

var builder = WebApplication.CreateBuilder(args);
var myAllowSpecificOrigins = "_myAllowSpecificOrigins";

builder.Services.AddControllers();
builder.Services.AddSignalR();
builder.Services.AddSwaggerGen();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddMemoryCache();

builder.Services.Configure<EnvVariables>(
    builder.Configuration.GetSection(nameof(EnvVariables)));
builder.Services.AddSingleton<IRequests, Requests>();
builder.Services.AddSingleton<IDataProcessor, DataProcessor>();

builder.Services.AddHostedService<DataUpdateService>();

builder.Services.AddCors(options =>
{
    options.AddPolicy(name: myAllowSpecificOrigins,
        policyBuilder =>
        {
            policyBuilder.WithOrigins("https://monad-dashboard.duckdns.org/")
                .AllowAnyHeader()
                .AllowAnyMethod()
                .AllowCredentials();
        });
});

var app = builder.Build();
app.UseHttpsRedirection();
app.UseCors(myAllowSpecificOrigins);

app.UseSwagger();
app.UseSwaggerUI();

app.MapControllers();
app.MapHub<DashboardHub>("/dashboard");

app.Run();