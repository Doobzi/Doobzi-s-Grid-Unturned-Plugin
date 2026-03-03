using Rocket.API;
using Rocket.Unturned.Chat;
using Rocket.Unturned.Player;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace BountyPlugin
{
    public class CommandShop : IRocketCommand
    {
        public AllowedCaller AllowedCaller => AllowedCaller.Player;
        public string Name => "shop";
        public string Help => "Browse the shop";
        public string Syntax => "/shop [page]";
        public List<string> Aliases => new List<string>();
        public List<string> Permissions => new List<string> { "bounty.shop" };

        public void Execute(IRocketPlayer caller, string[] command)
        {
            var player = (UnturnedPlayer)caller;
            var plugin = BountyPlugin.Instance;
            var items = plugin.ShopManager.GetItems();

            if (items.Count == 0)
            {
                UnturnedChat.Say(player, $"{Msg.Prefix} Shop is empty!", Color.yellow);
                return;
            }

            int page = 1;
            if (command.Length > 0) int.TryParse(command[0], out page);
            page = System.Math.Max(1, page);

            int perPage = 6;
            int totalPages = (int)System.Math.Ceiling(items.Count / (double)perPage);
            page = System.Math.Min(page, totalPages);

            UnturnedChat.Say(player, $"{Msg.Prefix} === SHOP === Page {page}/{totalPages}", BountyPlugin.Gold);

            var pageItems = items.OrderBy(i => i.Name).Skip((page - 1) * perPage).Take(perPage);
            foreach (var item in pageItems)
            {
                string stock = item.Stock >= 0 ? $" [{item.Stock} left]" : " [Unlimited]";
                UnturnedChat.Say(player, $"  {item.Name} (ID: {item.ItemId}) - ${item.Price:N0}{stock}", Color.white);
            }

            string balance = plugin.EconomyManager.GetBalance(player.CSteamID.ToString()).ToString("N0");
            UnturnedChat.Say(player, $"{Msg.Prefix} Bal: ${balance} | /shopbuy <id> | /shopsearch <name> | /shopcats", Color.gray);
        }
    }
}
