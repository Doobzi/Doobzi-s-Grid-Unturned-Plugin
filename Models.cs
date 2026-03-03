using System;
using System.Collections.Generic;

namespace BountyPlugin
{
    // ==================== CHAT HELPERS ====================

    public static class Msg
    {
        public static string Prefix
        {
            get
            {
                var gen = BountyPlugin.Instance?.Configuration?.Instance?.General;
                return gen != null ? gen.Prefix : "[Doobzi's Grid]";
            }
        }

        public static string PluginName
        {
            get
            {
                var gen = BountyPlugin.Instance?.Configuration?.Instance?.General;
                return gen != null ? gen.PluginName : "Doobzi's Grid";
            }
        }

        public static string Currency
        {
            get
            {
                var gen = BountyPlugin.Instance?.Configuration?.Instance?.General;
                return gen != null ? gen.CurrencySymbol : "$";
            }
        }
    }

    // ==================== BOUNTY MODELS ====================

    public class BountyContribution
    {
        public string IssuerSteamId { get; set; }
        public string IssuerName { get; set; }
        public decimal Amount { get; set; }
        public bool Anonymous { get; set; }
        public DateTime AddedAt { get; set; }
    }

    public class Bounty
    {
        public int Id { get; set; }
        public string TargetSteamId { get; set; }
        public string TargetName { get; set; }
        public decimal TotalAmount { get; set; }
        public List<BountyContribution> Contributions { get; set; } = new List<BountyContribution>();
        public DateTime CreatedAt { get; set; }
        public DateTime ExpiresAt { get; set; }

        public string GetTier()
        {
            var tiers = BountyPlugin.Instance?.Configuration?.Instance?.BountyTiers;
            return tiers != null ? tiers.GetTier(TotalAmount) : "BRONZE";
        }

        public string GetTierIcon()
        {
            var tiers = BountyPlugin.Instance?.Configuration?.Instance?.BountyTiers;
            return tiers != null ? tiers.GetTierIcon(TotalAmount) : "(BRONZE)";
        }
    }

    public class BountyData
    {
        public int NextId { get; set; } = 1;
        public List<Bounty> ActiveBounties { get; set; } = new List<Bounty>();
    }

    // ==================== ECONOMY MODELS ====================

    public class Transaction
    {
        public string Type { get; set; }
        public string Description { get; set; }
        public decimal Amount { get; set; }
        public decimal BalanceAfter { get; set; }
        public DateTime Timestamp { get; set; }
    }

    public class PlayerAccount
    {
        public string SteamId { get; set; }
        public string PlayerName { get; set; }
        public decimal Balance { get; set; }
        public DateTime LastUpdated { get; set; }
        public DateTime LastDailyBonus { get; set; }
        public DateTime LastInterest { get; set; }
        public DateTime LastSalary { get; set; }
        public decimal TotalShopSpent { get; set; }
        public List<Transaction> Transactions { get; set; } = new List<Transaction>();
    }

    public class EconomyData
    {
        public Dictionary<string, PlayerAccount> Accounts { get; set; } = new Dictionary<string, PlayerAccount>();
    }

    // ==================== SHOP MODELS ====================

    public class ShopItem
    {
        public ushort ItemId { get; set; }
        public string ItemName { get; set; }
        public string Category { get; set; }
        public decimal Price { get; set; }
        public int DefaultStock { get; set; }
        public int CurrentStock { get; set; }
        public bool IsEnabled { get; set; } = true;

        // Convenience accessors used by commands
        [Newtonsoft.Json.JsonIgnore]
        public string Name => ItemName;
        [Newtonsoft.Json.JsonIgnore]
        public int Stock => CurrentStock;
    }

    public class ShopData
    {
        public DateTime LastRefresh { get; set; }
        public Dictionary<ushort, ShopItem> Items { get; set; } = new Dictionary<ushort, ShopItem>();
    }

    // ==================== HUNTER MODELS ====================

    public class HunterStats
    {
        public string SteamId { get; set; }
        public string PlayerName { get; set; }
        public int BountiesCompleted { get; set; }
        public decimal TotalEarned { get; set; }

        [Newtonsoft.Json.JsonIgnore]
        public int BountiesClaimed => BountiesCompleted;
        public int CurrentStreak { get; set; }
        public int BestStreak { get; set; }
        public int WeeklyBounties { get; set; }
        public decimal WeeklyEarned { get; set; }
        public DateTime WeekStart { get; set; }
        public string Title { get; set; } = "Novice";
    }

    public class HunterData
    {
        public Dictionary<string, HunterStats> Hunters { get; set; } = new Dictionary<string, HunterStats>();
    }

    // ==================== ACHIEVEMENT MODELS ====================

    public class PlayerAchievements
    {
        public string SteamId { get; set; }
        public string PlayerName { get; set; }
        public List<string> Unlocked { get; set; } = new List<string>();
        public decimal TotalBountiesPlaced { get; set; }
        public decimal TotalMoneySpent { get; set; }
    }

    public class AchievementData
    {
        public Dictionary<string, PlayerAchievements> Players { get; set; } = new Dictionary<string, PlayerAchievements>();
    }

    public static class AchievementDefs
    {
        // Keep the ID constants (never changes — these are keys, not display names)
        public const string FirstBlood = "FIRST_BLOOD";
        public const string FiveBounties = "FIVE_BOUNTIES";
        public const string TenBounties = "TEN_BOUNTIES";
        public const string TwentyFive = "TWENTY_FIVE";
        public const string FiftyBounties = "FIFTY_BOUNTIES";
        public const string BigSpender = "BIG_SPENDER";
        public const string Streak3 = "STREAK_3";
        public const string Streak5 = "STREAK_5";
        public const string Streak10 = "STREAK_10";
        public const string LegendaryHunter = "LEGENDARY_HUNTER";
        public const string Survivalist = "SURVIVALIST";
        public const string Shopaholic = "SHOPAHOLIC";
        public const string Auctioneer = "AUCTIONEER";

        private static AchievementSettings Cfg =>
            BountyPlugin.Instance?.Configuration?.Instance?.Achievements;

        public static bool IsEnabled => Cfg?.Enabled ?? true;

        public static bool IsAchievementEnabled(string id) => Cfg?.Get(id) != null;

        public static decimal GetThreshold(string id) => Cfg?.Get(id)?.Threshold ?? 1;

        public static string GetName(string id)
        {
            return Cfg?.GetName(id) ?? id;
        }

        public static string GetDescription(string id)
        {
            return Cfg?.GetDescription(id) ?? "???";
        }
    }

    // ==================== AUCTION MODELS ====================

    public class AuctionListing
    {
        public int Id { get; set; }
        public string SellerSteamId { get; set; }
        public string SellerName { get; set; }
        public ushort ItemId { get; set; }
        public string ItemName { get; set; }
        public int Amount { get; set; } = 1;
        public decimal Price { get; set; }
        public bool Active { get; set; } = true;
        public DateTime ListedAt { get; set; }
        public DateTime ExpiresAt { get; set; }
    }

    public class AuctionData
    {
        public int NextId { get; set; } = 1;
        public List<AuctionListing> Listings { get; set; } = new List<AuctionListing>();
    }
}
