using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TournamentInitiationToolSolo
{
    public class TournamentConfig
    {
        public ulong ID = 0;
        public ConcurrentDictionary<ulong, Player> Players = new ConcurrentDictionary<ulong, Player>();
        public int PlayersPerTeam = 0;
        public int TeamsPerMatch = 0;
        public int Rounds = 0;
        public int CurrentRound = 0;
        public ConcurrentBag<Round> RoundData = new ConcurrentBag<Round>();
        public ConcurrentBag<Match> matches = new ConcurrentBag<Match>();
        public ConcurrentBag<string> AllCombinations = new ConcurrentBag<string>();
        public int MatchesPerPlayDay = 0;
        public int PlayerSitoutsPerRound = 0;
        public ulong Initiator = 0;
        public DateTime InitTime = DateTime.UtcNow;
        public ConcurrentDictionary<Player, double> PlayerScores = new ConcurrentDictionary<Player, double>();
        public ConcurrentDictionary<ulong, int> ReportStep = new ConcurrentDictionary<ulong, int>();
        public ConcurrentDictionary<string, ulong> MatchReporter =new ConcurrentDictionary<string, ulong>();

        public void GenerateMatches()
        {
            AllCombinations = getAllPossibleCombinationWithLength(SortedPlayerList(), Convert.ToInt32(PlayersPerTeam));
            MatchesPerPlayDay = Convert.ToInt32(Players.Count / PlayersPerTeam);
            PlayerSitoutsPerRound = Players.Count - Convert.ToInt32(MatchesPerPlayDay * PlayersPerTeam);

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
            for (int i = 0; i < Rounds; ++i)
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
                }
                while (!r.AreAllMatchesFilled())
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
                    for (int z = 0; z < r.matches.Count; ++z)
                    {
                        if (r.matches.ElementAt(z).ParticipatingTeams.Count < r.matches.ElementAt(z).maxTeamsPerMatch)
                        {
                            r.matches.ElementAt(z).ParticipatingTeams.Add(t);
                            r.matches.ElementAt(z).Agreement.Add(t, MATCH_AGREEMENT.UNREPORTED);
                            break;
                        }
                    }
                    foreach (Player pl in t.Players)
                    {
                        plannedMatches[pl]++;
                    }
                    plannedTeamMatches[t]++;
                }
            }
        }
        public void CheckCurrentRound()
        {
            for(int i=0; i<Rounds; i++)
            {
                bool allMatchesCompleted = true;
                foreach(Match m in RoundData.ElementAt(i).matches)
                {
                    foreach(KeyValuePair<Team, MATCH_AGREEMENT> kvp in m.Agreement)
                    {
                        if(kvp.Value != MATCH_AGREEMENT.AGREED)
                        {
                            CurrentRound = i;
                            return;
                        }
                    }
                }
            }
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
            foreach (string s in ids)
            {
                ulong pid = Convert.ToUInt64(s);
                team.Players.Add(Players[pid]);
            }
            team.PlayersPerTeam = PlayersPerTeam;
            return team;
        }
    }
}
