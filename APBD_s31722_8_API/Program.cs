using APBD_s31722_8_API.Datalayer;
using APBD_s31722_8_API.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers(); 
builder.Services.AddScoped<DbClient>();
builder.Services.AddScoped<ClientService>();
builder.Services.AddScoped<TripService>();


var app = builder.Build();

app.UseRouting();
app.MapControllers();
app.Run();