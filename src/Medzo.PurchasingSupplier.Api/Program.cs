using Medzo.PurchasingSupplier.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddProblemDetails();
builder.Services.AddHealthChecks();
builder.Services.AddInfrastructure(builder.Configuration);

var app = builder.Build();

app.MapControllers();
app.MapHealthChecks("/health");

app.Run();

public partial class Program;
