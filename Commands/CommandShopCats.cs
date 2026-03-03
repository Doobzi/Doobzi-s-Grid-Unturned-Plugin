using Rocket.API;
using Rocket.Unturned.Chat;
using Rocket.Unturned.Player;
using System.Collections.Generic;
using UnityEngine;

namespace BountyPlugin
{
    public class CommandShopCats : IRocketCommand
    {
        public AllowedCaller AllowedCaller => AllowedCaller.Player;
        public string Name => "shopcats";
        public string Help => "View shop categories";
        public string Syntax => "/shopcats";
        public List<string> Aliases => new List<string>();
        public List<string> Permissions => new List<string> { "bounty.shop" };

        public void Execute(IRocketPlayer caller, string[] command)
        {
            var player = (UnturnedPlayer)caller;
            UnturnedChat.Say(player, $"{Msg.Prefix} === SHOP CATEGORIES ===", BountyPlugin.Gold);
            UnturnedChat.Say(player, $"  {string.Join(", ", ShopManager.ValidCategories)}", Color.white);
            UnturnedChat.Say(player, $"{Msg.Prefix} Use /shopcat <category> to browse.", Color.gray);
        }
    }
}
