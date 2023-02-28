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
        public ConcurrentBag<Player> Players = new ConcurrentBag<Player>();
        public int PlayersPerTeam = 0;
        public bool ContainsPlayer(Player player)
        {
            if (Players.Contains(player)) return true;
            return false;
        }
        public bool ContainsPlayer(ulong uid)
        {
            for (int i = 0; i < Players.Count; ++i)
            {
                if (Players.ElementAt(i).ID == uid) return true;
            }
            return false;
        }

        public override string ToString()
        {
            if (Players.Count == 0) return "";
            string result = "<@" + Players.ElementAt(0).ID.ToString() + ">";
            for (int i = 1; i < Players.Count; ++i)
            {
                result += " + <@" + Players.ElementAt(i).ID.ToString() + ">";
            }
            return result;
        }
    }
}
