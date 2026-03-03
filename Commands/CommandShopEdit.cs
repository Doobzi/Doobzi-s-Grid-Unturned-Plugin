using Rocket.API;
using Rocket.Unturned.Chat;
using Rocket.Unturned.Player;
using System.Collections.Generic;
using UnityEngine;

namespace BountyPlugin
{
    public class CommandShopEdit : IRocketCommand
    {
        public AllowedCaller AllowedCaller => AllowedCaller.Both;
        public string Name => "shopedit";
        public string Help => "Edit a shop item's price and stock";
        public string Syntax => "/shopedit <id> <price> [stock]";
        public List<string> Aliases => new List<string>();
        public List<string> Permissions => new List<string> { "bounty.shopadmin" };

        public void Execute(IRocketPlayer caller, string[] command)
        {
            if (command.Length < 2)
            {
                Say(caller, $"{Msg.Prefix} Usage: /shopedit <id> <price> [stock]", Color.yellow);
                return;
            }

            if (!ushort.TryParse(command[0], out ushort itemId))
            {
                Say(caller, $"{Msg.Prefix} Invalid item ID.", Color.red);
                return;
            }

            if (!decimal.TryParse(command[1], out decimal price) || price <= 0)
            {
                Say(caller, $"{Msg.Prefix} Invalid price.", Color.red);
                return;
            }

            var plugin = BountyPlugin.Instance;
            var item = plugin.ShopManager.GetItem(itemId);
            if (item == null)
            {
                Say(caller, $"{Msg.Prefix} Item not found.", Color.red);
                return;
            }

            int stock = item.Stock;
            if (command.Length > 2) int.TryParse(command[2], out stock);

            plugin.ShopManager.EditItem(itemId, price, stock);
            string stockDisplay = stock < 0 ? "Unlimited" : stock.ToString();
            Say(caller, $"{Msg.Prefix} Updated {item.Name}: ${price:N0}, Stock: {stockDisplay}", Color.green);
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
