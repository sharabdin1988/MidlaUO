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

        private ResizePic _background;
        private Button _resizeButton;
        private NiceButton _btnRecharge;
        private NiceButton _btnClose;
        private NiceButton _btnPlus;
        private NiceButton _btnMinus;
        private bool _resizing;
        private Point _startSize;

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
            Width = WinWidth;
            Height = WinHeight;

            Clear();
            Children.Clear();

            if (X + Width > MobileUiController.ScreenWidth)
            {
                X = Math.Max(0, MobileUiController.ScreenWidth - Width);
            }
            if (Y + Height > MobileUiController.ScreenHeight)
            {
                Y = Math.Max(0, MobileUiController.ScreenHeight - Height);
            }

            Build();
        }

        private void Build()
        {
            if (Width <= 0 || Height <= 0)
            {
                Width = WinWidth;
                Height = WinHeight;
            }

            _background = new ResizePic(0x0A3C)
            {
                X = 0,
                Y = 0,
                Width = Width,
                Height = Height
            };
            Add(_background);

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
            _btnRecharge = new NiceButton(Width - 216, 12, 80, 26, ButtonAction.Activate, MobileUiController.T("recharge"))
            {
                ButtonParameter = 800,
                IsSelectable = false
            };
            Add(_btnRecharge);

            // Кнопка уменьшения масштаба [ − ]
            _btnMinus = new NiceButton(Width - 130, 12, 30, 26, ButtonAction.Activate, "−")
            {
                ButtonParameter = 201,
                IsSelectable = false
            };
            Add(_btnMinus);

            // Кнопка увеличения масштаба [ + ]
            _btnPlus = new NiceButton(Width - 96, 12, 30, 26, ButtonAction.Activate, "+")
            {
                ButtonParameter = 202,
                IsSelectable = false
            };
            Add(_btnPlus);

            // Кнопка закрытия [X]
            _btnClose = new NiceButton(Width - 62, 12, 52, 26, ButtonAction.Activate, MobileUiController.T("close"))
            {
                ButtonParameter = 0,
                IsSelectable = false
            };
            Add(_btnClose);

            // Кружочек изменения размера справа внизу над углом (как в главном окне ClassicUO / ResizableGump: 0x837/0x838)
            _resizeButton = new Button(203, 0x837, 0x838, 0x838)
            {
                ButtonAction = ButtonAction.Activate
            };

            if (UnityEngine.Application.isMobilePlatform)
            {
                _resizeButton.Width *= 2;
                _resizeButton.Height *= 2;
                _resizeButton.ContainsByBounds = true;
            }

            _resizeButton.X = Width - _resizeButton.Width + 2;
            _resizeButton.Y = Height - _resizeButton.Height + 2;

            _resizeButton.MouseDown += (sender, e) =>
            {
                _resizing = true;
                _startSize = new Point(Width, Height);
            };

            _resizeButton.MouseUp += (sender, e) =>
            {
                if (_resizing)
                {
                    _resizing = false;
                    _startSize = new Point(Width, Height);
                    Fill();
                }
            };

            Add(_resizeButton);

            // Область со списком рун
            _list = new ScrollArea(10, HeaderHeight, Width - 20, Math.Max(50, Height - HeaderHeight - FooterHeight), true)
            {
                AcceptMouseInput = true
            };

            Add(_list);

            Fill();
        }

        private void OnResize()
        {
            if (_background != null)
            {
                _background.Width = Width;
                _background.Height = Height;
            }

            if (_resizeButton != null)
            {
                _resizeButton.X = Width - _resizeButton.Width + 2;
                _resizeButton.Y = Height - _resizeButton.Height + 2;
            }

            if (_btnClose != null) _btnClose.X = Width - 62;
            if (_btnPlus != null) _btnPlus.X = Width - 96;
            if (_btnMinus != null) _btnMinus.X = Width - 130;
            if (_btnRecharge != null) _btnRecharge.X = Width - 216;

            if (_list != null)
            {
                _list.Width = Width - 20;
                _list.Height = Math.Max(50, Height - HeaderHeight - FooterHeight);
            }
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
                    Width - 40,
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
            int rowWidth = Width - 44;

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
                26,
                255,
                FontStyle.BlackBorder)
            {
                X = 4,
                Y = y + 12
            });

            // Название руны
            int maxNameWidth = Math.Max(70, rowWidth - 365);
            _list.Add(new Label(
                rune.Name,
                true,
                0xFFFF,
                maxNameWidth,
                255,
                FontStyle.BlackBorder)
            {
                X = 30,
                Y = y + 12
            });

            // 0. Кнопка «📌» (вынести точку телепорта на экран в виде быстрой кнопки)
            var btnPin = new NiceButton(rowWidth - 326, y + 6, 32, 32, ButtonAction.Activate, "📌")
            {
                IsSelectable = false
            };
            btnPin.MouseUp += (s, e) =>
            {
                if (e.Button == MouseButtonType.Left)
                {
                    PinRuneToScreen(rune);
                }
            };
            _list.Add(btnPin);

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

        private void PinRuneToScreen(RuneEntry rune)
        {
            var world = World;
            if (world == null)
            {
                return;
            }

            MobileRuneButtonGump existing = null;
            int count = 0;

            for (LinkedListNode<Gump> node = UIManager.Gumps.First; node != null; node = node.Next)
            {
                if (node.Value is MobileRuneButtonGump btn)
                {
                    count++;
                    if (btn.BookSerial == LocalSerial && btn.ButtonID == rune.ChargeButton)
                    {
                        existing = btn;
                    }
                }
            }

            if (existing != null)
            {
                existing.BringOnTop();
                return;
            }

            int screenX = 120 + (count % 3) * 94;
            int screenY = 180 + (count / 3) * 50;

            var gump = new MobileRuneButtonGump(world, LocalSerial, rune.ChargeButton, rune.Name, screenX, screenY);
            UIManager.Add(gump);
        }

        public override void Update()
        {
            base.Update();

            if (IsDisposed)
            {
                return;
            }

            if (_resizing)
            {
                Point offset = Mouse.LDragOffset;
                if (offset != Point.Zero)
                {
                    int w = _startSize.X + offset.X;
                    int h = _startSize.Y + offset.Y;

                    w = Math.Max(420, Math.Min(MobileUiController.ScreenWidth, w));
                    h = Math.Max(320, Math.Min(MobileUiController.ScreenHeight, h));

                    if (w != Width || h != Height)
                    {
                        Width = w;
                        Height = h;
                        OnResize();
                    }
                }
            }
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
