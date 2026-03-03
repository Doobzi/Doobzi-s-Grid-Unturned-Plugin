using Rocket.API;
using Rocket.Unturned.Chat;
using Rocket.Unturned.Player;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace BountyPlugin
{
    public class CommandShopSearch : IRocketCommand
    {
        public AllowedCaller AllowedCaller => AllowedCaller.Player;
        public string Name => "shopsearch";
        public string Help => "Search the shop by keyword";
        public string Syntax => "/shopsearch <keyword>";
        public List<string> Aliases => new List<string> { "ss" };
        public List<string> Permissions => new List<string> { "bounty.shop" };

        public void Execute(IRocketPlayer caller, string[] command)
        {
            var player = (UnturnedPlayer)caller;
            var plugin = BountyPlugin.Instance;

            if (command.Length < 1)
            {
                UnturnedChat.Say(player, $"{Msg.Prefix} Usage: /shopsearch <keyword>", Color.yellow);
                return;
            }

            string keyword = string.Join(" ", command).ToLower();
            var items = plugin.ShopManager.GetItems();
            var results = items.Where(i => i.Name.ToLower().Contains(keyword)).ToList();

            if (results.Count == 0)
            {
                UnturnedChat.Say(player, $"{Msg.Prefix} No items matching \"{keyword}\".", Color.yellow);
                return;
            }

            UnturnedChat.Say(player, $"{Msg.Prefix} === Search: \"{keyword}\" ({results.Count} results) ===", BountyPlugin.Gold);
            foreach (var item in results.Take(8))
            {
                string cat = plugin.ShopManager.GetItemCategory(item.ItemId);
                string stock = item.Stock >= 0 ? $" [{item.Stock} left]" : " [Unlimited]";
                UnturnedChat.Say(player, $"  [{cat}] {item.Name} (ID: {item.ItemId}) - ${item.Price:N0}{stock}", Color.white);
            }

            if (results.Count > 8)
                UnturnedChat.Say(player, $"{Msg.Prefix} ...and {results.Count - 8} more. Be more specific.", Color.gray);
        }
    }
}
