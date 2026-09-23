// Мобильный интерфейс: экран контейнера (рюкзак, сумки, трупы).
//
// Зачем: на телефоне мелкие предметы в штатной сумке почти невозможно попасть
// пальцем. Здесь содержимое контейнера показывается крупными строками:
//   [иконка]  имя предмета            xN     вес
// Нажатие по строке — «использовать» предмет (как двойной клик в клиенте),
// удержание/наведение — подсказка со свойствами (OPL от сервера).
//
// Данные берутся из того же места, что и в штатном клиенте: предметы лежат
// связанным списком у контейнера (container.Items), имя — из tiledata/OPL,
// вес — из tiledata. Серверу всё равно, как мы это рисуем.

using System;
using System.Collections.Generic;
using ClassicUO.Assets;
using ClassicUO.Game.GameObjects;
using ClassicUO.Game.Managers;
using ClassicUO.Game.UI.Controls;
using ClassicUO.Input;
using ClassicUO.Renderer;
using Microsoft.Xna.Framework;

namespace ClassicUO.Game.UI.Gumps
{
    internal class MobileContainerGump : Gump
    {
        private const int WinWidth = 620;
        private const int WinHeight = 420;
        private const int RowHeight = 44;
        private const int HeaderHeight = 46;
        private const int FooterHeight = 40;

        private readonly uint _containerSerial;
        private readonly List<uint> _serials = new List<uint>();

        private ScrollArea _list;
        private Label _summary;

        public MobileContainerGump(World world, uint containerSerial) : base(world, 0, 0)
        {
            _containerSerial = containerSerial;

            CanMove = true;
            AcceptMouseInput = true;
            CanCloseWithRightClick = true;

            Build();
        }

        public override GumpType GumpType => GumpType.None;

        private DateTime _lastRebuild = DateTime.MinValue;

        /// <summary>Перечитать содержимое (вызывается, когда сервер присылает предметы).</summary>
        public void Rebuild()
        {
            // сервер может присылать содержимое пачками — не перестраиваем чаще 4 раз в секунду
            if ((DateTime.UtcNow - _lastRebuild).TotalMilliseconds < 250)
            {
                return;
            }

            _lastRebuild = DateTime.UtcNow;
            Fill();
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

            Item container = World.Items.Get(_containerSerial);
            string title = container != null ? GetDisplayName(container) : "";

            Add(new Label(
                ClassicUO.MobileUI.MobileUiController.T("container") + ": " + title,
                true,
                0x0386,
                WinWidth - 130,
                255,
                FontStyle.BlackBorder)
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

            Add(new NiceButton(1, WinWidth - 200, WinHeight - 32, 90, 24, ButtonAction.Activate,
                ClassicUO.MobileUI.MobileUiController.T("refresh"))
            {
                ButtonParameter = 1,
                IsSelectable = false
            });

            Add(new NiceButton(0, WinWidth - 100, WinHeight - 32, 90, 24, ButtonAction.Activate,
                ClassicUO.MobileUI.MobileUiController.T("close"))
            {
                ButtonParameter = 0,
                IsSelectable = false
            });

            Fill();
        }

        private void Fill()
        {
            // ScrollArea.Clear() удаляет только строки, полосу прокрутки не трогает
            _list.Clear();

            _serials.Clear();

            Item container = World.Items.Get(_containerSerial);

            if (container == null)
            {
                _summary.Text = "";

                return;
            }

            int y = 0;
            int count = 0;
            double weight = 0.0;

            for (LinkedObject linked = container.Items; linked != null; linked = linked.Next)
            {
                var item = (Item)linked;

                if (item == null || item.IsDestroyed)
                {
                    continue;
                }

                _serials.Add(item.Serial);
                count++;

                double itemWeight = item.ItemData.Weight;

                if (item.Amount > 1)
                {
                    itemWeight *= item.Amount;
                }

                weight += itemWeight;

                var row = new MobileItemRow(this, item, WinWidth - 44, RowHeight)
                {
                    X = 0,
                    Y = y
                };

                _list.Add(row);

                y += RowHeight;
            }

            _summary.Text = string.Format(
                ClassicUO.MobileUI.MobileUiController.T("container_summary"),
                count,
                Math.Round(weight)
            );

            if (count == 0)
            {
                _list.Add(new Label(
                    ClassicUO.MobileUI.MobileUiController.T("container_empty"),
                    true,
                    0x03B2,
                    WinWidth - 60,
                    255,
                    FontStyle.BlackBorder)
                {
                    X = 16,
                    Y = 8
                });
            }
        }

        /// <summary>Имя предмета: сначала OPL от сервера, иначе из tiledata.</summary>
        private string GetDisplayName(Item item)
        {
            try
            {
                if (World.OPL.TryGetNameAndData(item.Serial, out string name, out _) && !string.IsNullOrEmpty(name))
                {
                    return name;
                }
            }
            catch (Exception)
            {
                // OPL может быть ещё не получен — не критично
            }

            return item.ItemData.Name ?? "";
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

        /// <summary>Крупная строка списка: иконка + имя + количество + вес.</summary>
        private class MobileItemRow : Control
        {
            private readonly MobileContainerGump _owner;
            private readonly Item _item;
            private readonly HitBox _hit;

            public MobileItemRow(MobileContainerGump owner, Item item, int width, int height)
            {
                _owner = owner;
                _item = item;
                LocalSerial = item.Serial;

                Width = width;
                Height = height;
                WantUpdateSize = false;

                var background = new AlphaBlendControl(0.35f)
                {
                    X = 0,
                    Y = 0,
                    Width = width,
                    Height = height
                };

                Add(background);

                _hit = new HitBox(0, 0, width, height, null, 0f);
                Add(_hit);

                try
                {
                    _hit.SetTooltip(item);
                }
                catch (Exception)
                {
                }

                _hit.MouseUp += (sender, e) =>
                {
                    if (e.Button != MouseButtonType.Left)
                    {
                        return;
                    }

                    // нажатие по строке = использовать предмет (двойной клик)
                    GameActions.DoubleClick(_owner.World, LocalSerial);
                };

                string name = owner.GetDisplayName(item);
                string amountText = item.Amount > 1 ? "x" + item.Amount : "";
                double weight = item.ItemData.Weight;

                if (item.Amount > 1)
                {
                    weight *= item.Amount;
                }

                Add(new Label(name, true, 0xFFFF, width - 260, 255, FontStyle.BlackBorder)
                {
                    X = 52,
                    Y = 13
                });

                Add(new Label(amountText, true, 0x0035, 70, 255, FontStyle.BlackBorder)
                {
                    X = width - 190,
                    Y = 13
                });

                Add(new Label(Math.Round(weight) + " " + ClassicUO.MobileUI.MobileUiController.T("weight_short"),
                    true, 0x03B2, 110, 255, FontStyle.BlackBorder)
                {
                    X = width - 105,
                    Y = 13
                });
            }

            public override bool Draw(UltimaBatcher2D batcher, int x, int y)
            {
                base.Draw(batcher, x, y);

                Item item = _owner.World.Items.Get(LocalSerial);

                if (item == null)
                {
                    return true;
                }

                try
                {
                    ref readonly var artInfo = ref Client.Game.UO.Arts.GetArt(item.DisplayedGraphic);
                    var rect = Client.Game.UO.Arts.GetRealArtBounds(item.DisplayedGraphic);

                    if (artInfo.Texture == null || rect.Width <= 0 || rect.Height <= 0)
                    {
                        return true;
                    }

                    Vector3 hueVector = ShaderHueTranslator.GetHueVector(item.Hue, item.ItemData.IsPartialHue, 1f);

                    // вписываем иконку в квадрат 40x40 и центрируем по вертикали строки
                    int box = Height - 4;
                    int w = rect.Width;
                    int h = rect.Height;

                    if (w > box || h > box)
                    {
                        float scale = Math.Min((float)box / w, (float)box / h);
                        w = Math.Max(1, (int)(w * scale));
                        h = Math.Max(1, (int)(h * scale));
                    }

                    batcher.Draw(
                        artInfo.Texture,
                        new Rectangle(x + 6 + ((box - w) >> 1), y + 2 + ((box - h) >> 1), w, h),
                        new Rectangle(artInfo.UV.X + rect.X, artInfo.UV.Y + rect.Y, rect.Width, rect.Height),
                        hueVector
                    );
                }
                catch (Exception)
                {
                    // если арт не найден — строка всё равно остаётся кликабельной
                }

                return true;
            }
        }
    }
}
