// SPDX-License-Identifier: BSD-2-Clause
//
// Мобильный интерфейс: выносная кнопка быстрой телепортации на экран (HUD).
//
// Создаётся перетаскиванием руны из Runebook на экран или нажатием [ 📌 Экран ].
// Является AnchorableGump — магнитится к кнопкам заклинаний и макросов,
// образуя удобную панель быстрых действий у края экрана.
// При тапе мгновенно телепортирует в сохранённую точку (даже если книга рун закрыта).

using System;
using System.Xml;
using ClassicUO.Assets;
using ClassicUO.Configuration;
using ClassicUO.Game;
using ClassicUO.Game.Managers;
using ClassicUO.Game.UI.Controls;
using ClassicUO.Game.UI.Gumps;
using ClassicUO.Input;
using ClassicUO.Renderer;
using Microsoft.Xna.Framework;

namespace ClassicUO.MobileUI
{
    internal sealed class MobileRuneButtonGump : AnchorableGump
    {
        private uint _bookSerial;
        private int _buttonID;
        private string _runeName = "";

        public MobileRuneButtonGump(World world) : base(world, 0, 0)
        {
            CanMove = true;
            AcceptMouseInput = true;
            CanCloseWithRightClick = true;
            WantUpdateSize = false;
            WidthMultiplier = 2;
            HeightMultiplier = 1;
            GroupMatrixWidth = 44;
            GroupMatrixHeight = 44;
            AnchorType = ANCHOR_TYPE.SPELL;
        }

        public MobileRuneButtonGump(World world, uint bookSerial, int buttonID, string runeName, int x, int y)
            : this(world)
        {
            _bookSerial = bookSerial;
            _buttonID = buttonID;
            _runeName = runeName ?? "Rune";
            X = x;
            Y = y;

            BuildGump();
        }

        public override GumpType GumpType => GumpType.None;

        public uint BookSerial => _bookSerial;
        public int ButtonID => _buttonID;
        public string RuneName => _runeName;

        private void BuildGump()
        {
            Clear();
            Children.Clear();

            Width = 88;
            Height = 44;

            // Тёмная полупрозрачная подложка
            Add(new AlphaBlendControl(0.6f)
            {
                X = 0,
                Y = 0,
                Width = Width,
                Height = Height
            });

            // Тонкая рамка (фиолетовый / магия)
            Add(new BorderControl(0, 0, Width, Height, 1)
            {
                Hue = 0x0035
            });

            // Иконка телепортации (маленький кристалл или свиток)
            Add(new GumpPic(4, 12, 0x08DF, 0) // 0x08DF — иконка заклинания Recall
            {
                AcceptMouseInput = false
            });

            // Название руны
            string displayName = _runeName;
            if (displayName.Length > 9)
            {
                displayName = displayName.Substring(0, 8) + "…";
            }

            Add(new Label(
                "⚡ " + displayName,
                true,
                0xFFFF,
                Width - 28,
                255,
                FontStyle.BlackBorder)
            {
                X = 26,
                Y = 6
            });

            Add(new Label(
                MobileUiController.T("recall"),
                true,
                0x0035,
                Width - 28,
                255,
                FontStyle.BlackBorder)
            {
                X = 26,
                Y = 24
            });

            SetTooltip($"[⚡ {_runeName}] — {MobileUiController.T("recall")}");
        }

        protected override void OnMouseUp(int x, int y, MouseButtonType button)
        {
            base.OnMouseUp(x, y, button);

            Point offset = Mouse.LDragOffset;

            // Срабатывает только по чистому клику (без перетаскивания кнопки)
            if (button == MouseButtonType.Left && Math.Abs(offset.X) < 6 && Math.Abs(offset.Y) < 6)
            {
                MobileUiController.TeleportToRune(_bookSerial, _buttonID, _runeName);
            }
        }

        public override void Save(XmlTextWriter writer)
        {
            base.Save(writer);
            writer.WriteAttributeString("book", _bookSerial.ToString());
            writer.WriteAttributeString("btn", _buttonID.ToString());
            writer.WriteAttributeString("name", _runeName ?? "");
        }

        public override void Restore(XmlElement xml)
        {
            base.Restore(xml);
            if (uint.TryParse(xml.GetAttribute("book"), out uint b)) _bookSerial = b;
            if (int.TryParse(xml.GetAttribute("btn"), out int btn)) _buttonID = btn;
            _runeName = xml.GetAttribute("name") ?? "Rune";
            BuildGump();
        }
    }
}
