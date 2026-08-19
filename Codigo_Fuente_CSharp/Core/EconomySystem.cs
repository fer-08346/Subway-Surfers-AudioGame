using System;

namespace SubwaySurfersAudioGame.Core
{
    public enum ShopItemType
    {
        BuyHoverboard,
        BuyHeadstart,
        UpgradeMagnet,
        UpgradeJetpack,
        UpgradeSuperSneakers,
        UpgradeMultiplier
    }

    public static class EconomySystem
    {
        public const int HoverboardPrice = 300;
        public const int HeadstartPrice = 500;

        // Upgrade costs for levels 2, 3, 4, 5
        private static readonly int[] UpgradeCosts = { 500, 1500, 3000, 5000 };

        public static int GetUpgradeCost(int currentLevel)
        {
            if (currentLevel < 1 || currentLevel >= 5) return 0; // Max level or invalid
            return UpgradeCosts[currentLevel - 1];
        }

        public static bool TryBuyItem(Inventory inventory, ShopItemType itemType, out string message)
        {
            switch (itemType)
            {
                case ShopItemType.BuyHoverboard:
                    if (inventory.TotalCoins >= HoverboardPrice)
                    {
                        inventory.TotalCoins -= HoverboardPrice;
                        inventory.HoverboardCount++;
                        message = $"¡Compraste 1 Tabla Hoverboard! Tienes {inventory.HoverboardCount} en inventario. Monedas restantes: {inventory.TotalCoins}.";
                        return true;
                    }
                    else
                    {
                        message = $"Monedas insuficientes. Cuesta {HoverboardPrice} monedas y tienes {inventory.TotalCoins}.";
                        return false;
                    }

                case ShopItemType.BuyHeadstart:
                    if (inventory.TotalCoins >= HeadstartPrice)
                    {
                        inventory.TotalCoins -= HeadstartPrice;
                        inventory.HeadstartCount++;
                        message = $"¡Compraste 1 Cohete Headstart! Tienes {inventory.HeadstartCount} en inventario. Monedas restantes: {inventory.TotalCoins}.";
                        return true;
                    }
                    else
                    {
                        message = $"Monedas insuficientes. Cuesta {HeadstartPrice} monedas y tienes {inventory.TotalCoins}.";
                        return false;
                    }

                case ShopItemType.UpgradeMagnet:
                    return TryUpgrade(
                        "Imán",
                        inventory.MagnetLevel,
                        lvl => inventory.MagnetLevel = lvl,
                        inventory,
                        out message
                    );

                case ShopItemType.UpgradeJetpack:
                    return TryUpgrade(
                        "Mochila Jetpack",
                        inventory.JetpackLevel,
                        lvl => inventory.JetpackLevel = lvl,
                        inventory,
                        out message
                    );

                case ShopItemType.UpgradeSuperSneakers:
                    return TryUpgrade(
                        "Super Zapatillas",
                        inventory.SuperSneakersLevel,
                        lvl => inventory.SuperSneakersLevel = lvl,
                        inventory,
                        out message
                    );

                case ShopItemType.UpgradeMultiplier:
                    return TryUpgrade(
                        "Multiplicador 2X",
                        inventory.MultiplierLevel,
                        lvl => inventory.MultiplierLevel = lvl,
                        inventory,
                        out message
                    );

                default:
                    message = "Opción inválida.";
                    return false;
            }
        }

        private static bool TryUpgrade(string name, int currentLevel, Action<int> setLevel, Inventory inventory, out string message)
        {
            if (currentLevel >= 5)
            {
                message = $"{name} ya está en su nivel máximo (Nivel 5).";
                return false;
            }

            int cost = GetUpgradeCost(currentLevel);
            if (inventory.TotalCoins >= cost)
            {
                inventory.TotalCoins -= cost;
                int newLevel = currentLevel + 1;
                setLevel(newLevel);
                message = $"¡Mejora exitosa! {name} subió al Nivel {newLevel} de 5. Monedas restantes: {inventory.TotalCoins}.";
                return true;
            }
            else
            {
                message = $"Monedas insuficientes para mejorar {name} al Nivel {currentLevel + 1}. Necesitas {cost} monedas y tienes {inventory.TotalCoins}.";
                return false;
            }
        }
    }
}
