using Rocket.API;
using Rocket.Unturned.Chat;
using Rocket.Unturned.Player;
using System.Collections.Generic;
using UnityEngine;

namespace BountyPlugin
{
    public class CommandEcoGive : IRocketCommand
    {
        public AllowedCaller AllowedCaller => AllowedCaller.Both;
        public string Name => "ecogive";
        public string Help => "Admin: Give money to a player";
        public string Syntax => "/ecogive <player> <amount>";
        public List<string> Aliases => new List<string>();
        public List<string> Permissions => new List<string> { "bounty.ecoadmin" };

        public void Execute(IRocketPlayer caller, string[] command)
        {
            if (command.Length < 2)
            {
                Say(caller, $"{Msg.Prefix} Usage: /ecogive <player> <amount>", Color.yellow);
                return;
            }

            var target = UnturnedPlayer.FromName(command[0]);
            if (target == null) { Say(caller, $"{Msg.Prefix} Player not found.", Color.red); return; }
            if (!decimal.TryParse(command[1], out decimal amount) || amount <= 0) { Say(caller, $"{Msg.Prefix} Invalid amount.", Color.red); return; }

            var plugin = BountyPlugin.Instance;
            string callerName = caller is UnturnedPlayer cp ? cp.DisplayName : "Console";
            string targetId = target.CSteamID.ToString();

            plugin.EconomyManager.AddBalance(targetId, amount, "ADMIN_GIVE", $"Admin: {callerName}");
            decimal newBal = plugin.EconomyManager.GetBalance(targetId);

            Say(caller, $"{Msg.Prefix} Gave ${amount:N0} to {target.DisplayName}. New balance: ${newBal:N0}", Color.green);
            UnturnedChat.Say(target, $"{Msg.Prefix} Admin gave you ${amount:N0}!", Color.green);
        }

        private void Say(IRocketPlayer caller, string msg, Color color)
        {
            if (caller is UnturnedPlayer p) UnturnedChat.Say(p, msg, color);
            else Rocket.Core.Logging.Logger.Log(msg);
        }
    }
}
