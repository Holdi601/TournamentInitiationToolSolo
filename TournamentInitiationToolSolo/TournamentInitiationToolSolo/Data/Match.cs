using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TournamentInitiationToolSolo
{
    public class Match
    {
        public ConcurrentBag<Team> ParticipatingTeams = new ConcurrentBag<Team>();
        public int Round = 0;
        public ConcurrentBag<double> Scores = new ConcurrentBag<double>();
        public int maxTeamsPerMatch = -1;
        public bool lockReporting = false;
        public ulong Reporter = 0;
        public DateTime ReportInit = DateTime.UtcNow;
        public bool finished = false;
        public Dictionary<Team, MATCH_AGREEMENT> Agreement = new Dictionary<Team, MATCH_AGREEMENT>();

        public string GenerateID()
        {
            string result=Round.ToString();
            foreach(Team t in ParticipatingTeams)
            {
                foreach(Player player in t.Players)
                {
                    result += "+"+ player.ID.ToString();
                }
            }
            return result;
        }

        public void ConfirmResults(Player p)
        {
            foreach(Team t in ParticipatingTeams)
            {
                if (t.Players.Contains(p))
                {
                    if (!Agreement.ContainsKey(t)) Agreement.TryAdd(t, MATCH_AGREEMENT.AGREED);
                    else Agreement[t]=MATCH_AGREEMENT.AGREED;
                    return;
                }
            }
        }

        public void ResetMatch()
        {
            lockReporting = false;
            Scores.Clear();
            foreach(Team t in ParticipatingTeams)
            {
                Agreement[t] = MATCH_AGREEMENT.UNREPORTED;
            }
        }

        public override string ToString()
        {
            string result = "Team 0: "+ ParticipatingTeams.ElementAt(0).ToString();
            for(int i=1; i< ParticipatingTeams.Count; ++i)
            {
                result += "\r\n versus \r\nTeam "+i.ToString() + ParticipatingTeams.ElementAt(i).ToString();
            }
            
            return result;
        }
    }

}
