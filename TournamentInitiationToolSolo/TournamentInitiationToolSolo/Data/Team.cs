using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TournamentInitiationToolSolo
{
    public class Team
    {
        public Player[] Players = null;
        public int PlayersPerTeam = 0;
        public bool ContainsPlayer(Player player)
        {
            if (Players.Contains(player)) return true;
            return false;
        }
        public bool ContainsPlayer(ulong uid)
        {
            for (int i = 0; i < Players.Length; ++i)
            {
                if (Players[i] != null && Players[i].ID == uid) return true;
            }
            return false;
        }

        public override string ToString()
        {
            if (CountPlayers() == 0) return "";
            string result = "<@" + Players.ElementAt(0).ID.ToString() + ">";
            for (int i = 1; i < CountPlayers(); ++i)
            {
                result += " + <@" + Players[i].ID.ToString() + ">";
            }
            return result;
        }

        public int CountPlayers()
        {
            int result = 0;
            if(Players==null)
            {
                Players = new Player[PlayersPerTeam];
            }
            for(int i = 0; i<Players.Length; ++i)
            {
                result++;
            }
            return result;
        }
    }
}
