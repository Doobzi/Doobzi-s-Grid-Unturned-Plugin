using System;
using System.IO;
using System.Net;
using System.Text;
using System.Threading;
using Newtonsoft.Json;

namespace BountyPlugin
{
    /// <summary>
    /// Sends rich Discord webhook embeds for auction and bounty events.
    /// All HTTP calls are fire-and-forget on a background thread.
    /// </summary>
    public static class DiscordWebhook
    {
        private const string LogoUrl = "https://i.ibb.co/XxK0qrFt/2c781d93-d521-478c-a687-6c803eebfed6.png";
        private static string FooterText => Msg.PluginName;

        // Discord embed color codes (decimal)
        private const int ColorGreen  = 0x2ECC71; // listed / claimed
        private const int ColorBlue   = 0x3498DB; // bought
        private const int ColorRed    = 0xE74C3C; // cancelled / bounty placed
        private const int ColorGold   = 0xF1C40F; // bounty completed
        private const int ColorPurple = 0x9B59B6; // bounty placed

        // ═════════════════════════════════════════════
        //  AUCTION EVENTS
        // ═════════════════════════════════════════════

        public static void AuctionListed(string itemName, ushort itemId, decimal price, string sellerName, int expiryHours)
        {
            var cfg = BountyPlugin.Instance?.Configuration?.Instance?.Discord;
            if (cfg == null || !cfg.Enabled || !cfg.NotifyAuctionListed || string.IsNullOrEmpty(cfg.WebhookUrl)) return;

            var embed = new
            {
                embeds = new[]
                {
                    new
                    {
                        title = "\U0001f4e6  New Auction Listing",
                        color = ColorGreen,
                        thumbnail = new { url = LogoUrl },
                        fields = new object[]
                        {
                            new { name = "Item",       value = $"**{itemName}** (ID: {itemId})", inline = true },
                            new { name = "Price",      value = $"**${price:N0}**",               inline = true },
                            new { name = "Seller",     value = sellerName,                        inline = true },
                            new { name = "Expires In", value = $"{expiryHours}h",                inline = true }
                        },
                        footer = new { text = FooterText, icon_url = LogoUrl },
                        timestamp = DateTime.UtcNow.ToString("o")
                    }
                }
            };

            SendAsync(cfg.WebhookUrl, embed);
        }

        public static void AuctionSold(string itemName, ushort itemId, decimal price, string sellerName, string buyerName)
        {
            var cfg = BountyPlugin.Instance?.Configuration?.Instance?.Discord;
            if (cfg == null || !cfg.Enabled || !cfg.NotifyAuctionSold || string.IsNullOrEmpty(cfg.WebhookUrl)) return;

            var embed = new
            {
                embeds = new[]
                {
                    new
                    {
                        title = "\U0001f4b0  Auction Sold!",
                        color = ColorBlue,
                        thumbnail = new { url = LogoUrl },
                        fields = new object[]
                        {
                            new { name = "Item",   value = $"**{itemName}** (ID: {itemId})", inline = true },
                            new { name = "Price",  value = $"**${price:N0}**",               inline = true },
                            new { name = "\u200b", value = "\u200b",                          inline = true },
                            new { name = "Seller", value = sellerName,                        inline = true },
                            new { name = "Buyer",  value = buyerName,                         inline = true }
                        },
                        footer = new { text = FooterText, icon_url = LogoUrl },
                        timestamp = DateTime.UtcNow.ToString("o")
                    }
                }
            };

            SendAsync(cfg.WebhookUrl, embed);
        }

        public static void AuctionCancelled(string itemName, ushort itemId, decimal price, string sellerName)
        {
            var cfg = BountyPlugin.Instance?.Configuration?.Instance?.Discord;
            if (cfg == null || !cfg.Enabled || !cfg.NotifyAuctionCancelled || string.IsNullOrEmpty(cfg.WebhookUrl)) return;

            var embed = new
            {
                embeds = new[]
                {
                    new
                    {
                        title = "\u274c  Auction Cancelled",
                        color = ColorRed,
                        thumbnail = new { url = LogoUrl },
                        fields = new object[]
                        {
                            new { name = "Item",   value = $"**{itemName}** (ID: {itemId})", inline = true },
                            new { name = "Price",  value = $"~~${price:N0}~~",               inline = true },
                            new { name = "Seller", value = sellerName,                        inline = true }
                        },
                        footer = new { text = FooterText, icon_url = LogoUrl },
                        timestamp = DateTime.UtcNow.ToString("o")
                    }
                }
            };

            SendAsync(cfg.WebhookUrl, embed);
        }

        // ═════════════════════════════════════════════
        //  BOUNTY EVENTS
        // ═════════════════════════════════════════════

        public static void BountyPlaced(string targetName, decimal amount, string placerName, decimal totalBounty, string tier)
        {
            var cfg = BountyPlugin.Instance?.Configuration?.Instance?.Discord;
            if (cfg == null || !cfg.Enabled || !cfg.NotifyBountyPlaced || string.IsNullOrEmpty(cfg.WebhookUrl)) return;

            var embed = new
            {
                embeds = new[]
                {
                    new
                    {
                        title = "\U0001f3af  New Bounty Placed!",
                        color = ColorPurple,
                        thumbnail = new { url = LogoUrl },
                        fields = new object[]
                        {
                            new { name = "Target",       value = $"**{targetName}**",      inline = true },
                            new { name = "Amount",       value = $"**${amount:N0}**",      inline = true },
                            new { name = "Placed By",    value = placerName,                inline = true },
                            new { name = "Total Bounty", value = $"${totalBounty:N0} {tier}", inline = true }
                        },
                        footer = new { text = FooterText, icon_url = LogoUrl },
                        timestamp = DateTime.UtcNow.ToString("o")
                    }
                }
            };

            SendAsync(cfg.WebhookUrl, embed);
        }

        public static void BountyClaimed(string targetName, decimal reward, string hunterName, int streak, string tier)
        {
            var cfg = BountyPlugin.Instance?.Configuration?.Instance?.Discord;
            if (cfg == null || !cfg.Enabled || !cfg.NotifyBountyClaimed || string.IsNullOrEmpty(cfg.WebhookUrl)) return;

            string streakText = streak > 1 ? $"{streak}x Kill Streak!" : "—";

            var embed = new
            {
                embeds = new[]
                {
                    new
                    {
                        title = "\U0001f480  Bounty Claimed!",
                        color = ColorGold,
                        thumbnail = new { url = LogoUrl },
                        fields = new object[]
                        {
                            new { name = "Target",  value = $"**{targetName}** {tier}",  inline = true },
                            new { name = "Reward",  value = $"**${reward:N0}**",         inline = true },
                            new { name = "Hunter",  value = $"**{hunterName}**",         inline = true },
                            new { name = "Streak",  value = streakText,                   inline = true }
                        },
                        footer = new { text = FooterText, icon_url = LogoUrl },
                        timestamp = DateTime.UtcNow.ToString("o")
                    }
                }
            };

            SendAsync(cfg.WebhookUrl, embed);
        }

        // ═════════════════════════════════════════════
        //  HTTP SENDER (fire-and-forget)
        // ═════════════════════════════════════════════

        private static void SendAsync(string url, object payload)
        {
            string json = JsonConvert.SerializeObject(payload);

            ThreadPool.QueueUserWorkItem(_ =>
            {
                try
                {
                    var request = (HttpWebRequest)WebRequest.Create(url);
                    request.Method = "POST";
                    request.ContentType = "application/json";
                    request.Timeout = 10000;

                    byte[] bytes = Encoding.UTF8.GetBytes(json);
                    request.ContentLength = bytes.Length;

                    using (var stream = request.GetRequestStream())
                        stream.Write(bytes, 0, bytes.Length);

                    using (var response = (HttpWebResponse)request.GetResponse())
                    {
                        // Discord returns 204 No Content on success
                        if ((int)response.StatusCode >= 400)
                            Rocket.Core.Logging.Logger.LogWarning($"[{Msg.PluginName}] Discord webhook returned {response.StatusCode}");
                    }
                }
                catch (WebException wex)
                {
                    // Rate limited or bad URL — log but don't crash
                    Rocket.Core.Logging.Logger.LogWarning($"[{Msg.PluginName}] Discord webhook error: {wex.Message}");
                }
                catch (Exception ex)
                {
                    Rocket.Core.Logging.Logger.LogWarning($"[{Msg.PluginName}] Discord webhook error: {ex.Message}");
                }
            });
        }
    }
}
