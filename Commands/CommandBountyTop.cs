using Rocket.API;
using Rocket.Unturned.Chat;
using Rocket.Unturned.Player;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace BountyPlugin
{
    public class CommandBountyTop : IRocketCommand
    {
        public AllowedCaller AllowedCaller => AllowedCaller.Both;
        public string Name => "bountytop";
        public string Help => "View top bounty hunters";
        public string Syntax => "/bountytop [weekly]";
        public List<string> Aliases => new List<string> { "bt" };
        public List<string> Permissions => new List<string> { "bounty.top" };

        public void Execute(IRocketPlayer caller, string[] command)
        {
            var plugin = BountyPlugin.Instance;
            bool weekly = command.Length > 0 && command[0].ToLower() == "weekly";

            if (weekly)
            {
                var top = plugin.BountyManager.GetWeeklyTopHunters(10);
                if (top.Count == 0)
                {
                    Say(caller, $"{Msg.Prefix} No weekly bounty kills yet.", Color.yellow);
                    return;
                }

                Say(caller, $"{Msg.Prefix} === WEEKLY TOP HUNTERS ===", BountyPlugin.Gold);

                int rank = 1;
                foreach (var h in top)
                {
                    string prefix = rank == 1 ? ">>>" : rank == 2 ? ">>" : rank == 3 ? ">" : " ";
                    Say(caller, $"  {prefix} #{rank} {h.PlayerName} [{h.Title}] - {h.WeeklyBounties} kills, ${h.WeeklyEarned:N0}", BountyPlugin.Gold);
                    rank++;
                }
            }
            else
            {
                var top = plugin.BountyManager.GetTopHunters(10);
                if (top.Count == 0)
                {
                    Say(caller, $"{Msg.Prefix} No bounties have been claimed yet.", Color.yellow);
                    return;
                }

                Say(caller, $"{Msg.Prefix} === ALL-TIME TOP HUNTERS ===", BountyPlugin.Gold);

                int rank = 1;
                foreach (var h in top)
                {
                    string prefix = rank == 1 ? ">>>" : rank == 2 ? ">>" : rank == 3 ? ">" : " ";
                    string streak = h.BestStreak > 1 ? $" (Best: {h.BestStreak}x)" : "";
                    Say(caller, $"  {prefix} #{rank} {h.PlayerName} [{h.Title}] - {h.BountiesClaimed} kills, ${h.TotalEarned:N0}{streak}", BountyPlugin.Gold);
                    rank++;
                }
            }
        }

        private void Say(IRocketPlayer caller, string msg, Color color)
        {
            if (caller is UnturnedPlayer p)
                UnturnedChat.Say(p, msg, color);
            else
                Rocket.Core.Logging.Logger.Log(msg);
        }
    }
}
