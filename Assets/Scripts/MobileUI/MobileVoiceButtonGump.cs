// SPDX-License-Identifier: BSD-2-Clause
// MidlaUO: экранная кнопка голосового ввода «[ 🎤 ]».
//
// Выполнена как AnchorableGump: стыкуется и примагничивается к остальным экранным кнопкам
// (спеллы, руны, макросы, мобильный UI).
// Поддерживает как одиночный тап (кликнул -> сказал -> авто-отправка),
// так и Push-to-Talk (зажал -> говоришь -> отпустил -> отправка).

using System;
using ClassicUO.Assets;
using ClassicUO.Game;
using ClassicUO.Game.UI.Controls;
using ClassicUO.Input;
using ClassicUO.Renderer;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace ClassicUO.Game.UI.Gumps
{
    internal sealed class MobileVoiceButtonGump : AnchorableGump
    {
        private Texture2D _background;
        private Label _statusLabel;
        private Point _mouseDownPos;
        private uint _touchDownTime;
        private bool _isTouchHeld;

        public MobileVoiceButtonGump(World world) : base(world, 0, 0)
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

            // Начальная позиция: вверху рядом с кнопкой Моб. UI (не мешает джойстику)
            X = 112;
            Y = 12;

            Build();

            ClassicUO.MobileUI.MobileVoiceInput.StateChanged += OnVoiceStateChanged;
        }

        public override GumpType GumpType => GumpType.VoiceButton;

        private void Build()
        {
            Width = 96;
            Height = 44;

            _background = SolidColorTextureCache.GetTexture(new Color(28, 28, 28));

            _statusLabel = new Label(
                "🎤  " + (ClassicUO.MobileUI.MobileUiTranslation.IsRussian ? "Голос" : "Voice"),
                true,
                1001,
                Width,
                255,
                FontStyle.BlackBorder,
                TEXT_ALIGN_TYPE.TS_CENTER
            )
            {
                X = 0,
                Y = (Height >> 1) - 8,
                Width = Width - 6,
                AcceptMouseInput = false
            };
            Add(_statusLabel);

            UpdateVisualState(ClassicUO.MobileUI.MobileVoiceInput.CurrentState);
            SetTooltip(ClassicUO.MobileUI.MobileUiController.T("voice_hint"));
        }

        private void OnVoiceStateChanged(ClassicUO.MobileUI.VoiceState state)
        {
            UpdateVisualState(state);
        }

        private void UpdateVisualState(ClassicUO.MobileUI.VoiceState state)
        {
            if (_statusLabel == null)
            {
                return;
            }

            switch (state)
            {
                case ClassicUO.MobileUI.VoiceState.Ready:
                case ClassicUO.MobileUI.VoiceState.Speaking:
                    _statusLabel.Text = "🔴  " + (ClassicUO.MobileUI.MobileUiTranslation.IsRussian ? "Слушаю..." : "Rec...");
                    _statusLabel.Hue = 0x0020; // красный
                    _background = SolidColorTextureCache.GetTexture(new Color(60, 15, 15));
                    break;

                case ClassicUO.MobileUI.VoiceState.Processing:
                    _statusLabel.Text = "⚡  " + (ClassicUO.MobileUI.MobileUiTranslation.IsRussian ? "Обраб." : "Proc...");
                    _statusLabel.Hue = 53; // жёлтый
                    _background = SolidColorTextureCache.GetTexture(new Color(45, 45, 15));
                    break;

                case ClassicUO.MobileUI.VoiceState.Error:
                    _statusLabel.Text = "❌  " + (ClassicUO.MobileUI.MobileUiTranslation.IsRussian ? "Ошибка" : "Error");
                    _statusLabel.Hue = 0x0020;
                    _background = SolidColorTextureCache.GetTexture(new Color(45, 20, 20));
                    break;

                case ClassicUO.MobileUI.VoiceState.Idle:
                default:
                    _statusLabel.Text = "🎤  " + (ClassicUO.MobileUI.MobileUiTranslation.IsRussian ? "Голос" : "Voice");
                    _statusLabel.Hue = 1001;
                    _background = SolidColorTextureCache.GetTexture(new Color(28, 28, 28));
                    break;
            }
        }

        protected override void OnMouseDown(int x, int y, MouseButtonType button)
        {
            base.OnMouseDown(x, y, button);

            if (button == MouseButtonType.Left)
            {
                _mouseDownPos = Mouse.Position;
                _touchDownTime = Time.Ticks;
                _isTouchHeld = false;
            }
        }

        public override void Update()
        {
            base.Update();

            // Push-to-Talk детектор: если удерживаем палец более 300 мс без перетаскивания
            if (_touchDownTime > 0 && Mouse.LButtonPressed)
            {
                Point delta = Mouse.Position - _mouseDownPos;
                if (Math.Abs(delta.X) < 16 && Math.Abs(delta.Y) < 16)
                {
                    if (!_isTouchHeld && Time.Ticks - _touchDownTime > 300)
                    {
                        _isTouchHeld = true;
                        if (!ClassicUO.MobileUI.MobileVoiceInput.IsListening)
                        {
                            ClassicUO.MobileUI.MobileVoiceInput.StartListening(World);
                        }
                    }
                }
                else
                {
                    // Палец сместился более 16 px — это перетаскивание кнопки, сбрасываем PTT
                    _touchDownTime = 0;
                }
            }
        }

        protected override void OnMouseUp(int x, int y, MouseButtonType button)
        {
            base.OnMouseUp(x, y, button);

            if (button != MouseButtonType.Left)
            {
                return;
            }

            Point delta = Mouse.Position - _mouseDownPos;

            // Если палец не двигался далеко (чистый тап, а не перетаскивание кнопки)
            if (Math.Abs(delta.X) < 16 && Math.Abs(delta.Y) < 16)
            {
                if (_isTouchHeld)
                {
                    // Завершение Push-to-Talk
                    ClassicUO.MobileUI.MobileVoiceInput.StopListening();
                }
                else
                {
                    // Одиночный тап (переключение состояния вкл/выкл)
                    if (ClassicUO.MobileUI.MobileVoiceInput.IsListening)
                    {
                        ClassicUO.MobileUI.MobileVoiceInput.StopListening();
                    }
                    else
                    {
                        ClassicUO.MobileUI.MobileVoiceInput.StartListening(World);
                    }
                }
            }

            _touchDownTime = 0;
            _isTouchHeld = false;
        }

        public override bool Draw(UltimaBatcher2D batcher, int x, int y)
        {
            var hueVector = ShaderHueTranslator.GetHueVector(0);
            hueVector.Z = 0.9f;
            batcher.Draw2D(_background, x, y, Width, Height, ref hueVector);

            hueVector.Z = 1f;
            Color borderColor = ClassicUO.MobileUI.MobileVoiceInput.IsListening ? Color.Red : Color.DimGray;
            batcher.DrawRectangle(SolidColorTextureCache.GetTexture(borderColor), x, y, Width, Height, hueVector);

            base.Draw(batcher, x, y);
            return true;
        }

        public override void Dispose()
        {
            ClassicUO.MobileUI.MobileVoiceInput.StateChanged -= OnVoiceStateChanged;
            base.Dispose();
        }
    }
}
