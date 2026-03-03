using Rocket.API;
using Rocket.Unturned.Chat;
using Rocket.Unturned.Player;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace BountyPlugin
{
    public class CommandProfile : IRocketCommand
    {
        public AllowedCaller AllowedCaller => AllowedCaller.Both;
        public string Name => "profile";
        public string Help => "View a player's full profile";
        public string Syntax => "/profile [player]";
        public List<string> Aliases => new List<string>();
        public List<string> Permissions => new List<string> { "bounty.profile" };

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
                Say(caller, $"{Msg.Prefix} Specify a player.", Color.red);
                return;
            }

            // Economy
            decimal balance = plugin.EconomyManager.GetBalance(targetId);
            var account = plugin.EconomyManager.GetAccount(targetId);

            // Hunter Stats
            var stats = plugin.BountyManager.GetHunterStats(targetId);

            // Active Bounty
            var bounty = plugin.BountyManager.GetBounty(targetId);

            // Achievements
            var achievements = plugin.BountyManager.GetAchievements(targetId);

            Say(caller, $"{Msg.Prefix} ===== PROFILE: {targetName} =====", BountyPlugin.Gold);

            // Title line
            string title = stats != null ? stats.Title : "Novice";
            Say(caller, $"  Title: {title}", Color.cyan);

            // Economy
            Say(caller, $"  Balance: ${balance:N0}", Color.green);
            if (account != null)
            {
                decimal totalSpent = account.TotalShopSpent;
                Say(caller, $"  Total Shop Spent: ${totalSpent:N0}", Color.white);
            }

            // Hunter
            if (stats != null && stats.BountiesClaimed > 0)
            {
                Say(caller, $"  Bounties Claimed: {stats.BountiesClaimed} (${stats.TotalEarned:N0} earned)", BountyPlugin.Gold);
                Say(caller, $"  Streak: {stats.CurrentStreak}x (Best: {stats.BestStreak}x)", BountyPlugin.Gold);
                Say(caller, $"  Weekly: {stats.WeeklyBounties} kills, ${stats.WeeklyEarned:N0}", Color.cyan);
            }
            else
            {
                Say(caller, $"  Bounties Claimed: 0", Color.gray);
            }

            // Active bounty on them
            if (bounty != null)
            {
                string tier = bounty.GetTierIcon();
                Say(caller, $"  WANTED: {tier} ${bounty.TotalAmount:N0}", Color.red);
            }

            // Achievements
            if (achievements != null && achievements.Unlocked.Count > 0)
            {
                string achStr = string.Join(", ", achievements.Unlocked.Select(a => AchievementDefs.GetName(a)));
                Say(caller, $"  Achievements ({achievements.Unlocked.Count}): {achStr}", Color.magenta);
            }
            else
            {
                Say(caller, $"  Achievements: None yet", Color.gray);
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
