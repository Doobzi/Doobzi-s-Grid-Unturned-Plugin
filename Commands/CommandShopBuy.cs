using Rocket.API;
using Rocket.Unturned.Chat;
using Rocket.Unturned.Player;
using SDG.Unturned;
using System.Collections.Generic;
using UnityEngine;

namespace BountyPlugin
{
    public class CommandShopBuy : IRocketCommand
    {
        public AllowedCaller AllowedCaller => AllowedCaller.Player;
        public string Name => "shopbuy";
        public string Help => "Buy an item from the shop";
        public string Syntax => "/shopbuy <itemId> [amount]";
        public List<string> Aliases => new List<string> { "buy" };
        public List<string> Permissions => new List<string> { "bounty.shop" };

        public void Execute(IRocketPlayer caller, string[] command)
        {
            var player = (UnturnedPlayer)caller;
            var plugin = BountyPlugin.Instance;
            string sid = player.CSteamID.ToString();

            if (command.Length < 1)
            {
                UnturnedChat.Say(player, $"{Msg.Prefix} Usage: /shopbuy <itemId> [amount]", Color.yellow);
                return;
            }

            if (!ushort.TryParse(command[0], out ushort itemId))
            {
                UnturnedChat.Say(player, $"{Msg.Prefix} Invalid item ID.", Color.red);
                return;
            }

            int amount = 1;
            if (command.Length > 1 && int.TryParse(command[1], out int a)) amount = System.Math.Max(1, a);

            var shopItem = plugin.ShopManager.GetItem(itemId);
            if (shopItem == null)
            {
                UnturnedChat.Say(player, $"{Msg.Prefix} Item not found in shop.", Color.red);
                return;
            }

            if (shopItem.Stock >= 0 && shopItem.Stock < amount)
            {
                UnturnedChat.Say(player, $"{Msg.Prefix} Not enough stock! Available: {shopItem.Stock}", Color.red);
                return;
            }

            decimal totalCost = shopItem.Price * amount;
            decimal balance = plugin.EconomyManager.GetBalance(sid);
            if (balance < totalCost)
            {
                UnturnedChat.Say(player, $"{Msg.Prefix} Insufficient funds. Need ${totalCost:N0}, have ${balance:N0}.", Color.red);
                return;
            }

            // Give items
            for (int i = 0; i < amount; i++)
                player.GiveItem(itemId, 1);

            plugin.EconomyManager.RemoveBalance(sid, totalCost, "SHOP_BUY", $"Bought {amount}x {shopItem.Name}");
            plugin.ShopManager.DeductStock(itemId, amount);
            plugin.EconomyManager.TrackShopSpend(sid, totalCost);
            plugin.BountyManager.TrackMoneySpent(sid, player.DisplayName, totalCost);

            UnturnedChat.Say(player, $"{Msg.Prefix} Purchased {amount}x {shopItem.Name} for ${totalCost:N0}!", Color.green);
        }
    }
}
