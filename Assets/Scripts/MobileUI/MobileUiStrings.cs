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
            ["container"]         = new[] { "Контейнер", "Container" },
            ["container_summary"] = new[] { "Предметов: {0} · вес: {1}", "Items: {0} · weight: {1}" },
            ["container_empty"]   = new[] { "Пусто", "Empty" },
            ["open_backpack"]     = new[] { "Открыть сумку", "Open backpack" },
            ["refresh"]           = new[] { "Обновить", "Refresh" },
            ["weight_short"]      = new[] { "в.", "w" },
            ["search"]            = new[] { "Поиск", "Search" },
            ["equipped"]          = new[] { "надето", "worn" },
            ["not_equipped"]      = new[] { "снято", "off" },
            ["size_small"]        = new[] { "Размер: мелкий", "Size: small" },
            ["size_normal"]       = new[] { "Размер: обычный", "Size: normal" },
            ["size_big"]          = new[] { "Размер: крупный", "Size: big" },
            ["equipment"]        = new[] { "Экипировка", "Equipment" },
            ["container_not_found"] = new[] { "Ничего не найдено", "Nothing found" },
            ["spellbook"]         = new[] { "Книга заклинаний", "Spellbook" },
            ["spellbook_summary"] = new[] { "Заклинаний: {0} · нажми, чтобы прочитать", "Spells: {0} · tap to cast" },
            ["spellbook_wait"]    = new[] { "Ждём список заклинаний от сервера…", "Waiting for the spell list…" },
            ["spell"]             = new[] { "Заклинание", "Spell" },
            ["runebook"]          = new[] { "Книга рун", "Runebook" },
            ["runebook_empty"]    = new[] { "В книге нет записанных рун", "No runes inscribed in this book" },
            ["charges"]           = new[] { "Заряды", "Charges" },
            ["recharge"]          = new[] { "Зарядить", "Recharge" },
            ["rename"]            = new[] { "Имя", "Rename" },
            ["cast"]              = new[] { "Каст", "Cast" },
            ["pin"]               = new[] { "+ Экран", "+ Pin" },
            ["pinned"]            = new[] { "Добавлено на экран!", "Pinned to screen!" },
            ["recall"]            = new[] { "Рекол", "Recall" },
            ["gate"]              = new[] { "Гейт", "Gate" },
            ["charge_recall"]     = new[] { "⚡ Заряд", "⚡ Charge" },
            ["set_default"]       = new[] { "По умолч.", "Default" },
            ["drop_rune"]         = new[] { "Вынуть", "Drop" },
            ["tab_quick"]         = new[] { "✈ Быстрые", "✈ Quick" },
            ["tab_all"]           = new[] { "Все", "All" },
            ["circle"]            = new[] { "Круг", "Circle" },
            ["mana"]              = new[] { "Мана", "Mana" },
            ["backpack_hint"]     = new[] { "Сумки: нативный вид с Drag & Drop (Опции → Контейнеры)", "Bags: native mode with Drag & Drop (Options → Containers)" },
            ["books_hint"]        = new[] { "Книги заклинаний и рун: крупные сенсорные кнопки", "Books: touch-friendly spellbook & runebook" },
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
