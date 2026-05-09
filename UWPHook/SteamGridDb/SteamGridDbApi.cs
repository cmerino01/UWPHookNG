using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Threading.Tasks;
using Serilog;
using UWPHook.Properties;

namespace UWPHook.SteamGridDb;

internal sealed class SteamGridDbApi
{
    private const string BaseUrl = "https://www.steamgriddb.com/api/v2/";

    private static readonly JsonSerializerOptions s_jsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    private readonly HttpClient _httpClient;
    private readonly Settings _settings;

    /// <summary>
    /// Constructor
    /// </summary>
    /// <param name="apiKey">A SteamGridDB API key retrieved from https://www.steamgriddb.com/profile/preferences </param>
    public SteamGridDbApi(string apiKey)
    {
        _settings = Settings.Default;
        _httpClient = new HttpClient
        {
            BaseAddress = new Uri(BaseUrl)
        };
        _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);
    }

    /// <summary>
    /// Search SteamGridDB for a game.
    /// </summary>
    /// <param name="gameName">Name of the game</param>
    /// <returns>Array of games corresponding to the provided name</returns>
    public async Task<GameResponse[]?> SearchGame(string gameName)
    {
        var path = $"search/autocomplete/{gameName}";

        GameResponse[]? games = null;
        var response = await _httpClient.GetAsync(path);

        if (response.IsSuccessStatusCode)
        {
            var parsedResponse = await response.Content.ReadFromJsonAsync<ResponseWrapper<GameResponse>>(s_jsonOptions);
            games = parsedResponse?.Data;
        }
        else if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
        {
            Log.Verbose("ERROR RESPONSE: " + response);

            _settings.SteamGridDbApiKey = string.Empty;
            _settings.Save();

            Log.Error("Warning: SteamGrid API Key Invalid. Please generate a new key and add it to settings.");
            throw new TaskCanceledException("Warning: SteamGrid API Key Invalid. Please generate a new key and add it to settings.");
        }

        return games;
    }

    /// <summary>
    /// Builds the user-selected query parameters for SteamGridDB requests.
    /// </summary>
    /// <param name="dimensions">Comma separated list of resolutions, see https://www.steamgriddb.com/api/v2#tag/GRIDS</param>
    /// <returns>The formatted query parameters</returns>
    public string BuildParameters(string? dimensions)
    {
        var result = string.Empty;
        var style = _settings.SteamGridDB_Style[_settings.SelectedSteamGridDB_Style];
        var type = _settings.SteamGridDB_Type[_settings.SelectedSteamGridDB_Type];
        var nsfw = _settings.SteamGridDB_nfsw[_settings.SelectedSteamGridDB_nfsw];
        var humor = _settings.SteamGridDB_Humor[_settings.SelectedSteamGridDB_Humor];

        if (!string.IsNullOrEmpty(dimensions))
            result += $"dimensions={dimensions}&";

        if (type != "any")
            result += $"types={type}&";

        if (style != "any")
            result += $"styles={style}&";

        if (nsfw != "any")
            result += $"nsfw={nsfw}&";

        if (humor != "any")
            result += $"humor={humor}&";

        return result;
    }

    /// <summary>
    /// Performs a request to a given url and returns the parsed image array.
    /// </summary>
    public async Task<ImageResponse[]?> GetResponse(string url)
    {
        var response = await _httpClient.GetAsync(url);
        ImageResponse[]? images = null;

        if (response.IsSuccessStatusCode)
        {
            var parsedResponse = await response.Content.ReadFromJsonAsync<ResponseWrapper<ImageResponse>>(s_jsonOptions);
            if (parsedResponse is { Success: true })
            {
                images = parsedResponse.Data;
            }
        }

        return images;
    }

    public Task<ImageResponse[]?> GetGameGrids(int gameId, string? dimensions = null) =>
        GetResponse($"grids/game/{gameId}?{BuildParameters(dimensions)}");

    public Task<ImageResponse[]?> GetGameHeroes(int gameId, string? dimensions = null) =>
        GetResponse($"heroes/game/{gameId}?{BuildParameters(dimensions)}");

    public Task<ImageResponse[]?> GetGameLogos(int gameId, string? dimensions = null) =>
        GetResponse($"logos/game/{gameId}?{BuildParameters(dimensions)}");

    private sealed class ResponseWrapper<T>
    {
        public bool Success { get; set; }
        public T[]? Data { get; set; }
    }
}
