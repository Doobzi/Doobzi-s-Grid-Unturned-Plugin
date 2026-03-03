using Rocket.API;
using Rocket.Unturned.Chat;
using Rocket.Unturned.Player;
using SDG.Unturned;
using System.Collections.Generic;
using UnityEngine;

namespace BountyPlugin
{
    public class CommandSell : IRocketCommand
    {
        public AllowedCaller AllowedCaller => AllowedCaller.Player;
        public string Name => "sell";
        public string Help => "Sell an item back to the shop at 50% price";
        public string Syntax => "/sell <itemId> [amount]";
        public List<string> Aliases => new List<string>();
        public List<string> Permissions => new List<string> { "bounty.sell" };

        public void Execute(IRocketPlayer caller, string[] command)
        {
            var player = (UnturnedPlayer)caller;
            var plugin = BountyPlugin.Instance;
            string sid = player.CSteamID.ToString();

            if (command.Length < 1)
            {
                UnturnedChat.Say(player, $"{Msg.Prefix} Usage: /sell <itemId> [amount]", Color.yellow);
                return;
            }

            if (!ushort.TryParse(command[0], out ushort itemId))
            {
                UnturnedChat.Say(player, $"{Msg.Prefix} Invalid item ID.", Color.red);
                return;
            }

            int amount = 1;
            if (command.Length > 1 && int.TryParse(command[1], out int a)) amount = System.Math.Max(1, a);

            // Check if item is in shop
            decimal sellPrice = plugin.ShopManager.GetSellPrice(itemId);
            if (sellPrice <= 0)
            {
                UnturnedChat.Say(player, $"{Msg.Prefix} This item can't be sold (not in shop).", Color.red);
                return;
            }

            // Check player inventory
            var inventory = player.Inventory;
            int found = 0;
            var itemPositions = new List<System.Tuple<byte, byte, byte>>();

            for (byte pg = 0; pg < PlayerInventory.PAGES - 1; pg++)
            {
                if (inventory.items[pg] == null) continue;
                for (byte idx = 0; idx < inventory.items[pg].getItemCount(); idx++)
                {
                    var jar = inventory.items[pg].getItem(idx);
                    if (jar != null && jar.item.id == itemId)
                    {
                        itemPositions.Add(new System.Tuple<byte, byte, byte>(pg, jar.x, jar.y));
                        found++;
                        if (found >= amount) break;
                    }
                }
                if (found >= amount) break;
            }

            if (found < amount)
            {
                UnturnedChat.Say(player, $"{Msg.Prefix} You only have {found}x of that item.", Color.red);
                return;
            }

            // Remove items (in reverse to avoid index issues)
            for (int i = itemPositions.Count - 1; i >= 0; i--)
            {
                var pos = itemPositions[i];
                inventory.removeItem(pos.Item1, inventory.items[pos.Item1].getIndex(pos.Item2, pos.Item3));
            }

            decimal totalEarned = sellPrice * amount;
            plugin.EconomyManager.AddBalance(sid, totalEarned, "SHOP_SELL", $"Sold {amount}x (ID: {itemId})");

            var shopItem = plugin.ShopManager.GetItem(itemId);
            string itemName = shopItem != null ? shopItem.Name : $"Item {itemId}";

            UnturnedChat.Say(player, $"{Msg.Prefix} Sold {amount}x {itemName} for ${totalEarned:N0}!", Color.green);
        }
    }
}
