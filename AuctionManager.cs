using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Newtonsoft.Json;

namespace BountyPlugin
{
    public class AuctionManager
    {
        private AuctionData _data;
        private string _filePath;

        public void Load()
        {
            _filePath = Path.Combine(BountyPlugin.Instance.Directory, "auctions.json");
            var mysql = BountyPlugin.Instance.MySql;

            if (mysql != null && mysql.IsReady)
            {
                _data = mysql.Load<AuctionData>("auctions") ?? new AuctionData();
            }
            else if (File.Exists(_filePath))
            {
                try
                {
                    _data = JsonConvert.DeserializeObject<AuctionData>(File.ReadAllText(_filePath)) ?? new AuctionData();
                }
                catch (Exception ex)
                {
                    Rocket.Core.Logging.Logger.LogError($"[Doobzi] Failed to load auctions: {ex.Message}");
                    _data = new AuctionData();
                }
            }
            else
            {
                _data = new AuctionData();
            }
            Rocket.Core.Logging.Logger.Log($"[Doobzi] Loaded {_data.Listings.Count} auction listings.");
        }

        public void Save()
        {
            try
            {
                var mysql = BountyPlugin.Instance.MySql;
                if (mysql != null && mysql.IsReady)
                {
                    mysql.Save("auctions", _data);
                    return;
                }

                File.WriteAllText(_filePath, JsonConvert.SerializeObject(_data, Formatting.Indented));
            }
            catch (Exception ex)
            {
                Rocket.Core.Logging.Logger.LogError($"[Doobzi] Failed to save auctions: {ex.Message}");
            }
        }

        public AuctionListing CreateListing(string sellerSteamId, string sellerName, ushort itemId, string itemName, int amount, decimal price, int expiryHours = 24)
        {
            var listing = new AuctionListing
            {
                Id = _data.NextId++,
                SellerSteamId = sellerSteamId,
                SellerName = sellerName,
                ItemId = itemId,
                ItemName = itemName,
                Amount = amount,
                Price = price,
                Active = true,
                ListedAt = DateTime.UtcNow,
                ExpiresAt = DateTime.UtcNow.AddHours(expiryHours)
            };
            _data.Listings.Add(listing);
            Save();
            return listing;
        }

        public AuctionListing GetListing(int id)
        {
            return _data.Listings.FirstOrDefault(l => l.Id == id);
        }

        public List<AuctionListing> GetActiveListings(int take = 50)
        {
            return _data.Listings
                .Where(l => l.Active && l.ExpiresAt > DateTime.UtcNow)
                .OrderByDescending(l => l.ListedAt)
                .Take(take).ToList();
        }

        public List<AuctionListing> SearchListings(string query, int take = 10)
        {
            return _data.Listings
                .Where(l => l.Active && l.ExpiresAt > DateTime.UtcNow &&
                    l.ItemName.IndexOf(query, StringComparison.OrdinalIgnoreCase) >= 0)
                .OrderBy(l => l.Price)
                .Take(take).ToList();
        }

        public int GetPlayerListingCount(string steamId)
        {
            return _data.Listings.Count(l => l.SellerSteamId == steamId && l.Active && l.ExpiresAt > DateTime.UtcNow);
        }

        public void RemoveListing(int id)
        {
            var listing = _data.Listings.FirstOrDefault(l => l.Id == id);
            if (listing != null)
            {
                listing.Active = false;
                Save();
            }
        }

        public bool RemoveListing(int id, out AuctionListing removed)
        {
            removed = _data.Listings.FirstOrDefault(l => l.Id == id);
            if (removed == null) return false;
            removed.Active = false;
            Save();
            return true;
        }

        /// <summary>
        /// Removes expired listings and returns seller+itemId pairs for refunding items.
        /// </summary>
        public List<AuctionListing> ProcessExpiredListings()
        {
            var now = DateTime.UtcNow;
            var expired = _data.Listings.Where(l => l.Active && l.ExpiresAt <= now).ToList();
            foreach (var l in expired)
                l.Active = false;
            if (expired.Count > 0)
            {
                Save();
                Rocket.Core.Logging.Logger.Log($"[Doobzi] Expired {expired.Count} auction listings.");
            }
            return expired;
        }
    }
}
