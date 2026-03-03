using Rocket.Core.Plugins;
using Rocket.Unturned;
using Rocket.Unturned.Events;
using Rocket.Unturned.Player;
using Rocket.Unturned.Chat;
using Rocket.API.Collections;
using SDG.Unturned;
using Steamworks;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace BountyPlugin
{
    public class BountyPlugin : RocketPlugin<BountyConfiguration>
    {
        public static BountyPlugin Instance { get; private set; }
        public EconomyManager EconomyManager { get; private set; }
        public BountyManager BountyManager { get; private set; }
        public ShopManager ShopManager { get; private set; }
        public AuctionManager AuctionManager { get; private set; }
        public MySqlStorage MySql { get; private set; }

        // Brand colors
        public static readonly Color Gold = new Color(1f, 0.84f, 0f);
        public static readonly Color BronzeColor = new Color(0.80f, 0.50f, 0.20f);
        public static readonly Color SilverColor = new Color(0.75f, 0.75f, 0.75f);
        public static readonly Color GoldTier = new Color(1f, 0.84f, 0f);
        public static readonly Color LegendaryColor = new Color(1f, 0.27f, 0f);

        protected override void Load()
        {
            Instance = this;

            // Validate config - fix fields defaulting to 0 from old config
            ValidateConfig();

            // Optional MySQL
            var mysqlCfg = Configuration.Instance.MySQL;
            if (mysqlCfg != null && mysqlCfg.Enabled)
            {
                MySql = new MySqlStorage(mysqlCfg);
                MySql.Initialize(); // logs success/failure
            }

            EconomyManager = new EconomyManager();
            BountyManager = new BountyManager();
            ShopManager = new ShopManager();
            AuctionManager = new AuctionManager();

            EconomyManager.Load();
            BountyManager.Load();
            ShopManager.Load();
            AuctionManager.Load();

            // Hook events
            UnturnedPlayerEvents.OnPlayerDeath += OnPlayerDeath;
            U.Events.OnPlayerConnected += OnPlayerConnected;

            // Timers
            float shopRefresh = Configuration.Instance.Shop.StockRefreshMinutes * 60f;
            InvokeRepeating("RefreshShopStock", shopRefresh, shopRefresh);

            float bountyExpiry = 300f; // check every 5 min
            InvokeRepeating("ProcessExpiredBounties", bountyExpiry, bountyExpiry);

            float mostWanted = Configuration.Instance.Bounties.MostWantedAnnouncementMinutes * 60f;
            if (mostWanted > 0)
                InvokeRepeating("BroadcastMostWanted", mostWanted, mostWanted);

            float salaryCheck = 60f; // check every 60 seconds
            InvokeRepeating("ProcessSalaries", salaryCheck, salaryCheck);

            float interestCheck = Configuration.Instance.Economy.InterestPayoutMinutes * 60f;
            InvokeRepeating("ProcessInterest", interestCheck, interestCheck);

            float auctionExpiry = 300f;
            InvokeRepeating("ProcessExpiredAuctions", auctionExpiry, auctionExpiry);

            Rocket.Core.Logging.Logger.Log("=========================================");
            Rocket.Core.Logging.Logger.Log($"  {Msg.PluginName} v2.3.0 Loaded!");
            Rocket.Core.Logging.Logger.Log($"  Active Bounties: {BountyManager.GetActiveBounties().Count}");
            Rocket.Core.Logging.Logger.Log($"  Auction Listings: {AuctionManager.GetActiveListings().Count}");
            Rocket.Core.Logging.Logger.Log($"  Storage: {(MySql != null && MySql.IsReady ? "MySQL" : "JSON Files")}");
            Rocket.Core.Logging.Logger.Log($"  Discord Webhooks: {(Configuration.Instance.Discord?.Enabled == true ? "ON" : "OFF")}");
            Rocket.Core.Logging.Logger.Log($"  Achievements: {(Configuration.Instance.Achievements?.Enabled == true ? "ON" : "OFF")}");
            Rocket.Core.Logging.Logger.Log("=========================================");
        }

        protected override void Unload()
        {
            try
            {
                UnturnedPlayerEvents.OnPlayerDeath -= OnPlayerDeath;
                U.Events.OnPlayerConnected -= OnPlayerConnected;

                CancelInvoke("RefreshShopStock");
                CancelInvoke("ProcessExpiredBounties");
                CancelInvoke("BroadcastMostWanted");
                CancelInvoke("ProcessSalaries");
                CancelInvoke("ProcessInterest");
                CancelInvoke("ProcessExpiredAuctions");

                if (EconomyManager != null) EconomyManager.Save();
                if (BountyManager != null) BountyManager.Save();
                if (ShopManager != null) ShopManager.Save();
                if (AuctionManager != null) AuctionManager.Save();

                Rocket.Core.Logging.Logger.Log($"{Msg.PluginName} v2.3.0 unloaded.");
            }
            catch (Exception ex)
            {
                Rocket.Core.Logging.Logger.LogError($"[{Msg.PluginName}] Error during unload: {ex.Message}");
            }
        }

        // ==================== PLAYER CONNECTED ====================

        private void OnPlayerConnected(UnturnedPlayer player)
        {
            string sid = player.CSteamID.ToString();
            bool isNew = EconomyManager.IsNewAccount(sid);

            EconomyManager.EnsureAccount(sid, player.DisplayName);
            BountyManager.EnsureAchievements(sid, player.DisplayName);

            if (isNew)
            {
                decimal bal = Configuration.Instance.Economy.StartingBalance;
                UnturnedChat.Say(player, $"{Msg.Prefix} Welcome to the server! Starting balance: ${bal:N0}", Gold);
            }

            // Daily bonus
            if (EconomyManager.CanClaimDailyBonus(sid))
            {
                decimal bonus = EconomyManager.ClaimDailyBonus(sid);
                UnturnedChat.Say(player, $"{Msg.Prefix} Daily bonus claimed: +${bonus:N0}!", Color.green);
            }

            // Bounty warning
            var bounty = BountyManager.GetBounty(sid);
            if (bounty != null)
            {
                string tier = bounty.GetTierIcon();
                UnturnedChat.Say(player, $"{Msg.Prefix} WARNING! You have a {tier} bounty of ${bounty.TotalAmount:N0} on your head!", Color.red);
            }

            // Show tips
            UnturnedChat.Say(player, $"{Msg.Prefix} Type /gridhelp for commands. Happy hunting!", Gold);
        }

        // ==================== PLAYER DEATH ====================

        private void OnPlayerDeath(UnturnedPlayer victim, EDeathCause cause, ELimb limb, CSteamID murderer)
        {
            if (murderer == CSteamID.Nil || murderer.m_SteamID == 0) return;

            string victimId = victim.CSteamID.ToString();
            string killerId = murderer.ToString();
            if (victimId == killerId) return;

            // Always reset the victim's bounty kill streak when they die
            BountyManager.ResetStreak(victimId);

            string killerName = GetPlayerName(murderer);
            var killerPlayer = UnturnedPlayer.FromCSteamID(murderer);

            // Self-defense: killer has a bounty and victim was a hunter
            var killerBounty = BountyManager.GetBounty(killerId);
            if (killerBounty != null)
            {
                decimal defenseBonus = Math.Round(killerBounty.TotalAmount * (Configuration.Instance.Bounties.SelfDefenseBonusPercent / 100m), 0);
                if (defenseBonus > 0)
                {
                    EconomyManager.AddBalance(killerId, defenseBonus, "SELF_DEFENSE", $"Self-defense vs {victim.DisplayName}");
                    if (killerPlayer != null)
                        UnturnedChat.Say(killerPlayer, $"{Msg.Prefix} Self-defense bonus! +${defenseBonus:N0} for defending against your hunter!", Color.green);

                    // Survivalist achievement
                    if (AchievementDefs.IsEnabled && AchievementDefs.IsAchievementEnabled(AchievementDefs.Survivalist)
                        && BountyManager.TryUnlockAchievement(killerId, killerName, AchievementDefs.Survivalist))
                        if (killerPlayer != null)
                            UnturnedChat.Say(killerPlayer, $"{Msg.Prefix} ACHIEVEMENT UNLOCKED: {AchievementDefs.GetName(AchievementDefs.Survivalist)}!", Color.magenta);
                }
            }

            // Check if victim has a bounty
            var victimBounty = BountyManager.GetBounty(victimId);
            if (victimBounty == null)
            {
                // If killer had a streak and died (wasn't a bounty kill), reset streak on death elsewhere
                // Actually, reset streak when a hunter dies without making a bounty kill
                return;
            }

            // Bounty kill! Calculate reward with streak bonus
            decimal baseAmount = BountyManager.CompleteBounty(victimId);
            if (baseAmount <= 0) return;

            decimal streakBonus = BountyManager.GetStreakBonus(killerId);
            decimal streakAmount = Math.Round(baseAmount * streakBonus, 0);
            decimal totalReward = baseAmount + streakAmount;

            // Pay the hunter
            EconomyManager.AddBalance(killerId, totalReward, "BOUNTY_EARNED", $"Bounty on {victim.DisplayName}");
            BountyManager.RecordHunterKill(killerId, killerName, totalReward);

            // Check achievements
            var newAchievements = BountyManager.CheckHunterAchievements(killerId, killerName, baseAmount);

            // Broadcast
            if (Configuration.Instance.Bounties.AnnounceCompletedBounties)
            {
                string tier = victimBounty.GetTierIcon();
                UnturnedChat.Say(
                    $"{Msg.Prefix} {killerName} has claimed the {tier} ${baseAmount:N0} bounty on {victim.DisplayName}!",
                    Color.magenta
                );
            }

            // Direct message to killer
            if (killerPlayer != null)
            {
                string streakMsg = streakAmount > 0 ? $" (+${streakAmount:N0} streak bonus!)" : "";
                int streak = BountyManager.GetStreak(killerId);
                UnturnedChat.Say(killerPlayer,
                    $"{Msg.Prefix} +${totalReward:N0} bounty collected!{streakMsg}", Color.green);

                if (streak > 1)
                    UnturnedChat.Say(killerPlayer, $"{Msg.Prefix} Kill streak: {streak}x ({streak * 10}% bonus on next bounty!)", Gold);

                // Title update
                var stats = BountyManager.GetHunterStats(killerId);
                if (stats != null)
                    UnturnedChat.Say(killerPlayer, $"{Msg.Prefix} Title: {stats.Title}", Color.cyan);

                // Achievement notifications
                foreach (var achId in newAchievements)
                    UnturnedChat.Say(killerPlayer, $"{Msg.Prefix} ACHIEVEMENT UNLOCKED: {AchievementDefs.GetName(achId)}!", Color.magenta);
            }

            // Message to victim
            UnturnedChat.Say(victim, $"{Msg.Prefix} Your bounty has been collected by {killerName}.", Color.red);

            // Discord webhook
            DiscordWebhook.BountyClaimed(victim.DisplayName, totalReward, killerName,
                BountyManager.GetStreak(killerId), victimBounty.GetTierIcon());
        }

        // ==================== TIMED CALLBACKS ====================

        private void RefreshShopStock()
        {
            ShopManager.RefreshStock();
        }

        private void ProcessExpiredBounties()
        {
            var refunds = BountyManager.ProcessExpiredBounties();
            foreach (var kv in refunds)
            {
                EconomyManager.AddBalance(kv.Key, kv.Value, "BOUNTY_REFUND", "Bounty expired - refund");
                var player = TryGetPlayer(kv.Key);
                if (player != null)
                    UnturnedChat.Say(player, $"{Msg.Prefix} A bounty you placed has expired. Refunded ${kv.Value:N0}.", Color.yellow);
            }
        }

        private void BroadcastMostWanted()
        {
            var top = BountyManager.GetHighestBounty();
            if (top == null) return;
            string tier = top.GetTierIcon();
            UnturnedChat.Say(
                $"{Msg.Prefix} MOST WANTED: {top.TargetName} {tier} - ${top.TotalAmount:N0} bounty! Hunt them down!",
                Color.magenta
            );
        }

        private void ProcessSalaries()
        {
            foreach (var sp in Provider.clients)
            {
                try
                {
                    var player = UnturnedPlayer.FromSteamPlayer(sp);
                    if (player == null) continue;
                    string sid = player.CSteamID.ToString();
                    decimal salary = EconomyManager.ProcessSalary(sid);
                    if (salary > 0)
                        UnturnedChat.Say(player, $"{Msg.Prefix} Salary received: +${salary:N0}", Color.green);
                }
                catch { /* player may be disconnecting */ }
            }
        }

        private void ProcessInterest()
        {
            foreach (var sp in Provider.clients)
            {
                try
                {
                    var player = UnturnedPlayer.FromSteamPlayer(sp);
                    if (player == null) continue;
                    EconomyManager.ProcessInterest(player.CSteamID.ToString());
                }
                catch { }
            }
        }

        private void ProcessExpiredAuctions()
        {
            var expired = AuctionManager.ProcessExpiredListings();
            // Note: items from expired auctions are lost (seller should have been online to collect)
            // We just log it - a more advanced system could mail items
            foreach (var listing in expired)
            {
                var player = TryGetPlayer(listing.SellerSteamId);
                if (player != null)
                    UnturnedChat.Say(player, $"{Msg.Prefix} Your auction for {listing.ItemName} has expired.", Color.yellow);
            }
        }

        // ==================== HELPERS ====================

        private string GetPlayerName(CSteamID steamId)
        {
            try
            {
                var p = UnturnedPlayer.FromCSteamID(steamId);
                return p?.DisplayName ?? steamId.ToString();
            }
            catch { return steamId.ToString(); }
        }

        private UnturnedPlayer TryGetPlayer(string steamId)
        {
            try
            {
                if (ulong.TryParse(steamId, out ulong id))
                    return UnturnedPlayer.FromCSteamID(new CSteamID(id));
            }
            catch { }
            return null;
        }

        public static Color GetTierColor(string tier)
        {
            var tiers = Instance?.Configuration?.Instance?.BountyTiers;
            if (tiers != null)
            {
                if (tier == tiers.LegendaryName) return LegendaryColor;
                if (tier == tiers.GoldName) return GoldTier;
                if (tier == tiers.SilverName) return SilverColor;
            }
            return BronzeColor;
        }

        private void ValidateConfig()
        {
            var c = Configuration.Instance;
            bool changed = false;

            // Ensure nested objects exist
            if (c.General == null) { c.General = new GeneralSettings(); changed = true; }
            if (c.Economy == null) { c.Economy = new EconomySettings(); changed = true; }
            if (c.Bounties == null) { c.Bounties = new BountySettings(); changed = true; }
            if (c.BountyTiers == null) { c.BountyTiers = new BountyTierSettings(); changed = true; }
            if (c.Achievements == null) { c.Achievements = new AchievementSettings(); changed = true; }
            if (c.Shop == null) { c.Shop = new ShopSettings(); changed = true; }
            if (c.AuctionHouse == null) { c.AuctionHouse = new AuctionSettings(); changed = true; }
            if (c.MySQL == null) { c.MySQL = new MySqlSettings(); changed = true; }
            if (c.Discord == null) { c.Discord = new DiscordSettings(); changed = true; }

            // Economy
            if (c.Economy.StartingBalance <= 0) { c.Economy.StartingBalance = 1000; changed = true; }
            if (c.Economy.DailyBonusMin <= 0) { c.Economy.DailyBonusMin = 200; changed = true; }
            if (c.Economy.DailyBonusMax <= 0) { c.Economy.DailyBonusMax = 500; changed = true; }
            if (c.Economy.InterestRatePercent <= 0) { c.Economy.InterestRatePercent = 1; changed = true; }
            if (c.Economy.InterestMaxPayout <= 0) { c.Economy.InterestMaxPayout = 500; changed = true; }
            if (c.Economy.InterestPayoutMinutes <= 0) { c.Economy.InterestPayoutMinutes = 60; changed = true; }
            if (c.Economy.SalaryAmount <= 0) { c.Economy.SalaryAmount = 50; changed = true; }
            if (c.Economy.SalaryIntervalMinutes <= 0) { c.Economy.SalaryIntervalMinutes = 10; changed = true; }
            if (c.Economy.TransferTaxPercent <= 0) { c.Economy.TransferTaxPercent = 5; changed = true; }

            // Bounties
            if (c.Bounties.MinimumAmount <= 0) { c.Bounties.MinimumAmount = 100; changed = true; }
            if (c.Bounties.ExpiryHours <= 0) { c.Bounties.ExpiryHours = 48; changed = true; }
            if (c.Bounties.AnonymousFeePercent <= 0) { c.Bounties.AnonymousFeePercent = 10; changed = true; }
            if (c.Bounties.SelfDefenseBonusPercent <= 0) { c.Bounties.SelfDefenseBonusPercent = 25; changed = true; }
            if (c.Bounties.StreakBonusPercent <= 0) { c.Bounties.StreakBonusPercent = 10; changed = true; }
            if (c.Bounties.MostWantedAnnouncementMinutes <= 0) { c.Bounties.MostWantedAnnouncementMinutes = 15; changed = true; }

            // Shop
            if (c.Shop.StockRefreshMinutes <= 0) { c.Shop.StockRefreshMinutes = 60; changed = true; }
            if (c.Shop.DefaultStockPerItem <= 0) { c.Shop.DefaultStockPerItem = 10; changed = true; }
            if (c.Shop.SellPricePercent <= 0) { c.Shop.SellPricePercent = 50; changed = true; }

            // Auction
            if (c.AuctionHouse.ListingExpiryHours <= 0) { c.AuctionHouse.ListingExpiryHours = 24; changed = true; }
            if (c.AuctionHouse.MaxListingsPerPlayer <= 0) { c.AuctionHouse.MaxListingsPerPlayer = 5; changed = true; }

            if (changed)
            {
                Configuration.Save();
                Rocket.Core.Logging.Logger.Log($"[{Msg.PluginName}] Config validated - missing/zero values set to defaults.");
            }
        }

        public override TranslationList DefaultTranslations => new TranslationList();
    }
}
