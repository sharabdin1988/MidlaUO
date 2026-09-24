// Мобильный интерфейс: точка входа и переключатели.
//
// Init() вызывается один раз при входе в мир (из GameScene.Load). Модуль:
//   * читает свои настройки (включён/выключен + язык) из mobileui.json;
//   * показывает экранную кнопку «Моб. UI» (её можно перетаскивать);
//   * открывает панель с переключателями интерфейса и языка.
//
// Всё живёт в отдельных файлах (Assets/Scripts/MobileUI/*) и в штатный код
// клиента врезается тремя однострочными хуками — так обновляться проще.

using System;
using System.Linq;
using ClassicUO.Game;
using ClassicUO.Game.Managers;
using ClassicUO.Game.UI.Gumps;
using ClassicUO.Utility.Logging;

namespace ClassicUO.MobileUI
{
    public static class MobileUiController
    {
        public static MobileUiConfig Config { get; private set; }

        public static bool Enabled => Config != null && Config.Enabled;

        public static string Language
        {
            get => Config?.Language ?? MobileUiStrings.Ru;
            set
            {
                if (Config != null)
                {
                    Config.Language = MobileUiStrings.Normalize(value);
                }
            }
        }

        public static bool Ready => Config != null;

        /// <summary>Перевод строки нашего интерфейса на текущий язык.</summary>
        public static string T(string key) => MobileUiStrings.Get(key, Language);

        public static void Init()
        {
            if (Config != null)
            {
                EnsureButton();

                return;
            }

            Config = MobileUiConfig.Load();

            if (UserPreferences.Language != null)
            {
                string prefLang = UserPreferences.Language.CurrentValue == (int)PreferenceEnums.LanguageMode.Russian
                    ? MobileUiStrings.Ru
                    : MobileUiStrings.En;
                if (!string.Equals(Config.Language, prefLang, StringComparison.OrdinalIgnoreCase))
                {
                    Config.Language = prefLang;
                    Config.Save();
                }
            }

            EnableReconnectAndAutoLogin();
            EnsureButton();
        }

        /// <summary>
        /// Включить автологин и автопереподключение: у клиента это уже есть
        /// (Settings.GlobalSettings.Reconnect — при обрыве сам возвращается ко входу,
        /// AutoLogin — сам входит). Без них после сворачивания игра остаётся
        /// на экране «Connection lost», и надо входить вручную.
        /// </summary>
        private static void EnableReconnectAndAutoLogin()
        {
            try
            {
                var settings = ClassicUO.Configuration.Settings.GlobalSettings;
                bool changed = false;

                if (!settings.Reconnect)
                {
                    settings.Reconnect = true;
                    changed = true;
                }

                if (!settings.AutoLogin)
                {
                    settings.AutoLogin = true;
                    changed = true;
                }

                if (changed)
                {
                    settings.Save();
                    Log.Info("[MobileUI] включены автологин и автопереподключение");
                }
            }
            catch (Exception ex)
            {
                Log.Error($"[MobileUI] не удалось включить автологин: {ex.Message}");
            }
        }

        /// <summary>Показать экранную кнопку, если её ещё нет.</summary>
        public static void EnsureButton()
        {
            if (Config == null || !Config.ShowButton)
            {
                return;
            }

            var world = ClassicUO.Client.Game.UO.World;

            if (world == null)
            {
                return;
            }

            if (UIManager.Gumps.OfType<MobileUiButtonGump>().Any())
            {
                return;
            }

            UIManager.Add(new MobileUiButtonGump(world));
            Log.Info("[MobileUI] кнопка добавлена");
        }

        public static void ToggleEnabled()
        {
            Init();

            Config.Enabled = !Config.Enabled;
            Config.Save();

            Log.Info($"[MobileUI] мобильный интерфейс: {(Config.Enabled ? "включён" : "выключен")}");
        }

        public static void ToggleLanguage()
        {
            Init();

            Config.Language = Config.Language == MobileUiStrings.Ru ? MobileUiStrings.En : MobileUiStrings.Ru;
            Config.Save();

            if (UserPreferences.Language != null)
            {
                UserPreferences.Language.CurrentValue = Config.Language == MobileUiStrings.Ru
                    ? (int)PreferenceEnums.LanguageMode.Russian
                    : (int)PreferenceEnums.LanguageMode.English;
            }

            Log.Info($"[MobileUI] язык интерфейса: {Config.Language}");
        }

        /// <summary>Сервер прислал содержимое контейнера.</summary>
        public static void OnContainerUpdated()
        {
        }

        /// <summary>Серийники надетых вещей игрока (для отметок «надето/снято»).</summary>
        public static System.Collections.Generic.HashSet<uint> GetEquippedSerials()
        {
            var result = new System.Collections.Generic.HashSet<uint>();

            var world = ClassicUO.Client.Game.UO.World;

            if (world == null || world.Player == null)
            {
                return result;
            }

            for (var linked = world.Player.Items; linked != null; linked = linked.Next)
            {
                var item = linked as ClassicUO.Game.GameObjects.Item;

                if (item != null && !item.IsDestroyed && item.Layer != ClassicUO.Game.Data.Layer.Invalid)
                {
                    result.Add(item.Serial);
                }
            }

            return result;
        }

        /// <summary>Открыть мобильный список заклинаний для книги.</summary>
        public static void OpenSpellbook(uint bookSerial)
        {
            var world = ClassicUO.Client.Game.UO.World;

            if (world == null)
            {
                return;
            }

            var existing = UIManager.Gumps.OfType<MobileSpellbookGump>().FirstOrDefault(g => g.BookSerial == bookSerial);

            if (existing != null)
            {
                existing.Dispose();
            }

            UIManager.Add(new MobileSpellbookGump(world, bookSerial));
        }

        /// <summary>Открыть рюкзак игрока (нативная масштабируемая сумка).</summary>
        public static void OpenBackpack()
        {
            var world = ClassicUO.Client.Game.UO.World;

            if (world == null || world.Player == null)
            {
                return;
            }

            var backpack = world.Player.FindItemByLayer(ClassicUO.Game.Data.Layer.Backpack);

            if (backpack != null)
            {
                GameActions.DoubleClick(world, backpack.Serial);
            }
        }

        /// <summary>Открыть (или пересоздать) панель настроек, чтобы подписи были актуальными.</summary>
        public static void OpenSettings()
        {
            Init();

            var world = ClassicUO.Client.Game.UO.World;

            if (world == null)
            {
                return;
            }

            var existing = UIManager.Gumps.OfType<MobileUiSettingsGump>().FirstOrDefault();

            if (existing != null)
            {
                existing.Dispose();
            }

            UIManager.Add(new MobileUiSettingsGump(world));
        }
    }
}
