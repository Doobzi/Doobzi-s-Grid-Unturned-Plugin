using Rocket.API;
using Rocket.Unturned.Chat;
using Rocket.Unturned.Player;
using System.Collections.Generic;
using UnityEngine;

namespace BountyPlugin
{
    public class CommandPay : IRocketCommand
    {
        public AllowedCaller AllowedCaller => AllowedCaller.Player;
        public string Name => "pay";
        public string Help => "Pay another player (5% tax)";
        public string Syntax => "/pay <player> <amount>";
        public List<string> Aliases => new List<string>();
        public List<string> Permissions => new List<string> { "bounty.pay" };

        public void Execute(IRocketPlayer caller, string[] command)
        {
            var player = (UnturnedPlayer)caller;
            var plugin = BountyPlugin.Instance;
            string senderId = player.CSteamID.ToString();

            if (command.Length < 2)
            {
                UnturnedChat.Say(player, $"{Msg.Prefix} Usage: /pay <player> <amount>", Color.yellow);
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
                UnturnedChat.Say(player, $"{Msg.Prefix} You can't pay yourself.", Color.red);
                return;
            }

            if (!decimal.TryParse(command[1], out decimal amount) || amount <= 0)
            {
                UnturnedChat.Say(player, $"{Msg.Prefix} Invalid amount.", Color.red);
                return;
            }

            decimal balance = plugin.EconomyManager.GetBalance(senderId);
            if (balance < amount)
            {
                UnturnedChat.Say(player, $"{Msg.Prefix} Insufficient funds. Balance: ${balance:N0}", Color.red);
                return;
            }

            decimal tax = System.Math.Round(amount * (plugin.Configuration.Instance.Economy.TransferTaxPercent / 100m), 0);
            decimal received = amount - tax;

            string targetId = target.CSteamID.ToString();
            plugin.EconomyManager.RemoveBalance(senderId, amount, "PAY_SENT", $"Paid {target.DisplayName}");
            plugin.EconomyManager.AddBalance(targetId, received, "PAY_RECEIVED", $"From {player.DisplayName}");

            UnturnedChat.Say(player, $"{Msg.Prefix} Sent ${received:N0} to {target.DisplayName} (${tax:N0} tax)", BountyPlugin.Gold);
            UnturnedChat.Say(target, $"{Msg.Prefix} Received ${received:N0} from {player.DisplayName}!", Color.green);

            // Achievement: Big Spender (pay over 10k total)
            plugin.BountyManager.TrackMoneySpent(senderId, player.DisplayName, amount);
        }
    }
}
