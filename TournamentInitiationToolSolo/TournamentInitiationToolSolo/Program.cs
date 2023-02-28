using System;
using System.Collections.Concurrent;
using System.Threading.Channels;
using System.Threading.Tasks;
using DSharpPlus;
using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using DSharpPlus.Entities;
using DSharpPlus.EventArgs;

namespace TournamentInitiationToolSolo
{
    // We're sealing it because nothing will be inheriting this class
    public class Program
    {
        public static ConcurrentDictionary<ulong, string> MasterModePerServer = new ConcurrentDictionary<ulong, string>();
        public static ConcurrentDictionary<ulong, TournamentConfig> Tournaments = new ConcurrentDictionary<ulong, TournamentConfig>();
        public static DiscordClient discord;
        public static CommandsNextExtension commands;

        static async Task Main(string[] args)
        {
            discord = new DiscordClient(new DiscordConfiguration
            {
                Token = Environment.GetEnvironmentVariable("TITS_TOKEN", EnvironmentVariableTarget.User),
                TokenType = TokenType.Bot,
                Intents = DiscordIntents.All
            });

            commands = discord.UseCommandsNext(new CommandsNextConfiguration
            {
                StringPrefixes = new[] { "?" },
                EnableMentionPrefix = true,
                EnableDms=false,
                EnableDefaultHelp = true
                
            });
            MyCommands mc = new MyCommands();
            commands.RegisterCommands<MyCommands>();
            discord.MessageCreated += mc.Client_MessageCreated;
            discord.ComponentInteractionCreated += mc.Client_InteractionCreated;
            await discord.ConnectAsync();
            await Task.Delay(-1);
        }
    }
    public enum MATCH_AGREEMENT { UNREPORTED, AGREED, DISPUTE}
}
