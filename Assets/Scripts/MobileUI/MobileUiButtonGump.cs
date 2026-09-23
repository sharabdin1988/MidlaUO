// Мобильный интерфейс: экранная кнопка «Моб. UI».
//
// Сделана по образцу кнопок ассистента (AssistantMacroButtonGump): её можно
// перетаскивать, она прилипает к сетке как остальные экранные кнопки, а по
// нажатию открывает панель настроек мобильного интерфейса.

using System;
using ClassicUO.Game.UI.Controls;
using ClassicUO.Input;
using ClassicUO.Renderer;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace ClassicUO.Game.UI.Gumps
{
    internal class MobileUiButtonGump : AnchorableGump
    {
        private Texture2D _background;
        private Label _label;

        public MobileUiButtonGump(World world) : base(world, 0, 0)
        {
            CanMove = true;
            AcceptMouseInput = true;
            CanCloseWithRightClick = false;
            WantUpdateSize = false;
            WidthMultiplier = 2;
            HeightMultiplier = 1;
            GroupMatrixWidth = 44;
            GroupMatrixHeight = 44;
            AnchorType = ANCHOR_TYPE.SPELL;

            // start position: below the top bar, on the left
            X = 12;
            Y = 150;

            Build();
        }

        public override GumpType GumpType => GumpType.MobileUiButton;

        private void Build()
        {
            Width = 96;
            Height = 44;

            _background = SolidColorTextureCache.GetTexture(new Color(30, 30, 30));

            _label = new Label(
                ClassicUO.MobileUI.MobileUiController.T("button"),
                true,
                1001,
                Width,
                255,
                FontStyle.BlackBorder,
                TEXT_ALIGN_TYPE.TS_CENTER
            )
            {
                X = 0,
                Width = Width - 10
            };

            _label.Y = (Height >> 1) - (_label.Height >> 1);
            Add(_label);
        }

        protected override void OnMouseEnter(int x, int y)
        {
            _label.Hue = 53;
            _background = SolidColorTextureCache.GetTexture(Color.DimGray);
            base.OnMouseEnter(x, y);
        }

        protected override void OnMouseExit(int x, int y)
        {
            _label.Hue = 1001;
            _background = SolidColorTextureCache.GetTexture(new Color(30, 30, 30));
            base.OnMouseExit(x, y);
        }

        protected override void OnMouseUp(int x, int y, MouseButtonType button)
        {
            base.OnMouseUp(x, y, MouseButtonType.Left);

            if (button != MouseButtonType.Left)
            {
                return;
            }

            Point offset = Mouse.LDragOffset;

            // короткое нажатие открывает настройки, перетаскивание — двигает кнопку
            if (Math.Abs(offset.X) < 5 && Math.Abs(offset.Y) < 5)
            {
                ClassicUO.MobileUI.MobileUiController.OpenSettings();
            }
        }

        public override bool Draw(UltimaBatcher2D batcher, int x, int y)
        {
            var hueVector = ShaderHueTranslator.GetHueVector(0);
            hueVector.Z = 0.9f;
            batcher.Draw2D(_background, x, y, Width, Height, ref hueVector);

            hueVector.Z = 1;
            batcher.DrawRectangle(SolidColorTextureCache.GetTexture(Color.Gray), x, y, Width, Height, hueVector);

            base.Draw(batcher, x, y);

            return true;
        }
    }
}
