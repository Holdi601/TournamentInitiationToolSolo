using System;
using System.Collections.Concurrent;
using System.Threading.Tasks;
using DSharpPlus;
using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using DSharpPlus.Entities;
using DSharpPlus.EventArgs;


namespace DSharpPlus.ExampleBots.CommandsNext.HelloWorld
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

            commands.RegisterCommands<MyCommands>();
            await discord.ConnectAsync();
            await Task.Delay(-1);
        }

        private static Task MsgReceiver(DiscordClient sender, MessageCreateEventArgs e)
        {
            Console.WriteLine("msg: "+e.Message.Content+" by "+e.Author.Username);
            return Task.CompletedTask;
        }


    }


    public class MyCommands : BaseCommandModule
    {
        private bool waitingForMessage = false;
        private DiscordMessage lastMessage = null;

        [Command("StartTournament")]
        public async Task StartTournament(CommandContext ctx)
        {
            ulong serverID=ctx.Guild.Id;
            if (!Program.MasterModePerServer.ContainsKey(serverID))
            {
                Program.MasterModePerServer.TryAdd(serverID, "CollectTeamSize");
                Program.Tournaments.TryAdd(serverID, new TournamentConfig());
                Program.Tournaments[serverID].ID = serverID;
                Program.Tournaments[serverID].Initiator = ctx.User.Id;
                Program.Tournaments[serverID].InitTime = DateTime.UtcNow;
                await ctx.RespondAsync("Tournament Started");
                await ctx.RespondAsync("How Many players are in a team?");
                waitingForMessage = true;
                ctx.Client.MessageCreated += Client_MessageCreated;
            }
            else
            {
                DateTime now = DateTime.UtcNow;
                if ((now - Program.Tournaments[serverID].InitTime).Minutes > 15&&
                    (Program.MasterModePerServer[serverID]!="Live"&&
                    Program.MasterModePerServer[serverID]!= "CollectPlayers"))
                {
                    Program.MasterModePerServer[serverID] = "CollectTeamSize";
                    Program.Tournaments.TryRemove(serverID, out var tournaments);
                    Program.Tournaments.TryAdd(serverID, new TournamentConfig());
                    Program.Tournaments[serverID].ID = serverID;
                    Program.Tournaments[serverID].Initiator = ctx.User.Id;
                    Program.Tournaments[serverID].InitTime = DateTime.UtcNow;
                    await ctx.RespondAsync("Tournament Started");
                    await ctx.RespondAsync("How Many players are in a team?");
                    waitingForMessage = true;
                }
                await ctx.RespondAsync("Tournament already Started");
            }
        }

        [Command("EndTournament")]
        public async Task EndTournament(CommandContext ctx)
        {
            ulong serverID = ctx.Guild.Id;
            DateTime now= DateTime.UtcNow;
            if (Program.MasterModePerServer.ContainsKey(serverID) && Program.Tournaments[serverID].Initiator==ctx.User.Id)
            {
                Program.MasterModePerServer.TryRemove(serverID, out var server);
                Program.Tournaments.TryRemove(serverID, out var tournaments);
            }else if (Program.MasterModePerServer.ContainsKey(serverID)&& 
                (now - Program.Tournaments[serverID].InitTime).Minutes > 15 &&
                    (Program.MasterModePerServer[serverID] != "Live" &&
                    Program.MasterModePerServer[serverID] != "CollectPlayers"))
            {
                Program.MasterModePerServer.TryRemove(serverID, out var server);
                Program.Tournaments.TryRemove(serverID, out var tournaments);
            }
            await ctx.RespondAsync("Tournament Ended");
        }

        [Command("RegisterMe")]
        public async Task RegisterMe(CommandContext ctx)
        {
            ulong serverID = ctx.Guild.Id;
            if (Program.MasterModePerServer.ContainsKey(serverID)&& Program.MasterModePerServer[serverID]== "CollectPlayers")
            {
                if (Program.Tournaments[serverID].Players.ContainsKey(ctx.User.Id))
                {
                    await ctx.RespondAsync("User " + ctx.User.Username + " was already in the Roster");
                }
                else
                {
                    Program.Tournaments[serverID].Players.TryAdd(ctx.User.Id, new Player());
                    Program.Tournaments[serverID].Players[ctx.User.Id].ID = ctx.User.Id;
                    Program.Tournaments[serverID].Players[ctx.User.Id].Name = ctx.User.Username;
                    Player p = Program.Tournaments[serverID].Players[ctx.User.Id];
                    Program.Tournaments[serverID].PlayerScores.TryAdd(p, 0);
                    await ctx.RespondAsync("User "+ ctx.User.Username+" added to the Roster");
                }
            }
            else
            {
                await ctx.RespondAsync("Tournament on this server is not in collectable mode.");
            }
            
        }

        [Command("UnregisterMe")]
        public async Task UnregisterMe(CommandContext ctx)
        {
            ulong serverID = ctx.Guild.Id;
            if (Program.MasterModePerServer.ContainsKey(serverID) && Program.MasterModePerServer[serverID] == "CollectPlayers")
            {
                if (Program.Tournaments[serverID].Players.ContainsKey(ctx.User.Id))
                {
                    Player player = Program.Tournaments[serverID].Players[ctx.User.Id];
                    Program.Tournaments[serverID].Players.Remove(serverID, out var p);
                    Program.Tournaments[serverID].PlayerScores.Remove(player, out double value);
                    await ctx.RespondAsync("User " + ctx.User.Username + " is removed from the Roster");
                }
                else
                {
                    await ctx.RespondAsync("User " + ctx.User.Username + " was not in the Roster");
                }
            }
            else
            {
                await ctx.RespondAsync("Tournament on this server is not in collectable mode.");
            }

        }

        [Command("GoLive")]
        public async Task GoLive(CommandContext ctx)
        {
            ulong serverID = ctx.Guild.Id;
            if (Program.MasterModePerServer.ContainsKey(serverID) && Program.MasterModePerServer[serverID] == "CollectPlayers"&&
                Program.Tournaments[serverID].Initiator==ctx.User.Id)
            {
                await ctx.RespondAsync("Tournament goes live");
                Program.MasterModePerServer[serverID] = "Live";
                Program.Tournaments[serverID].GenerateMatches();
                await ctx.RespondAsync("All possible combination of people:");
                foreach(string p in Program.Tournaments[serverID].AllCombinations)
                {
                    string[] pInTeam = p.Split('+');
                    string team = "";
                    Console.WriteLine("Combo: "+p);
                    foreach(string play in pInTeam)
                    {
                        ulong ut = Convert.ToUInt64(play);
                        team += Program.Tournaments[serverID].Players[ut].Name + " ";
                        
                    }
                    Console.WriteLine(team);
                    await ctx.RespondAsync("Team: " + team);
                }
            }
            else
            {
                await ctx.RespondAsync("Tournament on this server is not in collectable mode.");
            }

        }
        [Command("Report")]
        public async Task Report(CommandContext ctx)
        {
            ulong serverID = ctx.Guild.Id;
            if(Program.MasterModePerServer[serverID] == "Live")
            {
                int offset = 0;
                Round r = null;
                bool found = false;
                Player p = Program.Tournaments[serverID].Players[ctx.User.Id];
                Match mi = null;
                while (Program.Tournaments[serverID].CurrentRound+offset< Program.Tournaments[serverID].Rounds)
                {
                    r = Program.Tournaments[serverID].RoundData.ElementAt(Program.Tournaments[serverID].CurrentRound + offset);
                    Match m = r.GetMatchForPlayer(p);
                    if(m==null)
                    {
                        offset++;
                        continue;
                    }
                    bool allTeamsAccepted = true;
                    for (int i = 0; i < m.Agreement.Count; ++i)
                    {
                        if(m.Agreement.ElementAt(i).Value!= MATCH_AGREEMENT.AGREED)
                        {
                            allTeamsAccepted = false;
                        }
                        if (!allTeamsAccepted)
                        {
                            mi= m;
                            break;
                        }
                    }
                    if (!allTeamsAccepted)
                    {
                        break;
                    }
                    offset++;
                }
                if(Program.Tournaments[serverID].CurrentRound + offset == Program.Tournaments[serverID].Rounds)
                {
                    await ctx.RespondAsync("No matches for you to report on");
                    return;
                }
                if (mi == null)
                {
                    await ctx.RespondAsync("Something went wrong");
                    return;
                }
                bool alreadyReported=false;
                for(int i=0; i<mi.Agreement.Count; ++i)
                {
                    if (mi.Agreement.ElementAt(i).Value == MATCH_AGREEMENT.AGREED)
                    {
                        alreadyReported = true;
                        break;
                    }
                }if(alreadyReported)
                {
                    string msg = "The Reported result is:\r\n";
                    for(int i=0; i<mi.Scores.Count; ++i)
                    {
                        msg += mi.Scores.ElementAt(i) + " : " + mi.ParticipatingTeams.ElementAt(i).ToString() + " \r\n";
                    }
                    await ctx.RespondAsync(msg);

                    return;
                }
                
            }
        }

        [Command("ListPlayers")]
        public async Task ListPlayers(CommandContext ctx)
        {
            ulong serverID = ctx.Guild.Id;
            if (Program.MasterModePerServer.ContainsKey(serverID) && (Program.MasterModePerServer[serverID] == "CollectPlayers"||
                Program.MasterModePerServer[serverID] == "Live"))
            {
                Console.WriteLine("Registered Players are:");
                await ctx.RespondAsync("Registered Players are:");
                foreach (KeyValuePair<ulong, Player> kvp in Program.Tournaments[serverID].Players)
                {
                    Console.WriteLine(kvp.Value.Name);
                    await ctx.RespondAsync("Player: " + kvp.Value.Name);
                }

            }
            else
            {
                await ctx.RespondAsync("Tournament on this server is not in collectable mode or live.");
            }

        }

        private Task Client_MessageCreated(DiscordClient sender, DSharpPlus.EventArgs.MessageCreateEventArgs e)
        {
            if (e.Author == sender.CurrentUser)
            {
                // ignore messages sent by the bot itself
                return Task.CompletedTask;
            }
            ulong sid = e.Guild.Id;
            if (waitingForMessage && lastMessage != e.Message&& e.Author.Id == Program.Tournaments[sid].Initiator)
            {
                ulong serverID = e.Guild.Id;
                string[] splitted = e.Message.Content.Split(' ');
                string num = "";
                if(splitted.Length>1)
                {
                    num = splitted[1];
                }
                else
                {
                    num= splitted[0];
                }
                if(Program.Tournaments.ContainsKey(serverID))
                {
                    switch(Program.MasterModePerServer[serverID]) 
                    {
                        case "CollectTeamSize":
                            if (!int.TryParse(num, out Program.Tournaments[serverID].PlayersPerTeam))
                            {
                                sender.SendMessageAsync(e.Channel, "The Number you have given is not a valid integer");
                            }
                            else
                            {
                                Program.MasterModePerServer[serverID] = "CollectTeamsInMatch";
                                sender.SendMessageAsync(e.Channel, "Team size is set to "+ Program.Tournaments[serverID].PlayersPerTeam);
                                sender.SendMessageAsync(e.Channel, "How many Teams are in a Match?");
                            }
                            break;
                        case "CollectTeamsInMatch":
                            if (!int.TryParse(num, out Program.Tournaments[serverID].TeamsPerMatch))
                            {
                                sender.SendMessageAsync(e.Channel, "The Number you have given is not a valid integer");
                            }
                            else
                            {
                                Program.MasterModePerServer[serverID] = "CollectRounds";
                                sender.SendMessageAsync(e.Channel, "Teams per Match is set to " + Program.Tournaments[serverID].TeamsPerMatch);
                                sender.SendMessageAsync(e.Channel, "How many Rounds should be played?");
                            }
                            break;
                        case "CollectRounds":
                            if (!int.TryParse(num, out Program.Tournaments[serverID].Rounds))
                            {
                                sender.SendMessageAsync(e.Channel, "The Number you have given is not a valid integer");
                            }
                            else
                            {
                                Program.MasterModePerServer[serverID] = "CollectPlayers";
                                sender.SendMessageAsync(e.Channel, "Rounds is set to " + Program.Tournaments[serverID].Rounds);
                                sender.SendMessageAsync(e.Channel, "Every player that wants to participate needs to use the command RegisterMe. If the admin is happy execute the command GoLive");
                                lastMessage = e.Message;

                                waitingForMessage = false;
                                sender.MessageCreated -= Client_MessageCreated;
                            }
                            break;
                        default:break;
                    }
                }  
            }
            return Task.CompletedTask;
        }
    }

    public class TournamentConfig
    {
        public ulong ID = 0;
        public ConcurrentDictionary<ulong, Player> Players = new ConcurrentDictionary<ulong, Player>();
        public int PlayersPerTeam = 0;
        public int TeamsPerMatch = 0;
        public int Rounds = 0;
        public int CurrentRound = 0;
        public ConcurrentBag<Round> RoundData=new ConcurrentBag<Round>();
        public ConcurrentBag<Match> matches = new ConcurrentBag<Match>();
        public ConcurrentBag<string> AllCombinations = new ConcurrentBag<string>();
        public int MatchesPerPlayDay = 0;
        public int PlayerSitoutsPerRound = 0;
        public ulong Initiator = 0;
        public DateTime InitTime = DateTime.UtcNow;
        public ConcurrentDictionary<Player, double> PlayerScores = new ConcurrentDictionary<Player, double>();

        public void GenerateMatches()
        {
            AllCombinations = getAllPossibleCombinationWithLength(SortedPlayerList(), Convert.ToInt32(PlayersPerTeam));
            MatchesPerPlayDay = Convert.ToInt32(Players.Count / PlayersPerTeam);
            PlayerSitoutsPerRound = Players.Count - Convert.ToInt32(MatchesPerPlayDay * PlayersPerTeam);

            Dictionary<Player, int> plannedMatches = new Dictionary<Player, int>();
            Dictionary<Team, int> plannedTeamMatches = new Dictionary<Team, int>();
            foreach(string combo in AllCombinations)
            {
                plannedTeamMatches.Add(CreateTeamFromCombo(combo),0);
            }
            for(int i=0; i<Players.Count; ++i)
            {
                plannedMatches.Add(Players.ElementAt(i).Value, 0);
            }
            for(int i=0; i<Rounds; ++i)
            {
                Round r = new Round();
                r.number = i;
                r.MatchesPerPlayDay = MatchesPerPlayDay;
                for (int k = 0; k < MatchesPerPlayDay; ++k)
                {
                    Match m = new Match();
                    m.maxTeamsPerMatch = TeamsPerMatch;
                    m.Round = i;
                    r.matches.Add(m);
                    m.played = false;
                }
                while(!r.AreAllMatchesFilled())
                {
                    List<KeyValuePair<Player, int>> list = plannedMatches.ToList();
                    list.Sort((x, y) => x.Value.CompareTo(y.Value));
                    List<KeyValuePair<Team, int>> tlist = plannedTeamMatches.ToList();
                    tlist.Sort((x, y) => x.Value.CompareTo(y.Value));
                    int j = 0;
                    while (!r.PlayerActiveInRound(list[j].Key))
                    {
                        ++j;
                    }
                    Player p = list[j].Key;
                    int k = 0;
                    while (true)
                    {
                        if (tlist.ElementAt(k).Key.ContainsPlayer(p))
                        {
                            bool teamIsFree = true;
                            foreach(Player pr in tlist.ElementAt(k).Key.Players)
                            {
                                if (r.PlayerActiveInRound(pr))
                                {
                                    teamIsFree = false;
                                }
                            }
                            if (teamIsFree)
                            {
                                break;
                            }
                        }
                        ++k;
                    }
                    Team t = tlist[k].Key;
                    for(int z=0; z<r.matches.Count; ++z)
                    {
                        if (r.matches.ElementAt(z).ParticipatingTeams.Count< r.matches.ElementAt(z).maxTeamsPerMatch)
                        {
                            r.matches.ElementAt(z).ParticipatingTeams.Add(t);
                            r.matches.ElementAt(z).Agreement.Add(t, MATCH_AGREEMENT.UNREPORTED);
                            break;
                        }
                    }
                    foreach(Player pl in t.Players)
                    {
                        plannedMatches[pl]++;
                    }
                    plannedTeamMatches[t]++;
                }
            }
        }
        public List<ulong> SortedPlayerList()
        {
            List<ulong> list = new List<ulong>();
            foreach(KeyValuePair<ulong, Player> pair in Players)
            {
                list.Add(pair.Key);
            }
            list.Sort();
            return list;
        }

        ConcurrentBag<string> getAllPossibleCombinationWithLength(List<ulong> li, int leng)
        {
            if (leng > li.Count) return null;
            else if (leng == 0) return new ConcurrentBag<string>();
            ConcurrentBag<string> res = new ConcurrentBag<string>();
            UInt16 max = (UInt16)Math.Pow(2, li.Count);
            for (UInt16 groups = 1; groups < max; ++groups)
            {
                if (countSetBits(groups) == (UInt16)leng)
                {
                    string tempResult = "";
                    for (int i = 0; i < 16; ++i)
                    {
                        if ((groups & (1 << i)) != 0)
                        {
                            if (tempResult.Length == 0)
                            {
                                tempResult = li[i].ToString();
                            }
                            else
                            {
                                tempResult += "+" + li[i].ToString();
                            }
                        }
                    }
                    res.Add(tempResult);
                }
            }
            return res;
        }

        UInt16 countSetBits(UInt16 tester)
        {
            UInt16 j = 0;
            for (UInt16 i = 0; i < 16; ++i)
            {
                if ((tester & (1 << i)) != 0)
                    ++j;
            }
            return j;
        }

        public Team CreateTeamFromCombo(string combo)
        {
            string[] ids = combo.Split('+');
            Team team = new Team();
            foreach(string s in ids)
            {
                ulong pid = Convert.ToUInt64(s);
                team.Players.Add(Players[pid]);
            }
            team.PlayersPerTeam = PlayersPerTeam;
            return team;
        }
    }

    public class Team
    {
        public ConcurrentBag<Player> Players = new ConcurrentBag<Player>();
        public int PlayersPerTeam = 0;
        public bool ContainsPlayer(Player player)
        {
            if(Players.Contains(player)) return true;
            return false;
        }
        public bool ContainsPlayer(ulong uid)
        {
            for(int i=0; i<Players.Count; ++i)
            {
                if (Players.ElementAt(i).ID == uid) return true;
            }
            return false;
        }

        public override string ToString()
        {
            if (Players.Count == 0) return "";
            string result = "<@"+Players.ElementAt(0).ID.ToString()+">";
            for(int i=1; i<Players.Count; ++i)
            {
                result += " + <@" + Players.ElementAt(i).ID.ToString() + ">";
            }
            return result;
        }
    }
    public enum MATCH_AGREEMENT { UNREPORTED, AGREED, DISPUTE}
    public class Match
    {
        public ConcurrentBag<Team> ParticipatingTeams = new ConcurrentBag<Team>();
        public int Round = 0;
        public ConcurrentBag<double> Scores = new ConcurrentBag<double>();
        public int maxTeamsPerMatch = -1;
        public bool played = false;
        public bool lockReporting = false;
        public ulong Reporter = 0;
        public DateTime ReportInit = DateTime.UtcNow;
        public Dictionary<Team, MATCH_AGREEMENT> Agreement = new Dictionary<Team, MATCH_AGREEMENT>();
    }

    public class Player
    {
        public string Name { get; set; }
        public ulong ID { get; set; }
    }

    public class Round
    {
        public int number = -1;
        public ConcurrentBag<Match> matches = new ConcurrentBag<Match>();
        public bool finished=false;
        public int MatchesPerPlayDay = -1;

        public Match GetMatchForPlayer(Player player)
        {
            foreach(Match match in matches)
            {
                foreach(Team team in match.ParticipatingTeams)
                {
                    if (team.Players.Contains(player)) return match;
                }
            }
            return null;
        }

        public int PlayersActiveInRound()
        {
            int result = 0;
            foreach(Match m in matches)
            {
                foreach(Team t in m.ParticipatingTeams)
                {
                    result += t.Players.Count;
                }
            }
            return result;
        }
        public bool PlayerActiveInRound(Player p)
        {
            foreach(Match m in matches)
            {
                foreach(Team t in m.ParticipatingTeams)
                {
                    if(t.Players.Contains(p))
                    {
                        return true;
                    }
                }
            }
            return false;
        }

        public bool AreAllMatchesFilled()
        {
            int iMatchtes = 0;
            foreach(Match m in matches)
            {
                iMatchtes++;
                int iTeams = 0;
                foreach(Team t in m.ParticipatingTeams)
                {
                    iTeams++;
                    int iPlayers = 0;
                    foreach(Player p in t.Players)
                    {
                        iPlayers++;
                    }
                    if (iPlayers != t.PlayersPerTeam)
                    {
                        return false;
                    }
                }
                if(iTeams!=m.maxTeamsPerMatch)
                {
                    return false;
                }
            }
            if (iMatchtes != MatchesPerPlayDay) return false;
            else return true;
        }
    }
}
