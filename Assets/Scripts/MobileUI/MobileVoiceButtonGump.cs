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
        private Label _iconLabel;
        private Label _statusLabel;
        private uint _touchDownTime;
        private bool _isTouchHeld;

        public MobileVoiceButtonGump(World world) : base(world, 0, 0)
        {
            CanMove = true;
            AcceptMouseInput = true;
            CanCloseWithRightClick = false;
            WantUpdateSize = false;
            WidthMultiplier = 1;
            HeightMultiplier = 1;
            GroupMatrixWidth = 44;
            GroupMatrixHeight = 44;
            AnchorType = ANCHOR_TYPE.SPELL;

            // Начальная позиция: слева под кнопкой Моб. UI
            X = 12;
            Y = 202;

            Build();

            ClassicUO.MobileUI.MobileVoiceInput.StateChanged += OnVoiceStateChanged;
        }

        public override GumpType GumpType => GumpType.VoiceButton;

        private void Build()
        {
            Width = 48;
            Height = 44;

            _background = SolidColorTextureCache.GetTexture(new Color(28, 28, 28));

            _iconLabel = new Label(
                "🎤",
                true,
                0x0035, // приятный голубоватый оттенок
                Width,
                255,
                FontStyle.BlackBorder,
                TEXT_ALIGN_TYPE.TS_CENTER
            )
            {
                X = 0,
                Y = 4,
                AcceptMouseInput = false
            };
            Add(_iconLabel);

            _statusLabel = new Label(
                ClassicUO.MobileUI.MobileUiTranslation.IsRussian ? "Голос" : "Voice",
                true,
                1001,
                Width,
                255,
                FontStyle.BlackBorder,
                TEXT_ALIGN_TYPE.TS_CENTER
            )
            {
                X = 0,
                Y = 22,
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
            switch (state)
            {
                case ClassicUO.MobileUI.VoiceState.Ready:
                case ClassicUO.MobileUI.VoiceState.Speaking:
                    _iconLabel.Text = "🔴";
                    _iconLabel.Hue = 0x0020; // красный
                    _statusLabel.Text = ClassicUO.MobileUI.MobileUiTranslation.IsRussian ? "Жду..." : "Rec...";
                    _statusLabel.Hue = 0x0020;
                    _background = SolidColorTextureCache.GetTexture(new Color(60, 15, 15));
                    break;

                case ClassicUO.MobileUI.VoiceState.Processing:
                    _iconLabel.Text = "⚡";
                    _iconLabel.Hue = 53; // жёлтый
                    _statusLabel.Text = ClassicUO.MobileUI.MobileUiTranslation.IsRussian ? "Обраб." : "Proc...";
                    _statusLabel.Hue = 53;
                    _background = SolidColorTextureCache.GetTexture(new Color(45, 45, 15));
                    break;

                case ClassicUO.MobileUI.VoiceState.Error:
                    _iconLabel.Text = "❌";
                    _statusLabel.Text = ClassicUO.MobileUI.MobileUiTranslation.IsRussian ? "Ошибка" : "Error";
                    _background = SolidColorTextureCache.GetTexture(new Color(45, 20, 20));
                    break;

                case ClassicUO.MobileUI.VoiceState.Idle:
                default:
                    _iconLabel.Text = "🎤";
                    _iconLabel.Hue = 0x0035;
                    _statusLabel.Text = ClassicUO.MobileUI.MobileUiTranslation.IsRussian ? "Голос" : "Voice";
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
                _touchDownTime = Time.Ticks;
                _isTouchHeld = false;
            }
        }

        public override void Update()
        {
            base.Update();

            // Push-to-Talk детектор: если удерживаем палец более 350 мс без перетаскивания
            if (Mouse.LButtonPressed && MouseIsOver && _touchDownTime > 0)
            {
                Point offset = Mouse.LDragOffset;
                if (Math.Abs(offset.X) < 8 && Math.Abs(offset.Y) < 8)
                {
                    if (!_isTouchHeld && Time.Ticks - _touchDownTime > 350)
                    {
                        _isTouchHeld = true;
                        if (!ClassicUO.MobileUI.MobileVoiceInput.IsListening)
                        {
                            ClassicUO.MobileUI.MobileVoiceInput.StartListening(World);
                        }
                    }
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

            Point offset = Mouse.LDragOffset;

            // Если не было смещения кнопки (не перетаскивание)
            if (Math.Abs(offset.X) < 8 && Math.Abs(offset.Y) < 8)
            {
                if (_isTouchHeld)
                {
                    // Завершение Push-to-Talk
                    ClassicUO.MobileUI.MobileVoiceInput.StopListening();
                }
                else
                {
                    // Одиночный тап (переключение состояния)
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
