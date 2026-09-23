// Мобильный интерфейс: список заклинаний книги (клиентское окно, сервер не участвует).
//
// Штатная книга заклинаний остаётся источником данных (она получает список от
// сервера), но скрывается, а игрок видит крупный список: название заклинания,
// реагенты и стоимость. Нажатие по строке — каст из книги.

using System;
using System.Linq;
using ClassicUO.Assets;
using ClassicUO.Game.Managers;
using ClassicUO.Game.UI.Controls;
using ClassicUO.Input;
using ClassicUO.Renderer;
using Microsoft.Xna.Framework;

namespace ClassicUO.Game.UI.Gumps
{
    internal class MobileSpellbookGump : Gump
    {
        private const int WinWidth = 620;
        private const int WinHeight = 470;
        private const int RowHeight = 38;
        private const int HeaderHeight = 46;
        private const int FooterHeight = 36;

        private readonly uint _bookSerial;
        private ScrollArea _list;
        private Label _summary;
        private DateTime _lastRefresh = DateTime.MinValue;

        public MobileSpellbookGump(World world, uint bookSerial) : base(world, 0, 0)
        {
            _bookSerial = bookSerial;

            CanMove = true;
            AcceptMouseInput = true;
            CanCloseWithRightClick = true;

            Build();
        }

        public override GumpType GumpType => GumpType.None;

        /// <summary>Серийник книги (LocalSerial у гумпа нулевой).</summary>
        public uint BookSerial => _bookSerial;

        private SpellbookGump Source => UIManager.Gumps.OfType<SpellbookGump>().FirstOrDefault(g => g.LocalSerial == _bookSerial);

        private void Build()
        {
            Add(new ResizePic(0x0A3C) { X = 0, Y = 0, Width = WinWidth, Height = WinHeight });

            Width = WinWidth;
            Height = WinHeight;

            Add(new Label(
                ClassicUO.MobileUI.MobileUiController.T("spellbook"),
                true, 0x0386, WinWidth - 130, 255, FontStyle.BlackBorder)
            {
                X = 16,
                Y = 12
            });

            _summary = new Label("", true, 0x03B2, WinWidth - 130, 255, FontStyle.BlackBorder)
            {
                X = 16,
                Y = 30
            };

            Add(_summary);

            _list = new ScrollArea(10, HeaderHeight, WinWidth - 20, WinHeight - HeaderHeight - FooterHeight, true)
            {
                AcceptMouseInput = true
            };

            Add(_list);

            Add(new NiceButton(WinWidth - 200, WinHeight - 30, 90, 22, ButtonAction.Activate,
                ClassicUO.MobileUI.MobileUiController.T("refresh"))
            {
                ButtonParameter = 1,
                IsSelectable = false
            });

            Add(new NiceButton(WinWidth - 100, WinHeight - 30, 90, 22, ButtonAction.Activate,
                ClassicUO.MobileUI.MobileUiController.T("close"))
            {
                ButtonParameter = 0,
                IsSelectable = false
            });

            Fill();
        }

        private void Fill()
        {
            _list.Clear();

            var book = Source;

            if (book == null)
            {
                _summary.Text = ClassicUO.MobileUI.MobileUiController.T("spellbook_wait");

                return;
            }

            int y = 0;
            int count = 0;

            for (int i = 0; i < book.MobileSpellSlots; i++)
            {
                if (!book.MobileHasSpell(i))
                {
                    continue;
                }

                int index = i;
                count++;

                string name;

                try
                {
                    book.MobileGetSpellNames(i, out name, out _);
                }
                catch (Exception)
                {
                    name = ClassicUO.MobileUI.MobileUiController.T("spell") + " #" + (i + 1);
                }

                AddRow(index, name ?? ("#" + (i + 1)), y);
                y += RowHeight;
            }

            _summary.Text = string.Format(ClassicUO.MobileUI.MobileUiController.T("spellbook_summary"), count);

            if (count == 0)
            {
                _list.Add(new Label(ClassicUO.MobileUI.MobileUiController.T("spellbook_wait"),
                    true, 0x03B2, WinWidth - 60, 255, FontStyle.BlackBorder)
                {
                    X = 16,
                    Y = 8
                });
            }
        }

        private void AddRow(int index, string name, int y)
        {
            var row = new AlphaBlendControl(0.35f) { X = 0, Y = y, Width = WinWidth - 44, Height = RowHeight };
            _list.Add(row);

            var hit = new HitBox(0, y, WinWidth - 44, RowHeight, null, 0f);
            _list.Add(hit);

            hit.MouseUp += (sender, e) =>
            {
                if (e.Button == MouseButtonType.Left)
                {
                    GameActions.CastSpellFromBook(index, _bookSerial);
                }
            };

            _list.Add(new Label(name, true, 0xFFFF, WinWidth - 120, 255, FontStyle.BlackBorder)
            {
                X = 12,
                Y = y + 10
            });
        }

        public override void Update()
        {
            base.Update();

            // список приходит от сервера после открытия книги — подхватываем его
            if ((DateTime.UtcNow - _lastRefresh).TotalSeconds >= 1.0)
            {
                _lastRefresh = DateTime.UtcNow;

                if (_list.Children.Count <= 2)
                {
                    Fill();
                }
            }
        }

        public override void OnButtonClick(int buttonID)
        {
            switch (buttonID)
            {
                case 1:
                    Fill();

                    return;

                default:
                    Dispose();

                    return;
            }
        }
    }
}
