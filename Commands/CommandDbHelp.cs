using Rocket.API;
using Rocket.Unturned.Chat;
using Rocket.Unturned.Player;
using System.Collections.Generic;
using UnityEngine;

namespace BountyPlugin
{
    public class CommandDbHelp : IRocketCommand
    {
        public AllowedCaller AllowedCaller => AllowedCaller.Both;
        public string Name => "gridhelp";
        public string Help => "Shows all plugin commands";
        public string Syntax => "/gridhelp";
        public List<string> Aliases => new List<string>();
        public List<string> Permissions => new List<string> { "bounty.help" };

        public void Execute(IRocketPlayer caller, string[] command)
        {
            Say(caller, $"{Msg.Prefix} ===== {Msg.PluginName.ToUpper()} =====", BountyPlugin.Gold);
            Say(caller, "", Color.white);
            Say(caller, "  BOUNTY COMMANDS:", Color.cyan);
            Say(caller, "  /bountyadd <player> <amount> [anonymous]", Color.white);
            Say(caller, "  /bountylist [page] - View active bounties", Color.white);
            Say(caller, "  /bountytop [weekly] - Leaderboard", Color.white);
            Say(caller, "  /bountyhunter [player] - Hunter stats", Color.white);
            Say(caller, "", Color.white);
            Say(caller, "  ECONOMY COMMANDS:", Color.cyan);
            Say(caller, "  /balance - Check balance", Color.white);
            Say(caller, "  /pay <player> <amount> - Send money (5% tax)", Color.white);
            Say(caller, "  /transactions [count] - Transaction history", Color.white);
            Say(caller, "  /profile [player] - Full player profile", Color.white);
            Say(caller, "", Color.white);
            Say(caller, "  SHOP COMMANDS:", Color.cyan);
            Say(caller, "  /shop [page] - Browse all items", Color.white);
            Say(caller, "  /shopsearch <keyword> - Search items", Color.white);
            Say(caller, "  /shopcats - View categories", Color.white);
            Say(caller, "  /shopcat <category> [page] - Browse category", Color.white);
            Say(caller, "  /shopbuy <id> [amount] - Buy item", Color.white);
            Say(caller, "  /sell <id> [amount] - Sell item (50% price)", Color.white);
            Say(caller, "", Color.white);
            Say(caller, "  AUCTION HOUSE:", Color.cyan);
            Say(caller, "  /ahlist [page] - Browse listings", Color.white);
            Say(caller, "  /ahsell <itemId> <price> - List item", Color.white);
            Say(caller, "  /ahbuy <id> - Buy listing", Color.white);
            Say(caller, "  /ahcancel <id> - Cancel listing", Color.white);
            Say(caller, "  /ahsearch <keyword> - Search", Color.white);
            Say(caller, "  /ahmy - Your listings", Color.white);
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
