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
        private const int Width_ = 450;
        private const int Height_ = 250;

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
                Y = 48
            });

            Add(new Button(1, 0x0481, 0x0482, 0x0483)
            {
                X = 24,
                Y = 70,
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
                Y = 76
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
                Y = 104
            });

            Add(new Button(2, 0x0481, 0x0482, 0x0483)
            {
                X = 24,
                Y = 126,
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
                Y = 132
            });

            // --- подсказки по книгам и сумкам ---
            Add(new Label(
                "• " + ClassicUO.MobileUI.MobileUiController.T("books_hint"),
                true,
                0x0035,
                Width_ - 48,
                255,
                FontStyle.BlackBorder)
            {
                X = 24,
                Y = 162
            });

            Add(new Label(
                "• " + ClassicUO.MobileUI.MobileUiController.T("backpack_hint"),
                true,
                0x03B2,
                Width_ - 48,
                255,
                FontStyle.BlackBorder)
            {
                X = 24,
                Y = 184
            });

            // --- закрыть ---
            Add(new Button(0, 0x0481, 0x0482, 0x0483)
            {
                X = Width_ - 130,
                Y = Height_ - 38,
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
                X = Width_ - 108,
                Y = Height_ - 32
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
