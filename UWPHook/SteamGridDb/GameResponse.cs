namespace UWPHook.SteamGridDb;

internal sealed class GameResponse
{
    /// <summary>
    /// SteamGridDB id of the game.
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// Name of the game in SteamGridDB.
    /// </summary>
    public string? Name { get; set; }
}
