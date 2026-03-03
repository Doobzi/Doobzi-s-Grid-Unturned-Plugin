using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Newtonsoft.Json;

namespace BountyPlugin
{
    public class BountyManager
    {
        private BountyData _bountyData;
        private HunterData _hunterData;
        private AchievementData _achievementData;
        private string _bountyFilePath;
        private string _hunterFilePath;
        private string _achievementFilePath;

        public void Load()
        {
            _bountyFilePath = Path.Combine(BountyPlugin.Instance.Directory, "bounties.json");
            _hunterFilePath = Path.Combine(BountyPlugin.Instance.Directory, "hunters.json");
            _achievementFilePath = Path.Combine(BountyPlugin.Instance.Directory, "achievements.json");

            var mysql = BountyPlugin.Instance.MySql;
            if (mysql != null && mysql.IsReady)
            {
                _bountyData = mysql.Load<BountyData>("bounties") ?? new BountyData();
                _hunterData = mysql.Load<HunterData>("hunters") ?? new HunterData();
                _achievementData = mysql.Load<AchievementData>("achievements") ?? new AchievementData();
            }
            else
            {
                _bountyData = LoadJson<BountyData>(_bountyFilePath) ?? new BountyData();
                _hunterData = LoadJson<HunterData>(_hunterFilePath) ?? new HunterData();
                _achievementData = LoadJson<AchievementData>(_achievementFilePath) ?? new AchievementData();
            }

            Rocket.Core.Logging.Logger.Log($"[Doobzi] Loaded {_bountyData.ActiveBounties.Count} bounties, {_hunterData.Hunters.Count} hunters, {_achievementData.Players.Count} achievement records.");
        }

        private T LoadJson<T>(string path) where T : class
        {
            if (!File.Exists(path)) return null;
            try
            {
                return JsonConvert.DeserializeObject<T>(File.ReadAllText(path));
            }
            catch (Exception ex)
            {
                Rocket.Core.Logging.Logger.LogError($"[Doobzi] Failed to load {Path.GetFileName(path)}: {ex.Message}");
                return null;
            }
        }

        private void SaveJson(string path, object data)
        {
            try { File.WriteAllText(path, JsonConvert.SerializeObject(data, Formatting.Indented)); }
            catch (Exception ex) { Rocket.Core.Logging.Logger.LogError($"[Doobzi] Failed to save {Path.GetFileName(path)}: {ex.Message}"); }
        }

        public void Save()
        {
            var mysql = BountyPlugin.Instance.MySql;
            if (mysql != null && mysql.IsReady)
            {
                mysql.Save("bounties", _bountyData);
                mysql.Save("hunters", _hunterData);
                mysql.Save("achievements", _achievementData);
                return;
            }
            SaveJson(_bountyFilePath, _bountyData);
            SaveJson(_hunterFilePath, _hunterData);
            SaveJson(_achievementFilePath, _achievementData);
        }

        public void SaveBounties()
        {
            var mysql = BountyPlugin.Instance.MySql;
            if (mysql != null && mysql.IsReady) { mysql.Save("bounties", _bountyData); return; }
            SaveJson(_bountyFilePath, _bountyData);
        }

        public void SaveHunters()
        {
            var mysql = BountyPlugin.Instance.MySql;
            if (mysql != null && mysql.IsReady) { mysql.Save("hunters", _hunterData); return; }
            SaveJson(_hunterFilePath, _hunterData);
        }

        public void SaveAchievements()
        {
            var mysql = BountyPlugin.Instance.MySql;
            if (mysql != null && mysql.IsReady) { mysql.Save("achievements", _achievementData); return; }
            SaveJson(_achievementFilePath, _achievementData);
        }

        // ==================== BOUNTY OPERATIONS ====================

        public Bounty AddBounty(string targetSteamId, string targetName,
            string issuerSteamId, string issuerName, decimal amount, bool anonymous = false)
        {
            int expiryHours = BountyPlugin.Instance.Configuration.Instance.Bounties.ExpiryHours;
            var existing = _bountyData.ActiveBounties.FirstOrDefault(b => b.TargetSteamId == targetSteamId);

            if (existing != null)
            {
                existing.TotalAmount += amount;
                existing.TargetName = targetName;
                // Extend expiry from now
                existing.ExpiresAt = DateTime.UtcNow.AddHours(expiryHours);
                existing.Contributions.Add(new BountyContribution
                {
                    IssuerSteamId = issuerSteamId,
                    IssuerName = issuerName,
                    Amount = amount,
                    Anonymous = anonymous,
                    AddedAt = DateTime.UtcNow
                });
                SaveBounties();
                return existing;
            }

            var bounty = new Bounty
            {
                Id = _bountyData.NextId++,
                TargetSteamId = targetSteamId,
                TargetName = targetName,
                TotalAmount = amount,
                CreatedAt = DateTime.UtcNow,
                ExpiresAt = DateTime.UtcNow.AddHours(expiryHours),
                Contributions = new List<BountyContribution>
                {
                    new BountyContribution
                    {
                        IssuerSteamId = issuerSteamId,
                        IssuerName = issuerName,
                        Amount = amount,
                        Anonymous = anonymous,
                        AddedAt = DateTime.UtcNow
                    }
                }
            };
            _bountyData.ActiveBounties.Add(bounty);
            SaveBounties();
            return bounty;
        }

        public Bounty GetBounty(string targetSteamId)
        {
            return _bountyData.ActiveBounties.FirstOrDefault(b => b.TargetSteamId == targetSteamId);
        }

        public List<Bounty> GetActiveBounties()
        {
            return _bountyData.ActiveBounties.OrderByDescending(b => b.TotalAmount).ToList();
        }

        public Bounty GetHighestBounty()
        {
            return _bountyData.ActiveBounties.OrderByDescending(b => b.TotalAmount).FirstOrDefault();
        }

        public decimal CompleteBounty(string targetSteamId)
        {
            var bounty = _bountyData.ActiveBounties.FirstOrDefault(b => b.TargetSteamId == targetSteamId);
            if (bounty == null) return 0;
            decimal amount = bounty.TotalAmount;
            _bountyData.ActiveBounties.Remove(bounty);
            SaveBounties();
            return amount;
        }

        public void ClearBounty(string targetSteamId)
        {
            _bountyData.ActiveBounties.RemoveAll(b => b.TargetSteamId == targetSteamId);
            SaveBounties();
        }

        /// <summary>
        /// Removes expired bounties and returns refund info: list of (issuerSteamId, refundAmount).
        /// </summary>
        public List<KeyValuePair<string, decimal>> ProcessExpiredBounties()
        {
            var refunds = new List<KeyValuePair<string, decimal>>();
            var now = DateTime.UtcNow;
            var expired = _bountyData.ActiveBounties.Where(b => b.ExpiresAt <= now).ToList();

            foreach (var bounty in expired)
            {
                foreach (var c in bounty.Contributions)
                {
                    refunds.Add(new KeyValuePair<string, decimal>(c.IssuerSteamId, c.Amount));
                }
                _bountyData.ActiveBounties.Remove(bounty);
            }

            if (expired.Count > 0)
            {
                SaveBounties();
                Rocket.Core.Logging.Logger.Log($"[Doobzi] Expired {expired.Count} bounties, processing {refunds.Count} refunds.");
            }

            return refunds;
        }

        // ==================== HUNTER STATS ====================

        public void RecordHunterKill(string hunterSteamId, string hunterName, decimal earned)
        {
            EnsureHunter(hunterSteamId, hunterName);
            var hunter = _hunterData.Hunters[hunterSteamId];
            hunter.PlayerName = hunterName;
            hunter.BountiesCompleted++;
            hunter.TotalEarned += earned;
            hunter.CurrentStreak++;
            if (hunter.CurrentStreak > hunter.BestStreak)
                hunter.BestStreak = hunter.CurrentStreak;
            hunter.WeeklyBounties++;
            hunter.WeeklyEarned += earned;

            // Update title
            hunter.Title = GetTitle(hunter.BountiesCompleted);
            SaveHunters();
        }

        public void ResetStreak(string steamId)
        {
            if (_hunterData.Hunters.ContainsKey(steamId))
            {
                _hunterData.Hunters[steamId].CurrentStreak = 0;
                SaveHunters();
            }
        }

        public int GetStreak(string steamId)
        {
            if (_hunterData.Hunters.TryGetValue(steamId, out var h))
                return h.CurrentStreak;
            return 0;
        }

        public decimal GetStreakBonus(string steamId)
        {
            int streak = GetStreak(steamId);
            if (streak <= 0) return 0;
            return streak * (BountyPlugin.Instance.Configuration.Instance.Bounties.StreakBonusPercent / 100m);
        }

        private void EnsureHunter(string steamId, string name)
        {
            if (!_hunterData.Hunters.ContainsKey(steamId))
            {
                _hunterData.Hunters[steamId] = new HunterStats
                {
                    SteamId = steamId,
                    PlayerName = name,
                    WeekStart = GetWeekStart()
                };
            }
            // Reset weekly if needed
            var h = _hunterData.Hunters[steamId];
            if (h.WeekStart < GetWeekStart())
            {
                h.WeeklyBounties = 0;
                h.WeeklyEarned = 0;
                h.WeekStart = GetWeekStart();
            }
        }

        private static DateTime GetWeekStart()
        {
            var now = DateTime.UtcNow;
            int diff = (7 + (now.DayOfWeek - DayOfWeek.Monday)) % 7;
            return now.AddDays(-diff).Date;
        }

        public static string GetTitle(int completions)
        {
            if (completions >= 100) return "Bounty God";
            if (completions >= 50) return "Bounty King";
            if (completions >= 25) return "Elite Hunter";
            if (completions >= 10) return "Veteran Hunter";
            if (completions >= 5) return "Skilled Hunter";
            if (completions >= 1) return "Hunter";
            return "Novice";
        }

        public List<HunterStats> GetTopHunters(int count = 10)
        {
            return _hunterData.Hunters.Values
                .OrderByDescending(h => h.BountiesCompleted)
                .ThenByDescending(h => h.TotalEarned)
                .Take(count).ToList();
        }

        public List<HunterStats> GetWeeklyTopHunters(int count = 10)
        {
            var weekStart = GetWeekStart();
            return _hunterData.Hunters.Values
                .Where(h => h.WeekStart >= weekStart)
                .OrderByDescending(h => h.WeeklyBounties)
                .ThenByDescending(h => h.WeeklyEarned)
                .Take(count).ToList();
        }

        public int GetHunterRank(string steamId)
        {
            var ranked = _hunterData.Hunters.Values
                .OrderByDescending(h => h.BountiesCompleted)
                .ThenByDescending(h => h.TotalEarned).ToList();
            for (int i = 0; i < ranked.Count; i++)
                if (ranked[i].SteamId == steamId) return i + 1;
            return -1;
        }

        public HunterStats GetHunterStats(string steamId)
        {
            _hunterData.Hunters.TryGetValue(steamId, out var stats);
            return stats;
        }

        // ==================== ACHIEVEMENTS ====================

        public PlayerAchievements EnsureAchievements(string steamId, string name)
        {
            if (!_achievementData.Players.ContainsKey(steamId))
            {
                _achievementData.Players[steamId] = new PlayerAchievements
                {
                    SteamId = steamId,
                    PlayerName = name
                };
            }
            _achievementData.Players[steamId].PlayerName = name;
            return _achievementData.Players[steamId];
        }

        /// <summary>
        /// Tries to unlock an achievement. Returns true if newly unlocked.
        /// </summary>
        public bool TryUnlockAchievement(string steamId, string name, string achievementId)
        {
            var pa = EnsureAchievements(steamId, name);
            if (pa.Unlocked.Contains(achievementId)) return false;
            pa.Unlocked.Add(achievementId);
            SaveAchievements();
            return true;
        }

        public PlayerAchievements GetAchievements(string steamId)
        {
            _achievementData.Players.TryGetValue(steamId, out var pa);
            return pa;
        }

        public void TrackBountyPlaced(string steamId, string name, decimal amount)
        {
            var pa = EnsureAchievements(steamId, name);
            pa.TotalBountiesPlaced += amount;
            SaveAchievements();
        }

        public void TrackMoneySpent(string steamId, string name, decimal amount)
        {
            var pa = EnsureAchievements(steamId, name);
            pa.TotalMoneySpent += amount;
            SaveAchievements();

            // Shopaholic achievement (use config threshold)
            if (AchievementDefs.IsEnabled && AchievementDefs.IsAchievementEnabled(AchievementDefs.Shopaholic)
                && pa.TotalMoneySpent >= AchievementDefs.GetThreshold(AchievementDefs.Shopaholic))
                TryUnlockAchievement(steamId, name, AchievementDefs.Shopaholic);
        }

        /// <summary>
        /// Checks achievements for bounty placers - returns newly earned.
        /// </summary>
        public List<string> CheckPlacerAchievements(string steamId, string name)
        {
            var earned = new List<string>();
            if (!AchievementDefs.IsEnabled) return earned;

            var pa = EnsureAchievements(steamId, name);

            if (AchievementDefs.IsAchievementEnabled(AchievementDefs.BigSpender)
                && pa.TotalBountiesPlaced >= AchievementDefs.GetThreshold(AchievementDefs.BigSpender)
                && TryUnlockAchievement(steamId, name, AchievementDefs.BigSpender))
                earned.Add(AchievementDefs.BigSpender);

            return earned;
        }

        /// <summary>
        /// Checks and returns any newly earned achievements after a bounty kill.
        /// </summary>
        public List<string> CheckHunterAchievements(string steamId, string name, decimal bountyAmount)
        {
            var earned = new List<string>();
            if (!AchievementDefs.IsEnabled) return earned;

            var stats = GetHunterStats(steamId);
            if (stats == null) return earned;

            // Bounty completion milestones
            TryCheckAchievement(earned, steamId, name, AchievementDefs.FirstBlood, stats.BountiesCompleted);
            TryCheckAchievement(earned, steamId, name, AchievementDefs.FiveBounties, stats.BountiesCompleted);
            TryCheckAchievement(earned, steamId, name, AchievementDefs.TenBounties, stats.BountiesCompleted);
            TryCheckAchievement(earned, steamId, name, AchievementDefs.TwentyFive, stats.BountiesCompleted);
            TryCheckAchievement(earned, steamId, name, AchievementDefs.FiftyBounties, stats.BountiesCompleted);

            // Streak milestones
            TryCheckAchievement(earned, steamId, name, AchievementDefs.Streak3, stats.CurrentStreak);
            TryCheckAchievement(earned, steamId, name, AchievementDefs.Streak5, stats.CurrentStreak);
            TryCheckAchievement(earned, steamId, name, AchievementDefs.Streak10, stats.CurrentStreak);

            // Legendary hunter (bounty amount based)
            TryCheckAchievement(earned, steamId, name, AchievementDefs.LegendaryHunter, bountyAmount);

            // BigSpender check
            var pa = EnsureAchievements(steamId, name);
            TryCheckAchievement(earned, steamId, name, AchievementDefs.BigSpender, pa.TotalBountiesPlaced);

            return earned;
        }

        /// <summary>
        /// Helper: checks if achievementId is enabled and the value meets the config threshold.
        /// </summary>
        private void TryCheckAchievement(List<string> earned, string steamId, string name, string achievementId, decimal currentValue)
        {
            if (!AchievementDefs.IsAchievementEnabled(achievementId)) return;
            if (currentValue >= AchievementDefs.GetThreshold(achievementId)
                && TryUnlockAchievement(steamId, name, achievementId))
                earned.Add(achievementId);
        }
    }
}
