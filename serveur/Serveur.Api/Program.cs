using Serveur.Domain.Interfaces;
using Serveur.Application;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllers();

// Register ConnectionManager as a Singleton service (S1 dependency)
builder.Services.AddSingleton<IConnectionManager, ConnectionManager>(); 

// Configure the application pipeline
var app = builder.Build();

// Configure the HTTP request pipeline.
app.UseHttpsRedirection();

// Map controllers, including AlarmStreamController
app.MapControllers();

// Run the application
await app.RunAsync();
