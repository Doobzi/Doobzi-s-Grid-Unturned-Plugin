using Rocket.API;
using Rocket.Unturned.Chat;
using Rocket.Unturned.Player;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace BountyPlugin
{
    public class CommandAhList : IRocketCommand
    {
        public AllowedCaller AllowedCaller => AllowedCaller.Player;
        public string Name => "ahlist";
        public string Help => "Browse auction house listings";
        public string Syntax => "/ahlist [page]";
        public List<string> Aliases => new List<string>();
        public List<string> Permissions => new List<string> { "bounty.auction" };

        public void Execute(IRocketPlayer caller, string[] command)
        {
            var player = (UnturnedPlayer)caller;
            var plugin = BountyPlugin.Instance;
            var listings = plugin.AuctionManager.GetActiveListings();

            if (listings.Count == 0)
            {
                UnturnedChat.Say(player, $"{Msg.Prefix} No active auction listings.", Color.yellow);
                return;
            }

            int page = 1;
            if (command.Length > 0) int.TryParse(command[0], out page);
            page = System.Math.Max(1, page);

            int perPage = 5;
            int totalPages = (int)System.Math.Ceiling(listings.Count / (double)perPage);
            page = System.Math.Min(page, totalPages);

            UnturnedChat.Say(player, $"{Msg.Prefix} === AUCTION HOUSE ({listings.Count} listings) === Page {page}/{totalPages}", BountyPlugin.Gold);

            var pageItems = listings.Skip((page - 1) * perPage).Take(perPage);
            foreach (var l in pageItems)
            {
                string timeLeft = AuctionHelper.GetTimeLeft(l.ExpiresAt);
                UnturnedChat.Say(player, $"  #{l.Id} {l.ItemName} x{l.Amount} - ${l.Price:N0} by {l.SellerName} ({timeLeft})", Color.white);
            }

            if (page < totalPages)
                UnturnedChat.Say(player, $"{Msg.Prefix} /ahlist {page + 1} for next page", Color.gray);
        }
    }

    public static class AuctionHelper
    {
        public static string GetTimeLeft(System.DateTime expiry)
        {
            var remaining = expiry - System.DateTime.UtcNow;
            if (remaining.TotalMinutes < 1) return "< 1m";
            if (remaining.TotalHours < 1) return $"{(int)remaining.TotalMinutes}m";
            return $"{(int)remaining.TotalHours}h {remaining.Minutes}m";
        }
    }
}
