// SPDX-License-Identifier: BSD-2-Clause
//
// Мобильный интерфейс: сенсорная книга рун (Runebook).
//
// Перехватывает серверное окно рунбука (0xB0 / 0xDD) и вместо микроскопических
// кнопок 10x10 пикселей открывает крупный удобный мобильный интерфейс:
//   * крупные кнопки быстрого перемещения под большой палец:
//     [ ⚡ Заряд ]  [ Рекол ]  [ Гейт ]
//   * индикатор зарядов книги и кнопка быстрой перезарядки [ Зарядить ]
//   * кнопки выбора руны по умолчанию [ ⭐ ] и извлечения руны [ ⏏ ]

using System;
using System.Collections.Generic;
using System.Linq;
using ClassicUO.Game;
using ClassicUO.Game.GameObjects;
using ClassicUO.Game.Managers;
using ClassicUO.Game.UI.Controls;
using ClassicUO.Game.UI.Gumps;
using ClassicUO.Input;
using ClassicUO.Renderer;
using Microsoft.Xna.Framework;

namespace ClassicUO.MobileUI
{
    internal sealed class MobileRunebookGump : Gump
    {
        private int WinWidth => MobileUiController.GetWindowWidth();
        private int WinHeight => MobileUiController.GetWindowHeight();
        private int RowHeight => MobileUiController.GetRowHeight();
        private const int HeaderHeight = 52;
        private const int FooterHeight = 36;

        public struct RuneEntry
        {
            public int Index;
            public string Name;
            public int ChargeButton;
            public int RecallButton;
            public int GateButton;
            public int DefaultButton;
            public int DropButton;
        }

        private readonly List<RuneEntry> _runes = new List<RuneEntry>();
        private readonly int _charges;
        private readonly int _maxCharges;
        private ScrollArea _list;

        public MobileRunebookGump(World world, uint sender, uint gumpID, string layout, string[] lines)
            : base(world, sender, gumpID)
        {
            CanMove = true;
            AcceptMouseInput = true;
            CanCloseWithRightClick = true;

            ParseRunebook(layout, lines, out _charges, out _maxCharges, _runes);
            Build();
        }

        public override GumpType GumpType => GumpType.None;

        /// <summary>
        /// Проверка: является ли серверный гумп окном книги рун (Runebook).
        /// </summary>
        public static bool IsRunebook(string layout, string[] lines)
        {
            if (string.IsNullOrEmpty(layout))
            {
                return false;
            }

            // Графика 2220 (0x08AC) — классическая открытая книга рун в UO
            bool hasGraphic = layout.Contains("2220");

            if (!hasGraphic)
            {
                return false;
            }

            // Наличие кнопки прыжка по заряду (601) или упоминания зарядов в тексте
            bool hasChargeBtn = layout.Contains("601");
            bool hasChargeText = lines != null && lines.Any(l =>
                l != null && (l.IndexOf("Charges", StringComparison.OrdinalIgnoreCase) >= 0 ||
                              l.IndexOf("Заряд", StringComparison.OrdinalIgnoreCase) >= 0));

            return hasChargeBtn || hasChargeText;
        }

        private static void ParseRunebook(string layout, string[] lines, out int charges, out int maxCharges, List<RuneEntry> runes)
        {
            charges = 0;
            maxCharges = 0;

            if (lines != null)
            {
                // Стандартное расположение в Sphere / POL / RunUO:
                // lines[3] = текущие заряды, lines[4] = макс. заряды
                if (lines.Length > 3 && int.TryParse(lines[3], out int cur))
                {
                    charges = cur;
                }

                if (lines.Length > 4 && int.TryParse(lines[4], out int max))
                {
                    maxCharges = max;
                }

                // Имена рун начинаются с lines[7] (до 16 рун)
                for (int i = 0; i < 16; i++)
                {
                    int textIndex = 7 + i;

                    if (textIndex < lines.Length)
                    {
                        string name = lines[textIndex]?.Trim();

                        if (!string.IsNullOrEmpty(name) && !name.Equals("Empty", StringComparison.OrdinalIgnoreCase))
                        {
                            runes.Add(new RuneEntry
                            {
                                Index = i,
                                Name = name,
                                ChargeButton = 601 + i,
                                RecallButton = 2 + i * 2,
                                GateButton = 3 + i * 2,
                                DefaultButton = 501 + i,
                                DropButton = 81 + i
                            });
                        }
                    }
                }
            }
        }

        public void Rebuild()
        {
            Clear();
            Children.Clear();

            if (X + WinWidth > UIManager.Width)
            {
                X = Math.Max(0, UIManager.Width - WinWidth);
            }
            if (Y + WinHeight > UIManager.Height)
            {
                Y = Math.Max(0, UIManager.Height - WinHeight);
            }

            Build();
        }

        private void Build()
        {
            Add(new ResizePic(0x0A3C)
            {
                X = 0,
                Y = 0,
                Width = WinWidth,
                Height = WinHeight
            });

            Width = WinWidth;
            Height = WinHeight;

            // Заголовок
            Add(new Label(
                MobileUiController.T("runebook"),
                true,
                0x0386,
                240,
                255,
                FontStyle.BlackBorder)
            {
                X = 16,
                Y = 14
            });

            // Индикатор зарядов
            string chargeText = $"{MobileUiController.T("charges")}: {_charges} / {_maxCharges}";
            Add(new Label(
                chargeText,
                true,
                0x0035,
                200,
                255,
                FontStyle.BlackBorder)
            {
                X = 16,
                Y = 32
            });

            // Кнопка подзарядки
            Add(new NiceButton(WinWidth - 216, 12, 80, 26, ButtonAction.Activate, MobileUiController.T("recharge"))
            {
                ButtonParameter = 800,
                IsSelectable = false
            });

            // Кнопка уменьшения масштаба [ − ]
            Add(new NiceButton(WinWidth - 130, 12, 30, 26, ButtonAction.Activate, "−")
            {
                ButtonParameter = 201,
                IsSelectable = false
            });

            // Кнопка увеличения масштаба [ + ]
            Add(new NiceButton(WinWidth - 96, 12, 30, 26, ButtonAction.Activate, "+")
            {
                ButtonParameter = 202,
                IsSelectable = false
            });

            // Кнопка закрытия [X]
            Add(new NiceButton(WinWidth - 62, 12, 52, 26, ButtonAction.Activate, MobileUiController.T("close"))
            {
                ButtonParameter = 0,
                IsSelectable = false
            });

            // Уголок переключения размера внизу справа [ ⇲ ]
            Add(new NiceButton(WinWidth - 28, WinHeight - 28, 24, 24, ButtonAction.Activate, "⇲")
            {
                ButtonParameter = 203,
                IsSelectable = false
            });

            // Область со списком рун
            _list = new ScrollArea(10, HeaderHeight, WinWidth - 20, WinHeight - HeaderHeight - FooterHeight, true)
            {
                AcceptMouseInput = true
            };

            Add(_list);

            Fill();
        }

        private void Fill()
        {
            _list.Clear();

            if (_runes.Count == 0)
            {
                _list.Add(new Label(
                    MobileUiController.T("runebook_empty"),
                    true,
                    0x03B2,
                    WinWidth - 40,
                    255,
                    FontStyle.BlackBorder)
                {
                    X = 16,
                    Y = 20
                });

                return;
            }

            int y = 0;

            for (int i = 0; i < _runes.Count; i++)
            {
                var rune = _runes[i];
                AddRuneRow(rune, y);
                y += RowHeight;
            }
        }

        private void AddRuneRow(RuneEntry rune, int y)
        {
            int rowWidth = WinWidth - 44;

            // Фоновая подложка строки
            var bg = new AlphaBlendControl(0.35f)
            {
                X = 0,
                Y = y,
                Width = rowWidth,
                Height = RowHeight - 4
            };
            _list.Add(bg);

            // Метка номера руны
            _list.Add(new Label(
                $"{rune.Index + 1}.",
                true,
                0x0386,
                30,
                255,
                FontStyle.BlackBorder)
            {
                X = 6,
                Y = y + 12
            });

            // Название руны
            int maxNameWidth = Math.Max(100, rowWidth - 330);
            _list.Add(new Label(
                rune.Name,
                true,
                0xFFFF,
                maxNameWidth,
                255,
                FontStyle.BlackBorder)
            {
                X = 32,
                Y = y + 12
            });

            // 1. Кнопка «⚡ Заряд» (мгновенный рекол за счёт заряда книги, без затрат маны)
            _list.Add(new NiceButton(rowWidth - 290, y + 6, 76, 32, ButtonAction.Activate, MobileUiController.T("charge_recall"))
            {
                ButtonParameter = rune.ChargeButton,
                IsSelectable = false
            });

            // 2. Кнопка «Рекол» (каст заклинания Recall)
            _list.Add(new NiceButton(rowWidth - 210, y + 6, 68, 32, ButtonAction.Activate, MobileUiController.T("recall"))
            {
                ButtonParameter = rune.RecallButton,
                IsSelectable = false
            });

            // 3. Кнопка «Гейт» (каст заклинания Gate Travel)
            _list.Add(new NiceButton(rowWidth - 138, y + 6, 62, 32, ButtonAction.Activate, MobileUiController.T("gate"))
            {
                ButtonParameter = rune.GateButton,
                IsSelectable = false
            });

            // 4. Кнопка «⭐» (сделать руной по умолчанию)
            _list.Add(new NiceButton(rowWidth - 72, y + 6, 32, 32, ButtonAction.Activate, "⭐")
            {
                ButtonParameter = rune.DefaultButton,
                IsSelectable = false
            });

            // 5. Кнопка «⏏» (извлечь руну из книги)
            _list.Add(new NiceButton(rowWidth - 36, y + 6, 32, 32, ButtonAction.Activate, "⏏")
            {
                ButtonParameter = rune.DropButton,
                IsSelectable = false
            });
        }

        public override void OnButtonClick(int buttonID)
        {
            if (buttonID == 201)
            {
                MobileUiController.PrevWindowPreset();
                return;
            }

            if (buttonID == 202)
            {
                MobileUiController.NextWindowPreset();
                return;
            }

            if (buttonID == 203)
            {
                MobileUiController.CycleWindowPreset();
                return;
            }

            base.OnButtonClick(buttonID);
        }
    }
}
