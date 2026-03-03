using Rocket.API;
using Rocket.Unturned.Chat;
using Rocket.Unturned.Player;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace BountyPlugin
{
    public class CommandShopCat : IRocketCommand
    {
        public AllowedCaller AllowedCaller => AllowedCaller.Player;
        public string Name => "shopcat";
        public string Help => "Browse shop by category";
        public string Syntax => "/shopcat <category> [page]";
        public List<string> Aliases => new List<string>();
        public List<string> Permissions => new List<string> { "bounty.shop" };

        public void Execute(IRocketPlayer caller, string[] command)
        {
            var player = (UnturnedPlayer)caller;
            var plugin = BountyPlugin.Instance;

            if (command.Length < 1)
            {
                UnturnedChat.Say(player, $"{Msg.Prefix} Usage: /shopcat <category> [page]", Color.yellow);
                UnturnedChat.Say(player, $"{Msg.Prefix} Categories: {string.Join(", ", ShopManager.ValidCategories)}", Color.gray);
                return;
            }

            string cat = command[0].ToLower();
            string matchedCat = ShopManager.ValidCategories.FirstOrDefault(c => c.ToLower() == cat);
            if (matchedCat == null)
            {
                UnturnedChat.Say(player, $"{Msg.Prefix} Unknown category. Use /shopcats to see all.", Color.red);
                return;
            }

            var items = plugin.ShopManager.GetItemsByCategory(matchedCat);
            if (items.Count == 0)
            {
                UnturnedChat.Say(player, $"{Msg.Prefix} No items in '{matchedCat}'.", Color.yellow);
                return;
            }

            int page = 1;
            if (command.Length > 1) int.TryParse(command[1], out page);
            page = System.Math.Max(1, page);

            int perPage = 6;
            int totalPages = (int)System.Math.Ceiling(items.Count / (double)perPage);
            page = System.Math.Min(page, totalPages);

            UnturnedChat.Say(player, $"{Msg.Prefix} === {matchedCat.ToUpper()} === Page {page}/{totalPages}", BountyPlugin.Gold);

            var pageItems = items.Skip((page - 1) * perPage).Take(perPage);
            foreach (var item in pageItems)
            {
                string stock = item.Stock >= 0 ? $" [{item.Stock} left]" : " [Unlimited]";
                UnturnedChat.Say(player, $"  {item.Name} (ID: {item.ItemId}) - ${item.Price:N0}{stock}", Color.white);
            }

            if (page < totalPages)
                UnturnedChat.Say(player, $"{Msg.Prefix} /shopcat {matchedCat} {page + 1} for next page", Color.gray);
        }
    }
}
