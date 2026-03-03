using Rocket.API;
using Rocket.Unturned.Chat;
using Rocket.Unturned.Player;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace BountyPlugin
{
    public class CommandBountyList : IRocketCommand
    {
        public AllowedCaller AllowedCaller => AllowedCaller.Both;
        public string Name => "bountylist";
        public string Help => "View active bounties";
        public string Syntax => "/bountylist [page]";
        public List<string> Aliases => new List<string> { "bl" };
        public List<string> Permissions => new List<string> { "bounty.list" };

        public void Execute(IRocketPlayer caller, string[] command)
        {
            var plugin = BountyPlugin.Instance;
            var bounties = plugin.BountyManager.GetActiveBounties()
                .OrderByDescending(b => b.TotalAmount)
                .ToList();

            if (bounties.Count == 0)
            {
                Say(caller, $"{Msg.Prefix} No active bounties.", Color.yellow);
                return;
            }

            int page = 1;
            if (command.Length > 0) int.TryParse(command[0], out page);
            page = System.Math.Max(1, page);

            int perPage = 5;
            int totalPages = (int)System.Math.Ceiling(bounties.Count / (double)perPage);
            page = System.Math.Min(page, totalPages);

            Say(caller, $"{Msg.Prefix} === ACTIVE BOUNTIES ({bounties.Count}) === Page {page}/{totalPages}", BountyPlugin.Gold);

            var pageItems = bounties.Skip((page - 1) * perPage).Take(perPage);
            int rank = (page - 1) * perPage + 1;

            foreach (var b in pageItems)
            {
                string tier = b.GetTierIcon();
                Color c = BountyPlugin.GetTierColor(b.GetTier());
                Say(caller, $"  #{rank} {tier} {b.TargetName} - ${b.TotalAmount:N0}", c);
                rank++;
            }

            if (page < totalPages)
                Say(caller, $"{Msg.Prefix} Use /bountylist {page + 1} for next page.", Color.gray);
        }

        private void Say(IRocketPlayer caller, string msg, Color color)
        {
            if (caller is UnturnedPlayer p)
                UnturnedChat.Say(p, msg, color);
            else
                Rocket.Core.Logging.Logger.Log(msg);
        }
    }
}
