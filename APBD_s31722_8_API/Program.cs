using Microsoft.AspNetCore.Mvc;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers(); 

var app = builder.Build();



app.UseHttpsRedirection();

app.UseRouting();
app.MapControllers();
app.Run();