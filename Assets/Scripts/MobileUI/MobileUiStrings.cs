// Мобильный интерфейс: строки на русском и английском.
//
// Своя маленькая таблица переводов — она покрывает НАШИ экраны (мобильные списки,
// настройки, кнопки). Текст самой игры приходит от шарда и уже русский, а штатные
// окна клиента переводятся постепенно, добавлением ключей сюда же.

using System.Collections.Generic;

namespace ClassicUO.MobileUI
{
    public static class MobileUiStrings
    {
        public const string Ru = "ru";
        public const string En = "en";

        // ключ -> { русский, английский }
        private static readonly Dictionary<string, string[]> Table = new Dictionary<string, string[]>
        {
            ["button"]          = new[] { "Моб. UI", "Mobile UI" },
            ["window_title"]    = new[] { "Мобильный интерфейс", "Mobile UI" },
            ["enabled"]         = new[] { "Мобильный интерфейс", "Mobile interface" },
            ["state_on"]        = new[] { "ВКЛ", "ON" },
            ["state_off"]       = new[] { "ВЫКЛ", "OFF" },
            ["language"]        = new[] { "Язык интерфейса", "Interface language" },
            ["lang_ru"]         = new[] { "Русский", "Russian" },
            ["lang_en"]         = new[] { "Английский", "English" },
            ["close"]           = new[] { "Закрыть", "Close" },
            ["data_folder"]     = new[] { "Папка данных", "Data folder" },
            ["hint_switch"]     = new[] { "Нажми, чтобы переключить", "Tap to switch" },
            ["hint_button"]     = new[] { "Кнопку «Моб. UI» можно перетаскивать", "The “Mobile UI” button can be dragged" },
            ["screens_soon"]    = new[] { "Экраны: сумка, рунбук, крафт — в работе", "Screens: backpack, runebook, crafting — in progress" },
        };

        public static string Normalize(string language)
        {
            if (string.IsNullOrEmpty(language))
            {
                return Ru;
            }

            string value = language.Trim().ToLowerInvariant();

            return value.StartsWith("en") ? En : Ru;
        }

        public static string Get(string key, string language)
        {
            string lang = Normalize(language);

            if (!Table.TryGetValue(key, out string[] variants) || variants == null || variants.Length == 0)
            {
                return key;
            }

            return lang == En ? variants[1] : variants[0];
        }
    }
}
