<p align="center">
  <img src="https://i.ibb.co/XxK0qrFt/2c781d93-d521-478c-a687-6c803eebfed6.png" alt="Doobzi's Grid Logo" width="300" />
</p>

# Doobzi's Grid — Unturned Plugin (RocketMod v4.23.1)

A full bounty hunting + economy + shop + auction house plugin for Unturned servers running RocketMod v4.

---

## Setup

### 1. Add Required DLLs

Create a `lib/` folder next to `BountyPlugin.csproj` and copy these DLLs into it:

**From your Unturned server `Unturned_Data/Managed/` folder:**
- `Assembly-CSharp.dll`
- `UnityEngine.dll`
- `UnityEngine.CoreModule.dll`
- `Newtonsoft.Json.dll`
- `com.rlabrecque.steamworks.net.dll`

**From your server `Modules/Rocket.Unturned/Binaries/` folder:**
- `Rocket.API.dll`
- `Rocket.Core.dll`
- `Rocket.Unturned.dll`

### 2. Build

```bash
dotnet build -c Release
```

The compiled `DoobzisGrid.dll` will be in `bin/Release/net48/`.

### 3. Install

Copy `DoobzisGrid.dll` into your server's `Rocket/Plugins/` folder and restart.

---

## Commands

### Bounty System
| Command | Alias | Permission | Description |
|---------|-------|------------|-------------|
| `/bountyadd <player> <amount>` | `/ba` | `bounty.add` | Place a bounty on a player |
| `/bountylist` | `/bl` | `bounty.list` | View all active bounties |
| `/bountytop` | `/bt` | `bounty.top` | See who has the highest bounty |
| `/bountyhunter` | `/bh` | `bounty.hunter` | Top 10 bounty hunters leaderboard |
| `/bountyclear <player>` | `/bc` | `bounty.clear` | Remove all bounties on a player (admin) |

### Economy
| Command | Alias | Permission | Description |
|---------|-------|------------|-------------|
| `/balance` | `/bal` | `economy.balance` | Check your current balance |
| `/pay <player> <amount>` | - | `economy.pay` | Send money to another player (5% tax) |
| `/transactions` | `/tx` | `economy.balance` | View recent transactions |
| `/profile` | `/pf` | `economy.balance` | View your full profile & achievements |

### Shop
| Command | Alias | Permission | Description |
|---------|-------|------------|-------------|
| `/shop [page]` | - | `shop.buy` | Browse all shop items |
| `/shopbuy <ID> [qty]` | `/sb` | `shop.buy` | Buy an item by ID |
| `/sell <ID> [qty]` | - | `shop.sell` | Sell an item from your inventory |
| `/shopsearch <keyword>` | `/ss` | `shop.buy` | Search for items by name |
| `/shopcats` | - | `shop.buy` | View all shop categories |
| `/shopcat <category> [page]` | - | `shop.buy` | Browse items in a category |
| `/shopadd <ID> <price>` | - | `shop.add` | Add an item to the shop (admin) |
| `/shoprem <ID>` | - | `shop.remove` | Remove an item from the shop (admin) |
| `/shopedit <ID> <price> <stock>` | - | `shop.edit` | Edit item price/stock (admin) |

### Auction House
| Command | Alias | Permission | Description |
|---------|-------|------------|-------------|
| `/ahlist [page]` | - | `auction.use` | Browse active auction listings |
| `/ahsell <itemId> <price>` | - | `auction.use` | List an item from your inventory for sale |
| `/ahbuy <listingId>` | - | `auction.use` | Buy an auction listing |
| `/ahcancel <listingId>` | - | `auction.use` | Cancel your own listing |
| `/ahsearch <keyword>` | - | `auction.use` | Search auction listings |
| `/ahmy` | - | `auction.use` | View your active listings |

### Admin Economy
| Command | Permission | Description |
|---------|------------|-------------|
| `/ecogive <player> <amount>` | `bounty.ecoadmin` | Give money to a player |
| `/ecotake <player> <amount>` | `bounty.ecoadmin` | Take money from a player |
| `/ecoset <player> <amount>` | `bounty.ecoadmin` | Set a player's balance |
| `/ecoreset <player>` | `bounty.ecoadmin` | Reset a player's balance |
| `/bountyreload` | `bounty.reload` | Reload plugin config |

### Help
| Command | Description |
|---------|-------------|
| `/gridhelp` | Show all available commands |

---

## How It Works

### Bounties
- Players use `/bountyadd` to place a bounty on another player. The money is **deducted from their balance**.
- Multiple players can stack bounties on the same target — the amounts accumulate.
- When someone **kills a player with a bounty**, the killer automatically collects the full bounty amount.
- The bounty completion is broadcast to the whole server.
- Hunter statistics are tracked — see the leaderboard with `/bountyhunter`.

### Economy
- New players receive a starting balance (default: `$1,000`).
- Money is used for placing bounties and buying from the shop.
- Killing bounty targets earns money.

### Shop
- On first load, the plugin auto-generates a shop containing **every single Unturned item** with prices based on item type and rarity.
- Each item has a **stock of 10** (configurable) that refreshes **every hour**.
- Admins can customize prices with `/shopedit`, remove items with `/shoprem`, or add items with `/shopadd`.
- The shop data is saved to `shop.json` — you can also edit it directly.

### Pricing Formula
```
Price = BasePrice(type) × RarityMultiplier
```

**Base prices by type:**
| Type | Base Price | Type | Base Price |
|------|-----------|------|-----------|
| Gun | $750 | Sentry | $500 |
| Melee | $200 | Generator | $300 |
| Throwable | $300 | Backpack | $250 |
| Trap | $250 | Vest | $200 |
| Medical | $75 | Storage | $150 |
| Food | $30 | Sight | $150 |
| Water | $30 | Barrel | $150 |
| Clothing | $100-150 | Magazine | $25 |
| Tool | $100 | Farm | $20 |

**Rarity multipliers:**
| Rarity | Multiplier |
|--------|-----------|
| Common | 1.0x |
| Uncommon | 1.5x |
| Rare | 2.5x |
| Epic | 4.0x |
| Legendary | 7.0x |
| Mythical | 12.0x |

---

## Configuration

The plugin auto-generates its config in `Rocket/Plugins/DoobzisGrid/DoobzisGrid.configuration.xml`:

```xml
<?xml version="1.0" encoding="utf-8"?>
<BountyConfiguration>
  <DefaultStartingBalance>1000</DefaultStartingBalance>
  <MinBountyAmount>100</MinBountyAmount>
  <ShopRefreshIntervalMinutes>60</ShopRefreshIntervalMinutes>
  <DefaultShopStock>10</DefaultShopStock>
  <BroadcastBountyAdded>true</BroadcastBountyAdded>
  <BroadcastBountyCompleted>true</BroadcastBountyCompleted>
</BountyConfiguration>
```

| Setting | Default | Description |
|---------|---------|-------------|
| `DefaultStartingBalance` | 1000 | Money new players receive |
| `MinBountyAmount` | 100 | Minimum bounty amount |
| `ShopRefreshIntervalMinutes` | 60 | How often shop stock resets (minutes) |
| `DefaultShopStock` | 10 | Default stock per item per refresh |
| `BroadcastBountyAdded` | true | Announce new bounties to server |
| `BroadcastBountyCompleted` | true | Announce bounty completions to server |

---

## Permissions (Rocket Permissions.config.xml)

Example permission group for regular players:
```xml
<Group>
  <Id>default</Id>
  <DisplayName>Player</DisplayName>
  <Permissions>
    <Permission>bounty.add</Permission>
    <Permission>bounty.list</Permission>
    <Permission>bounty.top</Permission>
    <Permission>bounty.hunter</Permission>
    <Permission>economy.balance</Permission>
    <Permission>economy.pay</Permission>
    <Permission>shop.buy</Permission>
    <Permission>shop.sell</Permission>
    <Permission>auction.use</Permission>
  </Permissions>
</Group>
```

Admin-only permissions: `shop.remove`, `shop.add`, `shop.edit`, `bounty.clear`, `bounty.ecoadmin`, `bounty.reload`

---

## Data Files

All data is stored as JSON in `Rocket/Plugins/DoobzisGrid/`:

| File | Contents |
|------|----------|
| `economy.json` | Player balances & transactions |
| `bounties.json` | Active bounties |
| `hunters.json` | Hunter leaderboard stats & streaks |
| `shop.json` | Shop items, prices, and stock |
| `achievements.json` | Player achievements |
| `auctions.json` | Auction house listings |

---

## Troubleshooting

**`Assets.find(allItems)` doesn't compile:**  
If your Unturned version doesn't support the generic `Assets.find<T>(List<T>)`, replace it in `ShopManager.cs` with:
```csharp
var allAssets = Assets.find(EAssetType.ITEM);
var allItems = allAssets?.OfType<ItemAsset>().ToList() ?? new List<ItemAsset>();
```
