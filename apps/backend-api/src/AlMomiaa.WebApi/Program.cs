using AlMomiaa.Application;
using AlMomiaa.Infrastructure;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddApplication();
builder.Services.AddInfrastructure();

var app = builder.Build();
app.MapGet("/health", () => Results.Ok(new { status = "Healthy" }))
    .AllowAnonymous();
app.Run();

public partial class Program { }
