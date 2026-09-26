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
        private const int Height_ = 380;

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

            // --- размер окон книг (пресеты) ---
            Add(new Label(
                ClassicUO.MobileUI.MobileUiController.T("window_size") + ": " + ClassicUO.MobileUI.MobileUiController.GetPresetName(),
                true,
                0xFFFF,
                Width_ - 40,
                255,
                FontStyle.BlackBorder)
            {
                X = 24,
                Y = 160
            });

            Add(new NiceButton(24, 182, 34, 26, ButtonAction.Activate, "−")
            {
                ButtonParameter = 10,
                IsSelectable = false
            });

            Add(new NiceButton(62, 182, 34, 26, ButtonAction.Activate, "+")
            {
                ButtonParameter = 11,
                IsSelectable = false
            });

            // --- масштаб сумок и контейнеров ---
            Add(new Label(
                ClassicUO.MobileUI.MobileUiController.T("containers_scale") + ": " + ClassicUO.MobileUI.MobileUiController.ContainerScale + "%",
                true,
                0xFFFF,
                Width_ - 40,
                255,
                FontStyle.BlackBorder)
            {
                X = 24,
                Y = 214
            });

            Add(new NiceButton(24, 236, 34, 26, ButtonAction.Activate, "−")
            {
                ButtonParameter = 20,
                IsSelectable = false
            });

            Add(new NiceButton(62, 236, 34, 26, ButtonAction.Activate, "+")
            {
                ButtonParameter = 21,
                IsSelectable = false
            });

            // --- режим рюкзака: сетка (Diablo) / классика ---
            bool isGrid = ClassicUO.MobileUI.MobileUiController.GridContainers;
            string gridModeText = ClassicUO.MobileUI.MobileUiController.T("grid_containers") + ": " +
                                  ClassicUO.MobileUI.MobileUiController.T(isGrid ? "grid_view" : "classic_view");
            Add(new Label(
                gridModeText,
                true,
                0xFFFF,
                Width_ - 40,
                255,
                FontStyle.BlackBorder)
            {
                X = 24,
                Y = 268
            });

            Add(new Button(30, 0x0481, 0x0482, 0x0483)
            {
                X = 24,
                Y = 290,
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
                Y = 296
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
                Y = 328
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

                case 10:
                    ClassicUO.MobileUI.MobileUiController.PrevWindowPreset();

                    break;

                case 11:
                    ClassicUO.MobileUI.MobileUiController.NextWindowPreset();

                    break;

                case 20:
                    ClassicUO.MobileUI.MobileUiController.ChangeContainerScale(-20);

                    break;

                case 21:
                    ClassicUO.MobileUI.MobileUiController.ChangeContainerScale(20);

                    break;

                case 30:
                    ClassicUO.MobileUI.MobileUiController.ToggleGridContainers();

                    break;

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
