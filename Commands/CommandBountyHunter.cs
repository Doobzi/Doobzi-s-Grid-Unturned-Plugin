using Rocket.API;
using Rocket.Unturned.Chat;
using Rocket.Unturned.Player;
using System.Collections.Generic;
using UnityEngine;

namespace BountyPlugin
{
    public class CommandBountyHunter : IRocketCommand
    {
        public AllowedCaller AllowedCaller => AllowedCaller.Both;
        public string Name => "bountyhunter";
        public string Help => "View hunter stats for a player";
        public string Syntax => "/bountyhunter [player]";
        public List<string> Aliases => new List<string> { "bh" };
        public List<string> Permissions => new List<string> { "bounty.hunter" };

        public void Execute(IRocketPlayer caller, string[] command)
        {
            var plugin = BountyPlugin.Instance;
            string targetId;
            string targetName;

            if (command.Length > 0)
            {
                var target = UnturnedPlayer.FromName(command[0]);
                if (target == null)
                {
                    Say(caller, $"{Msg.Prefix} Player not found.", Color.red);
                    return;
                }
                targetId = target.CSteamID.ToString();
                targetName = target.DisplayName;
            }
            else if (caller is UnturnedPlayer p)
            {
                targetId = p.CSteamID.ToString();
                targetName = p.DisplayName;
            }
            else
            {
                Say(caller, $"{Msg.Prefix} Specify a player name.", Color.red);
                return;
            }

            var stats = plugin.BountyManager.GetHunterStats(targetId);
            if (stats == null || stats.BountiesClaimed == 0)
            {
                Say(caller, $"{Msg.Prefix} {targetName} hasn't claimed any bounties yet.", Color.yellow);
                return;
            }

            Say(caller, $"{Msg.Prefix} === {targetName}'s Hunter Stats ===", BountyPlugin.Gold);
            Say(caller, $"  Title: {stats.Title}", Color.cyan);
            Say(caller, $"  Bounties Claimed: {stats.BountiesClaimed}", BountyPlugin.Gold);
            Say(caller, $"  Total Earned: ${stats.TotalEarned:N0}", Color.green);
            Say(caller, $"  Current Streak: {stats.CurrentStreak}x", BountyPlugin.Gold);
            Say(caller, $"  Best Streak: {stats.BestStreak}x", BountyPlugin.Gold);
            Say(caller, $"  Weekly Kills: {stats.WeeklyBounties} (${stats.WeeklyEarned:N0})", Color.cyan);
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
