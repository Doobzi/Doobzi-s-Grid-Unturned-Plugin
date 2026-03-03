using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Newtonsoft.Json;

namespace BountyPlugin
{
    public class EconomyManager
    {
        private EconomyData _data;
        private string _filePath;

        public void Load()
        {
            _filePath = Path.Combine(BountyPlugin.Instance.Directory, "economy.json");
            var mysql = BountyPlugin.Instance.MySql;

            if (mysql != null && mysql.IsReady)
            {
                _data = mysql.Load<EconomyData>("economy") ?? new EconomyData();
            }
            else if (File.Exists(_filePath))
            {
                try
                {
                    string json = File.ReadAllText(_filePath);
                    _data = JsonConvert.DeserializeObject<EconomyData>(json) ?? new EconomyData();
                }
                catch (Exception ex)
                {
                    Rocket.Core.Logging.Logger.LogError($"[Doobzi] Failed to load economy: {ex.Message}");
                    _data = new EconomyData();
                }
            }
            else
            {
                _data = new EconomyData();
                Save();
            }

            Rocket.Core.Logging.Logger.Log($"[Doobzi] Loaded {_data.Accounts.Count} economy accounts.");
        }

        public void Save()
        {
            try
            {
                var mysql = BountyPlugin.Instance.MySql;
                if (mysql != null && mysql.IsReady)
                {
                    mysql.Save("economy", _data);
                    return;
                }

                string json = JsonConvert.SerializeObject(_data, Formatting.Indented);
                File.WriteAllText(_filePath, json);
            }
            catch (Exception ex)
            {
                Rocket.Core.Logging.Logger.LogError($"[Doobzi] Failed to save economy: {ex.Message}");
            }
        }

        // ==================== ACCOUNT MANAGEMENT ====================

        public void EnsureAccount(string steamId, string playerName)
        {
            if (!_data.Accounts.ContainsKey(steamId))
            {
                _data.Accounts[steamId] = new PlayerAccount
                {
                    SteamId = steamId,
                    PlayerName = playerName,
                    Balance = BountyPlugin.Instance.Configuration.Instance.Economy.StartingBalance,
                    LastUpdated = DateTime.UtcNow,
                    LastDailyBonus = DateTime.MinValue,
                    LastInterest = DateTime.UtcNow,
                    LastSalary = DateTime.UtcNow
                };
                Save();
                return;
            }
            _data.Accounts[steamId].PlayerName = playerName;
        }

        public bool IsNewAccount(string steamId) => !_data.Accounts.ContainsKey(steamId);

        public decimal GetBalance(string steamId)
        {
            if (_data.Accounts.TryGetValue(steamId, out var account))
                return account.Balance;
            return 0;
        }

        public bool HasBalance(string steamId, decimal amount) => GetBalance(steamId) >= amount;

        public PlayerAccount GetAccount(string steamId)
        {
            _data.Accounts.TryGetValue(steamId, out var account);
            return account;
        }

        // ==================== BALANCE OPS WITH TRANSACTION LOG ====================

        public bool RemoveBalance(string steamId, decimal amount, string type = "DEBIT", string description = "")
        {
            if (!_data.Accounts.ContainsKey(steamId)) return false;
            if (_data.Accounts[steamId].Balance < amount) return false;

            var account = _data.Accounts[steamId];
            account.Balance -= amount;
            account.LastUpdated = DateTime.UtcNow;
            AddTransaction(account, type, description, -amount);
            Save();
            return true;
        }

        public void AddBalance(string steamId, decimal amount, string type = "CREDIT", string description = "")
        {
            if (!_data.Accounts.ContainsKey(steamId))
            {
                _data.Accounts[steamId] = new PlayerAccount
                {
                    SteamId = steamId,
                    PlayerName = "Unknown",
                    Balance = 0,
                    LastUpdated = DateTime.UtcNow,
                    LastDailyBonus = DateTime.MinValue,
                    LastInterest = DateTime.UtcNow,
                    LastSalary = DateTime.UtcNow
                };
            }

            var account = _data.Accounts[steamId];
            account.Balance += amount;
            account.LastUpdated = DateTime.UtcNow;
            AddTransaction(account, type, description, amount);
            Save();
        }

        public void SetBalance(string steamId, decimal amount)
        {
            if (_data.Accounts.ContainsKey(steamId))
            {
                _data.Accounts[steamId].Balance = amount;
                _data.Accounts[steamId].LastUpdated = DateTime.UtcNow;
                Save();
            }
        }

        public void ResetAccount(string steamId, decimal balance = -1)
        {
            if (_data.Accounts.ContainsKey(steamId))
            {
                _data.Accounts[steamId].Balance = balance >= 0 ? balance : BountyPlugin.Instance.Configuration.Instance.Economy.StartingBalance;
                _data.Accounts[steamId].Transactions.Clear();
                _data.Accounts[steamId].TotalShopSpent = 0;
                _data.Accounts[steamId].LastUpdated = DateTime.UtcNow;
                Save();
            }
        }

        private void AddTransaction(PlayerAccount account, string type, string description, decimal amount)
        {
            account.Transactions.Add(new Transaction
            {
                Type = type,
                Description = description,
                Amount = amount,
                BalanceAfter = account.Balance,
                Timestamp = DateTime.UtcNow
            });

            // Keep only last 50 transactions
            if (account.Transactions.Count > 50)
                account.Transactions.RemoveRange(0, account.Transactions.Count - 50);
        }

        public List<Transaction> GetTransactions(string steamId, int count = 10)
        {
            if (!_data.Accounts.ContainsKey(steamId)) return new List<Transaction>();
            return _data.Accounts[steamId].Transactions
                .OrderByDescending(t => t.Timestamp)
                .Take(count)
                .ToList();
        }

        // ==================== DAILY BONUS ====================

        public bool CanClaimDailyBonus(string steamId)
        {
            if (!_data.Accounts.ContainsKey(steamId)) return false;
            var account = _data.Accounts[steamId];
            return account.LastDailyBonus.Date < DateTime.UtcNow.Date;
        }

        public decimal ClaimDailyBonus(string steamId)
        {
            if (!_data.Accounts.ContainsKey(steamId)) return 0;
            var eco = BountyPlugin.Instance.Configuration.Instance.Economy;
            var account = _data.Accounts[steamId];

            var rng = new Random();
            decimal bonus = rng.Next((int)eco.DailyBonusMin, (int)eco.DailyBonusMax + 1);

            account.Balance += bonus;
            account.LastDailyBonus = DateTime.UtcNow;
            account.LastUpdated = DateTime.UtcNow;
            AddTransaction(account, "DAILY_BONUS", "Daily login bonus", bonus);
            Save();
            return bonus;
        }

        // ==================== INTEREST ====================

        public void ProcessInterest(string steamId)
        {
            if (!_data.Accounts.ContainsKey(steamId)) return;
            var eco = BountyPlugin.Instance.Configuration.Instance.Economy;
            var account = _data.Accounts[steamId];

            if ((DateTime.UtcNow - account.LastInterest).TotalMinutes < eco.InterestPayoutMinutes) return;

            decimal interest = Math.Round(account.Balance * (eco.InterestRatePercent / 100m), 0);
            if (interest > eco.InterestMaxPayout) interest = eco.InterestMaxPayout;
            if (interest <= 0) return;

            account.Balance += interest;
            account.LastInterest = DateTime.UtcNow;
            account.LastUpdated = DateTime.UtcNow;
            AddTransaction(account, "INTEREST", $"Hourly interest ({eco.InterestRatePercent:N0}%)", interest);
            Save();
        }

        // ==================== SALARY ====================

        public decimal ProcessSalary(string steamId)
        {
            if (!_data.Accounts.ContainsKey(steamId)) return 0;
            var eco = BountyPlugin.Instance.Configuration.Instance.Economy;
            var account = _data.Accounts[steamId];

            if ((DateTime.UtcNow - account.LastSalary).TotalMinutes < eco.SalaryIntervalMinutes) return 0;

            decimal salary = eco.SalaryAmount;
            account.Balance += salary;
            account.LastSalary = DateTime.UtcNow;
            account.LastUpdated = DateTime.UtcNow;
            AddTransaction(account, "SALARY", "Online salary", salary);
            Save();
            return salary;
        }

        // ==================== SHOP TRACKING ====================

        public void TrackShopSpend(string steamId, decimal amount)
        {
            if (_data.Accounts.ContainsKey(steamId))
            {
                _data.Accounts[steamId].TotalShopSpent += amount;
            }
        }

        public decimal GetTotalShopSpent(string steamId)
        {
            if (_data.Accounts.TryGetValue(steamId, out var account))
                return account.TotalShopSpent;
            return 0;
        }
    }
}
