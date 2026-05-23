using EKG.App.GamesManagement.BLL.Handlers;
using EKG.App.GamesManagement.Model;
using Microsoft.AspNetCore.Mvc;

namespace EKG.App.GamesManagement.Host.Controllers;

[ApiController]
[Route("games")]
public class GamesController : ControllerBase
{
    private readonly SaveGameHandler _saveGameHandler;
    private readonly SaveGameOverrideHandler _saveGameOverrideHandler;
    private readonly SaveOperatorFilterHandler _saveOperatorFilterHandler;
    private readonly ImportGamesHandler _importGamesHandler;

    public GamesController(
        SaveGameHandler saveGameHandler,
        SaveGameOverrideHandler saveGameOverrideHandler,
        SaveOperatorFilterHandler saveOperatorFilterHandler,
        ImportGamesHandler importGamesHandler)
    {
        _saveGameHandler = saveGameHandler;
        _saveGameOverrideHandler = saveGameOverrideHandler;
        _saveOperatorFilterHandler = saveOperatorFilterHandler;
        _importGamesHandler = importGamesHandler;
    }

    [HttpPost("save")]
    public Task<SaveGameResponse> SaveGame(SaveGameRequest request) =>
        _saveGameHandler.Handle(request);

    [HttpPost("save-override")]
    public Task<SaveGameOverrideResponse> SaveGameOverride(SaveGameOverrideRequest request) =>
        _saveGameOverrideHandler.Handle(request);

    [HttpPost("save-filter")]
    public Task<SaveOperatorFilterResponse> SaveFilter(SaveOperatorFilterRequest request) =>
        _saveOperatorFilterHandler.Handle(request);

    [HttpPost("import")]
    [Consumes("multipart/form-data")]
    public Task<ImportGamesResponse> ImportGames(IFormFile file) =>
        _importGamesHandler.ImportAsync(file.OpenReadStream(), file.FileName);
}
