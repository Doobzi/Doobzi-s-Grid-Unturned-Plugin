using Rocket.API;
using Rocket.Unturned.Chat;
using Rocket.Unturned.Player;
using System.Collections.Generic;
using UnityEngine;

namespace BountyPlugin
{
    public class CommandBountyReload : IRocketCommand
    {
        public AllowedCaller AllowedCaller => AllowedCaller.Both;
        public string Name => "bountyreload";
        public string Help => "Reload all plugin data from disk";
        public string Syntax => "/bountyreload";
        public List<string> Aliases => new List<string>();
        public List<string> Permissions => new List<string> { "bounty.admin" };

        public void Execute(IRocketPlayer caller, string[] command)
        {
            var plugin = BountyPlugin.Instance;

            plugin.EconomyManager.Load();
            plugin.BountyManager.Load();
            plugin.ShopManager.Load();
            plugin.AuctionManager.Load();

            Say(caller, $"{Msg.Prefix} All data reloaded from disk!", Color.green);
            Rocket.Core.Logging.Logger.Log($"[{Msg.PluginName}] All data reloaded by admin.");
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
