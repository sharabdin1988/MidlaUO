// Мобильный интерфейс: точка входа и переключатели.
//
// Init() вызывается один раз при входе в мир (из GameScene.Load). Модуль:
//   * читает свои настройки (включён/выключен + язык) из mobileui.json;
//   * показывает экранную кнопку «Моб. UI» (её можно перетаскивать);
//   * открывает панель с переключателями интерфейса и языка.
//
// Всё живёт в отдельных файлах (Assets/Scripts/MobileUI/*) и в штатный код
// клиента врезается тремя однострочными хуками — так обновляться проще.

using System.Linq;
using ClassicUO.Game.Managers;
using ClassicUO.Game.UI.Gumps;
using ClassicUO.Utility.Logging;

namespace ClassicUO.MobileUI
{
    public static class MobileUiController
    {
        public static MobileUiConfig Config { get; private set; }

        public static bool Enabled => Config != null && Config.Enabled;

        public static string Language => Config?.Language ?? MobileUiStrings.Ru;

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
            EnsureButton();
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

            Log.Info($"[MobileUI] язык интерфейса: {Config.Language}");
        }

        /// <summary>Сервер прислал содержимое контейнера — перечитать открытые мобильные списки.</summary>
        public static void OnContainerUpdated()
        {
            if (Config == null || !Config.Enabled)
            {
                return;
            }

            foreach (var gump in UIManager.Gumps.OfType<MobileContainerGump>())
            {
                gump.Rebuild();
            }
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

        /// <summary>Переключить пресет размера окна (0/1/2) и сохранить.</summary>
        public static void CycleWindowPreset()
        {
            Init();

            Config.WindowPreset = (Config.WindowPreset + 1) % 3;
            Config.Save();
        }

        public static void ChangeRowHeight(int delta)
        {
            Init();

            int value = Config.RowHeight + delta;

            if (value < 28)
            {
                value = 28;
            }

            if (value > 72)
            {
                value = 72;
            }

            Config.RowHeight = value;
            Config.Save();
        }

        /// <summary>Открыть мобильный список заклинаний для книги.</summary>
        public static void OpenSpellbook(uint bookSerial)
        {
            var world = ClassicUO.Client.Game.UO.World;

            if (world == null)
            {
                return;
            }

            var existing = UIManager.Gumps.OfType<MobileSpellbookGump>().FirstOrDefault(g => g.LocalSerial == bookSerial);

            if (existing != null)
            {
                existing.Dispose();
            }

            UIManager.Add(new MobileSpellbookGump(world, bookSerial));
        }

        /// <summary>Переоткрыть экран контейнера — применяет новый размер окна и строк.</summary>
        public static void ReopenContainer(uint serial)
        {
            var world = ClassicUO.Client.Game.UO.World;

            if (world == null || world.Player == null)
            {
                return;
            }

            var existing = UIManager.Gumps.OfType<MobileContainerGump>().FirstOrDefault(g => g.LocalSerial == serial);

            if (existing != null)
            {
                existing.Dispose();
            }

            UIManager.Add(new MobileContainerGump(world, serial));
        }

        /// <summary>Открыть экран рюкзака (мобильный список предметов).</summary>
        public static void OpenBackpack()
        {
            Init();

            var world = ClassicUO.Client.Game.UO.World;

            if (world == null || world.Player == null)
            {
                return;
            }

            var backpack = world.Player.FindItemByLayer(ClassicUO.Game.Data.Layer.Backpack);

            if (backpack == null)
            {
                return;
            }

            var existing = UIManager.Gumps.OfType<MobileContainerGump>().FirstOrDefault(g => g.LocalSerial == backpack.Serial);

            if (existing != null)
            {
                existing.Dispose();
            }

            UIManager.Add(new MobileContainerGump(world, backpack.Serial));
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
