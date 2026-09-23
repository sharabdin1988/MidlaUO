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
using ClassicUO.Game.Data;
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
        private static readonly int[][] Presets =
        {
            new[] { 460, 330 },
            new[] { 620, 430 },
            new[] { 780, 540 }
        };

        private static readonly Layer[] EquipmentSlots =
        {
            Layer.OneHanded, Layer.TwoHanded, Layer.Helmet, Layer.Torso, Layer.Tunic,
            Layer.Shirt, Layer.Pants, Layer.Shoes, Layer.Gloves, Layer.Cloak,
            Layer.Robe, Layer.Waist, Layer.Necklace, Layer.Ring, Layer.Bracelet,
            Layer.Earrings, Layer.Backpack, Layer.Mount
        };

        private const int HeaderHeight = 78;
        private const int FooterHeight = 38;

        private readonly int _width;
        private readonly int _height;
        private readonly int _rowHeight;
        private StbTextBox _search;
        private bool _showEquipment;

        private readonly uint _containerSerial;
        private readonly List<uint> _serials = new List<uint>();

        private ScrollArea _list;
        private Label _summary;

        public MobileContainerGump(World world, uint containerSerial) : base(world, 0, 0)
        {
            _containerSerial = containerSerial;

            var cfg = ClassicUO.MobileUI.MobileUiController.Config;
            int preset = cfg != null ? Math.Max(0, Math.Min(2, cfg.WindowPreset)) : 1;

            _width = Presets[preset][0];
            _height = Presets[preset][1];
            _rowHeight = cfg != null ? Math.Max(28, Math.Min(72, cfg._rowHeight)) : 44;

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
                Width = _width,
                Height = _height
            });

            Width = _width;
            Height = _height;

            Item container = World.Items.Get(_containerSerial);
            string title = container != null ? GetDisplayName(container) : "";

            Add(new Label(
                ClassicUO.MobileUI.MobileUiController.T("container") + ": " + title,
                true,
                0x0386,
                _width - 130,
                255,
                FontStyle.BlackBorder)
            {
                X = 16,
                Y = 12
            });

            _summary = new Label("", true, 0x03B2, _width - 130, 255, FontStyle.BlackBorder)
            {
                X = 16,
                Y = 30
            };

            Add(_summary);

            Add(new Label(ClassicUO.MobileUI.MobileUiController.T("search"), true, 0x03B2, 70, 255, FontStyle.BlackBorder)
            {
                X = 16,
                Y = 54
            });

            _search = new StbTextBox(255, 40, _width - 150, true, FontStyle.BlackBorder, 0xFFFF)
            {
                X = 80,
                Y = 54,
                Width = _width - 160,
                Height = 20
            };

            _search.TextChanged += (sender, args) => Fill();

            Add(_search);

            _list = new ScrollArea(10, HeaderHeight, _width - 20, _height - HeaderHeight - FooterHeight, true)
            {
                AcceptMouseInput = true
            };

            Add(_list);

            int bx = 10;
            int by = _height - 32;

            Add(new NiceButton(bx, by, 30, 24, ButtonAction.Activate, "-") { ButtonParameter = 4, IsSelectable = false });
            Add(new NiceButton(bx + 34, by, 30, 24, ButtonAction.Activate, "+") { ButtonParameter = 5, IsSelectable = false });
            Add(new NiceButton(bx + 72, by, 130, 24, ButtonAction.Activate,
                ClassicUO.MobileUI.MobileUiController.T("equipment"))
            {
                ButtonParameter = 7,
                IsSelectable = false
            });
            Add(new NiceButton(bx + 206, by, 120, 24, ButtonAction.Activate, SizeLabel())
            {
                ButtonParameter = 6,
                IsSelectable = false
            });

            Add(new NiceButton(_width - 200, _height - 32, 90, 24, ButtonAction.Activate,
                ClassicUO.MobileUI.MobileUiController.T("refresh"))
            {
                ButtonParameter = 1,
                IsSelectable = false
            });

            Add(new NiceButton(_width - 100, _height - 32, 90, 24, ButtonAction.Activate,
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

            string filter = _search != null ? (_search.Text ?? "").Trim() : "";

            int y = 0;
            int count = 0;
            double weight = 0.0;

            if (_showEquipment && World.Player != null)
            {
                int equipped = 0;

                foreach (Layer layer in EquipmentSlots)
                {
                    Item worn = World.Player.FindItemByLayer(layer);
                    string text = layer + ": " + (worn != null ? GetDisplayName(worn) : "—");

                    if (worn != null)
                    {
                        equipped++;
                    }

                    _list.Add(new Label(text, true, worn != null ? (ushort)0x0044 : (ushort)0x03B2,
                        _width - 60, 255, FontStyle.BlackBorder)
                    {
                        X = 12,
                        Y = y + 10
                    });

                    y += _rowHeight;
                }

                _summary.Text = ClassicUO.MobileUI.MobileUiController.T("equipment") + ": " + equipped + "/" + EquipmentSlots.Length;
            }

            for (LinkedObject linked = container.Items; linked != null; linked = linked.Next)
            {
                var item = (Item)linked;

                if (item == null || item.IsDestroyed)
                {
                    continue;
                }

                string name = GetDisplayName(item);

                if (filter.Length > 0 && (name == null || name.IndexOf(filter, StringComparison.OrdinalIgnoreCase) < 0))
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

                var row = new MobileItemRow(this, item, _width - 44, _rowHeight)
                {
                    X = 0,
                    Y = y
                };

                _list.Add(row);

                y += _rowHeight;
            }

            if (!_showEquipment)
            {
                _summary.Text = string.Format(
                    ClassicUO.MobileUI.MobileUiController.T("container_summary"),
                    count,
                    Math.Round(weight)
                );
            }

            if (count == 0 && !_showEquipment)
            {
                _list.Add(new Label(
                    filter.Length > 0
                        ? ClassicUO.MobileUI.MobileUiController.T("container_not_found")
                        : ClassicUO.MobileUI.MobileUiController.T("container_empty"),
                    true,
                    0x03B2,
                    _width - 60,
                    255,
                    FontStyle.BlackBorder)
                {
                    X = 16,
                    Y = 8
                });
            }
        }

        private string SizeLabel()
        {
            var cfg = ClassicUO.MobileUI.MobileUiController.Config;

            switch (cfg != null ? cfg.WindowPreset : 1)
            {
                case 0: return ClassicUO.MobileUI.MobileUiController.T("size_small");
                case 2: return ClassicUO.MobileUI.MobileUiController.T("size_big");
                default: return ClassicUO.MobileUI.MobileUiController.T("size_normal");
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

                case 4:
                    ClassicUO.MobileUI.MobileUiController.ChangeRowHeight(-6);
                    ClassicUO.MobileUI.MobileUiController.ReopenContainer(_containerSerial);

                    return;

                case 5:
                    ClassicUO.MobileUI.MobileUiController.ChangeRowHeight(6);
                    ClassicUO.MobileUI.MobileUiController.ReopenContainer(_containerSerial);

                    return;

                case 6:
                    ClassicUO.MobileUI.MobileUiController.CycleWindowPreset();
                    ClassicUO.MobileUI.MobileUiController.ReopenContainer(_containerSerial);

                    return;

                case 7:
                    _showEquipment = !_showEquipment;
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

                int textY = Math.Max(3, (height - 18) >> 1);

                Add(new Label(name, true, 0xFFFF, width - 260, 255, FontStyle.BlackBorder)
                {
                    X = 52,
                    Y = textY
                });

                Add(new Label(amountText, true, 0x0035, 70, 255, FontStyle.BlackBorder)
                {
                    X = width - 190,
                    Y = textY
                });

                Add(new Label(Math.Round(weight) + " " + ClassicUO.MobileUI.MobileUiController.T("weight_short"),
                    true, 0x03B2, 110, 255, FontStyle.BlackBorder)
                {
                    X = width - 105,
                    Y = textY
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
