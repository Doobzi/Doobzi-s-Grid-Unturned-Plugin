using Rocket.API;
using Rocket.Unturned.Chat;
using Rocket.Unturned.Player;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace BountyPlugin
{
    public class CommandAhSearch : IRocketCommand
    {
        public AllowedCaller AllowedCaller => AllowedCaller.Player;
        public string Name => "ahsearch";
        public string Help => "Search auction listings";
        public string Syntax => "/ahsearch <keyword>";
        public List<string> Aliases => new List<string>();
        public List<string> Permissions => new List<string> { "bounty.auction" };

        public void Execute(IRocketPlayer caller, string[] command)
        {
            var player = (UnturnedPlayer)caller;
            var plugin = BountyPlugin.Instance;

            if (command.Length < 1)
            {
                UnturnedChat.Say(player, $"{Msg.Prefix} Usage: /ahsearch <keyword>", Color.yellow);
                return;
            }

            string keyword = string.Join(" ", command);
            var results = plugin.AuctionManager.SearchListings(keyword);

            if (results.Count == 0)
            {
                UnturnedChat.Say(player, $"{Msg.Prefix} No listings matching \"{keyword}\".", Color.yellow);
                return;
            }

            UnturnedChat.Say(player, $"{Msg.Prefix} === Search: \"{keyword}\" ({results.Count} results) ===", BountyPlugin.Gold);
            foreach (var l in results.Take(8))
            {
                string timeLeft = AuctionHelper.GetTimeLeft(l.ExpiresAt);
                UnturnedChat.Say(player, $"  #{l.Id} {l.ItemName} x{l.Amount} - ${l.Price:N0} by {l.SellerName} ({timeLeft})", Color.white);
            }
        }
    }
}
