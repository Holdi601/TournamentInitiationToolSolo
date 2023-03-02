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
        public static string ProgPath="";
        public static string DataPath="";
        

        static async Task Main(string[] args)
        {
            string exePath =  System.Reflection.Assembly.GetExecutingAssembly().Location;
            string[] rawPath = exePath.Split('\\');
            for(int i=0; i<rawPath.Length-1; ++i)
            {
                ProgPath+=rawPath[i]+"\\";   
            }
            DataPath=ProgPath+"\\Data";
            if (!Directory.Exists(DataPath))
            {
                Directory.CreateDirectory(DataPath);
            }
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
