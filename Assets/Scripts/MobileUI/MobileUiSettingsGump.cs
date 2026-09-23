// Мобильный интерфейс: панель настроек.
//
// Открывается по нажатию экранной кнопки «Моб. UI». Здесь два переключателя:
//   * включить/выключить мобильный интерфейс;
//   * язык интерфейса (русский / английский).
// Оба сохраняются в mobileui.json рядом с данными шарда.

using ClassicUO.Assets;
using ClassicUO.Game.UI.Controls;

namespace ClassicUO.Game.UI.Gumps
{
    internal class MobileUiSettingsGump : Gump
    {
        private const int Width_ = 420;
        private const int Height_ = 210;

        public MobileUiSettingsGump(World world) : base(world, 0, 0)
        {
            CanMove = true;
            AcceptMouseInput = true;
            CanCloseWithRightClick = true;

            Build();
        }

        public override GumpType GumpType => GumpType.None;

        private void Build()
        {
            var cfg = ClassicUO.MobileUI.MobileUiController.Config;
            string t = "window_title";
            string on = "state_on";
            string off = "state_off";

            Add(new ResizePic(0x0A3C)
            {
                X = 0,
                Y = 0,
                Width = Width_,
                Height = Height_
            });

            Width = Width_;
            Height = Height_;

            Add(new Label(
                ClassicUO.MobileUI.MobileUiController.T(t),
                true,
                0x0386,
                Width_ - 20,
                255,
                FontStyle.BlackBorder,
                TEXT_ALIGN_TYPE.TS_CENTER)
            {
                X = 10,
                Y = 12
            });

            // --- переключатель мобильного интерфейса ---
            bool enabled = cfg != null && cfg.Enabled;

            Add(new Label(
                ClassicUO.MobileUI.MobileUiController.T("enabled") + ": " + ClassicUO.MobileUI.MobileUiController.T(enabled ? on : off),
                true,
                0xFFFF,
                Width_ - 40,
                255,
                FontStyle.BlackBorder)
            {
                X = 24,
                Y = 52
            });

            Add(new Button(1, 0x0481, 0x0482, 0x0483)
            {
                X = 24,
                Y = 74,
                ButtonAction = ButtonAction.Activate
            });

            Add(new Label(
                ClassicUO.MobileUI.MobileUiController.T("hint_switch"),
                true,
                0x03B2,
                Width_ - 40,
                255,
                FontStyle.BlackBorder)
            {
                X = 66,
                Y = 80
            });

            // --- переключатель языка ---
            Add(new Label(
                ClassicUO.MobileUI.MobileUiController.T("language") + ": " +
                ClassicUO.MobileUI.MobileUiController.T(ClassicUO.MobileUI.MobileUiController.Language == ClassicUO.MobileUI.MobileUiStrings.Ru ? "lang_ru" : "lang_en"),
                true,
                0xFFFF,
                Width_ - 40,
                255,
                FontStyle.BlackBorder)
            {
                X = 24,
                Y = 116
            });

            Add(new Button(3, 0x0481, 0x0482, 0x0483)
            {
                X = 24,
                Y = 162,
                ButtonAction = ButtonAction.Activate
            });

            Add(new Label(
                ClassicUO.MobileUI.MobileUiController.T("open_backpack"),
                true,
                0x0035,
                Width_ - 40,
                255,
                FontStyle.BlackBorder)
            {
                X = 66,
                Y = 168
            });

            Add(new Button(2, 0x0481, 0x0482, 0x0483)
            {
                X = 24,
                Y = 138,
                ButtonAction = ButtonAction.Activate
            });

            Add(new Label(
                ClassicUO.MobileUI.MobileUiController.T("hint_button"),
                true,
                0x03B2,
                Width_ - 40,
                255,
                FontStyle.BlackBorder)
            {
                X = 66,
                Y = 144
            });

            Add(new Label(
                ClassicUO.MobileUI.MobileUiController.T("screens_soon"),
                true,
                0x0035,
                Width_ - 40,
                255,
                FontStyle.BlackBorder)
            {
                X = 24,
                Y = 178
            });

            // --- закрыть ---
            Add(new Button(0, 0x0481, 0x0482, 0x0483)
            {
                X = Width_ - 140,
                Y = Height_ - 40,
                ButtonAction = ButtonAction.Activate
            });

            Add(new Label(
                ClassicUO.MobileUI.MobileUiController.T("close"),
                true,
                0xFFFF,
                -1,
                255,
                FontStyle.BlackBorder)
            {
                X = Width_ - 118,
                Y = Height_ - 34
            });

            X = 60;
            Y = 120;
        }

        public override void OnButtonClick(int buttonID)
        {
            switch (buttonID)
            {
                case 1:
                    ClassicUO.MobileUI.MobileUiController.ToggleEnabled();

                    break;

                case 2:
                    ClassicUO.MobileUI.MobileUiController.ToggleLanguage();

                    break;

                case 3:
                    ClassicUO.MobileUI.MobileUiController.OpenBackpack();

                    return;

                case 0:
                default:
                    Dispose();

                    return;
            }

            // пересоздаём панель, чтобы подписи обновились
            ClassicUO.MobileUI.MobileUiController.OpenSettings();

            base.OnButtonClick(buttonID);
        }
    }
}
