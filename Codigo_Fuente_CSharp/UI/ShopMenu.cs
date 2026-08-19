using System;
using SubwaySurfersAudioGame.Audio;
using SubwaySurfersAudioGame.Core;

namespace SubwaySurfersAudioGame.UI
{
    public class ShopMenu
    {
        private readonly GameEngine _engine;
        private int _selectedIndex = 0;

        private readonly ShopItemType[] _itemTypes = new ShopItemType[]
        {
            ShopItemType.BuyHoverboard,
            ShopItemType.BuyHeadstart,
            ShopItemType.UpgradeMagnet,
            ShopItemType.UpgradeJetpack,
            ShopItemType.UpgradeSuperSneakers,
            ShopItemType.UpgradeMultiplier
        };

        public ShopMenu(GameEngine engine)
        {
            _engine = engine;
        }

        public void Open()
        {
            _selectedIndex = 0;
            _engine.AudioEngine.Play2D(AudioMap.UI.StoreOpen, gain: 0.7f);
            SpeakCurrentItem();
        }

        public void SpeakCurrentItem()
        {
            var inv = _engine.Inventory;
            string itemName = "";
            string itemDetails = "";

            switch (_itemTypes[_selectedIndex])
            {
                case ShopItemType.BuyHoverboard:
                    itemName = "Comprar Tabla Hoverboard";
                    itemDetails = $"Precio: {EconomySystem.HoverboardPrice} monedas. En inventario: {inv.HoverboardCount}. Te protege de 1 choque fatal.";
                    break;

                case ShopItemType.BuyHeadstart:
                    itemName = "Comprar Cohete Headstart";
                    itemDetails = $"Precio: {EconomySystem.HeadstartPrice} monedas. En inventario: {inv.HeadstartCount}. Vuela los primeros 1,000 metros a ultra velocidad.";
                    break;

                case ShopItemType.UpgradeMagnet:
                    int magCost = EconomySystem.GetUpgradeCost(inv.MagnetLevel);
                    string magCostStr = inv.MagnetLevel >= 5 ? "Nivel Máximo" : $"Mejorar a Nivel {inv.MagnetLevel + 1} por {magCost} monedas";
                    itemName = $"Mejora de Imán (Nivel {inv.MagnetLevel} de 5)";
                    itemDetails = $"{magCostStr}. Duración actual: {inv.GetMagnetDuration():F0} segundos.";
                    break;

                case ShopItemType.UpgradeJetpack:
                    int jetCost = EconomySystem.GetUpgradeCost(inv.JetpackLevel);
                    string jetCostStr = inv.JetpackLevel >= 5 ? "Nivel Máximo" : $"Mejorar a Nivel {inv.JetpackLevel + 1} por {jetCost} monedas";
                    itemName = $"Mejora de Mochila Jetpack (Nivel {inv.JetpackLevel} de 5)";
                    itemDetails = $"{jetCostStr}. Duración actual: {inv.GetJetpackDuration():F0} segundos.";
                    break;

                case ShopItemType.UpgradeSuperSneakers:
                    int snkCost = EconomySystem.GetUpgradeCost(inv.SuperSneakersLevel);
                    string snkCostStr = inv.SuperSneakersLevel >= 5 ? "Nivel Máximo" : $"Mejorar a Nivel {inv.SuperSneakersLevel + 1} por {snkCost} monedas";
                    itemName = $"Mejora de Super Zapatillas (Nivel {inv.SuperSneakersLevel} de 5)";
                    itemDetails = $"{snkCostStr}. Duración actual: {inv.GetSuperSneakersDuration():F0} segundos.";
                    break;

                case ShopItemType.UpgradeMultiplier:
                    int mulCost = EconomySystem.GetUpgradeCost(inv.MultiplierLevel);
                    string mulCostStr = inv.MultiplierLevel >= 5 ? "Nivel Máximo" : $"Mejorar a Nivel {inv.MultiplierLevel + 1} por {mulCost} monedas";
                    itemName = $"Mejora de Multiplicador 2X (Nivel {inv.MultiplierLevel} de 5)";
                    itemDetails = $"{mulCostStr}. Duración actual: {inv.GetMultiplierDuration():F0} segundos.";
                    break;
            }

            string text = $"{itemName}. {itemDetails} Tienes {inv.TotalCoins} monedas. Pulsa Enter para comprar. Opción {_selectedIndex + 1} de {_itemTypes.Length + 1}.";
            _engine.Accessibility.Speak(text, interrupt: true);
        }

        public void HandleInput(ConsoleKeyInfo key)
        {
            switch (key.Key)
            {
                case ConsoleKey.UpArrow:
                    _selectedIndex = (_selectedIndex - 1 + _itemTypes.Length + 1) % (_itemTypes.Length + 1);
                    _engine.AudioEngine.Play2D(AudioMap.UI.MenuBrowseTap, gain: 0.5f);
                    if (_selectedIndex == _itemTypes.Length)
                    {
                        _engine.Accessibility.Speak($"Volver al Menú Principal. Opción {_itemTypes.Length + 1} de {_itemTypes.Length + 1}.", interrupt: true);
                    }
                    else
                    {
                        SpeakCurrentItem();
                    }
                    break;

                case ConsoleKey.DownArrow:
                    _selectedIndex = (_selectedIndex + 1) % (_itemTypes.Length + 1);
                    _engine.AudioEngine.Play2D(AudioMap.UI.MenuBrowseTap, gain: 0.5f);
                    if (_selectedIndex == _itemTypes.Length)
                    {
                        _engine.Accessibility.Speak($"Volver al Menú Principal. Opción {_itemTypes.Length + 1} de {_itemTypes.Length + 1}.", interrupt: true);
                    }
                    else
                    {
                        SpeakCurrentItem();
                    }
                    break;

                case ConsoleKey.Enter:
                case ConsoleKey.Spacebar:
                    if (_selectedIndex == _itemTypes.Length)
                    {
                        _engine.CurrentState = GameState.MainMenu;
                        _engine.Menu.OpenMainMenu();
                    }
                    else
                    {
                        var item = _itemTypes[_selectedIndex];
                        bool success = EconomySystem.TryBuyItem(_engine.Inventory, item, out string message);
                        if (success)
                        {
                            _engine.AudioEngine.Play2D(AudioMap.Collectibles.MissionReward, gain: 0.8f);
                            GameSettings.Save(_engine);
                        }
                        else
                        {
                            _engine.AudioEngine.Play2D(AudioMap.Obstacles.StumbleLight, gain: 0.6f);
                        }
                        _engine.Accessibility.Speak(message, interrupt: true);
                    }
                    break;

                case ConsoleKey.Escape:
                    _engine.CurrentState = GameState.MainMenu;
                    _engine.Menu.OpenMainMenu();
                    break;
            }
        }
    }
}
