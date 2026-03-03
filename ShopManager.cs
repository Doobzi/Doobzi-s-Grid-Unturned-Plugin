using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Newtonsoft.Json;
using SDG.Unturned;

namespace BountyPlugin
{
    public class ShopManager
    {
        private ShopData _data;
        private string _filePath;

        public void Load()
        {
            _filePath = Path.Combine(BountyPlugin.Instance.Directory, "shop.json");
            var mysql = BountyPlugin.Instance.MySql;

            bool loaded = false;
            if (mysql != null && mysql.IsReady)
            {
                _data = mysql.Load<ShopData>("shop");
                if (_data != null)
                {
                    loaded = true;
                    Rocket.Core.Logging.Logger.Log($"[Doobzi] Loaded {_data.Items.Count} shop items from MySQL.");
                    AddMissingItems();
                }
            }

            if (!loaded && File.Exists(_filePath))
            {
                try
                {
                    string json = File.ReadAllText(_filePath);
                    _data = JsonConvert.DeserializeObject<ShopData>(json) ?? new ShopData();
                    Rocket.Core.Logging.Logger.Log($"[Doobzi] Loaded {_data.Items.Count} shop items.");
                    loaded = true;
                }
                catch (Exception ex)
                {
                    Rocket.Core.Logging.Logger.LogError($"[Doobzi] Failed to load shop: {ex.Message}");
                    _data = new ShopData();
                }
                AddMissingItems();
            }

            if (!loaded)
            {
                _data = new ShopData();
                GenerateDefaultShop();
            }
        }

        public void Save()
        {
            try
            {
                var mysql = BountyPlugin.Instance.MySql;
                if (mysql != null && mysql.IsReady)
                {
                    mysql.Save("shop", _data);
                    return;
                }

                File.WriteAllText(_filePath, JsonConvert.SerializeObject(_data, Formatting.Indented));
            }
            catch (Exception ex)
            {
                Rocket.Core.Logging.Logger.LogError($"[Doobzi] Failed to save shop: {ex.Message}");
            }
        }

        private void GenerateDefaultShop()
        {
            Rocket.Core.Logging.Logger.Log("[Doobzi] Generating shop with all Unturned items...");
            int defaultStock = BountyPlugin.Instance.Configuration.Instance.Shop.DefaultStockPerItem;
            int count = 0;

            List<ItemAsset> allItems = new List<ItemAsset>();
            Assets.find(allItems);

            foreach (var asset in allItems)
            {
                if (asset == null || asset.id == 0 || string.IsNullOrEmpty(asset.itemName)) continue;

                _data.Items[asset.id] = new ShopItem
                {
                    ItemId = asset.id,
                    ItemName = asset.itemName,
                    Category = GetCategory(asset.type),
                    Price = CalculatePrice(asset),
                    DefaultStock = defaultStock,
                    CurrentStock = defaultStock,
                    IsEnabled = true
                };
                count++;
            }

            _data.LastRefresh = DateTime.UtcNow;
            Save();
            Rocket.Core.Logging.Logger.Log($"[Doobzi] Generated shop with {count} items.");
        }

        private void AddMissingItems()
        {
            int defaultStock = BountyPlugin.Instance.Configuration.Instance.Shop.DefaultStockPerItem;
            int added = 0;

            List<ItemAsset> allItems = new List<ItemAsset>();
            Assets.find(allItems);

            foreach (var asset in allItems)
            {
                if (asset == null || asset.id == 0 || string.IsNullOrEmpty(asset.itemName)) continue;
                if (_data.Items.ContainsKey(asset.id)) continue;

                _data.Items[asset.id] = new ShopItem
                {
                    ItemId = asset.id,
                    ItemName = asset.itemName,
                    Category = GetCategory(asset.type),
                    Price = CalculatePrice(asset),
                    DefaultStock = defaultStock,
                    CurrentStock = defaultStock,
                    IsEnabled = true
                };
                added++;
            }

            if (added > 0)
            {
                Save();
                Rocket.Core.Logging.Logger.Log($"[Doobzi] Added {added} new items to shop.");
            }
        }

        // ==================== CATEGORIES ====================

        public static string GetCategory(EItemType type)
        {
            switch (type)
            {
                case EItemType.GUN: return "guns";
                case EItemType.MELEE: return "melee";
                case EItemType.THROWABLE: return "throwables";
                case EItemType.TRAP: return "traps";
                case EItemType.MEDICAL: return "medical";
                case EItemType.FOOD: return "food";
                case EItemType.WATER: return "food";
                case EItemType.FUEL: return "fuel";
                case EItemType.REFILL: return "fuel";
                case EItemType.HAT:
                case EItemType.SHIRT:
                case EItemType.PANTS:
                case EItemType.VEST:
                case EItemType.MASK:
                case EItemType.GLASSES:
                case EItemType.BACKPACK: return "clothing";
                case EItemType.MAGAZINE: return "attachments";
                case EItemType.SIGHT: return "attachments";
                case EItemType.OPTIC: return "attachments";
                case EItemType.TACTICAL: return "attachments";
                case EItemType.GRIP: return "attachments";
                case EItemType.BARREL: return "attachments";
                case EItemType.BARRICADE:
                case EItemType.STORAGE:
                case EItemType.BEACON:
                case EItemType.GENERATOR:
                case EItemType.SENTRY:
                case EItemType.TANK:
                case EItemType.OIL_PUMP:
                case EItemType.LIBRARY:
                case EItemType.CHARGE:
                case EItemType.DETONATOR: return "building";
                case EItemType.FARM:
                case EItemType.GROWER:
                case EItemType.FISHER: return "farming";
                case EItemType.TOOL:
                case EItemType.VEHICLE_REPAIR_TOOL:
                case EItemType.TIRE: return "tools";
                case EItemType.SUPPLY:
                case EItemType.FILTER: return "supplies";
                default: return "misc";
            }
        }

        public static readonly string[] ValidCategories = {
            "guns", "melee", "throwables", "traps", "medical", "food", "fuel",
            "clothing", "attachments", "building", "farming", "tools", "supplies", "misc"
        };

        // ==================== PRICING ====================

        public decimal CalculatePrice(ItemAsset asset)
        {
            return Math.Round(GetBasePrice(asset.type) * GetRarityMultiplier(asset.rarity), 0);
        }

        private decimal GetBasePrice(EItemType type)
        {
            switch (type)
            {
                case EItemType.GUN: return 750;
                case EItemType.MELEE: return 200;
                case EItemType.THROWABLE: return 300;
                case EItemType.TRAP: return 250;
                case EItemType.MEDICAL: return 75;
                case EItemType.FOOD: return 30;
                case EItemType.WATER: return 30;
                case EItemType.FUEL: return 50;
                case EItemType.REFILL: return 40;
                case EItemType.SUPPLY: return 60;
                case EItemType.FILTER: return 50;
                case EItemType.HAT: return 100;
                case EItemType.SHIRT: return 150;
                case EItemType.PANTS: return 150;
                case EItemType.VEST: return 200;
                case EItemType.MASK: return 100;
                case EItemType.GLASSES: return 80;
                case EItemType.BACKPACK: return 250;
                case EItemType.MAGAZINE: return 25;
                case EItemType.SIGHT: return 150;
                case EItemType.OPTIC: return 120;
                case EItemType.TACTICAL: return 100;
                case EItemType.GRIP: return 100;
                case EItemType.BARREL: return 150;
                case EItemType.BARRICADE: return 75;
                case EItemType.STORAGE: return 150;
                case EItemType.BEACON: return 200;
                case EItemType.GENERATOR: return 300;
                case EItemType.SENTRY: return 500;
                case EItemType.TANK: return 200;
                case EItemType.OIL_PUMP: return 350;
                case EItemType.LIBRARY: return 150;
                case EItemType.CHARGE: return 200;
                case EItemType.DETONATOR: return 100;
                case EItemType.FARM: return 20;
                case EItemType.GROWER: return 30;
                case EItemType.FISHER: return 50;
                case EItemType.TOOL: return 100;
                case EItemType.VEHICLE_REPAIR_TOOL: return 200;
                case EItemType.TIRE: return 100;
                case EItemType.KEY: return 50;
                case EItemType.BOX: return 100;
                case EItemType.CLOUD: return 50;
                case EItemType.MAP: return 50;
                case EItemType.COMPASS: return 75;
                case EItemType.ARREST_START: return 75;
                case EItemType.ARREST_END: return 25;
                default: return 50;
            }
        }

        private decimal GetRarityMultiplier(EItemRarity rarity)
        {
            switch (rarity)
            {
                case EItemRarity.COMMON: return 1.0m;
                case EItemRarity.UNCOMMON: return 1.5m;
                case EItemRarity.RARE: return 2.5m;
                case EItemRarity.EPIC: return 4.0m;
                case EItemRarity.LEGENDARY: return 7.0m;
                case EItemRarity.MYTHICAL: return 12.0m;
                default: return 1.0m;
            }
        }

        // ==================== SHOP OPERATIONS ====================

        public List<ShopItem> GetItems()
        {
            return _data.Items.Values.Where(i => i.IsEnabled).OrderBy(i => i.ItemName).ToList();
        }

        public ShopItem GetItem(ushort id)
        {
            if (_data.Items.TryGetValue(id, out var item) && item.IsEnabled)
                return item;
            return null;
        }

        public decimal GetSellPrice(ushort id)
        {
            var item = GetItem(id);
            if (item == null) return 0;
            return Math.Round(item.Price * (BountyPlugin.Instance.Configuration.Instance.Shop.SellPricePercent / 100m), 0);
        }

        public List<ShopItem> SearchItems(string query)
        {
            if (ushort.TryParse(query, out ushort id))
            {
                var item = GetItem(id);
                if (item != null) return new List<ShopItem> { item };
            }
            return _data.Items.Values
                .Where(i => i.IsEnabled && i.ItemName.IndexOf(query, StringComparison.OrdinalIgnoreCase) >= 0)
                .OrderBy(i => i.ItemName).Take(10).ToList();
        }

        public List<ShopItem> GetItemsByCategory(string category, int take = 100)
        {
            return _data.Items.Values
                .Where(i => i.IsEnabled && string.Equals(i.Category, category, StringComparison.OrdinalIgnoreCase))
                .OrderBy(i => i.Price).Take(take).ToList();
        }

        public bool BuyItem(ushort id, out ShopItem item, out string error)
        {
            item = null; error = null;
            if (!_data.Items.TryGetValue(id, out item)) { error = "Item not found in shop!"; return false; }
            if (!item.IsEnabled) { error = "This item has been removed from the shop!"; item = null; return false; }
            if (item.CurrentStock <= 0) { error = $"{item.ItemName} is out of stock! Stock refreshes hourly."; return false; }
            item.CurrentStock--;
            Save();
            return true;
        }

        public void RemoveItem(ushort id)
        {
            if (_data.Items.ContainsKey(id)) { _data.Items[id].IsEnabled = false; Save(); }
        }

        public void AddItem(ushort id, decimal price)
        {
            int ds = BountyPlugin.Instance.Configuration.Instance.Shop.DefaultStockPerItem;
            if (_data.Items.ContainsKey(id))
            {
                _data.Items[id].IsEnabled = true;
                _data.Items[id].Price = price;
                Save(); return;
            }
            ItemAsset asset = Assets.find(EAssetType.ITEM, id) as ItemAsset;
            string name = asset?.itemName ?? $"Item #{id}";
            string cat = asset != null ? GetCategory(asset.type) : "misc";
            _data.Items[id] = new ShopItem { ItemId = id, ItemName = name, Category = cat, Price = price, DefaultStock = ds, CurrentStock = ds, IsEnabled = true };
            Save();
        }

        public void AddItem(ushort id, string name, decimal price, int stock)
        {
            if (_data.Items.ContainsKey(id))
            {
                _data.Items[id].IsEnabled = true;
                _data.Items[id].Price = price;
                _data.Items[id].ItemName = name;
                _data.Items[id].DefaultStock = stock;
                _data.Items[id].CurrentStock = stock;
                Save(); return;
            }
            ItemAsset asset = Assets.find(EAssetType.ITEM, id) as ItemAsset;
            string cat = asset != null ? GetCategory(asset.type) : "misc";
            _data.Items[id] = new ShopItem { ItemId = id, ItemName = name, Category = cat, Price = price, DefaultStock = stock, CurrentStock = stock, IsEnabled = true };
            Save();
        }

        public void DeductStock(ushort id, int amount)
        {
            if (_data.Items.ContainsKey(id) && _data.Items[id].CurrentStock >= 0)
            {
                _data.Items[id].CurrentStock = Math.Max(0, _data.Items[id].CurrentStock - amount);
                Save();
            }
        }

        public string GetItemCategory(ushort itemId)
        {
            if (_data.Items.TryGetValue(itemId, out var item))
                return item.Category ?? "misc";
            return "misc";
        }

        public void EditItem(ushort id, decimal price, int stock)
        {
            if (_data.Items.ContainsKey(id))
            {
                _data.Items[id].Price = price;
                _data.Items[id].DefaultStock = stock;
                _data.Items[id].CurrentStock = stock;
                Save();
            }
        }

        public void RefreshStock()
        {
            foreach (var item in _data.Items.Values)
                item.CurrentStock = item.DefaultStock;
            _data.LastRefresh = DateTime.UtcNow;
            Save();
            Rocket.Core.Logging.Logger.Log("[Doobzi] Shop stock refreshed!");
        }
    }
}
