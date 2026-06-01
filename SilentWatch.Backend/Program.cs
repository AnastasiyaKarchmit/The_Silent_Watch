using System.Collections.Concurrent;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Options;

var builder = WebApplication.CreateBuilder(args);
var port = Environment.GetEnvironmentVariable("PORT") ?? "5227";
builder.WebHost.UseUrls($"http://0.0.0.0:{port}");

builder.Services.Configure<EdgegapOptions>(
    builder.Configuration.GetSection("Edgegap"));

builder.Services.AddSingleton<RoomStore>();
builder.Services.AddHttpClient<EdgegapDeploymentService>();

var app = builder.Build();

app.MapGet("/", () => Results.Ok("Silent Watch backend is running."));
app.MapGet("/health", () => Results.Ok(new { status = "ok" }));

app.MapPost("/rooms", (RoomStore store, CreateRoomRequest request) =>
{
    var room = store.CreateRoom(request.PlayerName);
    var player = room.Players[0];

    return Results.Ok(new CreateRoomResponse(
        room.Code,
        player.PlayerId,
        room.Status.ToString()));
});

app.MapPost("/rooms/{code}/join", async (
    RoomStore store,
    EdgegapDeploymentService edgegap,
    string code,
    JoinRoomRequest request) =>
{
    if (!store.TryGetRoom(code, out var room))
        return Results.NotFound("Room not found.");

    if (room.Players.Count >= 2)
        return Results.Conflict("Room is full.");

    if (room.Status != RoomStatus.WaitingForPlayers)
        return Results.Conflict("Room is not joinable.");

    var player = room.AddPlayer(request.PlayerName);

    // When second player joins, immediately start deployment.
    if (room.Players.Count == 2)
    {
        room.Status = RoomStatus.Deploying;

        try
        {
            string requestId = await edgegap.StartDeploymentAsync(room);
            room.EdgegapRequestId = requestId;

            _ = Task.Run(async () =>
            {
                try
                {
                    var endpoint = await edgegap.WaitForReadyAsync(requestId);

                    room.Host = endpoint.Host;
                    room.Port = endpoint.Port;
                    room.Status = RoomStatus.ServerReady;
                }
                catch (Exception exception)
                {
                    room.Status = RoomStatus.Failed;
                    Console.WriteLine($"Deployment failed for room {room.Code}: {exception}");
                }
            });
        }
        catch (Exception exception)
        {
            room.Status = RoomStatus.Failed;

            Console.WriteLine($"[Rooms] Failed to start deployment for room {room.Code}");
            Console.WriteLine(exception);

            return Results.Problem(exception.Message);
        }
    }

    return Results.Ok(new JoinRoomResponse(
        room.Code,
        player.PlayerId,
        room.Status.ToString()));
});

app.MapPost("/rooms/{code}/ready", (RoomStore store, string code, ReadyRequest request) =>
{
    if (!store.TryGetRoom(code, out var room))
        return Results.NotFound("Room not found.");

    var player = room.Players.FirstOrDefault(p => p.PlayerId == request.PlayerId);

    if (player == null)
        return Results.NotFound("Player not found.");

    player.IsReady = request.IsReady;

    return Results.Ok(new RoomStatusResponse(
        room.Status.ToString(),
        room.Host,
        room.Port,
        room.LastError));
});

app.MapPost("/rooms/{code}/start", async (
    RoomStore store,
    EdgegapDeploymentService edgegap,
    string code,
    StartRoomRequest request) =>
{
    if (!store.TryGetRoom(code, out var room))
        return Results.NotFound("Room not found.");

    if (room.Players.Count != 2)
        return Results.Conflict("Room needs 2 players.");

    if (room.Players.Any(p => !p.IsReady))
        return Results.Conflict("Both players must be ready.");

    if (room.Status is RoomStatus.Deploying or RoomStatus.ServerReady or RoomStatus.Running)
        return Results.Ok(new RoomStatusResponse( room.Status.ToString(),
            room.Host,
            room.Port,
            room.LastError));

    room.Status = RoomStatus.Deploying;

    try
    {
        string requestId = await edgegap.StartDeploymentAsync(room);
        room.EdgegapRequestId = requestId;

        _ = Task.Run(async () =>
        {
            try
            {
                var endpoint = await edgegap.WaitForReadyAsync(requestId);

                room.Host = endpoint.Host;
                room.Port = endpoint.Port;
                room.Status = RoomStatus.ServerReady;
            }
            catch
            {
                room.Status = RoomStatus.Failed;
            }
        });

        return Results.Ok(new RoomStatusResponse( room.Status.ToString(),
            room.Host,
            room.Port,
            room.LastError));
    }
    catch (Exception ex)
    {
        room.Status = RoomStatus.Failed;
        return Results.Problem(ex.Message);
    }
});

app.MapGet("/rooms/{code}/status", (RoomStore store, string code) =>
{
    if (!store.TryGetRoom(code, out var room))
        return Results.NotFound("Room not found.");

    return Results.Ok(new RoomStatusResponse(
        room.Status.ToString(),
        room.Host,
        room.Port,
        room.LastError));
});

app.MapPost("/rooms/{code}/ended", async (
    RoomStore store,
    EdgegapDeploymentService edgegap,
    string code) =>
{
    if (!store.TryGetRoom(code, out var room))
        return Results.NotFound("Room not found.");

    if (room.Status == RoomStatus.Ended)
        return Results.Ok(new { status = room.Status.ToString() });

    Console.WriteLine($"[Rooms] Ending room {room.Code}");

    room.Status = RoomStatus.Ended;

    if (!string.IsNullOrWhiteSpace(room.EdgegapRequestId))
    {
        try
        {
            Console.WriteLine($"[Rooms] Stopping Edgegap deployment {room.EdgegapRequestId}");
            await edgegap.StopDeploymentAsync(room.EdgegapRequestId);
        }
        catch (Exception exception)
        {
            room.LastError = exception.Message;
            Console.WriteLine($"[Rooms] Failed to stop deployment: {exception}");
        }
    }

    return Results.Ok(new { status = room.Status.ToString() });
});
app.Run();

public enum RoomStatus
{
    WaitingForPlayers,
    ReadyToStart,
    Deploying,
    ServerReady,
    Running,
    Ended,
    Failed
}

public sealed class GameRoom
{
    public Guid Id { get; init; } = Guid.NewGuid();
    public string Code { get; init; } = "";
    public RoomStatus Status { get; set; } = RoomStatus.WaitingForPlayers;
    public List<RoomPlayer> Players { get; } = new();

    public string? EdgegapRequestId { get; set; }
    public string? Host { get; set; }
    public ushort? Port { get; set; }
    public string? LastError { get; set; }

    public RoomPlayer AddPlayer(string name)
    {
        var player = new RoomPlayer
        {
            PlayerId = Guid.NewGuid(),
            Name = string.IsNullOrWhiteSpace(name) ? $"Player {Players.Count + 1}" : name
        };

        Players.Add(player);
        return player;
    }
}

public sealed class RoomPlayer
{
    public Guid PlayerId { get; init; }
    public string Name { get; init; } = "";
    public bool IsReady { get; set; }
}

public sealed class RoomStore
{
    private readonly ConcurrentDictionary<string, GameRoom> _rooms = new();

    public GameRoom CreateRoom(string playerName)
    {
        var code = GenerateCode();

        var room = new GameRoom
        {
            Code = code
        };

        room.AddPlayer(playerName);
        _rooms[code] = room;

        return room;
    }

    public bool TryGetRoom(string code, out GameRoom room)
    {
        return _rooms.TryGetValue(code.ToUpperInvariant(), out room!);
    }

    private string GenerateCode()
    {
        while (true)
        {
            var code = Random.Shared.Next(1000, 9999).ToString();

            if (!_rooms.ContainsKey(code))
                return code;
        }
    }
}

public sealed class EdgegapOptions
{
    public string ApiToken { get; set; } = "";
    public string Application { get; set; } = "";
    public string Version { get; set; } = "";
}

public sealed class EdgegapDeploymentService
{
    private readonly HttpClient _httpClient;
    private readonly EdgegapOptions _options;

    public EdgegapDeploymentService(HttpClient httpClient, IOptions<EdgegapOptions> options)
    {
        _httpClient = httpClient;
        _options = options.Value;
    }

    public async Task<string> StartDeploymentAsync(GameRoom room)
    {
        using var request = new HttpRequestMessage(
            HttpMethod.Post,
            "https://api.edgegap.com/v2/deployments");

        request.Headers.Authorization = new AuthenticationHeaderValue(
            "token",
            _options.ApiToken);

        var body = new
        {
            application = _options.Application,
            version = _options.Version,
            require_cached_locations = false,
            users = room.Players.Select(_ => new
            {
                user_type = "geo_coordinates",
                user_data = new
                {
                    latitude = 52.2297,
                    longitude = 21.0122
                }
            }).ToArray(),
            environment_variables = new[]
            {
                new
                {
                    key = "ROOM_CODE",
                    value = room.Code,
                    is_hidden = false
                },
                new
                {
                    key = "MAX_PLAYERS",
                    value = "2",
                    is_hidden = false
                }
            },
            tags = new[]
            {
                "silent-watch",
                $"room-{room.Code}"
            }
        };

        string json = JsonSerializer.Serialize(body);
        request.Content = new StringContent(json, Encoding.UTF8, "application/json");

        using var response = await _httpClient.SendAsync(request);
        string responseJson = await response.Content.ReadAsStringAsync();

        Console.WriteLine($"[Edgegap] Start deployment response: {(int)response.StatusCode}");
        Console.WriteLine(responseJson);

        if (!response.IsSuccessStatusCode)
        {
            throw new InvalidOperationException(
                $"Edgegap deployment failed: {(int)response.StatusCode} {response.ReasonPhrase}\n{responseJson}");
        }

        using var doc = JsonDocument.Parse(responseJson);

        return doc.RootElement.GetProperty("request_id").GetString()
               ?? throw new InvalidOperationException("Edgegap did not return request_id.");
    }

    public async Task<EdgegapEndpoint> WaitForReadyAsync(string requestId)
    {
        for (int i = 0; i < 60; i++)
        {
            var endpoint = await TryGetEndpointAsync(requestId);

            if (endpoint != null)
                return endpoint;

            await Task.Delay(2000);
        }

        throw new TimeoutException("Edgegap deployment did not become ready in time.");
    }

    private async Task<EdgegapEndpoint?> TryGetEndpointAsync(string requestId)
    {
        using var request = new HttpRequestMessage(
            HttpMethod.Get,
            $"https://api.edgegap.com/v1/status/{requestId}");

        request.Headers.Authorization = new AuthenticationHeaderValue(
            "token",
            _options.ApiToken);

        using var response = await _httpClient.SendAsync(request);
        string json = await response.Content.ReadAsStringAsync();

        response.EnsureSuccessStatusCode();

        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;

        bool running = root.TryGetProperty("running", out var runningElement)
                       && runningElement.GetBoolean();

        if (!running)
            return null;

        string host = root.GetProperty("fqdn").GetString()
                      ?? throw new InvalidOperationException("Missing fqdn.");

        var ports = root.GetProperty("ports");

        foreach (var portProperty in ports.EnumerateObject())
        {
            var portData = portProperty.Value;

            string protocol = portData.GetProperty("protocol").GetString() ?? "";

            if (!protocol.Equals("UDP", StringComparison.OrdinalIgnoreCase))
                continue;

            ushort externalPort = portData.GetProperty("external").GetUInt16();

            return new EdgegapEndpoint(host, externalPort);
        }

        return null;
    }

    public async Task StopDeploymentAsync(string requestId)
    {
        using var request = new HttpRequestMessage(
            HttpMethod.Delete,
            $"https://api.edgegap.com/v1/stop/{requestId}");

        request.Headers.Authorization = new AuthenticationHeaderValue(
            "token",
            _options.ApiToken);

        using var response = await _httpClient.SendAsync(request);
        response.EnsureSuccessStatusCode();
    }
}

public sealed record EdgegapEndpoint(string Host, ushort Port);

public sealed record CreateRoomRequest(string PlayerName);
public sealed record JoinRoomRequest(string PlayerName);
public sealed record ReadyRequest(Guid PlayerId, bool IsReady);
public sealed record StartRoomRequest(Guid PlayerId);

public sealed record CreateRoomResponse(string RoomCode, Guid PlayerId, string Status);
public sealed record JoinRoomResponse(string RoomCode, Guid PlayerId, string Status);
public sealed record RoomStatusResponse(
    string Status,
    string? Host,
    ushort? Port,
    string? Error);