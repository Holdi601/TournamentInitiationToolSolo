using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TournamentInitiationToolSolo
{
    public class Round
    {
        public int number = -1;
        public Match[] matches = null;
        public bool finished = false;
        public int MatchesPerPlayDay = -1;

        public Match GetMatchForPlayer(Player player)
        {
            foreach (Match match in matches)
            {
                foreach (Team team in match.ParticipatingTeams)
                {
                    if (team.Players.Contains(player)) return match;
                }
            }
            return null;
        }

        public int PlayersActiveInRound()
        {
            int result = 0;
            foreach (Match m in matches)
            {
                foreach (Team t in m.ParticipatingTeams)
                {
                    result += t.CountPlayers();
                }
            }
            return result;
        }
        public bool PlayerActiveInRound(Player p)
        {
            foreach (Match m in matches)
            {
                if(m!=null)
                foreach (Team t in m.ParticipatingTeams)
                {
                    if(t!=null)
                    if (t.Players.Contains(p))
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
            for(int p=0; p<matches.Length; p++)
            {
                if (matches[p] != null)
                {
                    iMatchtes++;
                    int iTeams = 0;
                    for (int q = 0; q < matches[p].ParticipatingTeams.Length; q++)
                    {
                        if (matches[p].ParticipatingTeams[q] != null)
                        {
                            iTeams++;
                            int iPlayers = 0;
                            for (int o = 0; o < matches[p].ParticipatingTeams[q].Players.Length; o++)
                            {
                                if (matches[p].ParticipatingTeams[q].Players[o] != null) iPlayers++;
                            }
                            if (iPlayers != matches[p].ParticipatingTeams[q].PlayersPerTeam)
                            {
                                return false;
                            }
                        }
                    }
                    if (iTeams != matches[p].maxTeamsPerMatch)
                    {
                        return false;
                    }
                }
            }
            if (iMatchtes != MatchesPerPlayDay) return false;
            else return true;
        }

        public override string ToString()
        {
            string result = "Match 0 :\r\n" +matches.ElementAt(0).ToString();
            for(int i=1; i<matches.Length; ++i)
            {
                if (matches[i] != null)result+= "Match "+i.ToString()+" :\r\n" + matches.ElementAt(0).ToString();
            }
            return result;
        }

        public int CurrentMatchCount()
        {
            int result = 0;
            if (matches == null)
            {
                matches = new Match[MatchesPerPlayDay];
            }
            for(int i=0; i<matches.Length; ++i)
            {
                result++;
            }
            return result;
        }
    }
}
