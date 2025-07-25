using Kook.Commands;
using PKHeX.Core;
using SysBot.Base;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace SysBot.Pokemon.Kook;

[Summary("Remotely controls a bot.")]
public class RemoteControlModule<T> : ModuleBase<SocketCommandContext> where T : PKM, new()
{
    [Command("click")]
    [Summary("Clicks the specified button.")]
    [RequireRoleAccess(nameof(KookManager.RolesRemoteControl))]
    public async Task ClickAsync(SwitchButton b)
    {
        var bot = KookBot<T>.Runner.Bots.Find(z => IsRemoteControlBot(z.Bot));
        if (bot == null)
        {
            await ReplyTextAsync($"No bot is available to execute your command: {b}").ConfigureAwait(false);
            return;
        }

        await ClickAsyncImpl(b, bot).ConfigureAwait(false);
    }

    [Command("click")]
    [Summary("Clicks the specified button.")]
    [RequireSudo]
    public async Task ClickAsync(string ip, SwitchButton b)
    {
        var bot = KookBot<T>.Runner.GetBot(ip);
        if (bot == null)
        {
            await ReplyTextAsync($"No bot is available to execute your command: {b}").ConfigureAwait(false);
            return;
        }

        await ClickAsyncImpl(b, bot).ConfigureAwait(false);
    }

    [Command("setStick")]
    [Summary("Sets the stick to the specified position.")]
    [RequireRoleAccess(nameof(KookManager.RolesRemoteControl))]
    public async Task SetStickAsync(SwitchStick s, short x, short y, ushort ms = 1_000)
    {
        var bot = KookBot<T>.Runner.Bots.Find(z => IsRemoteControlBot(z.Bot));
        if (bot == null)
        {
            await ReplyTextAsync($"No bot is available to execute your command: {s}").ConfigureAwait(false);
            return;
        }

        await SetStickAsyncImpl(s, x, y, ms, bot).ConfigureAwait(false);
    }

    [Command("setStick")]
    [Summary("Sets the stick to the specified position.")]
    [RequireSudo]
    public async Task SetStickAsync(string ip, SwitchStick s, short x, short y, ushort ms = 1_000)
    {
        var bot = KookBot<T>.Runner.GetBot(ip);
        if (bot == null)
        {
            await ReplyTextAsync($"No bot has that IP address ({ip}).").ConfigureAwait(false);
            return;
        }

        await SetStickAsyncImpl(s, x, y, ms, bot).ConfigureAwait(false);
    }

    [Command("setScreenOn")]
    [Alias("screenOn", "scrOn")]
    [Summary("Turns the screen on")]
    [RequireSudo]
    public Task SetScreenOnAsync([Remainder] string ip)
    {
        return SetScreen(true, ip);
    }

    [Command("setScreenOff")]
    [Alias("screenOff", "scrOff")]
    [Summary("Turns the screen off")]
    [RequireSudo]
    public Task SetScreenOffAsync([Remainder] string ip)
    {
        return SetScreen(false, ip);
    }

    private async Task SetScreen(bool on, string ip)
    {
        var bot = GetBot(ip);
        if (bot == null)
        {
            await ReplyTextAsync($"No bot has that IP address ({ip}).").ConfigureAwait(false);
            return;
        }

        var b = bot.Bot;
        var crlf = b is SwitchRoutineExecutor<PokeBotState> { UseCRLF: true };
        await b.Connection.SendAsync(SwitchCommand.SetScreen(on ? ScreenState.On : ScreenState.Off, crlf), CancellationToken.None).ConfigureAwait(false);
        await ReplyTextAsync("Screen state set to: " + (on ? "On" : "Off")).ConfigureAwait(false);
    }

    private static BotSource<PokeBotState>? GetBot(string ip)
    {
        var r = KookBot<T>.Runner;
        return r.GetBot(ip) ?? r.Bots.Find(x => x.IsRunning); // safe fallback for users who mistype IP address for single bot instances
    }

    private async Task ClickAsyncImpl(SwitchButton button, BotSource<PokeBotState> bot)
    {
        if (!Enum.IsDefined(button))
        {
            await ReplyTextAsync($"Unknown button value: {button}").ConfigureAwait(false);
            return;
        }

        var b = bot.Bot;
        var crlf = b is SwitchRoutineExecutor<PokeBotState> { UseCRLF: true };
        await b.Connection.SendAsync(SwitchCommand.Click(button, crlf), CancellationToken.None).ConfigureAwait(false);
        await ReplyTextAsync($"{b.Connection.Name} has performed: {button}").ConfigureAwait(false);
    }

    private async Task SetStickAsyncImpl(SwitchStick s, short x, short y, ushort ms, BotSource<PokeBotState> bot)
    {
        if (!Enum.IsDefined(s))
        {
            await ReplyTextAsync($"Unknown stick: {s}").ConfigureAwait(false);
            return;
        }

        var b = bot.Bot;
        var crlf = b is SwitchRoutineExecutor<PokeBotState> { UseCRLF: true };
        await b.Connection.SendAsync(SwitchCommand.SetStick(s, x, y, crlf), CancellationToken.None).ConfigureAwait(false);
        await ReplyTextAsync($"{b.Connection.Name} has performed: {s}").ConfigureAwait(false);
        await Task.Delay(ms).ConfigureAwait(false);
        await b.Connection.SendAsync(SwitchCommand.ResetStick(s, crlf), CancellationToken.None).ConfigureAwait(false);
        await ReplyTextAsync($"{b.Connection.Name} has reset the stick position.").ConfigureAwait(false);
    }

    private static bool IsRemoteControlBot(RoutineExecutor<PokeBotState> botstate)
        => botstate is RemoteControlBotSWSH or RemoteControlBotBS or RemoteControlBotLA or RemoteControlBotSV;
}
