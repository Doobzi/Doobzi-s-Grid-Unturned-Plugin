using Rocket.API;
using Rocket.Unturned.Chat;
using Rocket.Unturned.Player;
using System.Collections.Generic;
using UnityEngine;

namespace BountyPlugin
{
    public class CommandAhBuy : IRocketCommand
    {
        public AllowedCaller AllowedCaller => AllowedCaller.Player;
        public string Name => "ahbuy";
        public string Help => "Buy an auction listing";
        public string Syntax => "/ahbuy <listingId>";
        public List<string> Aliases => new List<string>();
        public List<string> Permissions => new List<string> { "bounty.auction" };

        public void Execute(IRocketPlayer caller, string[] command)
        {
            var player = (UnturnedPlayer)caller;
            var plugin = BountyPlugin.Instance;
            string sid = player.CSteamID.ToString();

            if (command.Length < 1 || !int.TryParse(command[0], out int listingId))
            {
                UnturnedChat.Say(player, $"{Msg.Prefix} Usage: /ahbuy <listingId>", Color.yellow);
                return;
            }

            var listing = plugin.AuctionManager.GetListing(listingId);
            if (listing == null || !listing.Active)
            {
                UnturnedChat.Say(player, $"{Msg.Prefix} Listing not found or expired.", Color.red);
                return;
            }

            if (listing.SellerSteamId == sid)
            {
                UnturnedChat.Say(player, $"{Msg.Prefix} You can't buy your own listing!", Color.red);
                return;
            }

            decimal balance = plugin.EconomyManager.GetBalance(sid);
            if (balance < listing.Price)
            {
                UnturnedChat.Say(player, $"{Msg.Prefix} Insufficient funds. Need ${listing.Price:N0}, have ${balance:N0}.", Color.red);
                return;
            }

            // Pay
            plugin.EconomyManager.RemoveBalance(sid, listing.Price, "AUCTION_BUY", $"Bought {listing.ItemName} from {listing.SellerName}");
            plugin.EconomyManager.AddBalance(listing.SellerSteamId, listing.Price, "AUCTION_SALE", $"Sold {listing.ItemName} to {player.DisplayName}");

            // Give item
            player.GiveItem(listing.ItemId, 1);

            // Remove listing
            plugin.AuctionManager.RemoveListing(listingId);

            UnturnedChat.Say(player, $"{Msg.Prefix} Purchased {listing.ItemName} for ${listing.Price:N0} from {listing.SellerName}!", Color.green);

            // Discord webhook
            DiscordWebhook.AuctionSold(listing.ItemName, listing.ItemId, listing.Price, listing.SellerName, player.DisplayName);

            // Notify seller if online
            try
            {
                if (ulong.TryParse(listing.SellerSteamId, out ulong sellerId))
                {
                    var seller = UnturnedPlayer.FromCSteamID(new Steamworks.CSteamID(sellerId));
                    if (seller != null)
                        UnturnedChat.Say(seller, $"{Msg.Prefix} Your {listing.ItemName} was bought by {player.DisplayName} for ${listing.Price:N0}!", Color.green);
                }
            }
            catch { }
        }
    }
}
