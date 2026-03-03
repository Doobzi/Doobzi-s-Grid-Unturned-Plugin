using Rocket.API;
using Rocket.Unturned.Chat;
using Rocket.Unturned.Player;
using System.Collections.Generic;
using UnityEngine;

namespace BountyPlugin
{
    public class CommandEcoReset : IRocketCommand
    {
        public AllowedCaller AllowedCaller => AllowedCaller.Both;
        public string Name => "ecoreset";
        public string Help => "Admin: Reset a player's balance to starting amount";
        public string Syntax => "/ecoreset <player>";
        public List<string> Aliases => new List<string>();
        public List<string> Permissions => new List<string> { "bounty.ecoadmin" };

        public void Execute(IRocketPlayer caller, string[] command)
        {
            if (command.Length < 1)
            {
                Say(caller, $"{Msg.Prefix} Usage: /ecoreset <player>", Color.yellow);
                return;
            }

            var target = UnturnedPlayer.FromName(command[0]);
            if (target == null) { Say(caller, $"{Msg.Prefix} Player not found.", Color.red); return; }

            var plugin = BountyPlugin.Instance;
            string targetId = target.CSteamID.ToString();

            plugin.EconomyManager.ResetAccount(targetId);

            decimal newBal = plugin.EconomyManager.GetBalance(targetId);
            Say(caller, $"{Msg.Prefix} Reset {target.DisplayName}'s balance to ${newBal:N0}.", Color.green);
            UnturnedChat.Say(target, $"{Msg.Prefix} Your balance was reset by an admin.", Color.red);
        }

        private void Say(IRocketPlayer caller, string msg, Color color)
        {
            if (caller is UnturnedPlayer p) UnturnedChat.Say(p, msg, color);
            else Rocket.Core.Logging.Logger.Log(msg);
        }
    }
}
