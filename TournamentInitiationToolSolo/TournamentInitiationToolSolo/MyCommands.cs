using DSharpPlus.CommandsNext.Attributes;
using DSharpPlus.CommandsNext;
using DSharpPlus.Entities;
using DSharpPlus;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.VisualBasic;

namespace TournamentInitiationToolSolo
{
    public class MyCommands : BaseCommandModule
    {
        private bool waitingForMessage = false;
        private DiscordMessage lastMessage = null;

        //Report Results
        //Show my current match
        //Admin set Results
        //Show current Ranking
        //Show current Round
        //Show all Matches
        //Tournament Status

        [Command("StartTournament")]
        public async Task StartTournament(CommandContext ctx)
        {
            ulong serverID = ctx.Guild.Id;
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
            }
            else
            {
                DateTime now = DateTime.UtcNow;
                if ((now - Program.Tournaments[serverID].InitTime).Minutes > 15 &&
                    (Program.MasterModePerServer[serverID] != "Live" &&
                    Program.MasterModePerServer[serverID] != "CollectPlayers"))
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
            DateTime now = DateTime.UtcNow;
            if (Program.MasterModePerServer.ContainsKey(serverID) && Program.Tournaments[serverID].Initiator == ctx.User.Id)
            {
                Program.MasterModePerServer.TryRemove(serverID, out var server);
                Program.Tournaments.TryRemove(serverID, out var tournaments);
            }
            else if (Program.MasterModePerServer.ContainsKey(serverID) &&
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
            if (Program.MasterModePerServer.ContainsKey(serverID) && Program.MasterModePerServer[serverID] == "CollectPlayers")
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
                    await ctx.RespondAsync("User " + ctx.User.Username + " added to the Roster");
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
            if (Program.MasterModePerServer.ContainsKey(serverID) && Program.MasterModePerServer[serverID] == "CollectPlayers" &&
                Program.Tournaments[serverID].Initiator == ctx.User.Id)
            {
                await ctx.RespondAsync("Tournament goes live");
                Program.MasterModePerServer[serverID] = "Live";
                Program.Tournaments[serverID].GenerateMatches();
                await ctx.RespondAsync("All possible combination of people:");
                foreach (string p in Program.Tournaments[serverID].AllCombinations)
                {
                    string[] pInTeam = p.Split('+');
                    string team = "";
                    Console.WriteLine("Combo: " + p);
                    foreach (string play in pInTeam)
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
            if (Program.MasterModePerServer[serverID] == "Live")
            {
                int offset = 0;
                Round r = null;
                bool found = false;
                Player p = Program.Tournaments[serverID].Players[ctx.User.Id];
                if (Program.Tournaments[serverID].ReportStep.ContainsKey(ctx.User.Id))
                {
                    await ctx.RespondAsync("Please continue the thread of reporting the match");
                    return;
                }
                Match mi = Program.Tournaments[serverID].GetPendingMatchForPlayer(p.ID);
                if (mi == null)
                {
                    await ctx.RespondAsync("Something went wrong or nothing to report");
                    return;
                }
                bool alreadyReported = false;
                for (int i = 0; i < mi.Agreement.Count; ++i)
                {
                    if (mi.Agreement.ElementAt(i).Value == MATCH_AGREEMENT.AGREED)
                    {
                        alreadyReported = true;
                        break;
                    }
                }
                if (alreadyReported)
                {
                    string msg = "The Reported result is:\r\n";
                    for (int i = 0; i < mi.Scores.Count; ++i)
                    {
                        msg += mi.Scores.ElementAt(i) + " : " + mi.ParticipatingTeams.ElementAt(i).ToString() + " \r\n";
                    }
                    await ctx.RespondAsync(msg);
                    var options = new[]
                    {
                        new DiscordSelectComponentOption("Confirm", "Confirm"),
                        new DiscordSelectComponentOption("Dispute", "Dispute")
                    };
                    var dropdown = new DiscordSelectComponent("ReportMenu" + p.ID.ToString(), "Select an option", options);
                    var builder = new DiscordMessageBuilder().AddComponents(dropdown);
                    var message = await ctx.Channel.SendMessageAsync(builder);
                    return;
                }
                else
                {
                    if (mi.lockReporting && (DateTime.UtcNow - mi.ReportInit).Minutes < 10 && mi.Reporter != ctx.User.Id)
                    {
                        await ctx.RespondAsync("Some one else is already reporting.Try again once its reported");
                        return;
                    }
                    else
                    {
                        mi.lockReporting = true;
                        if (Program.Tournaments[serverID].ReportStep.ContainsKey(mi.Reporter))
                        {
                            Program.Tournaments[serverID].ReportStep.Remove(mi.Reporter, out int mid);
                        }
                        mi.Reporter = ctx.User.Id;
                        Program.Tournaments[serverID].ReportStep.TryAdd(ctx.User.Id, 0);
                    }
                }
            }
        }

        [Command("ListPlayers")]
        public async Task ListPlayers(CommandContext ctx)
        {
            ulong serverID = ctx.Guild.Id;
            if (Program.MasterModePerServer.ContainsKey(serverID) && (Program.MasterModePerServer[serverID] == "CollectPlayers" ||
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
        public async Task Client_InteractionCreated(DiscordClient sender, DSharpPlus.EventArgs.InteractionCreateEventArgs e)
        {
            ulong serverId = e.Interaction.Guild.Id;
            if (Program.Tournaments.ContainsKey(serverId))
            {
                if (Program.MasterModePerServer[serverId] == "Live")
                {
                    if ("ReportMenu" + e.Interaction.User.Id.ToString() == e.Interaction.Data.CustomId)
                    {
                        Player p = Program.Tournaments[serverId].Players[e.Interaction.User.Id];
                        Match mi = Program.Tournaments[serverId].GetPendingMatchForPlayer(p.ID);
                        if (e.Interaction.Data.Values[0] == "Confirm")
                        {
                            mi.ConfirmResults(p);
                            await e.Interaction.CreateResponseAsync(InteractionResponseType.UpdateMessage, new DiscordInteractionResponseBuilder().WithContent("Thanks for reporting the Match."));
                            Program.Tournaments[serverId].CheckCurrentRound();
                        }
                        else
                        {
                            mi.ResetMatch();
                            await e.Interaction.CreateResponseAsync(InteractionResponseType.UpdateMessage, new DiscordInteractionResponseBuilder().WithContent("The Match results have been reset. Please talk to your opponents and find a Solution"));
                        }
                    }
                    else
                    {
                        await e.Interaction.Channel.SendMessageAsync("<@" + e.Interaction.User.Id + "> You were not supposed to reply.");
                    }
                }
                else
                {
                    await e.Interaction.CreateResponseAsync(InteractionResponseType.UpdateMessage, new DiscordInteractionResponseBuilder().WithContent("This Dropdown should not be here."));
                }
            }
            else
            {
                await e.Interaction.CreateResponseAsync(InteractionResponseType.UpdateMessage, new DiscordInteractionResponseBuilder().WithContent("This Dropdown should not be here."));
            }
        }

        public Task Client_MessageCreated(DiscordClient sender, DSharpPlus.EventArgs.MessageCreateEventArgs e)
        {
            if (e.Author == sender.CurrentUser)
            {
                // ignore messages sent by the bot itself
                return Task.CompletedTask;
            }
            ulong sid = e.Guild.Id;
            if (Program.Tournaments.ContainsKey(sid)
                && waitingForMessage
                && lastMessage != e.Message
                && e.Author.Id == Program.Tournaments[sid].Initiator
                && Program.MasterModePerServer[sid] != "Live"
                && Program.MasterModePerServer[sid] != "CollectPlayers")
            {
                ulong serverID = e.Guild.Id;
                string[] splitted = e.Message.Content.Split(' ');
                string num = "";
                if (splitted.Length > 1)
                {
                    num = splitted[1];
                }
                else
                {
                    num = splitted[0];
                }
                if (Program.Tournaments.ContainsKey(serverID))
                {
                    switch (Program.MasterModePerServer[serverID])
                    {
                        case "CollectTeamSize":
                            if (!int.TryParse(num, out Program.Tournaments[serverID].PlayersPerTeam))
                            {
                                sender.SendMessageAsync(e.Channel, "The Number you have given is not a valid integer");
                            }
                            else
                            {
                                Program.MasterModePerServer[serverID] = "CollectTeamsInMatch";
                                sender.SendMessageAsync(e.Channel, "Team size is set to " + Program.Tournaments[serverID].PlayersPerTeam);
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
                            }
                            break;
                        default: break;
                    }
                }
            }
            return Task.CompletedTask;
        }
    }
}
