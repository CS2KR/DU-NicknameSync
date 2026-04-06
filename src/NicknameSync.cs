using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Core.Attributes.Registration;
using CounterStrikeSharp.API.Core.Capabilities;
using CounterStrikeSharp.API.Modules.Commands;
using DiscordUtilitiesAPI;
using DiscordUtilitiesAPI.Events;
using DiscordUtilitiesAPI.Helpers;

namespace NicknameSync;

public class NicknameSync : BasePlugin
{
    public override string ModuleName => "[Discord Utilities] Nickname Sync";
    public override string ModuleVersion => "1.2.0";
    public override string ModuleAuthor => "CS2KR";
    public override string ModuleDescription => "Sets Discord server nickname to Steam profile name (linked users only)";

    private IDiscordUtilitiesAPI? DiscordUtilities { get; set; }
    private static readonly HttpClient HttpClient = new();
    private string _botToken = "";
    private string _guildId = "";

    public override void OnAllPluginsLoaded(bool hotReload)
    {
        GetDiscordUtilitiesEventSender().DiscordUtilitiesEventHandlers += DiscordUtilitiesEventHandler;
        DiscordUtilities!.CheckVersion(ModuleName, ModuleVersion);

        if (!LoadDiscordUtilitiesConfig())
        {
            Console.WriteLine("[NicknameSync] Failed to read Discord Utilities config!");
            return;
        }

        HttpClient.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bot", _botToken);

        RegisterEventHandler<EventPlayerConnectFull>(OnPlayerConnectFull);
        Console.WriteLine("[NicknameSync] Loaded successfully!");
    }

    public override void Unload(bool hotReload)
    {
        GetDiscordUtilitiesEventSender().DiscordUtilitiesEventHandlers -= DiscordUtilitiesEventHandler;
    }

    private bool LoadDiscordUtilitiesConfig()
    {
        try
        {
            var csgoPath = Path.Combine(Server.GameDirectory, "csgo");
            var configPath = Path.Combine(csgoPath, "addons", "counterstrikesharp", "configs", "plugins", "DiscordUtilities", "DiscordUtilities.json");

            if (!File.Exists(configPath))
            {
                Console.WriteLine($"[NicknameSync] Discord Utilities config not found: {configPath}");
                return false;
            }

            var json = File.ReadAllText(configPath);
            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;

            _botToken = root.GetProperty("Bot Token").GetString() ?? "";
            _guildId = root.GetProperty("Discord Server ID").GetString() ?? "";

            if (string.IsNullOrEmpty(_botToken) || string.IsNullOrEmpty(_guildId))
            {
                Console.WriteLine("[NicknameSync] Bot Token or Server ID is empty in Discord Utilities config!");
                return false;
            }

            return true;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[NicknameSync] Error reading DU config: {ex.Message}");
            return false;
        }
    }

    private void DiscordUtilitiesEventHandler(object? _, IDiscordUtilitiesEvent @event)
    {
        if (@event is LinkedUserDataLoaded linkedUser)
        {
            var player = linkedUser.player;
            if (player != null && player.IsValid)
            {
                SetDiscordNickname(linkedUser.User.ID, player.PlayerName);
                player.PrintToChat($"{Localizer["Chat.Prefix"]} {Localizer["Chat.NicknameSynced", player.PlayerName]}");
            }
        }
    }

    private HookResult OnPlayerConnectFull(EventPlayerConnectFull @event, GameEventInfo info)
    {
        var player = @event.Userid;
        if (player == null || !player.IsValid || player.IsBot) return HookResult.Continue;

        AddTimer(3.0f, () => SyncDiscordNickname(player));
        return HookResult.Continue;
    }

    private void SyncDiscordNickname(CCSPlayerController player)
    {
        if (player == null || !player.IsValid || DiscordUtilities == null) return;
        if (!DiscordUtilities.IsPlayerLinked(player) || !DiscordUtilities.IsPlayerDataLoaded(player))
            return;

        var userData = DiscordUtilities.GetUserDataByPlayerController(player);
        if (userData == null) return;

        var steamName = player.PlayerName;
        var discordName = userData.DisplayName ?? userData.GlobalName;
        if (steamName == discordName) return;

        SetDiscordNickname(userData.ID, steamName);
    }

    private async void SetDiscordNickname(ulong discordUserId, string newNickname)
    {
        if (DiscordUtilities == null || !DiscordUtilities.IsBotLoaded()) return;
        if (string.IsNullOrEmpty(_botToken) || string.IsNullOrEmpty(_guildId)) return;

        try
        {
            var url = $"https://discord.com/api/v10/guilds/{_guildId}/members/{discordUserId}";
            var payload = JsonSerializer.Serialize(new { nick = newNickname });
            var content = new StringContent(payload, Encoding.UTF8, "application/json");
            var response = await HttpClient.PatchAsync(url, content);

            if (response.IsSuccessStatusCode)
            {
                Console.WriteLine($"[NicknameSync] {discordUserId} -> {newNickname}");
            }
            else
            {
                var body = await response.Content.ReadAsStringAsync();
                Console.WriteLine($"[NicknameSync] Failed ({response.StatusCode}): {discordUserId} - {body}");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[NicknameSync] Error: {discordUserId} - {ex.Message}");
        }
    }

    [ConsoleCommand("css_syncnick", "Sync Discord nickname with Steam name")]
    public void OnSyncNickCommand(CCSPlayerController? player, CommandInfo command)
    {
        if (player == null || DiscordUtilities == null) return;

        if (!DiscordUtilities.IsPlayerLinked(player))
        {
            player.PrintToChat($"{Localizer["Chat.Prefix"]} {Localizer["Chat.NotLinked"]}");
            return;
        }

        SyncDiscordNickname(player);
        player.PrintToChat($"{Localizer["Chat.Prefix"]} {Localizer["Chat.SyncRequested"]}");
    }

    private IDiscordUtilitiesAPI GetDiscordUtilitiesEventSender()
    {
        if (DiscordUtilities is not null)
            return DiscordUtilities;

        var DUApi = new PluginCapability<IDiscordUtilitiesAPI>("discord_utilities").Get();
        if (DUApi is null)
            throw new Exception("Couldn't load Discord Utilities plugin");

        DiscordUtilities = DUApi;
        return DUApi;
    }
}
