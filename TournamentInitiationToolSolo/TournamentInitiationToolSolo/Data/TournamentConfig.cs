using DSharpPlus.Entities;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace TournamentInitiationToolSolo
{
    public static class ListExtensions
    {
        public static void Shuffle<T>(this IList<T> list, Random rng)
        {
            int n = list.Count;
            while (n > 1)
            {
                n--;
                int k = rng.Next(n + 1);
                T value = list[k];
                list[k] = list[n];
                list[n] = value;
            }
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
        public Round[] RoundData = null;
        public string[] AllCombinations = null;
        public int MatchesPerPlayDay = 0;
        public int PlayerSitoutsPerRound = 0;
        public ulong Initiator = 0;
        public DateTime InitTime = DateTime.UtcNow;
        public ConcurrentDictionary<ulong, int> ReportStep = new ConcurrentDictionary<ulong, int>();
        public int SetResultStep = -1;
        public int SetResultRound = -1;
        public int SetResultMatch = -1;
        public string MasterMode = "";

        public void SaveState(bool final=false)
        {
            try
            {
                JsonSerializerOptions options = new JsonSerializerOptions
                {
                    ReferenceHandler = ReferenceHandler.Preserve,
                    IncludeFields = true
                };
                string json = JsonSerializer.Serialize(this, options);
                string path = "";
                if (final)
                {
                    string toDelete = Program.DataPath + "\\run_" + ID.ToString() + ".json";
                    if (File.Exists(toDelete))
                    {
                        try
                        {
                            File.Delete(toDelete);
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine(ex.ToString());
                        }

                    }
                    path = Program.DataPath + "\\finished_" + ID.ToString() + ".json";
                }
                else
                {
                    path = Program.DataPath + "\\run_" + ID.ToString() + ".json";
                }
                try
                {
                    File.WriteAllText(path, json);
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.ToString());
                }
            }
            catch(Exception e)
            {
                Console.WriteLine(e.ToString());
            }
        }
            
        public void UnitTest()
        {
            int playerToGen = 5;
            Random r = new Random();
            for(int i=0; i<playerToGen; i++)
            {
                Player player = new Player();
                player.ID = Generate(r);
                player.Name = Generate(5,r);
                Players.TryAdd(player.ID, player);
            }
            GenerateMatches();
            string matchplan = GetMatchPlan();
            Console.WriteLine("Generating Done plan is:");
            Console.WriteLine(matchplan);

        }

        public static string Generate(int length, Random random)
        {
            const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789";
            return new string(Enumerable.Repeat(chars, length)
              .Select(s => s[random.Next(s.Length)]).ToArray());
        }

        public static ulong Generate(Random random)
        {
            byte[] bytes = new byte[8];
            random.NextBytes(bytes);
            return BitConverter.ToUInt64(bytes, 0);
        }

        public void GenerateMatches()
        {
            AllCombinations = getAllPossibleCombinationWithLength(SortedPlayerList(), Convert.ToInt32(PlayersPerTeam)).ToArray();
            MatchesPerPlayDay = Players.Count / (PlayersPerTeam * TeamsPerMatch);
            PlayerSitoutsPerRound = Players.Count - Convert.ToInt32(MatchesPerPlayDay * PlayersPerTeam * TeamsPerMatch);
            RoundData = new Round[Rounds];
            Dictionary<Player, int> plannedMatches = new Dictionary<Player, int>();
            Dictionary<Team, int> plannedTeamMatches = new Dictionary<Team, int>();
            foreach (string combo in AllCombinations)
            {
                plannedTeamMatches.Add(CreateTeamFromCombo(combo), 0);
            }
            for (int i = 0; i < Players.Count; ++i)
            {
                plannedMatches.Add(Players.ElementAt(i).Value, 0);
            }
            Random rng1 = new Random();
            Random rng2 = new Random();
            List<KeyValuePair<Player, int>> randomizedPlannedMatches = plannedMatches.ToList();
            List<KeyValuePair<Team, int>> randomizedPlannedTeamMatches = plannedTeamMatches.ToList();
            randomizedPlannedMatches.Shuffle(rng1);
            randomizedPlannedTeamMatches.Shuffle(rng2);

            for (int i = 0; i < Rounds; ++i)
            {
                Round r = new Round();
                r.number = i;
                r.MatchesPerPlayDay = MatchesPerPlayDay;
                r.matches = new Match[MatchesPerPlayDay]; 
                for (int k = 0; k < MatchesPerPlayDay; ++k)
                {
                    Match m = new Match();
                    m.maxTeamsPerMatch = TeamsPerMatch;
                    m.Scores = new double[TeamsPerMatch];
                    m.ParticipatingTeams= new Team[TeamsPerMatch];
                    m.Round = i;
                    r.matches[k] = m;
                    
                }
                while (!r.AreAllMatchesFilled())
                {
                    List<KeyValuePair<Player, int>> list = randomizedPlannedMatches.ToList();
                    list.Sort((x, y) => x.Value.CompareTo(y.Value));
                    List<KeyValuePair<Team, int>> tlist = randomizedPlannedTeamMatches.ToList();
                    tlist.Sort((x, y) => x.Value.CompareTo(y.Value));
                    int j = 0;
                    while (r.PlayerActiveInRound(list[j].Key))
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
                            foreach (Player pr in tlist.ElementAt(k).Key.Players)
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
                    for (int z = 0; z < r.matches.Length; ++z)
                    {
                        int cnt = r.matches.ElementAt(z).CurrentTeamCount();
                        if (cnt < r.matches.ElementAt(z).maxTeamsPerMatch)
                        {
                            r.matches.ElementAt(z).Agreement.Add(cnt, MATCH_AGREEMENT.UNREPORTED);
                            r.matches.ElementAt(z).ParticipatingTeams[cnt]=t;
                            break;
                        }
                    }
                    foreach (Player pl in t.Players)
                    {
                        for(int h=0; h<randomizedPlannedMatches.Count; ++h)
                        {
                            if (randomizedPlannedMatches[h].Key==pl)
                            {
                                var pair = randomizedPlannedMatches[h];
                                KeyValuePair<Player, int> newPair = new KeyValuePair<Player, int>(pl, pair.Value + 1);
                                randomizedPlannedMatches[h]= newPair;
                                break;
                            }
                        }
                    }
                    for(int h=0; h<randomizedPlannedTeamMatches.Count; ++h)
                    {
                        if (randomizedPlannedTeamMatches[h].Key == t)
                        {
                            var pair = randomizedPlannedTeamMatches[h];
                            KeyValuePair<Team, int> newPair = new KeyValuePair<Team, int>(t, pair.Value + 1);
                            randomizedPlannedTeamMatches[h]= newPair;
                            break;
                        }
                    }
                }
                RoundData[i]=r;
            }
            SaveState();
        }
        public Dictionary<Player, double> CalculatePlayerScores()
        {
            Dictionary<Player, double> result=  new Dictionary<Player, double>();
            foreach(KeyValuePair<ulong,Player> p in Players)
            {
                result.Add(p.Value, 0);
            }
            foreach(Round r in RoundData)
            {
                foreach(Match m in r.matches)
                {
                    if(m.finished)
                    {
                        for (int i = 0; i < m.CurrentTeamCount(); ++i)
                        {
                            foreach (Player p in m.ParticipatingTeams.ElementAt(i).Players)
                            {
                                result[p] += m.Scores.ElementAt(i);
                            }
                        }
                    }
                }
            }
            return result;
        }
        public void AdminSetResultMatch(int round, int match, double[] scores)
        {
            RoundData.ElementAt(round).matches.ElementAt(match).Scores=scores;
            for(int i=0; i< RoundData.ElementAt(round).matches.ElementAt(match).Agreement.Count; ++i)
            {
                RoundData.ElementAt(round).matches.ElementAt(match).Agreement[i] = MATCH_AGREEMENT.AGREED;
            }
        }
        public async void CheckCurrentRound(DiscordChannel dc)
        {
            int backup = CurrentRound;
            for(int i=0; i<Rounds; i++)
            {
                foreach(Match m in RoundData.ElementAt(i).matches)
                {
                    for(int asdf =0;  asdf < m.Agreement.Count; asdf++)
                    {
                        if (m.Agreement[asdf]!= MATCH_AGREEMENT.AGREED)
                        {
                            CurrentRound = i;
                            SaveState(false);
                            if (CurrentRound != backup)
                            {
                                await dc.SendMessageAsync("New Round has started:\r\n" + RoundData.ElementAt(CurrentRound).ToString());
                            }
                            return;
                        }
                    }
                    m.finished = true;
                }
            }
            await dc.SendMessageAsync("Tournament is over! Thanks everybody for taking part!");
            await dc.SendMessageAsync(ResultsToString(CalculatePlayerScores(), true));
            Program.Tournaments.Remove(ID, out TournamentConfig cfg);
            SaveState(true);
            //All played
        }
        public string ResultsToString(Dictionary<Player, double> results, bool final)
        {
            string message = "";
            if (final) message = "The final results are as followed:\r\n";
            else message = "The temporary results are as followed:\r\n";
            var orderedDict = results.OrderByDescending(x => x.Value);
            for(int i=0; i<orderedDict.Count(); ++i)
            {
                message += (i + 1).ToString() + " - "+orderedDict.ElementAt(i).Value.ToString()+" - "+"<@" + orderedDict.ElementAt(i).Key.ID + "> \r\n";
            }
            return message;
        }
        public List<ulong> SortedPlayerList()
        {
            List<ulong> list = new List<ulong>();
            foreach (KeyValuePair<ulong, Player> pair in Players)
            {
                list.Add(pair.Key);
            }
            list.Sort();
            return list;
        }

        public Match GetPendingMatchForPlayer(ulong playerId)
        {
            int offset = 0;
            Round r = null;
            bool found = false;
            Player p = Players[playerId];
            Match mi = null;
            while (CurrentRound + offset < Rounds)
            {
                r = RoundData.ElementAt(CurrentRound + offset);
                Match m = r.GetMatchForPlayer(p);
                if (m == null)
                {
                    offset++;
                    continue;
                }
                bool allTeamsAccepted = true;
                for (int i = 0; i < m.Agreement.Count; ++i)
                {
                    if (m.Agreement.ElementAt(i).Value != MATCH_AGREEMENT.AGREED)
                    {
                        allTeamsAccepted = false;
                    }
                    if (!allTeamsAccepted)
                    {
                        mi = m;
                        break;
                    }
                }
                if (!allTeamsAccepted)
                {
                    break;
                }
                offset++;
            }
            if (CurrentRound + offset == Rounds)
            {
                return null;
            }
            return mi;
        }

        ConcurrentBag<string> getAllPossibleCombinationWithLength(List<ulong> li, int leng)
        {
            if (leng > li.Count) return null;
            else if (leng == 0) return new ConcurrentBag<string>();
            ConcurrentBag<string> res = new ConcurrentBag<string>();
            ushort max = (ushort)Math.Pow(2, li.Count);
            for (ushort groups = 1; groups < max; ++groups)
            {
                if (countSetBits(groups) == (ushort)leng)
                {
                    string tempResult = "";
                    for (int i = 0; i < 16; ++i)
                    {
                        if ((groups & 1 << i) != 0)
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

        ushort countSetBits(ushort tester)
        {
            ushort j = 0;
            for (ushort i = 0; i < 16; ++i)
            {
                if ((tester & 1 << i) != 0)
                    ++j;
            }
            return j;
        }

        public Team CreateTeamFromCombo(string combo)
        {
            string[] ids = combo.Split('+');
            Team team = new Team();
            team.Players= new Player[ids.Length];
            for(int i=0; i < ids.Length; ++i)
            {
                ulong pid = Convert.ToUInt64(ids[i]);
                team.Players[i]=Players[pid];
            }

            team.PlayersPerTeam = PlayersPerTeam;
            return team;
        }

        public string GetMatchPlan()
        {
            string result = "";
            for(int i=0; i<RoundData.Length; ++i)
            {
                result += "\r\n\r\n\r\nRound " + i.ToString() + " : \r\n" + RoundData[i].ToString();
            }
            return result;
        }
    }
}
