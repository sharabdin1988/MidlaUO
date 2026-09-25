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

        public static readonly int[] WidthPresets = { 460, 560, 680, 800 };
        public static readonly int[] HeightPresets = { 370, 450, 540, 630 };
        public static readonly int[] RowPresets = { 44, 52, 60, 68 };

        public static int GetWindowWidth() => WidthPresets[Math.Max(0, Math.Min(3, Config?.WindowPreset ?? 1))];
        public static int GetWindowHeight() => HeightPresets[Math.Max(0, Math.Min(3, Config?.WindowPreset ?? 1))];
        public static int GetRowHeight() => RowPresets[Math.Max(0, Math.Min(3, Config?.WindowPreset ?? 1))];

        public static string GetPresetName()
        {
            int p = Math.Max(0, Math.Min(3, Config?.WindowPreset ?? 1));
            return T("preset_" + p);
        }

        public static void NextWindowPreset()
        {
            Init();
            if (Config.WindowPreset < 3)
            {
                Config.WindowPreset++;
                Config.Save();
                RefreshOpenWindows();
            }
        }

        public static void PrevWindowPreset()
        {
            Init();
            if (Config.WindowPreset > 0)
            {
                Config.WindowPreset--;
                Config.Save();
                RefreshOpenWindows();
            }
        }

        public static void CycleWindowPreset()
        {
            Init();
            Config.WindowPreset = (Config.WindowPreset + 1) % 4;
            Config.Save();
            RefreshOpenWindows();
        }

        public static void RefreshOpenWindows()
        {
            var spellbooks = new System.Collections.Generic.List<MobileSpellbookGump>();
            var runebooks = new System.Collections.Generic.List<MobileRunebookGump>();

            for (var node = UIManager.Gumps.First; node != null; node = node.Next)
            {
                if (node.Value is MobileSpellbookGump sg && !sg.IsDisposed)
                {
                    spellbooks.Add(sg);
                }
                else if (node.Value is MobileRunebookGump rg && !rg.IsDisposed)
                {
                    runebooks.Add(rg);
                }
            }

            foreach (var sg in spellbooks)
            {
                sg.Rebuild();
            }
            foreach (var rg in runebooks)
            {
                rg.Rebuild();
            }
        }

        public static int ScreenWidth => Client.Game?.Window?.ClientBounds.Width ?? 1280;
        public static int ScreenHeight => Client.Game?.Window?.ClientBounds.Height ?? 720;

        public static int ContainerScale => ClassicUO.Configuration.ProfileManager.CurrentProfile?.ContainersScale ?? 100;

        public static void ChangeContainerScale(int delta)
        {
            var profile = ClassicUO.Configuration.ProfileManager.CurrentProfile;
            if (profile == null)
            {
                return;
            }

            int cur = profile.ContainersScale;
            int next = Math.Max(100, Math.Min(200, cur + delta));
            if (next != cur)
            {
                profile.ContainersScale = (byte)next;
                UIManager.ContainerScale = next / 100f;
            }
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
