using Kook;
using Kook.Commands;
using Kook.WebSocket;
using SysBot.Base;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace SysBot.Pokemon.Kook;

public class LogModule : ModuleBase<SocketCommandContext>
{
    private static readonly Dictionary<ulong, ChannelLogger> Channels = [];

    public static void RestoreLogging(KookSocketClient Kook, KookSettings settings)
    {
        foreach (var ch in settings.LoggingChannels)
        {
            if (Kook.GetChannel(ch.ID) is ISocketMessageChannel c)
                AddLogChannel(c, ch.ID);
        }

        LogUtil.LogInfo("Added logging to Kook channel(s) on Bot startup.", "Kook");
    }

    [Command("logHere")]
    [Summary("Makes the bot log to the channel.")]
    [RequireSudo]
    public async Task AddLogAsync()
    {
        var c = Context.Channel;
        var cid = c.Id;
        if (Channels.TryGetValue(cid, out _))
        {
            await ReplyTextAsync("Already logging here.").ConfigureAwait(false);
            return;
        }

        AddLogChannel(c, cid);

        // Add to Kook global loggers (saves on program close)
        KookBotSettings.Settings.LoggingChannels.AddIfNew([GetReference(Context.Channel)]);
        await ReplyTextAsync("Added logging output to this channel!").ConfigureAwait(false);
    }

    private static void AddLogChannel(ISocketMessageChannel c, ulong cid)
    {
        var logger = new ChannelLogger(cid, c);
        LogUtil.Forwarders.Add(logger);
        Channels.Add(cid, logger);
    }

    [Command("logInfo")]
    [Summary("Dumps the logging settings.")]
    [RequireSudo]
    public async Task DumpLogInfoAsync()
    {
        foreach (var c in Channels)
            await ReplyTextAsync($"{c.Key} - {c.Value}").ConfigureAwait(false);
    }

    [Command("logClear")]
    [Summary("Clears the logging settings in that specific channel.")]
    [RequireSudo]
    public async Task ClearLogsAsync()
    {
        var id = Context.Channel.Id;
        if (!Channels.TryGetValue(id, out var log))
        {
            await ReplyTextAsync("Not echoing in this channel.").ConfigureAwait(false);
            return;
        }
        LogUtil.Forwarders.Remove(log);
        Channels.Remove(Context.Channel.Id);
        KookBotSettings.Settings.LoggingChannels.RemoveAll(z => z.ID == id);
        await ReplyTextAsync($"Logging cleared from channel: {Context.Channel.Name}").ConfigureAwait(false);
    }

    [Command("logClearAll")]
    [Summary("Clears all the logging settings.")]
    [RequireSudo]
    public async Task ClearLogsAllAsync()
    {
        foreach (var l in Channels)
        {
            var entry = l.Value;
            await ReplyTextAsync($"Logging cleared from {entry.ChannelName} ({entry.ChannelID}!").ConfigureAwait(false);
            LogUtil.Forwarders.Remove(entry);
        }

        LogUtil.Forwarders.RemoveAll(y => Channels.Select(z => z.Value).Contains(y));
        Channels.Clear();
        KookBotSettings.Settings.LoggingChannels.Clear();
        await ReplyTextAsync("Logging cleared from all channels!").ConfigureAwait(false);
    }

    private RemoteControlAccess GetReference(IChannel channel) => new()
    {
        ID = channel.Id,
        Name = channel.Name,
        Comment = $"Added by {Context.User.Username} on {DateTime.Now:yyyy.MM.dd-hh:mm:ss}",
    };
}
