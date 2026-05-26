using EKG.App.GamesManagement.Host.MCP.Tools;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddHttpClient("gamesmanagement-api", c =>
    c.BaseAddress = new Uri(builder.Configuration["GamesManagementApi:BaseUrl"]!));

builder.Services.AddMcpServer()
    .WithHttpTransport()
    .WithTools<GamesManagementTools>();

var app = builder.Build();

app.MapMcp("/mcp");

app.Run();
