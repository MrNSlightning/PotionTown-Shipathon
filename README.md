# 🧪 PotionTown

<p align="center">
  <strong>A Cozy 2D Alchemy Tavern & Potion Crafting Simulation Game</strong><br>
  <em>Built with Unity 6 & Powered by RevenueCat for Shipathon 2026</em>
</p>

<p align="center">
  <img src="https://img.shields.io/badge/Unity-6000.3.22f1%20(Unity%206)-blue.svg" alt="Unity Version">
  <img src="https://img.shields.io/badge/Render%20Pipeline-URP%202D-violet.svg" alt="URP 2D">
  <img src="https://img.shields.io/badge/RevenueCat%20SDK-8.6.0-orange.svg" alt="RevenueCat">
  <img src="https://img.shields.io/badge/License-Proprietary%20%2F%20Competition-green.svg" alt="License">
</p>

---

## 📖 About the Game

**PotionTown** is a cozy, atmospheric alchemy simulation game where you inherit a charming village potion shop. Gather mystical herbs, stir the cauldron, discover magical recipes, fulfill peculiar customer requests, and grow your humble shop into the most renowned tavern in the realm.

- **Interactive Cauldron Brewing:** Heat, stir, and balance ingredients to brew potions with varying qualities and effects.
- **Tavern & Customer Orders:** Villagers visit your shop with unique ailments and requests. Fulfill orders accurately to earn Gold, Reputation, and XP.
- **Potion Pass & Progression:** Level up your alchemist rank through daily quests and seasonal milestones.
- **Mini-Games & Exploration:** Engage in mini-games like the Crystal Altar and Rune Memory to harvest rare reagents.

---

## 💎 RevenueCat Integration (Shipathon Showcase)

PotionTown features a monetization architecture powered by **RevenueCat Purchases SDK (v8.6.0)**, designed with player-first progression balance and flexible live configuration.

### 1. Entitlements
| Entitlement ID | Description |
| :--- | :--- |
| `potion_pass_premium` | Grants access to the premium seasonal Potion Pass tier with exclusive recipes, cauldron skins, and bonus gold yields. |
| `remove_ads` | Permanently disables all banner and interstitial advertisements across the game. |

### 2. Products & Virtual Currency
- **Ancient Coins (Kadim Para):**
  - `com.potiontavern.kadimpara.small`: Small Pouch of Kadim Para
  - `com.potiontavern.kadimpara.medium`: Chest of Kadim Para
  - `com.potiontavern.kadimpara.large`: Treasury Vault of Kadim Para
- **Subscriptions & Non-Consumables:**
  - `com.potiontavern.potionpass.premium`: Premium Season Pass (auto-checks `potion_pass_premium` entitlement)
  - `com.potiontavern.removeads`: Permanent Ad-Free Experience (auto-checks `remove_ads` entitlement)

### 3. Editor Simulation Mode
For seamless evaluation and testing without active App Store / Google Play credentials:
- In the Unity Editor, `RevenueCatManager` automatically runs in **Simulation Mode**, allowing judges and developers to test product purchases, pass unlocking, and entitlement changes directly in Play Mode.

---

## 🚀 Getting Started for Reviewers & Judges

### Prerequisites
- **Unity 6** (Version `6000.3.22f1` or later recommended)
- **Universal Render Pipeline (URP)** with 2D Renderer

### Setup Instructions
1. **Clone the Repository:**
   ```bash
   git clone https://github.com/MrNSlightning/PotionTown-Shipathon.git
   ```
2. **Open in Unity:**
   - Launch Unity Hub.
   - Click **Add** and select the cloned `PotionTown-Shipathon` directory.
   - Open the project using Unity `6000.3.22f1`.

3. **Configure RevenueCat Public API Keys:**
   - In the Unity Project window, navigate to:  
     `Assets/_PotionTown/Resources/LiveMonetizationConfig.asset`
   - In the Inspector:
     - Set `Revenue Cat Android Api Key` to your public SDK key from the RevenueCat Dashboard.
     - Set `Revenue Cat IOS Api Key` to your public iOS SDK key.
   - *(Alternatively, you can test directly in Unity Editor using the built-in simulation mode without any keys!)*

4. **Launch the Game:**
   - Navigate to `Assets/_PotionTown/Scenes/`
   - Open and run `PotionTown.unity`
   - Press **Play** in Unity!

---

## 🛠️ Architecture Highlights

- **ScriptableObject-Driven Game Data:** Ingredients, recipes, customer dialogue, and monetization configuration are decoupled as ScriptableObjects for zero-code balancing.
- **Event-Driven UI:** Decoupled HUD and shop panels communicate via C# delegates and event channels (`OnPurchaseSuccess`, `OnOfferingsLoaded`, `OnCustomerInfoUpdated`).
- **Clean Separation of Concerns:** Core crafting, customer AI, audio management, and monetization each reside in dedicated namespaces under `PotionShop`.

---

## 🛡️ Security & Privacy Notice

To maintain public open-source repository safety:
- All proprietary production API keys, signing keystores, and Firebase private configs have been replaced with documented template placeholders (`YOUR_REVENUECAT_ANDROID_API_KEY`, sample test IDs).
- Evaluation requires entering your own test keys or utilizing the built-in Editor simulation mode.

---

## 👥 Credits

Developed with ❤️ by **MrNSlightning & Team** for the **RevenueCat Shipathon 2026**.
