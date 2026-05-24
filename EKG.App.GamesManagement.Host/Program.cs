using EKG.App.GamesManagement.BLL.Handlers;
using EKG.App.GamesManagement.BLL.Publishers;
using EKG.App.GamesManagement.DAL;
using EKG.Common.ACS;
using EKG.Common.App;
using EKG.Common.Cache.Redis;
using EKG.Common.Messages.Extensions;

var builder = WebApplication.CreateBuilder(args);

Startup.ConfigureCommonServices(builder.Services, builder.Configuration, builder.Host);
builder.Services.AddRedisCache(builder.Configuration);
builder.Services.AddAcs(builder.Configuration);
builder.Services.AddGamesManagementDal(builder.Configuration);

builder.Services.AddScoped<SaveGameHandler>();
builder.Services.AddScoped<SaveGameOverrideHandler>();
builder.Services.AddScoped<SaveOperatorFilterHandler>();
builder.Services.AddScoped<ImportGamesHandler>();

builder.Services.AddScoped<GamesChangedPublisher>();

builder.Services.AddMessageBroker("MessageBroker").Build();

var app = builder.Build();

Startup.UseCommon(app);

app.Run();
