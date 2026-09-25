// SPDX-License-Identifier: BSD-2-Clause
//
// Мобильный интерфейс: сенсорная книга заклинаний (Spellbook).
//
// Заменяет мелкую книгу 1998 года крупным сенсорным интерфейсом:
//   * фильтры по кругам магии (Круги 1–8) и вкладка «✈ Быстрые» (Recall, Gate, Heal, Cure...);
//   * крупные иконки заклинаний и список реагентов;
//   * кнопка мгновенного каста [ КАСТ ] (и каст по тапу на всю строку);
//   * кнопка [ + Экран ] — выносит иконку заклинания прямо на игровой экран (HUD).

using System;
using System.Collections.Generic;
using System.Linq;
using ClassicUO.Assets;
using ClassicUO.Game;
using ClassicUO.Game.Data;
using ClassicUO.Game.Managers;
using ClassicUO.Game.UI.Controls;
using ClassicUO.Game.UI.Gumps;
using ClassicUO.Input;
using ClassicUO.Renderer;
using Microsoft.Xna.Framework;

namespace ClassicUO.MobileUI
{
    internal sealed class MobileSpellbookGump : Gump
    {
        private int WinWidth => MobileUiController.GetWindowWidth();
        private int WinHeight => MobileUiController.GetWindowHeight();
        private int RowHeight => MobileUiController.GetRowHeight();
        private const int HeaderHeight = 44;
        private const int TabsHeight = 36;
        private const int FooterHeight = 34;

        private readonly uint _bookSerial;
        private ScrollArea _list;
        private Label _summary;
        private DateTime _lastRefresh = DateTime.MinValue;
        private int _selectedTab = 0; // 0: Быстрые, 1..8: Круги 1..8, 9: Все
        private readonly List<NiceButton> _tabButtons = new List<NiceButton>();

        // Быстрые/жизненно важные заклинания (индексы 0..63)
        // 31: Recall, 51: Gate Travel, 21: Teleport, 44: Mark,
        // 28: Greater Heal, 3: Heal, 10: Cure, 24: Arch Cure,
        // 16: Bless, 14: Protection, 6: Reactive Armor, 42: Invisibility, 35: Magic Reflection
        private static readonly int[] QuickSpellIndices = new int[]
        {
            31, // Recall (Круг 4)
            51, // Gate Travel (Круг 7)
            21, // Teleport (Круг 3)
            44, // Mark (Круг 6)
            28, // Greater Heal (Круг 4)
            3,  // Heal (Круг 1)
            10, // Cure (Круг 2)
            24, // Arch Cure (Круг 4)
            16, // Bless (Круг 3)
            14, // Protection (Круг 2)
            6,  // Reactive Armor (Круг 1)
            42, // Invisibility (Круг 6)
            35, // Magic Reflection (Круг 5)
        };

        public MobileSpellbookGump(World world, uint bookSerial) : base(world, 0, 0)
        {
            _bookSerial = bookSerial;

            CanMove = true;
            AcceptMouseInput = true;
            CanCloseWithRightClick = true;

            Build();
        }

        public override GumpType GumpType => GumpType.None;

        public uint BookSerial => _bookSerial;

        private SpellbookGump Source
        {
            get
            {
                for (LinkedListNode<Gump> node = UIManager.Gumps.First; node != null; node = node.Next)
                {
                    if (node.Value is SpellbookGump sg && sg.LocalSerial == _bookSerial && !sg.IsDisposed)
                    {
                        return sg;
                    }
                }

                return null;
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
                MobileUiController.T("spellbook"),
                true,
                0x0386,
                200,
                255,
                FontStyle.BlackBorder)
            {
                X = 16,
                Y = 12
            });

            _summary = new Label("", true, 0x03B2, 220, 255, FontStyle.BlackBorder)
            {
                X = 16,
                Y = 28
            };
            Add(_summary);

            // Кнопка уменьшения масштаба [ − ]
            Add(new NiceButton(WinWidth - 126, 10, 30, 26, ButtonAction.Activate, "−")
            {
                ButtonParameter = 201,
                IsSelectable = false
            });

            // Кнопка увеличения масштаба [ + ]
            Add(new NiceButton(WinWidth - 94, 10, 30, 26, ButtonAction.Activate, "+")
            {
                ButtonParameter = 202,
                IsSelectable = false
            });

            // Кнопка закрытия [ ✕ ]
            Add(new NiceButton(WinWidth - 62, 10, 52, 26, ButtonAction.Activate, MobileUiController.T("close"))
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

            // Панель вкладок: [ ✈ Быстрые ] [ 1 ] .. [ 8 ] [ Все ]
            BuildTabs();

            // Область со списком заклинаний
            int listTop = HeaderHeight + TabsHeight;
            int listHeight = WinHeight - listTop - FooterHeight;

            _list = new ScrollArea(10, listTop, WinWidth - 20, listHeight, true)
            {
                AcceptMouseInput = true
            };

            Add(_list);

            Fill();
        }

        private void BuildTabs()
        {
            _tabButtons.Clear();

            int y = HeaderHeight + 2;
            int x = 12;

            // Вкладка «✈ Быстрые»
            int quickWidth = WinWidth >= 560 ? 92 : 80;
            var btnQuick = new NiceButton(x, y, quickWidth, 26, ButtonAction.Activate, MobileUiController.T("tab_quick"))
            {
                ButtonParameter = 100,
                IsSelected = _selectedTab == 0
            };
            _tabButtons.Add(btnQuick);
            Add(btnQuick);
            x += quickWidth + 4;

            // Вкладки кругов 1..8
            int circleWidth = WinWidth >= 680 ? 44 : (WinWidth >= 560 ? 35 : 28);
            for (int circle = 1; circle <= 8; circle++)
            {
                int tabIndex = circle;
                var btn = new NiceButton(x, y, circleWidth, 26, ButtonAction.Activate, circle.ToString())
                {
                    ButtonParameter = 100 + tabIndex,
                    IsSelected = _selectedTab == tabIndex
                };
                _tabButtons.Add(btn);
                Add(btn);
                x += circleWidth + 3;
            }

            // Вкладка «Все»
            int allWidth = WinWidth >= 560 ? 52 : 44;
            var btnAll = new NiceButton(x, y, allWidth, 26, ButtonAction.Activate, MobileUiController.T("tab_all"))
            {
                ButtonParameter = 109,
                IsSelected = _selectedTab == 9
            };
            _tabButtons.Add(btnAll);
            Add(btnAll);
        }

        private void SelectTab(int tabIndex)
        {
            _selectedTab = tabIndex;

            for (int i = 0; i < _tabButtons.Count; i++)
            {
                _tabButtons[i].IsSelected = (i == tabIndex);
            }

            Fill();
        }

        public override void OnButtonClick(int buttonID)
        {
            if (buttonID == 0)
            {
                // Закрыть окно и связанный исходный гумп
                Source?.Dispose();
                Dispose();
                return;
            }

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

            if (buttonID >= 100 && buttonID <= 109)
            {
                SelectTab(buttonID - 100);
                return;
            }

            base.OnButtonClick(buttonID);
        }

        private void Fill()
        {
            _list.Clear();

            var book = Source;

            if (book == null)
            {
                _summary.Text = MobileUiController.T("spellbook_wait");
                return;
            }

            List<int> spellIndicesToShow = new List<int>();

            if (_selectedTab == 0)
            {
                // Быстрые заклинания
                foreach (int idx in QuickSpellIndices)
                {
                    if (book.MobileHasSpell(idx))
                    {
                        spellIndicesToShow.Add(idx);
                    }
                }
            }
            else if (_selectedTab >= 1 && _selectedTab <= 8)
            {
                // Круг магии 1..8 (по 8 заклинаний на круг)
                int start = (_selectedTab - 1) * 8;
                int end = Math.Min(start + 8, book.MobileSpellSlots);

                for (int i = start; i < end; i++)
                {
                    if (book.MobileHasSpell(i))
                    {
                        spellIndicesToShow.Add(i);
                    }
                }
            }
            else
            {
                // Все заклинания в книге
                for (int i = 0; i < book.MobileSpellSlots; i++)
                {
                    if (book.MobileHasSpell(i))
                    {
                        spellIndicesToShow.Add(i);
                    }
                }
            }

            int countTotal = 0;
            for (int i = 0; i < book.MobileSpellSlots; i++)
            {
                if (book.MobileHasSpell(i)) countTotal++;
            }

            _summary.Text = string.Format(MobileUiController.T("spellbook_summary"), countTotal);

            if (spellIndicesToShow.Count == 0)
            {
                _list.Add(new Label(
                    MobileUiController.T("spellbook_wait"),
                    true,
                    0x03B2,
                    WinWidth - 60,
                    255,
                    FontStyle.BlackBorder)
                {
                    X = 16,
                    Y = 16
                });
                return;
            }

            int y = 0;

            foreach (int index in spellIndicesToShow)
            {
                AddSpellRow(book, index, y);
                y += RowHeight;
            }
        }

        private void AddSpellRow(SpellbookGump book, int index, int y)
        {
            int rowWidth = WinWidth - 44;

            // Фоновая подложка
            var bg = new AlphaBlendControl(0.35f)
            {
                X = 0,
                Y = y,
                Width = rowWidth,
                Height = RowHeight - 4
            };
            _list.Add(bg);

            // Клик по строке запускает каст
            var hit = new HitBox(0, y, rowWidth - 170, RowHeight - 4, null, 0f);
            _list.Add(hit);

            hit.MouseUp += (sender, e) =>
            {
                if (e.Button == MouseButtonType.Left)
                {
                    GameActions.CastSpellFromBook(index, _bookSerial);
                }
            };

            // Иконка заклинания
            ushort iconGraphic = (ushort)(0x08C0 + index);
            var iconPic = new GumpPic(6, y + 2, iconGraphic, 0)
            {
                AcceptMouseInput = false
            };
            _list.Add(iconPic);

            // Получаем имя, круг и реагенты
            string name = null;
            string reagents = null;

            try
            {
                book.MobileGetSpellNames(index, out name, out reagents);
            }
            catch (Exception)
            {
                name = MobileUiController.T("spell") + " #" + (index + 1);
            }

            int circle = (index / 8) + 1;
            var spellDef = book.MobileGetSpellDefinition(index);
            int manaCost = spellDef?.ManaCost ?? 0;

            // Название заклинания
            _list.Add(new Label(
                name ?? ("#" + (index + 1)),
                true,
                0xFFFF,
                240,
                255,
                FontStyle.BlackBorder)
            {
                X = 56,
                Y = y + 6
            });

            // Описание: круг, мана, реагенты
            string details = $"{MobileUiController.T("circle")} {circle} · {MobileUiController.T("mana")}: {manaCost}";
            if (!string.IsNullOrEmpty(reagents))
            {
                details += $" · {reagents}";
            }

            _list.Add(new Label(
                details,
                true,
                0x0386,
                250,
                255,
                FontStyle.BlackBorder)
            {
                X = 56,
                Y = y + 26
            });

            // Кнопка «+ Экран» (вынести на игровой HUD)
            int spellIdxCopy = index;
            var btnPin = new NiceButton(rowWidth - 162, y + 8, 76, 32, ButtonAction.Activate, MobileUiController.T("pin"))
            {
                IsSelectable = false
            };
            btnPin.MouseUp += (s, e) =>
            {
                if (e.Button == MouseButtonType.Left)
                {
                    PinSpellToScreen(spellIdxCopy);
                }
            };
            _list.Add(btnPin);

            // Кнопка «КАСТ»
            var btnCast = new NiceButton(rowWidth - 80, y + 8, 74, 32, ButtonAction.Activate, MobileUiController.T("cast"))
            {
                IsSelectable = false
            };
            btnCast.MouseUp += (s, e) =>
            {
                if (e.Button == MouseButtonType.Left)
                {
                    GameActions.CastSpellFromBook(spellIdxCopy, _bookSerial);
                }
            };
            _list.Add(btnCast);
        }

        private void PinSpellToScreen(int index)
        {
            var book = Source;
            if (book == null) return;

            var def = book.MobileGetSpellDefinition(index);
            if (def == null) return;

            UseSpellButtonGump existing = null;
            int count = 0;
            for (LinkedListNode<Gump> node = UIManager.Gumps.First; node != null; node = node.Next)
            {
                if (node.Value is UseSpellButtonGump btn)
                {
                    count++;
                    if (btn.SpellID == def.ID)
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

            int screenX = 120 + (count % 4) * 50;
            int screenY = 120 + (count / 4) * 50;

            var gump = new UseSpellButtonGump(World, def)
            {
                X = screenX,
                Y = screenY
            };

            UIManager.Add(gump);
        }

        public override void Update()
        {
            base.Update();

            var book = Source;
            if (book == null || book.IsDisposed)
            {
                Dispose();
                return;
            }

            if ((DateTime.UtcNow - _lastRefresh).TotalSeconds >= 1.0)
            {
                _lastRefresh = DateTime.UtcNow;

                if (_list.Children.Count <= 1)
                {
                    Fill();
                }
            }
        }

        protected override void CloseWithRightClick()
        {
            Source?.Dispose();
            base.CloseWithRightClick();
        }
    }
}
