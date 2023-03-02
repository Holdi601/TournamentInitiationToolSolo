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
        public ConcurrentBag<Match> matches = new ConcurrentBag<Match>();
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
                    result += t.Players.Count;
                }
            }
            return result;
        }
        public bool PlayerActiveInRound(Player p)
        {
            foreach (Match m in matches)
            {
                foreach (Team t in m.ParticipatingTeams)
                {
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
            foreach (Match m in matches)
            {
                iMatchtes++;
                int iTeams = 0;
                foreach (Team t in m.ParticipatingTeams)
                {
                    iTeams++;
                    int iPlayers = 0;
                    foreach (Player p in t.Players)
                    {
                        iPlayers++;
                    }
                    if (iPlayers != t.PlayersPerTeam)
                    {
                        return false;
                    }
                }
                if (iTeams != m.maxTeamsPerMatch)
                {
                    return false;
                }
            }
            if (iMatchtes != MatchesPerPlayDay) return false;
            else return true;
        }

        public override string ToString()
        {
            string result = "\r\n\r\nMatch 0 :\r\n" +matches.ElementAt(0).ToString();
            for(int i=1; i<matches.Count; ++i)
            {
                result+= "\r\n\r\nMatch "+i.ToString()+" :\r\n" + matches.ElementAt(0).ToString();
            }
            return result;
        }
    }
}
