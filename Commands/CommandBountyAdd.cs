using Rocket.API;
using Rocket.Unturned.Chat;
using Rocket.Unturned.Player;
using System.Collections.Generic;
using UnityEngine;

namespace BountyPlugin
{
    public class CommandBountyAdd : IRocketCommand
    {
        public AllowedCaller AllowedCaller => AllowedCaller.Player;
        public string Name => "bountyadd";
        public string Help => "Place a bounty on a player";
        public string Syntax => "/bountyadd <player> <amount> [anonymous]";
        public List<string> Aliases => new List<string> { "ba" };
        public List<string> Permissions => new List<string> { "bounty.add" };

        public void Execute(IRocketPlayer caller, string[] command)
        {
            var player = (UnturnedPlayer)caller;
            var plugin = BountyPlugin.Instance;
            string playerId = player.CSteamID.ToString();

            if (command.Length < 2)
            {
                UnturnedChat.Say(player, $"{Msg.Prefix} Usage: /bountyadd <player> <amount> [anonymous]", Color.yellow);
                return;
            }

            var target = UnturnedPlayer.FromName(command[0]);
            if (target == null)
            {
                UnturnedChat.Say(player, $"{Msg.Prefix} Player not found.", Color.red);
                return;
            }

            if (target.CSteamID == player.CSteamID)
            {
                UnturnedChat.Say(player, $"{Msg.Prefix} You cannot place a bounty on yourself!", Color.red);
                return;
            }

            if (!decimal.TryParse(command[1], out decimal amount) || amount <= 0)
            {
                UnturnedChat.Say(player, $"{Msg.Prefix} Invalid amount.", Color.red);
                return;
            }

            if (amount < plugin.Configuration.Instance.Bounties.MinimumAmount)
            {
                UnturnedChat.Say(player, $"{Msg.Prefix} Minimum bounty is ${plugin.Configuration.Instance.Bounties.MinimumAmount:N0}.", Color.red);
                return;
            }

            bool anonymous = command.Length > 2 && command[2].ToLower() == "anonymous";
            decimal totalCost = amount;
            decimal anonFee = 0;

            if (anonymous)
            {
                anonFee = System.Math.Round(amount * (plugin.Configuration.Instance.Bounties.AnonymousFeePercent / 100m), 0);
                totalCost = amount + anonFee;
            }

            decimal balance = plugin.EconomyManager.GetBalance(playerId);
            if (balance < totalCost)
            {
                string msg = anonymous
                    ? $"{Msg.Prefix} Insufficient funds. Need ${totalCost:N0} (${amount:N0} + ${anonFee:N0} anonymity fee). Balance: ${balance:N0}"
                    : $"{Msg.Prefix} Insufficient funds. Need ${amount:N0}. Balance: ${balance:N0}";
                UnturnedChat.Say(player, msg, Color.red);
                return;
            }

            // Deduct
            plugin.EconomyManager.RemoveBalance(playerId, totalCost, "BOUNTY_PLACED",
                $"Bounty on {target.DisplayName}" + (anonymous ? " (anon)" : ""));

            // Place bounty
            string targetId = target.CSteamID.ToString();
            string placerName = anonymous ? "Anonymous" : player.DisplayName;
            plugin.BountyManager.AddBounty(targetId, target.DisplayName, playerId, placerName, amount, anonymous);
            plugin.BountyManager.TrackBountyPlaced(playerId, player.DisplayName, amount);

            // Get tier
            var bounty = plugin.BountyManager.GetBounty(targetId);
            string tier = bounty != null ? bounty.GetTierIcon() : "";

            // Confirm
            string anonSuffix = anonymous ? " (Anonymous)" : "";
            UnturnedChat.Say(player, $"{Msg.Prefix} Bounty of ${amount:N0} placed on {target.DisplayName} {tier}{anonSuffix}", BountyPlugin.Gold);

            if (anonymous && anonFee > 0)
                UnturnedChat.Say(player, $"{Msg.Prefix} Anonymity fee: ${anonFee:N0}", Color.gray);

            // Broadcast
            if (plugin.Configuration.Instance.Bounties.AnnounceNewBounties)
            {
                UnturnedChat.Say(
                    $"{Msg.Prefix} {placerName} placed a ${amount:N0} bounty on {target.DisplayName}! {tier}",
                    Color.magenta
                );
            }

            // Warn target
            UnturnedChat.Say(target, $"{Msg.Prefix} WARNING: Someone placed a ${amount:N0} bounty on you! Total: ${bounty?.TotalAmount ?? amount:N0} {tier}", Color.red);

            // Discord webhook
            DiscordWebhook.BountyPlaced(target.DisplayName, amount, placerName, bounty?.TotalAmount ?? amount, tier);

            // Achievement checks
            var newAchievements = plugin.BountyManager.CheckPlacerAchievements(playerId, player.DisplayName);
            foreach (var achId in newAchievements)
                UnturnedChat.Say(player, $"{Msg.Prefix} ACHIEVEMENT UNLOCKED: {AchievementDefs.GetName(achId)}!", Color.magenta);
        }
    }
}
