using Rocket.API;
using Rocket.Unturned.Chat;
using Rocket.Unturned.Player;
using System.Collections.Generic;
using UnityEngine;

namespace BountyPlugin
{
    public class CommandShopAdd : IRocketCommand
    {
        public AllowedCaller AllowedCaller => AllowedCaller.Both;
        public string Name => "shopadd";
        public string Help => "Add an item to the shop";
        public string Syntax => "/shopadd <id> <name> <price> [stock]";
        public List<string> Aliases => new List<string>();
        public List<string> Permissions => new List<string> { "bounty.shopadmin" };

        public void Execute(IRocketPlayer caller, string[] command)
        {
            if (command.Length < 3)
            {
                Say(caller, $"{Msg.Prefix} Usage: /shopadd <id> <name> <price> [stock]", Color.yellow);
                return;
            }

            if (!ushort.TryParse(command[0], out ushort itemId))
            {
                Say(caller, $"{Msg.Prefix} Invalid item ID.", Color.red);
                return;
            }

            string name = command[1];
            if (!decimal.TryParse(command[2], out decimal price) || price <= 0)
            {
                Say(caller, $"{Msg.Prefix} Invalid price.", Color.red);
                return;
            }

            int stock = BountyPlugin.Instance.Configuration.Instance.Shop.DefaultStockPerItem;
            if (command.Length > 3) int.TryParse(command[3], out stock);

            var plugin = BountyPlugin.Instance;

            if (plugin.ShopManager.GetItem(itemId) != null)
            {
                Say(caller, $"{Msg.Prefix} Item already exists. Use /shopedit to modify.", Color.red);
                return;
            }

            plugin.ShopManager.AddItem(itemId, name, price, stock);
            string stockDisplay = stock < 0 ? "Unlimited" : stock.ToString();
            Say(caller, $"{Msg.Prefix} Added {name} (ID: {itemId}) - ${price:N0} - Stock: {stockDisplay}", Color.green);
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
