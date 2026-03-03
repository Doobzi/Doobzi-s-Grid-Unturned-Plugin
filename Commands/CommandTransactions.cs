using Rocket.API;
using Rocket.Unturned.Chat;
using Rocket.Unturned.Player;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace BountyPlugin
{
    public class CommandTransactions : IRocketCommand
    {
        public AllowedCaller AllowedCaller => AllowedCaller.Player;
        public string Name => "transactions";
        public string Help => "View your recent transactions";
        public string Syntax => "/transactions [count]";
        public List<string> Aliases => new List<string> { "txn" };
        public List<string> Permissions => new List<string> { "bounty.transactions" };

        public void Execute(IRocketPlayer caller, string[] command)
        {
            var player = (UnturnedPlayer)caller;
            var plugin = BountyPlugin.Instance;
            string sid = player.CSteamID.ToString();

            int count = 10;
            if (command.Length > 0) int.TryParse(command[0], out count);
            count = System.Math.Max(1, System.Math.Min(count, 20));

            var transactions = plugin.EconomyManager.GetTransactions(sid, count);
            if (transactions == null || transactions.Count == 0)
            {
                UnturnedChat.Say(player, $"{Msg.Prefix} No transactions yet.", Color.yellow);
                return;
            }

            decimal balance = plugin.EconomyManager.GetBalance(sid);
            UnturnedChat.Say(player, $"{Msg.Prefix} === Recent Transactions === (Bal: ${balance:N0})", BountyPlugin.Gold);

            foreach (var tx in transactions)
            {
                string sign = tx.Amount >= 0 ? "+" : "";
                Color c = tx.Amount >= 0 ? Color.green : Color.red;
                string time = tx.Timestamp.ToString("MM/dd HH:mm");
                UnturnedChat.Say(player, $"  [{time}] {sign}${tx.Amount:N0} - {tx.Description}", c);
            }
        }
    }
}
