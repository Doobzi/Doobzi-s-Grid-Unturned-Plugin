using Rocket.API;
using Rocket.Unturned.Chat;
using Rocket.Unturned.Player;
using System.Collections.Generic;
using UnityEngine;

namespace BountyPlugin
{
    public class CommandAhCancel : IRocketCommand
    {
        public AllowedCaller AllowedCaller => AllowedCaller.Player;
        public string Name => "ahcancel";
        public string Help => "Cancel your auction listing";
        public string Syntax => "/ahcancel <listingId>";
        public List<string> Aliases => new List<string>();
        public List<string> Permissions => new List<string> { "bounty.auction" };

        public void Execute(IRocketPlayer caller, string[] command)
        {
            var player = (UnturnedPlayer)caller;
            var plugin = BountyPlugin.Instance;
            string sid = player.CSteamID.ToString();

            if (command.Length < 1 || !int.TryParse(command[0], out int listingId))
            {
                UnturnedChat.Say(player, $"{Msg.Prefix} Usage: /ahcancel <listingId>", Color.yellow);
                return;
            }

            var listing = plugin.AuctionManager.GetListing(listingId);
            if (listing == null || !listing.Active)
            {
                UnturnedChat.Say(player, $"{Msg.Prefix} Listing not found.", Color.red);
                return;
            }

            if (listing.SellerSteamId != sid)
            {
                UnturnedChat.Say(player, $"{Msg.Prefix} That's not your listing.", Color.red);
                return;
            }

            // Return item
            player.GiveItem(listing.ItemId, 1);
            plugin.AuctionManager.RemoveListing(listingId);

            UnturnedChat.Say(player, $"{Msg.Prefix} Cancelled listing for {listing.ItemName}. Item returned.", Color.green);

            // Discord webhook
            DiscordWebhook.AuctionCancelled(listing.ItemName, listing.ItemId, listing.Price, player.DisplayName);
        }
    }
}
