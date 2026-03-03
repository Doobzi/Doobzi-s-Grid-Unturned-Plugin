using Rocket.API;
using System.Collections.Generic;
using System.Xml.Serialization;

namespace BountyPlugin
{
    // ============================================================
    //  PLUGIN CONFIGURATION
    //  All percentage values are WHOLE NUMBERS (5 = 5%)
    //  Edit this file, then use /bountyreload in-game to apply.
    // ============================================================

    public class BountyConfiguration : IRocketPluginConfiguration
    {
        // ── General ──
        [XmlElement("General")]
        public GeneralSettings General;

        // ── Economy ──
        [XmlElement("Economy")]
        public EconomySettings Economy;

        // ── Bounties ──
        [XmlElement("Bounties")]
        public BountySettings Bounties;

        // ── Bounty Tiers ──
        [XmlElement("BountyTiers")]
        public BountyTierSettings BountyTiers;

        // ── Achievements ──
        [XmlElement("Achievements")]
        public AchievementSettings Achievements;

        // ── Shop ──
        [XmlElement("Shop")]
        public ShopSettings Shop;

        // ── Auction House ──
        [XmlElement("AuctionHouse")]
        public AuctionSettings AuctionHouse;

        // ── MySQL (Optional) ──
        [XmlElement("MySQL")]
        public MySqlSettings MySQL;

        // ── Discord Webhooks (Optional) ──
        [XmlElement("Discord")]
        public DiscordSettings Discord;

        public void LoadDefaults()
        {
            General = new GeneralSettings();
            Economy = new EconomySettings();
            Bounties = new BountySettings();
            BountyTiers = new BountyTierSettings();
            Achievements = new AchievementSettings();
            Shop = new ShopSettings();
            AuctionHouse = new AuctionSettings();
            MySQL = new MySqlSettings();
            Discord = new DiscordSettings();
        }
    }

    // ═══════════════════════════════════════════════════════════
    //  GENERAL SETTINGS
    //  Change the plugin's display name, chat prefix, and version tag.
    // ═══════════════════════════════════════════════════════════
    public class GeneralSettings
    {
        /// The name shown in chat messages, e.g. "[Doobzi's Grid]"
        public string PluginName = "Doobzi's Grid";

        /// The prefix shown before every chat message (auto-generated from PluginName if empty)
        public string ChatPrefix = "";

        /// Currency symbol used in all messages
        public string CurrencySymbol = "$";

        [XmlIgnore]
        public string Prefix => string.IsNullOrEmpty(ChatPrefix) ? $"[{PluginName}]" : ChatPrefix;
    }

    // ═══════════════════════════════════════════════════════════
    //  BOUNTY TIER SETTINGS
    //  Configure the bounty amount thresholds for each rank tier.
    //  Tiers are checked top-down (Legendary first, then Gold, Silver).
    //  Anything below Silver threshold = Bronze.
    // ═══════════════════════════════════════════════════════════
    public class BountyTierSettings
    {
        /// Amount needed for LEGENDARY tier
        public decimal LegendaryThreshold = 15000;
        /// Display name for Legendary tier
        public string LegendaryName = "LEGENDARY";
        /// Chat icon for Legendary bounties
        public string LegendaryIcon = "<<LEGENDARY>>";

        /// Amount needed for GOLD tier
        public decimal GoldThreshold = 5000;
        /// Display name for Gold tier
        public string GoldName = "GOLD";
        /// Chat icon for Gold bounties
        public string GoldIcon = "<GOLD>";

        /// Amount needed for SILVER tier
        public decimal SilverThreshold = 1000;
        /// Display name for Silver tier
        public string SilverName = "SILVER";
        /// Chat icon for Silver bounties
        public string SilverIcon = "[SILVER]";

        /// Display name for the default (lowest) tier
        public string BronzeName = "BRONZE";
        /// Chat icon for Bronze bounties
        public string BronzeIcon = "(BRONZE)";

        public string GetTier(decimal amount)
        {
            if (amount >= LegendaryThreshold) return LegendaryName;
            if (amount >= GoldThreshold) return GoldName;
            if (amount >= SilverThreshold) return SilverName;
            return BronzeName;
        }

        public string GetTierIcon(decimal amount)
        {
            if (amount >= LegendaryThreshold) return LegendaryIcon;
            if (amount >= GoldThreshold) return GoldIcon;
            if (amount >= SilverThreshold) return SilverIcon;
            return BronzeIcon;
        }
    }

    // ═══════════════════════════════════════════════════════════
    //  ACHIEVEMENT SETTINGS
    //  Customize every achievement: name, description, and the
    //  threshold required to unlock it. Set Enabled=false on any
    //  achievement to disable it entirely.
    // ═══════════════════════════════════════════════════════════
    public class AchievementSettings
    {
        /// Master toggle to enable/disable the entire achievement system
        public bool Enabled = true;

        [XmlElement("Achievement")]
        public List<AchievementDef> List = new List<AchievementDef>
        {
            new AchievementDef { Id = "FIRST_BLOOD", Name = "First Blood", Description = "Complete your first bounty kill", Threshold = 1, Enabled = true },
            new AchievementDef { Id = "FIVE_BOUNTIES", Name = "Bounty Rookie", Description = "Complete 5 bounty kills", Threshold = 5, Enabled = true },
            new AchievementDef { Id = "TEN_BOUNTIES", Name = "Seasoned Hunter", Description = "Complete 10 bounty kills", Threshold = 10, Enabled = true },
            new AchievementDef { Id = "TWENTY_FIVE", Name = "Veteran Hunter", Description = "Complete 25 bounty kills", Threshold = 25, Enabled = true },
            new AchievementDef { Id = "FIFTY_BOUNTIES", Name = "Master Hunter", Description = "Complete 50 bounty kills", Threshold = 50, Enabled = true },
            new AchievementDef { Id = "BIG_SPENDER", Name = "Big Spender", Description = "Place $50,000+ in total bounties", Threshold = 50000, Enabled = true },
            new AchievementDef { Id = "STREAK_3", Name = "Triple Threat", Description = "Get a 3 bounty kill streak", Threshold = 3, Enabled = true },
            new AchievementDef { Id = "STREAK_5", Name = "Unstoppable", Description = "Get a 5 bounty kill streak", Threshold = 5, Enabled = true },
            new AchievementDef { Id = "STREAK_10", Name = "Death Incarnate", Description = "Get a 10 bounty kill streak", Threshold = 10, Enabled = true },
            new AchievementDef { Id = "LEGENDARY_HUNTER", Name = "Legendary Hunter", Description = "Collect a Legendary-tier bounty", Threshold = 15000, Enabled = true },
            new AchievementDef { Id = "SURVIVALIST", Name = "Survivalist", Description = "Kill someone hunting your bounty", Threshold = 1, Enabled = true },
            new AchievementDef { Id = "SHOPAHOLIC", Name = "Shopaholic", Description = "Spend $100,000+ in the shop", Threshold = 100000, Enabled = true },
            new AchievementDef { Id = "AUCTIONEER", Name = "Auctioneer", Description = "Sell an item on the auction house", Threshold = 1, Enabled = true },
        };

        /// Look up an achievement def by its ID. Returns null if not found/disabled.
        public AchievementDef Get(string id)
        {
            for (int i = 0; i < List.Count; i++)
                if (List[i].Id == id && List[i].Enabled) return List[i];
            return null;
        }

        public string GetName(string id)
        {
            var def = Get(id);
            return def?.Name ?? id;
        }

        public string GetDescription(string id)
        {
            var def = Get(id);
            return def?.Description ?? "???";
        }
    }

    public class AchievementDef
    {
        [XmlAttribute("id")]
        public string Id = "";

        public string Name = "";
        public string Description = "";

        /// The numeric threshold to unlock (bounties completed, streak count, money spent, etc.)
        public decimal Threshold = 1;

        /// Set to false to disable this achievement
        public bool Enabled = true;
    }

    // ═══════════════════════════════════════════════════════════
    //  ECONOMY SETTINGS
    // ═══════════════════════════════════════════════════════════
    public class EconomySettings
    {
        /// How much money new players start with
        public decimal StartingBalance = 1000;

        /// Minimum daily login bonus (random between Min and Max)
        public decimal DailyBonusMin = 200;

        /// Maximum daily login bonus
        public decimal DailyBonusMax = 500;

        /// Interest rate earned on balance each interval (1 = 1%)
        public decimal InterestRatePercent = 1;

        /// Maximum interest payout per interval
        public decimal InterestMaxPayout = 500;

        /// How often interest is paid out (in minutes)
        public int InterestPayoutMinutes = 60;

        /// Salary amount paid per interval while online
        public decimal SalaryAmount = 50;

        /// How often salary is paid (in minutes)
        public int SalaryIntervalMinutes = 10;

        /// Tax deducted on /pay transfers (5 = 5%)
        public decimal TransferTaxPercent = 5;
    }

    // ═══════════════════════════════════════════════════════════
    //  BOUNTY SETTINGS
    // ═══════════════════════════════════════════════════════════
    public class BountySettings
    {
        /// Minimum amount for placing a bounty
        public decimal MinimumAmount = 100;

        /// Hours before a bounty expires and refunds
        public int ExpiryHours = 48;

        /// Extra fee for placing an anonymous bounty (10 = 10%)
        public decimal AnonymousFeePercent = 10;

        /// Bonus for killing someone who is hunting you (25 = 25% of your bounty)
        public decimal SelfDefenseBonusPercent = 25;

        /// Bonus per consecutive bounty kill streak (10 = +10% per kill)
        public decimal StreakBonusPercent = 10;

        /// Announce new bounties to the whole server
        public bool AnnounceNewBounties = true;

        /// Announce completed bounties to the whole server
        public bool AnnounceCompletedBounties = true;

        /// Minutes between "Most Wanted" broadcast (0 = disabled)
        public int MostWantedAnnouncementMinutes = 15;
    }

    // ═══════════════════════════════════════════════════════════
    //  SHOP SETTINGS
    // ═══════════════════════════════════════════════════════════
    public class ShopSettings
    {
        /// How often shop stock refreshes (in minutes)
        public int StockRefreshMinutes = 60;

        /// Default stock per item per refresh cycle
        public int DefaultStockPerItem = 10;

        /// What percentage of buy price players get when selling (50 = 50%)
        public decimal SellPricePercent = 50;
    }

    // ═══════════════════════════════════════════════════════════
    //  AUCTION HOUSE SETTINGS
    // ═══════════════════════════════════════════════════════════
    public class AuctionSettings
    {
        /// Hours before an auction listing expires
        public int ListingExpiryHours = 24;

        /// Maximum active listings per player
        public int MaxListingsPerPlayer = 5;
    }

    // ═══════════════════════════════════════════════════════════
    //  MYSQL DATABASE (Optional — leave Enabled as false to use JSON files)
    //  When enabled, ALL data is stored in MySQL instead of JSON.
    //  Great for multi-server setups or centralized data management.
    // ═══════════════════════════════════════════════════════════
    public class MySqlSettings
    {
        /// Set to true to use MySQL instead of JSON files
        public bool Enabled = false;

        /// MySQL server address
        public string Host = "localhost";

        /// MySQL server port
        public int Port = 3306;

        /// Database name (create this database first!)
        public string Database = "doobzis_grid";

        /// MySQL username
        public string Username = "root";

        /// MySQL password
        public string Password = "";

        /// Prefix for table names (useful if sharing a database)
        public string TablePrefix = "grid_";

        [XmlIgnore]
        public string ConnectionString =>
            $"Server={Host};Port={Port};Database={Database};Uid={Username};Pwd={Password};" +
            "SslMode=None;CharSet=utf8mb4;";
    }

    // ═══════════════════════════════════════════════════════════
    //  DISCORD WEBHOOKS (Optional — sends auction events to Discord)
    //  Create a webhook in your Discord channel settings and paste
    //  the URL below. Toggle which events you want posted.
    // ═══════════════════════════════════════════════════════════
    public class DiscordSettings
    {
        /// Set to true to enable Discord webhook notifications
        public bool Enabled = false;

        /// Discord Webhook URL (from Channel Settings → Integrations → Webhooks)
        public string WebhookUrl = "";

        /// Send a message when a new auction listing is created
        public bool NotifyAuctionListed = true;

        /// Send a message when an auction item is purchased
        public bool NotifyAuctionSold = true;

        /// Send a message when an auction listing is cancelled
        public bool NotifyAuctionCancelled = true;

        /// Send a message when a new bounty is placed
        public bool NotifyBountyPlaced = true;

        /// Send a message when a bounty is claimed
        public bool NotifyBountyClaimed = true;
    }
}
