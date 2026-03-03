using Rocket.API;
using Rocket.Unturned.Chat;
using Rocket.Unturned.Player;
using SDG.Unturned;
using System.Collections.Generic;
using UnityEngine;

namespace BountyPlugin
{
    public class CommandAhSell : IRocketCommand
    {
        public AllowedCaller AllowedCaller => AllowedCaller.Player;
        public string Name => "ahsell";
        public string Help => "List an item on the auction house";
        public string Syntax => "/ahsell <itemId> <price>";
        public List<string> Aliases => new List<string>();
        public List<string> Permissions => new List<string> { "bounty.auction" };

        public void Execute(IRocketPlayer caller, string[] command)
        {
            var player = (UnturnedPlayer)caller;
            var plugin = BountyPlugin.Instance;
            string sid = player.CSteamID.ToString();

            if (command.Length < 2)
            {
                UnturnedChat.Say(player, $"{Msg.Prefix} Usage: /ahsell <itemId> <price>", Color.yellow);
                return;
            }

            if (!ushort.TryParse(command[0], out ushort itemId))
            {
                UnturnedChat.Say(player, $"{Msg.Prefix} Invalid item ID.", Color.red);
                return;
            }

            if (!decimal.TryParse(command[1], out decimal price) || price <= 0)
            {
                UnturnedChat.Say(player, $"{Msg.Prefix} Invalid price.", Color.red);
                return;
            }

            int maxListings = plugin.Configuration.Instance.AuctionHouse.MaxListingsPerPlayer;
            if (plugin.AuctionManager.GetPlayerListingCount(sid) >= maxListings)
            {
                UnturnedChat.Say(player, $"{Msg.Prefix} You already have {maxListings} active listings.", Color.red);
                return;
            }

            // Check inventory for item
            var inventory = player.Inventory;
            bool found = false;
            byte foundPage = 0;
            byte foundIdx = 0;

            for (byte pg = 0; pg < PlayerInventory.PAGES - 1; pg++)
            {
                if (inventory.items[pg] == null) continue;
                for (byte idx = 0; idx < inventory.items[pg].getItemCount(); idx++)
                {
                    var jar = inventory.items[pg].getItem(idx);
                    if (jar != null && jar.item.id == itemId)
                    {
                        foundPage = pg;
                        foundIdx = inventory.items[pg].getIndex(jar.x, jar.y);
                        found = true;
                        break;
                    }
                }
                if (found) break;
            }

            if (!found)
            {
                UnturnedChat.Say(player, $"{Msg.Prefix} You don't have that item in your inventory.", Color.red);
                return;
            }

            // Get item name
            var asset = Assets.find(EAssetType.ITEM, itemId) as ItemAsset;
            string itemName = asset != null ? asset.itemName : $"Item {itemId}";

            // Remove from inventory
            inventory.removeItem(foundPage, foundIdx);

            // Create listing
            int hours = plugin.Configuration.Instance.AuctionHouse.ListingExpiryHours;
            plugin.AuctionManager.CreateListing(sid, player.DisplayName, itemId, itemName, 1, price, hours);

            UnturnedChat.Say(player, $"{Msg.Prefix} Listed {itemName} for ${price:N0} on the auction house!", Color.green);

            // Auctioneer achievement
            if (AchievementDefs.IsEnabled && AchievementDefs.IsAchievementEnabled(AchievementDefs.Auctioneer))
            {
                if (plugin.BountyManager.TryUnlockAchievement(sid, player.DisplayName, AchievementDefs.Auctioneer))
                    UnturnedChat.Say(player, $"{Msg.Prefix} ACHIEVEMENT UNLOCKED: {AchievementDefs.GetName(AchievementDefs.Auctioneer)}!", Color.magenta);
            }

            // Discord webhook
            DiscordWebhook.AuctionListed(itemName, itemId, price, player.DisplayName, hours);
        }
    }
}
