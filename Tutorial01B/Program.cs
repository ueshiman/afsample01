using Tutorial01B.Extensions;

#pragma warning disable OPENAI001

var builder = WebApplication.CreateBuilder(args);

builder.Configuration
    .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
    .AddEnvironmentVariables();

builder.Services.AddOpenAIChatModule(builder.Configuration);
builder.Services.AddControllers();

var app = builder.Build();

app.MapControllers();

app.MapGet("/", () => Results.Text("Tutorial01B Web API is running."));
app.MapGet("/health", () => Results.Ok(new { status = "ok" }));

app.Run();
