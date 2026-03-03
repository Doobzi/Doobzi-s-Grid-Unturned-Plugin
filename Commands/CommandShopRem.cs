using Rocket.API;
using Rocket.Unturned.Chat;
using Rocket.Unturned.Player;
using System.Collections.Generic;
using UnityEngine;

namespace BountyPlugin
{
    public class CommandShopRem : IRocketCommand
    {
        public AllowedCaller AllowedCaller => AllowedCaller.Both;
        public string Name => "shoprem";
        public string Help => "Remove an item from the shop";
        public string Syntax => "/shoprem <id>";
        public List<string> Aliases => new List<string>();
        public List<string> Permissions => new List<string> { "bounty.shopadmin" };

        public void Execute(IRocketPlayer caller, string[] command)
        {
            if (command.Length < 1)
            {
                Say(caller, $"{Msg.Prefix} Usage: /shoprem <id>", Color.yellow);
                return;
            }

            if (!ushort.TryParse(command[0], out ushort itemId))
            {
                Say(caller, $"{Msg.Prefix} Invalid item ID.", Color.red);
                return;
            }

            var plugin = BountyPlugin.Instance;
            var item = plugin.ShopManager.GetItem(itemId);
            if (item == null)
            {
                Say(caller, $"{Msg.Prefix} Item not found.", Color.red);
                return;
            }

            plugin.ShopManager.RemoveItem(itemId);
            Say(caller, $"{Msg.Prefix} Removed {item.Name} from the shop.", Color.green);
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
