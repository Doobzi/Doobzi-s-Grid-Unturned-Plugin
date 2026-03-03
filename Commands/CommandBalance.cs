using Rocket.API;
using Rocket.Unturned.Chat;
using Rocket.Unturned.Player;
using System.Collections.Generic;
using UnityEngine;

namespace BountyPlugin
{
    public class CommandBalance : IRocketCommand
    {
        public AllowedCaller AllowedCaller => AllowedCaller.Player;
        public string Name => "balance";
        public string Help => "Check your balance";
        public string Syntax => "/balance";
        public List<string> Aliases => new List<string> { "bal" };
        public List<string> Permissions => new List<string> { "bounty.balance" };

        public void Execute(IRocketPlayer caller, string[] command)
        {
            var player = (UnturnedPlayer)caller;
            var plugin = BountyPlugin.Instance;
            string sid = player.CSteamID.ToString();

            decimal balance = plugin.EconomyManager.GetBalance(sid);
            UnturnedChat.Say(player, $"{Msg.Prefix} Balance: ${balance:N0}", BountyPlugin.Gold);
        }
    }
}
