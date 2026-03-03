using Rocket.API;
using Rocket.Unturned.Chat;
using Rocket.Unturned.Player;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace BountyPlugin
{
    public class CommandAhMy : IRocketCommand
    {
        public AllowedCaller AllowedCaller => AllowedCaller.Player;
        public string Name => "ahmy";
        public string Help => "View your auction listings";
        public string Syntax => "/ahmy";
        public List<string> Aliases => new List<string>();
        public List<string> Permissions => new List<string> { "bounty.auction" };

        public void Execute(IRocketPlayer caller, string[] command)
        {
            var player = (UnturnedPlayer)caller;
            var plugin = BountyPlugin.Instance;
            string sid = player.CSteamID.ToString();

            var listings = plugin.AuctionManager.GetActiveListings()
                .Where(l => l.SellerSteamId == sid)
                .ToList();

            if (listings.Count == 0)
            {
                UnturnedChat.Say(player, $"{Msg.Prefix} You have no active listings.", Color.yellow);
                return;
            }

            UnturnedChat.Say(player, $"{Msg.Prefix} === YOUR LISTINGS ===", BountyPlugin.Gold);
            foreach (var l in listings)
            {
                string timeLeft = AuctionHelper.GetTimeLeft(l.ExpiresAt);
                UnturnedChat.Say(player, $"  #{l.Id} {l.ItemName} x{l.Amount} - ${l.Price:N0} ({timeLeft})", Color.white);
            }
        }
    }
}
