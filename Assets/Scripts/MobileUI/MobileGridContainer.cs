// SPDX-License-Identifier: BSD-2-Clause
//
// Мобильный интерфейс: инвентарь-сетка (Diablo-style Grid Inventory).
//
// Организует содержимое сумок и рюкзаков в аккуратную сенсорную сетку ячеек:
//   * каждый предмет лежит в отдельном крупном квадратном слоте под палец;
//   * поддержка Drag & Drop: перетаскивание предметов между ячейками и обмен местами (Swap);
//   * двойной тап — мгновенное использование предмета (выпить зелье, надеть вещь, открыть сумку);
//   * счётчик количества (Amount) в углу стопки;
//   * тултипы со свойствами при наведении/удержании;
//   * умная авто-сортировка предметов по категориям [ ⚡ Сорт ];
//   * кнопка быстрого переключения [ 🎒 Классика ] ⇄ [ ⊞ Сетка ].

using System;
using System.Collections.Generic;
using System.Linq;
using ClassicUO.Assets;
using ClassicUO.Game;
using ClassicUO.Game.Data;
using ClassicUO.Game.GameObjects;
using ClassicUO.Game.Managers;
using ClassicUO.Game.UI.Controls;
using ClassicUO.Game.UI.Gumps;
using ClassicUO.Input;
using ClassicUO.Renderer;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace ClassicUO.MobileUI
{
    /// <summary>
    /// Менеджер слотов сетки инвентаря.
    /// Хранит соответствие (ItemSerial -> SlotIndex) для каждого открытого контейнера.
    /// Переводит индекс слота в координаты (X, Y) контейнера и обратно.
    /// </summary>
    internal static class MobileGridSlotManager
    {
        // ContainerSerial -> (ItemSerial -> SlotIndex)
        private static readonly Dictionary<uint, Dictionary<uint, int>> _containerSlots =
            new Dictionary<uint, Dictionary<uint, int>>();

        public const int DefaultSlotSize = 54;
        public const int DefaultGap = 4;

        /// <summary>
        /// Получает слот предмета в контейнере. Если не назначен — ищет первый свободный.
        /// </summary>
        public static int GetOrAssignSlot(uint containerSerial, uint itemSerial, int itemX, int itemY, HashSet<int> occupiedSlots)
        {
            if (!_containerSlots.TryGetValue(containerSerial, out var slots))
            {
                slots = new Dictionary<uint, int>();
                _containerSlots[containerSerial] = slots;
            }

            if (slots.TryGetValue(itemSerial, out int existingSlot) && existingSlot >= 0)
            {
                occupiedSlots.Add(existingSlot);
                return existingSlot;
            }

            // Пытаемся восстановить слот из серверных координат предмета
            int slotFromCoords = ContainerCoordsToSlot(itemX, itemY);
            if (slotFromCoords >= 0 && !occupiedSlots.Contains(slotFromCoords))
            {
                slots[itemSerial] = slotFromCoords;
                occupiedSlots.Add(slotFromCoords);
                return slotFromCoords;
            }

            // Находим первый свободный слот
            int freeSlot = 0;
            while (occupiedSlots.Contains(freeSlot))
            {
                freeSlot++;
            }

            slots[itemSerial] = freeSlot;
            occupiedSlots.Add(freeSlot);
            return freeSlot;
        }

        public static void SetSlot(uint containerSerial, uint itemSerial, int slot)
        {
            if (!_containerSlots.TryGetValue(containerSerial, out var slots))
            {
                slots = new Dictionary<uint, int>();
                _containerSlots[containerSerial] = slots;
            }

            slots[itemSerial] = slot;
        }

        public static void SwapSlots(uint containerSerial, uint itemA, uint itemB)
        {
            if (!_containerSlots.TryGetValue(containerSerial, out var slots))
            {
                return;
            }

            slots.TryGetValue(itemA, out int slotA);
            slots.TryGetValue(itemB, out int slotB);

            slots[itemA] = slotB;
            slots[itemB] = slotA;
        }

        public static void ClearContainer(uint containerSerial)
        {
            _containerSlots.Remove(containerSerial);
        }

        /// <summary>
        /// Переводит индекс слота сетки (0..124) в стандартные валидные координаты сумки UO (X, Y).
        /// </summary>
        public static Point SlotToContainerCoords(int slotIndex)
        {
            int col = slotIndex % 8;
            int row = slotIndex / 8;
            int x = 44 + col * 16;
            int y = 65 + (row % 16) * 5;
            return new Point(x, y);
        }

        /// <summary>
        /// Переводит координаты сумки UO (X, Y) обратно в индекс слота сетки.
        /// </summary>
        public static int ContainerCoordsToSlot(int x, int y)
        {
            if (x < 40 || y < 60)
            {
                return -1;
            }

            int col = Math.Max(0, Math.Min(7, (x - 44 + 8) / 16));
            int row = Math.Max(0, (y - 65 + 2) / 5);
            return row * 8 + col;
        }
    }

    /// <summary>
    /// Авто-сортировщик инвентаря по категориям в стиле RPG (золото, оружие, броня, зелья, свитки, реагенты...).
    /// </summary>
    internal static class MobileGridSorter
    {
        public static int GetCategory(Item item)
        {
            if (item == null) return 99;
            ushort g = item.Graphic;

            // 0: Золото и валюта
            if (g == 0x0EED || g == 0x0EEF || g == 0x0EF0 || item.IsCoin) return 0;

            // 1: Оружие
            byte layer = (byte)item.ItemData.Layer;
            if (layer == (byte)Layer.OneHanded || layer == (byte)Layer.TwoHanded) return 1;

            // 2: Броня и щиты
            if (item.ItemData.IsWearable && (layer == (byte)Layer.Helm || layer == (byte)Layer.Tunic ||
                layer == (byte)Layer.Arms || layer == (byte)Layer.Gloves || layer == (byte)Layer.Pants ||
                layer == (byte)Layer.Shoes || layer == (byte)Layer.Shield)) return 2;

            // 3: Одежда и бижутерия
            if (item.ItemData.IsWearable) return 3;

            // 4: Зелья и бинты
            if ((g >= 0x0F06 && g <= 0x0F0D) || g == 0x0E21 || (g >= 0x182D && g <= 0x1848)) return 4;

            // 5: Книги магии, рунбуки, руны и свитки заклинаний
            if (g == 0x0EFA || g == 0x2252 || g == 0x2253 || (g >= 0x1F2D && g <= 0x1F6C) || g == 0x1F14 || g == 0x1F15) return 5;

            // 6: Реагенты
            if (g >= 0x0F7A && g <= 0x0F8D) return 6;

            // 7: Инструменты, ключи, отмычки
            if (g == 0x14FB || g == 0x0F9D || g == 0x0E85 || g == 0x0E86 || (g >= 0x100E && g <= 0x1013) || (g >= 0x104B && g <= 0x105E)) return 7;

            // 8: Вложенные контейнеры (сумки, мешочки, коробки)
            if (item.ItemData.IsContainer) return 8;

            return 9; // Прочее
        }

        public static void SortContainer(World world, Item container)
        {
            if (world == null || container == null) return;

            var items = new List<Item>();
            for (var cur = container.Items; cur != null; cur = cur.Next)
            {
                if (cur is Item it && !it.IsDestroyed && it.Amount > 0)
                {
                    items.Add(it);
                }
            }

            // Сортировка: по категории -> по графике -> по цвету -> по количеству
            var sorted = items.OrderBy(GetCategory)
                              .ThenBy(i => i.Graphic)
                              .ThenBy(i => i.Hue)
                              .ThenByDescending(i => i.Amount)
                              .ToList();

            for (int i = 0; i < sorted.Count; i++)
            {
                var item = sorted[i];
                MobileGridSlotManager.SetSlot(container.Serial, item.Serial, i);
                Point pt = MobileGridSlotManager.SlotToContainerCoords(i);
                GameActions.DropItem(item.Serial, pt.X, pt.Y, 0, container.Serial);
            }
        }
    }

    /// <summary>
    /// Контрол ячейки инвентаря в сетке Diablo.
    /// Отображает центрированную иконку с правильным Hue, счётчик стака, тултип,
    /// и обрабатывает тап (Use) и перетаскивание (Drag & Drop).
    /// </summary>
    internal sealed class MobileGridCellControl : Control
    {
        public int SlotIndex { get; }
        public uint ItemSerial { get; private set; }
        public ContainerGump ContainerGump { get; }

        private bool _isPressed;
        private Point _pressPos;
        private RenderedText _renderedAmount;

        public MobileGridCellControl(ContainerGump container, int slotIndex, uint itemSerial, int size)
        {
            ContainerGump = container;
            SlotIndex = slotIndex;
            ItemSerial = itemSerial;

            Width = size;
            Height = size;
            AcceptMouseInput = true;
            CanMove = false;

            if (itemSerial != 0)
            {
                Item it = ContainerGump.World.Items.Get(itemSerial);
                if (it != null)
                {
                    if (ContainerGump.World.ClientFeatures.TooltipsEnabled)
                    {
                        SetTooltip(it);
                    }

                    if (it.Amount > 1)
                    {
                        string amountText = it.Amount >= 10000
                            ? (it.Amount / 1000f).ToString("0.#") + "k"
                            : it.Amount.ToString();
                        _renderedAmount = RenderedText.Create(amountText, 0x0035, font: 1, isunicode: true, style: FontStyle.BlackBorder);
                    }
                }
            }
        }

        public void UpdateItem(uint itemSerial)
        {
            ItemSerial = itemSerial;
            ClearTooltip();
            _renderedAmount?.Destroy();
            _renderedAmount = null;

            if (itemSerial != 0)
            {
                Item it = ContainerGump.World.Items.Get(itemSerial);
                if (it != null)
                {
                    if (ContainerGump.World.ClientFeatures.TooltipsEnabled)
                    {
                        SetTooltip(it);
                    }

                    if (it.Amount > 1)
                    {
                        string amountText = it.Amount >= 10000
                            ? (it.Amount / 1000f).ToString("0.#") + "k"
                            : it.Amount.ToString();
                        _renderedAmount = RenderedText.Create(amountText, 0x0035, font: 1, isunicode: true, style: FontStyle.BlackBorder);
                    }
                }
            }
        }

        protected override void OnMouseDown(int x, int y, MouseButtonType button)
        {
            base.OnMouseDown(x, y, button);
            if (button == MouseButtonType.Left)
            {
                _isPressed = true;
                _pressPos = Mouse.Position;
                if (ItemSerial != 0)
                {
                    SelectedObject.Object = ContainerGump.World.Get(ItemSerial);
                }
            }
        }

        protected override void OnMouseUp(int x, int y, MouseButtonType button)
        {
            base.OnMouseUp(x, y, button);
            _isPressed = false;

            // Если в руке держится предмет — бросаем его в этот слот!
            if (button == MouseButtonType.Left && Client.Game.UO.GameCursor.ItemHold.Enabled)
            {
                ContainerGump.OnGridSlotDropped(SlotIndex, ItemSerial);
            }
        }

        protected override bool OnMouseDoubleClick(int x, int y, MouseButtonType button)
        {
            if (button == MouseButtonType.Left && ItemSerial != 0)
            {
                GameActions.DoubleClick(ContainerGump.World, ItemSerial);
                return true;
            }
            return base.OnMouseDoubleClick(x, y, button);
        }

        protected override void OnMouseOver(int x, int y)
        {
            base.OnMouseOver(x, y);
            if (ItemSerial != 0)
            {
                SelectedObject.Object = ContainerGump.World.Get(ItemSerial);
            }
        }

        public override void Update()
        {
            base.Update();

            if (IsDisposed) return;

            // Проверка начала перетаскивания предмета из ячейки
            if (_isPressed && ItemSerial != 0 && !Client.Game.UO.GameCursor.ItemHold.Enabled)
            {
                Point diff = Mouse.Position - _pressPos;
                if (Math.Abs(diff.X) > Constants.MIN_PICKUP_DRAG_DISTANCE_PIXELS ||
                    Math.Abs(diff.Y) > Constants.MIN_PICKUP_DRAG_DISTANCE_PIXELS)
                {
                    _isPressed = false;
                    GameActions.PickUp(ContainerGump.World, ItemSerial, 0, 0, -1, null, false);
                }
            }
        }

        public override bool Draw(UltimaBatcher2D batcher, int x, int y)
        {
            if (IsDisposed) return false;

            base.Draw(batcher, x, y);

            Vector3 hueVector = ShaderHueTranslator.GetHueVector(0);

            // 1. Фон ячейки (тёмно-серый полупрозрачный)
            Color bgColor = (MouseIsOver || (ItemSerial != 0 && SelectedObject.Object?.Serial == ItemSerial))
                ? new Color(45, 50, 65, 220)
                : new Color(24, 26, 34, 180);

            batcher.DrawRectangle(SolidColorTextureCache.GetTexture(bgColor), x, y, Width, Height, hueVector);

            // 2. Рамка слота
            Color borderColor = (MouseIsOver || (ItemSerial != 0 && SelectedObject.Object?.Serial == ItemSerial))
                ? new Color(0x35, 0x90, 0xE0, 255)
                : new Color(55, 60, 75, 180);

            batcher.DrawRectangle(SolidColorTextureCache.GetTexture(borderColor), x, y, Width, 1, hueVector);
            batcher.DrawRectangle(SolidColorTextureCache.GetTexture(borderColor), x, y + Height - 1, Width, 1, hueVector);
            batcher.DrawRectangle(SolidColorTextureCache.GetTexture(borderColor), x, y, 1, Height, hueVector);
            batcher.DrawRectangle(SolidColorTextureCache.GetTexture(borderColor), x + Width - 1, y, 1, Height, hueVector);

            // 3. Если есть предмет — рисуем его арт и стак
            if (ItemSerial != 0)
            {
                Item item = ContainerGump.World.Items.Get(ItemSerial);
                if (item != null && !item.IsDestroyed)
                {
                    ref readonly var artInfo = ref Client.Game.UO.Arts.GetArt(item.DisplayedGraphic);
                    if (artInfo.Texture != null)
                    {
                        var realBounds = Client.Game.UO.Arts.GetRealArtBounds(item.DisplayedGraphic);

                        int maxDrawW = Width - 8;
                        int maxDrawH = Height - 8;

                        int drawW = realBounds.Width;
                        int drawH = realBounds.Height;

                        if (drawW > maxDrawW || drawH > maxDrawH)
                        {
                            float factor = Math.Min((float)maxDrawW / drawW, (float)maxDrawH / drawH);
                            drawW = Math.Max(1, (int)(drawW * factor));
                            drawH = Math.Max(1, (int)(drawH * factor));
                        }

                        int drawX = x + (Width - drawW) / 2;
                        int drawY = y + (Height - drawH) / 2;

                        Vector3 itemHue = ShaderHueTranslator.GetHueVector(item.Hue, item.ItemData.IsPartialHue, 1f);

                        batcher.Draw(
                            artInfo.Texture,
                            new Rectangle(drawX, drawY, drawW, drawH),
                            new Rectangle(artInfo.UV.X + realBounds.X, artInfo.UV.Y + realBounds.Y, realBounds.Width, realBounds.Height),
                            itemHue
                        );
                    }

                    // 4. Счётчик количества предметов в стаке (> 1)
                    if (item.Amount > 1 && _renderedAmount != null)
                    {
                        int badgeW = _renderedAmount.Width + 4;
                        int badgeH = _renderedAmount.Height;
                        int badgeX = x + Width - badgeW - 2;
                        int badgeY = y + Height - badgeH - 2;
                        batcher.DrawRectangle(SolidColorTextureCache.GetTexture(new Color(10, 10, 15, 200)), badgeX, badgeY, badgeW, badgeH, hueVector);
                        _renderedAmount.Draw(batcher, badgeX + 2, badgeY);
                    }
                }
            }

            return true;
        }

        public override void Dispose()
        {
            _renderedAmount?.Destroy();
            _renderedAmount = null;
            base.Dispose();
        }
    }
}
