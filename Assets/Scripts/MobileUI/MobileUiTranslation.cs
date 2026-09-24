// SPDX-License-Identifier: BSD-2-Clause
//
// Мобильный интерфейс: централизованный словарь и переводчик настроек на русский язык.
// Покрывает:
//   1. Мобильное боковое меню Unity (MenuPresenter, OptionEnumView)
//   2. Внутриигровые настройки ClassicUO (OptionsGump: все вкладки, секции, чекбоксы, комбобоксы)

using System;
using System.Collections.Generic;

namespace ClassicUO.MobileUI
{
    public static class MobileUiTranslation
    {
        public static string CurrentLanguage
        {
            get
            {
                if (UserPreferences.Language != null)
                {
                    return UserPreferences.Language.CurrentValue == (int)PreferenceEnums.LanguageMode.Russian
                        ? MobileUiStrings.Ru
                        : MobileUiStrings.En;
                }
                return MobileUiController.Language ?? MobileUiStrings.Ru;
            }
        }

        public static bool IsRussian => string.Equals(CurrentLanguage, MobileUiStrings.Ru, StringComparison.OrdinalIgnoreCase);

        // Словарь перевода текстовых строк (английский -> русский)
        private static readonly Dictionary<string, string> Strings = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            // === Мобильное меню Unity (MenuPresenter) ===
            ["MobileUO Menu"]                       = "Меню MidlaUO",
            ["MidlaUO Menu"]                        = "Меню MidlaUO",
            ["Graphics"]                            = "Графика",
            ["Input"]                               = "Управление",
            ["Developer"]                           = "Разработчик",
            ["Language"]                            = "Язык интерфейса",
            ["Interface Language"]                  = "Язык интерфейса",
            ["Interface language"]                  = "Язык интерфейса",
            ["Interface Language:"]                 = "Язык интерфейса:",
            ["Close Buttons"]                       = "Кнопки закрытия [X]",
            ["Show Modifier Key Buttons"]           = "Кнопки Shift/Ctrl/Alt",
            ["Enable Assistant"]                    = "Включить ассистент",
            ["View Scale"]                          = "Масштаб интерфейса",
            ["Enlarge Small Buttons"]               = "Увеличить мелкие кнопки",
            ["Target Frame Rate"]                   = "Частота кадров (FPS)",
            ["Force Use Xbr"]                       = "Сглаживание xBR",
            ["Texture Filtering"]                   = "Фильтрация текстур",
            ["Container Item Selection"]            = "Захват предметов в сумках",
            ["Visualize Finger Input"]              = "Подсветка нажатий пальцем",
            ["Use Mouse"]                           = "Использовать мышь",
            ["Disable Touchscreen Keyboard"]        = "Отключить клавиатуру Android",
            ["Joystick Size"]                       = "Размер джойстика",
            ["Joystick Opacity"]                    = "Прозрачность джойстика",
            ["Joystick DeadZone"]                   = "Мёртвая зона джойстика",
            ["Joystick Run Threshold"]              = "Порог бега джойстика",
            ["Use Legacy Joystick"]                 = "Старый стиль джойстика",
            ["Joystick Cancels Follow"]             = "Джойстик сбивает следование",
            ["Show Error Details"]                  = "Подробности ошибок",
            ["Use DrawTexture"]                     = "Рендер DrawTexture",
            ["Use Sprite Sheets"]                   = "Использовать спрайт-листы",
            ["Sprite Sheet Size"]                   = "Размер спрайт-листа",
            ["Use Profiler"]                        = "Профилировщик",
            ["Customize Joystick"]                  = "Настроить джойстик",
            ["Login"]                               = "Вход",
            ["Quit"]                                = "Выход",
            ["Show Console"]                        = "Консоль",
            ["Console"]                             = "Консоль",

            // === Вкладки внутриигровых настроек ClassicUO (OptionsGump) ===
            ["General"]                             = "Основные",
            ["Sound"]                               = "Звук",
            ["Sounds"]                              = "Звуковые эффекты",
            ["Video"]                               = "Видео",
            ["Macros"]                              = "Макросы",
            ["Tooltip"]                             = "Подсказки",
            ["Fonts"]                               = "Шрифты",
            ["Speech"]                              = "Речь и чат",
            ["Combat-Spells"]                       = "Бой и магия",
            ["Combat & Spells"]                     = "Бой и магия",
            ["Counters"]                            = "Счётчики",
            ["Info Bar"]                            = "Инфо-панель",
            ["InfoBar"]                             = "Инфо-панель",
            ["Containers"]                          = "Контейнеры",
            ["Experimental"]                        = "Эксперименты",
            ["Ignore List"]                         = "Чёрный список",
            ["Ignore List Manager"]                 = "Чёрный список",

            // === Секции настроек (SettingsSection) ===
            ["Game window"]                         = "Игровое окно",
            ["Mobiles"]                             = "Существа и игроки",
            ["Misc"]                                = "Разное",
            ["Miscellaneous"]                       = "Разное",
            ["Zoom"]                                = "Масштабирование",
            ["Lights"]                              = "Освещение",
            ["Shadows"]                             = "Тени",
            ["Terrain & Statics"]                   = "Ландшафт и статика",
            ["Gumps & Context"]                     = "Окна и контекст",

            // === Кнопки управления окном настроек ===
            ["Apply"]                               = "Применить",
            ["Default"]                             = "По умолчанию",
            ["Cancel"]                              = "Отмена",
            ["OK"]                                  = "ОК",
            ["New macro"]                           = "Новый макрос",
            ["New Macro"]                           = "Новый макрос",
            ["Delete macro"]                        = "Удалить макрос",
            ["Delete Macro"]                        = "Удалить макрос",
            ["Macro name:"]                         = "Имя макроса:",
            ["+ Add item"]                          = "+ Добавить элемент",
            ["Do you want\ndelete it?"]             = "Вы действительно\nхотите удалить?",
            ["Save current settings as defaults for new users"] = "Сохранить как настройки по умолчанию",

            // === Вкладка «Основные» (General) ===
            ["Highlight game objects"]              = "Подсвечивать объекты мира",
            ["Enable pathfinding"]                  = "Автопоиск пути",
            ["Use SHIFT for pathfinding"]           = "Поиск пути только с Shift",
            ["Shift pathfinding"]                   = "Поиск пути только с Shift",
            ["Always run"]                          = "Всегда бежать",
            ["Always run unless hidden"]            = "Всегда бежать (кроме хайда)",
            ["Unless hidden"]                       = "Кроме состояния скрытности",
            ["Auto open doors"]                     = "Автоматически открывать двери",
            ["Auto Open Doors"]                     = "Автоматически открывать двери",
            ["Smooth doors"]                        = "Плавная анимация дверей",
            ["Auto open corpses"]                   = "Автоматически открывать трупы",
            ["Auto Open Corpses"]                   = "Автоматически открывать трупы",
            ["Corpse Open Range:"]                  = "Дистанция открытия трупов:",
            ["Corpse open range:"]                  = "Дистанция открытия трупов:",
            ["Corpse Open Options:"]                = "Условия открытия трупов:",
            ["Corpse open options:"]                = "Условия открытия трупов:",
            ["Skip empty corpses"]                  = "Пропускать пустые трупы",
            ["No color for object out of range"]    = "Не красить объекты вне зоны видимости",
            ["Sallos easy grab"]                    = "Быстрый захват предметов (Sallos)",
            ["Show houses content"]                 = "Показывать содержимое домов",
            ["Enable circle of transparency"]       = "Круг прозрачности вокруг персонажа",
            ["Transparency type:"]                  = "Тип прозрачности:",
            ["Lock game window moving/resizing"]    = "Заблокировать размер/перемещение окна игры",
            ["Game Play Window Position:"]          = "Позиция игрового окна:",
            ["Game Play Window Size:"]              = "Размер игрового окна:",
            ["Always use fullsize game window"]     = "Игровое окно на весь экран",
            ["Borderless window"]                   = "Окно без рамки",
            ["Disable default UO hotkeys"]          = "Отключить стандартные горячие клавиши UO",
            ["Disable TAB (toggle warmode)"]        = "Отключить TAB (боевой режим)",
            ["Disable arrows & numlock arrows (player moving)"] = "Отключить стрелки клавиатуры для движения",
            ["Disable Right+Left Click Automove"]   = "Отключить движение по ЛКМ+ПКМ",
            ["Disable Ctrl + Q/W (messageHistory)"] = "Отключить Ctrl+Q/W (история)",
            ["Disable the Menu Bar"]                = "Скрыть строку меню",
            ["Hold Shift to split stack of items"]  = "Удерживать Shift для разделения стопки",
            ["Hold Shift for Context Menus"]        = "Удерживать Shift для контекстного меню",
            ["Hold Alt to move gumps"]              = "Удерживать Alt для перемещения окон",
            ["Hold ALT key to move gumps"]          = "Удерживать Alt для перемещения окон",
            ["Hold ALT key + right click to close Anchored gumps"] = "Alt + ПКМ для закрытия связанных окон",
            ["Close all Anchored gumps when right click on a group"] = "Закрывать все связанные окна при ПКМ",
            ["Default zoom:"]                       = "Масштаб по умолчанию:",
            ["Enable mousewheel for in game zoom scaling (Ctrl + Scroll)"] = "Масштабирование колесом мыши (Ctrl + Колесо)",
            ["Releasing Ctrl Restores Scale"]       = "Отпускание Ctrl возвращает масштаб",

            // === Вкладка «Контейнеры» (Containers) ===
            ["Scale items inside containers"]       = "Масштабировать предметы внутри сумок",
            ["- Container scale:"]                  = "Масштаб контейнеров:",
            ["Container scale:"]                    = "Масштаб контейнеров:",
            ["Grid loot"]                           = "Лут сеткой (Grid Loot):",
            ["Double click to loot for Grid Loot"]  = "Двойной клик для лута в сетке",
            ["Double click to loot items inside containers"] = "Двойной клик для перемещения в сумку",
            ["Relative drag and drop items in containers"] = "Относительное перетаскивание предметов",
            ["Use large containers gump"]           = "Использовать большие окна контейнеров",
            ["Highlight container when mouse is over a container gump"] = "Подсвечивать сумку при наведении",
            ["Recolor container gump by item hue"]  = "Красить окно сумки в цвет предмета",
            ["Override container gump location"]    = "Позиция открытия контейнеров:",
            ["Backpack style:"]                     = "Стиль рюкзака:",
            ["Backpack Style"]                      = "Стиль рюкзака:",
            ["Select Character Backpack Style"]     = "Стиль рюкзака персонажа:",
            ["Rebuild containers.txt"]              = "Перестроить containers.txt",

            // === Вкладка «Звук» (Sound) ===
            ["Music"]                               = "Музыка",
            ["Login music"]                         = "Музыка при входе",
            ["Play Footsteps"]                      = "Звук шагов",
            ["Combat music"]                        = "Боевая музыка",
            ["Reproduce sounds and music when ClassicUO is not focused"] = "Звук и музыка в фоновом режиме",

            // === Вкладка «Видео» (Video) ===
            ["- FPS:"]                              = "Частота кадров (FPS):",
            ["FPS:"]                                = "Частота кадров (FPS):",
            ["Reduce FPS when game is inactive"]    = "Снижать FPS когда игра в фоне",
            ["Dark nights"]                         = "Тёмные ночи",
            ["Light level"]                         = "Уровень освещения:",
            ["Light Level Setting Type"]            = "Режим настройки освещения:",
            ["Use colored lights"]                  = "Цветное освещение",
            ["Alternative lights"]                  = "Альтернативное освещение",
            ["Trees to stumps"]                     = "Деревья в виде пеньков",
            ["Hide roof tiles"]                     = "Скрывать крыши",
            ["Hide vegetation"]                     = "Скрывать траву и кусты",
            ["Shadows"]                             = "Тени",
            ["Show tree and rock shadows"]          = "Тени деревьев и скал",
            ["Terrain shadows level:"]              = "Уровень теней ландшафта:",
            ["Enable Death Screen"]                 = "Экран смерти",
            ["Black and white mode for dead player"] = "Чёрно-белый режим для призрака",
            ["Black & White mode for dead player"]  = "Чёрно-белый режим для призрака",
            ["Hide chat gradient"]                  = "Скрыть градиент чата",
            ["Animated water effect"]               = "Анимация воды",
            ["Smooth boat movements"]               = "Плавное движение кораблей",
            ["Mark cave tiles"]                     = "Маркировать пещерные тайлы",
            ["Objects alpha fading"]                = "Плавное затухание объектов",
            ["Text alpha fading"]                   = "Плавное затухание текста",
            ["Run mouse in a separate thread"]      = "Курсор мыши в отдельном потоке",

            // === Вкладка «Бой и магия» (Combat & Spells) ===
            ["Hold TAB key for combat"]             = "Удерживать TAB для боя",
            ["Query before attack"]                 = "Подтверждение перед атакой",
            ["Query before performing beneficial acts on Murderers, Criminals, Grays (Monsters/Animals)"] = "Подтверждение лечения ПК и криминалов",
            ["Query before performing beneficial acts on\nMurderers, Criminals, Grays (Monsters/Animals)"] = "Подтверждение лечения ПК,\nкриминалов и серых мобов",
            ["Show HP"]                             = "Полоска здоровья над целями",
            ["mode:"]                               = "Режим отображения:",
            ["Close healthbar gump when:"]          = "Закрывать полоску здоровья когда:",
            ["Show target range indicator"]         = "Индикатор дистанции до цели",
            ["Use new target system"]               = "Новая система прицеливания",
            ["Single-click UI buttons"]             = "Заклинания в 1 клик (без дабл-клика)",
            ["Enable Fast Spells Assign (Ctrl + Alt)"] = "Быстрое назначение спеллов (Ctrl+Alt)",
            ["Show DPS with damage numbers"]        = "Показывать DPS вместе с уроном",
            ["Show buff duration"]                  = "Показывать длительность баффов",
            ["Aura on mouse target"]                = "Аура вокруг выбранной цели",
            ["Aura under feet"]                     = "Аура под ногами",
            ["Custom color aura for party members"] = "Особый цвет ауры для союзников",
            ["Use custom healthbars gump"]          = "Свой интерфейс полосок здоровья",
            ["Use old status gump"]                 = "Классическое окно статуса",
            ["Status Gump and Health Bar are mutually exclusive"] = "Окно статуса и полоска здоровья исключают друг друга",
            ["Save healthbars on logout"]           = "Сохранять полоски здоровья при выходе",
            ["Enable drag-select to open health bars"] = "Рамка выделения для вытягивания полосок здоровья",
            ["Drag-select modifier key"]            = "Клавиша рамки выделения полосок",
            ["Anchored Healthbar"]                  = "Связанные полоски здоровья",
            ["Starting X position of health bars"]  = "Начальная позиция X полосок",
            ["Starting Y position of health bars"]  = "Начальная позиция Y полосок",
            ["Select humanoids only"]               = "Выделять только гуманоидов",
            ["Select hostiles only"]                = "Выделять только врагов",
            ["Highlight invulnerable"]              = "Подсвечивать неуязвимых",
            ["Highlight paralyzed"]                 = "Подсвечивать парализованных",
            ["Highlight poisoned"]                  = "Подсвечивать отравленных",
            ["Enable Overhead Spell Format"]        = "Формат заклинаний над головой",
            ["Enable Overhead Spell Hue"]           = "Цвет заклинаний над головой",
            ["Spell Overhead format: ({power} for powerword - {spell} for spell name)"] = "Формат каста над головой ({power} — слова, {spell} — заклинание)",
            ["Benefic Spell Hue"]                   = "Цвет полезных заклинаний",
            ["Harmful Spell Hue"]                   = "Цвет вредоносных заклинаний",
            ["Neutral Spell Hue"]                   = "Цвет нейтральных заклинаний",

            // === Вкладка «Подсказки» (Tooltip) ===
            ["Use tooltip"]                         = "Включить подсказки к предметам",
            ["Delay before display:"]               = "Задержка перед показом:",
            ["Tooltip zoom:"]                       = "Масштаб подсказок:",
            ["Tooltip background opacity:"]         = "Прозрачность фона подсказок:",
            ["Tooltip font:"]                       = "Шрифт подсказок:",
            ["Tooltip font hue"]                    = "Цвет шрифта подсказок",
            ["Tooltip font!"]                       = "Шрифт подсказок!",

            // === Вкладка «Шрифты и речь» (Fonts / Speech) ===
            ["Override game font"]                  = "Заменить игровой шрифт",
            ["Force Unicode in journal"]            = "Принудительный Unicode в журнале",
            ["Save Journal to file in game folder"] = "Сохранять журнал в файл",
            ["Use alternative journal"]             = "Альтернативный журнал",
            ["Active chat when pressing ENTER"]     = "Активировать чат по нажатию Enter",
            ["Scale speech delay"]                  = "Масштабировать задержку речи",
            ["Hide alliance chat"]                  = "Скрыть чат альянса",
            ["Hide guild chat"]                     = "Скрыть чат гильдии",
            ["Speech Color"]                        = "Цвет обычной речи",
            ["Emote Color"]                         = "Цвет эмоций (Emote)",
            ["Party Message Color"]                 = "Цвет сообщений группы",
            ["Guild Message Color"]                 = "Цвет сообщений гильдии",
            ["Alliance Message Color"]              = "Цвет сообщений альянса",
            ["Whisper Color"]                       = "Цвет шёпота",
            ["Yell Color"]                          = "Цвет крика",
            ["Speech font:"]                        = "Шрифт речи:",
            ["Randomize speech hues"]               = "Случайные цвета речи",
            ["Use additional buttons to activate chat: ! ; : / \\\\ , . [ | -"] = "Активация чата символами: ! ; : / \\\\ , . [ | -",
            ["Use `Shift+Enter` to send message without closing chat"] = "Shift+Enter для отправки без закрытия чата",

            // === Цвета целей и существ ===
            ["Innocent Color"]                      = "Цвет мирных (синих)",
            ["Friend Color"]                        = "Цвет друзей (зелёных)",
            ["Criminal Color"]                      = "Цвет криминалов (серых)",
            ["Can Be Attacked Color"]               = "Цвет атакуемых целей",
            ["Enemy Color"]                         = "Цвет врагов (оранжевых)",
            ["Murderer Color"]                      = "Цвет ПК (красных)",
            ["Invulnerable Color"]                  = "Цвет неуязвимых (жёлтых)",
            ["Paralyzed Color"]                     = "Цвет парализованных",
            ["Poisoned Color"]                      = "Цвет отравленных",
            ["Party Aura Color"]                    = "Цвет ауры группы",

            // === Вкладка «Счётчики и инфо-панель» (Counters / InfoBar) ===
            ["Enable Counters"]                     = "Включить счётчики",
            ["Highlight On Use"]                    = "Подсвечивать при использовании",
            ["Show Info Bar"]                       = "Показывать инфо-панель",
            ["Data highlight type:"]                = "Тип подсветки данных:",
            ["Counter Layout:"]                     = "Расположение счётчиков:",
            ["Columns:"]                            = "Столбцы:",
            ["Rows:"]                               = "Строки:",
            ["Cell size:"]                          = "Размер ячейки:",
            ["Data"]                                = "Данные",
            ["Label"]                               = "Подпись",
            ["Color"]                               = "Цвет",
            ["Enable abbreviated amount values\nwhen amount is or exceeds"] = "Сокращать большие числа\nесли значение превышает",
            ["Highlight red when amount is below"]  = "Подсвечивать красным если меньше",

            // === Сообщения и уведомления ===
            ["Inform when stats change"]            = "Сообщать об изменении характеристик",
            ["Inform when skills change by (in tenths)"] = "Сообщать об изменении навыков (в десятых)",
            ["Show incoming new corpses"]           = "Сообщать о появлении трупов",
            ["Show incoming new mobiles"]           = "Сообщать о появлении персонажей",
            ["Show gump for party invites"]         = "Окно приглашения в группу",
            ["Hide \"Screenshot stored in\" message"] = "Скрыть сообщение о скриншоте",
            ["Use standard skills gump"]            = "Стандартное окно навыков",
            ["Opaque background"]                   = "Непрозрачный фон",
            ["That's ClassicUO!"]                   = "Это ClassicUO!",
        };

        // Словарь перевода значений перечислений и вариантов выпадающих списков
        private static readonly Dictionary<string, string> EnumValues = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            // Общие тумблеры
            ["Off"]                                 = "Выкл",
            ["On"]                                  = "Вкл",
            ["None"]                                = "Нет",
            ["Both"]                                = "Оба",
            ["Grid loot only"]                      = "Только сетка",
            ["Percentage"]                          = "Проценты",
            ["Line"]                                = "Полоса",
            ["Always"]                              = "Всегда",
            ["Less than 100 %"]                     = "Меньше 100%",
            ["Smart"]                               = "Умный режим",
            ["Mobile is dead"]                      = "Если цель погибла",
            ["Mobile out of range"]                 = "Вне зоны видимости",
            ["Near container position"]             = "Рядом с контейнером",
            ["Top right"]                           = "Правый верхний угол",
            ["Last dragged position"]               = "В месте перемещения",
            ["Remember every container"]            = "Запоминать для каждого",
            ["Default"]                             = "Стандартный",
            ["Suede"]                               = "Замшевый",
            ["Polar Bear"]                          = "Белый медведь",
            ["PolarBear"]                           = "Белый медведь",
            ["Ghoul Skin"]                          = "Кожа упыря",
            ["GhoulSkin"]                           = "Кожа упыря",
            ["In war mode"]                         = "В боевом режиме",
            ["Warmode"]                             = "В боевом режиме",
            ["CtrlShift"]                           = "Ctrl + Shift",
            ["Ctrl + Shift"]                        = "Ctrl + Shift",
            ["Ctrl+Shift"]                          = "Ctrl + Shift",
            ["Absolute"]                            = "Абсолютный",
            ["Minimum"]                             = "Минимальный",
            ["Text color"]                          = "Цвет текста",
            ["Colored bars"]                        = "Цветные полосы",
            ["Normal fields"]                       = "Обычные поля",
            ["Static fields"]                       = "Статичные поля",
            ["Tile fields"]                         = "Тайловые поля",
            ["Not Targeting"]                       = "Если нет таргета",
            ["Not Hiding"]                          = "Если не в хайде",
            ["Shift"]                               = "Shift",
            ["Ctrl"]                                = "Ctrl",
            ["Full"]                                = "Полная",
            ["Gradient"]                            = "Градиент",
            ["ASCII"]                               = "ASCII",
            ["Unicode"]                             = "Unicode",

            // Мобильные опции Unity
            ["Russian"]                             = "Русский",
            ["English"]                             = "English",
            ["Coarse"]                              = "Широкий",
            ["Fine"]                                = "Точный",
            ["Sharp"]                               = "Чёткая",
            ["Smooth"]                              = "Сглаженная",
            ["Small"]                               = "Маленький",
            ["Normal"]                              = "Обычный",
            ["Large"]                               = "Большой",
            ["Custom"]                              = "Настроенный",
            ["VeryLow"]                             = "Очень низкая",
            ["Low"]                                 = "Низкая",
            ["Medium"]                              = "Средняя",
            ["High"]                                = "Высокая",
            ["InGameFPS"]                           = "Как в игре",
            ["Disabled"]                            = "Отключено",
            ["Enabled"]                             = "Включено",
        };

        /// <summary>
        /// Перевести текст с английского на русский (если активен русский язык).
        /// Если язык английский или перевод не найден — возвращает исходный текст.
        /// </summary>
        public static string Translate(string text, string language = null)
        {
            if (string.IsNullOrEmpty(text))
            {
                return text;
            }

            string lang = language != null ? MobileUiStrings.Normalize(language) : CurrentLanguage;

            if (lang != MobileUiStrings.Ru)
            {
                return text;
            }

            // Прямое совпадение
            if (Strings.TryGetValue(text, out string translated))
            {
                return translated;
            }

            // Пробуем без начальных/конечных пробелов
            string trimmed = text.Trim();

            if (Strings.TryGetValue(trimmed, out translated))
            {
                // Сохраняем начальные/конечные пробелы оригинала
                int startSpaces = text.Length - text.TrimStart().Length;
                int endSpaces = text.Length - text.TrimEnd().Length;

                string prefix = startSpaces > 0 ? text.Substring(0, startSpaces) : string.Empty;
                string suffix = endSpaces > 0 ? text.Substring(text.Length - endSpaces) : string.Empty;

                return prefix + translated + suffix;
            }

            // Если не найдено в основном словаре — пробуем словарь enum-значений
            if (EnumValues.TryGetValue(trimmed, out translated))
            {
                return translated;
            }

            return text;
        }

        /// <summary>
        /// Перевести строковое имя enum-значения в мобильном меню Unity.
        /// </summary>
        public static string TranslateEnumValue(Type enumType, string rawName)
        {
            if (string.IsNullOrEmpty(rawName))
            {
                return rawName;
            }

            if (!IsRussian)
            {
                return rawName;
            }

            // Проверяем квалифицированное имя: EnumType.ValueName
            if (enumType != null)
            {
                string qualifiedKey = enumType.Name + "." + rawName;

                if (EnumValues.TryGetValue(qualifiedKey, out string translated))
                {
                    return translated;
                }
            }

            // Проверяем по голому имени
            if (EnumValues.TryGetValue(rawName, out string val))
            {
                return val;
            }

            return rawName;
        }
    }
}
