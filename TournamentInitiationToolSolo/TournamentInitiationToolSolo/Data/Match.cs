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
        public Team[] ParticipatingTeams = null;
        public int Round = 0;
        public double[] Scores = null;
        public int maxTeamsPerMatch = -1;
        public bool lockReporting = false;
        public ulong Reporter = 0;
        public DateTime ReportInit = DateTime.UtcNow;
        public bool finished = false;
        public Dictionary<int, MATCH_AGREEMENT> Agreement = new Dictionary<int, MATCH_AGREEMENT>();

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
            for(int i=0; i< ParticipatingTeams.Length; i++)
            {
                if (ParticipatingTeams.ElementAt(i).Players.Contains(p))
                {
                    Agreement[i] = MATCH_AGREEMENT.AGREED;
                    return;
                }
            }
        }

        public void ResetMatch()
        {
            lockReporting = false;
            Scores=new double[Scores.Length];
            for(int i=0; i<Agreement.Count; i++)
            {
                Agreement[i] = MATCH_AGREEMENT.UNREPORTED;
            }
        }

        public override string ToString()
        {
            string result = "Team 0: "+ ParticipatingTeams.ElementAt(0).ToString();
            for(int i=1; i< ParticipatingTeams.Length; ++i)
            {
                result += "\r\n versus \r\nTeam "+i.ToString()+": " + ParticipatingTeams.ElementAt(i).ToString();
            }
            
            return result;
        }

        public int CurrentTeamCount()
        {
            int result = 0;
            if (ParticipatingTeams == null)
            {
                ParticipatingTeams = new Team[maxTeamsPerMatch];
            }
            for(int i=0; i< ParticipatingTeams.Length;++i)
            {
                if (ParticipatingTeams[i] != null)
                {
                    result++;
                }
            }
            return result;
        }
    }

}
