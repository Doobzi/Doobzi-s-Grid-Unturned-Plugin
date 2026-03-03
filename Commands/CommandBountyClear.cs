using Rocket.API;
using Rocket.Unturned.Chat;
using Rocket.Unturned.Player;
using System.Collections.Generic;
using UnityEngine;

namespace BountyPlugin
{
    public class CommandBountyClear : IRocketCommand
    {
        public AllowedCaller AllowedCaller => AllowedCaller.Both;
        public string Name => "bountyclear";
        public string Help => "Admin: Clear a player's bounty";
        public string Syntax => "/bountyclear <player>";
        public List<string> Aliases => new List<string> { "bc" };
        public List<string> Permissions => new List<string> { "bounty.admin" };

        public void Execute(IRocketPlayer caller, string[] command)
        {
            if (command.Length < 1)
            {
                Say(caller, $"{Msg.Prefix} Usage: /bountyclear <player>", Color.yellow);
                return;
            }

            var target = UnturnedPlayer.FromName(command[0]);
            if (target == null)
            {
                Say(caller, $"{Msg.Prefix} Player not found.", Color.red);
                return;
            }

            var plugin = BountyPlugin.Instance;
            string targetId = target.CSteamID.ToString();
            var bounty = plugin.BountyManager.GetBounty(targetId);

            if (bounty == null)
            {
                Say(caller, $"{Msg.Prefix} {target.DisplayName} has no active bounty.", Color.yellow);
                return;
            }

            decimal amount = bounty.TotalAmount;
            plugin.BountyManager.ClearBounty(targetId);
            Say(caller, $"{Msg.Prefix} Cleared ${amount:N0} bounty on {target.DisplayName}.", Color.green);
            UnturnedChat.Say(target, $"{Msg.Prefix} Your bounty has been cleared by an admin.", Color.green);
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
