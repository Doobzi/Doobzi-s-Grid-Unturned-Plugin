using Rocket.API;
using Rocket.Unturned.Chat;
using Rocket.Unturned.Player;
using System.Collections.Generic;
using UnityEngine;

namespace BountyPlugin
{
    public class CommandEcoSet : IRocketCommand
    {
        public AllowedCaller AllowedCaller => AllowedCaller.Both;
        public string Name => "ecoset";
        public string Help => "Admin: Set a player's balance to a specific amount";
        public string Syntax => "/ecoset <player> <amount>";
        public List<string> Aliases => new List<string>();
        public List<string> Permissions => new List<string> { "bounty.ecoadmin" };

        public void Execute(IRocketPlayer caller, string[] command)
        {
            if (command.Length < 2)
            {
                Say(caller, $"{Msg.Prefix} Usage: /ecoset <player> <amount>", Color.yellow);
                return;
            }

            var target = UnturnedPlayer.FromName(command[0]);
            if (target == null) { Say(caller, $"{Msg.Prefix} Player not found.", Color.red); return; }
            if (!decimal.TryParse(command[1], out decimal amount) || amount < 0) { Say(caller, $"{Msg.Prefix} Invalid amount.", Color.red); return; }

            var plugin = BountyPlugin.Instance;
            string callerName = caller is UnturnedPlayer cp ? cp.DisplayName : "Console";
            string targetId = target.CSteamID.ToString();

            plugin.EconomyManager.ResetAccount(targetId, amount);

            Say(caller, $"{Msg.Prefix} Set {target.DisplayName}'s balance to ${amount:N0}.", Color.green);
            UnturnedChat.Say(target, $"{Msg.Prefix} Admin set your balance to ${amount:N0}.", Color.yellow);
        }

        private void Say(IRocketPlayer caller, string msg, Color color)
        {
            if (caller is UnturnedPlayer p) UnturnedChat.Say(p, msg, color);
            else Rocket.Core.Logging.Logger.Log(msg);
        }
    }
}
