// Мобильный интерфейс: настройки модуля.
//
// Лежит рядом с данными шарда: <папка данных UO>/mobileui.json
// Специально НЕ трогаем штатный settings.json клиента — свои настройки
// держим отдельно, чтобы правки не конфликтовали с апстримом и не терялись
// при обновлении клиента.

using System;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;
using ClassicUO.Configuration;
using ClassicUO.Utility.Logging;

namespace ClassicUO.MobileUI
{
    public sealed class MobileUiConfig
    {
        /// <summary>Мобильный интерфейс включён.</summary>
        [JsonPropertyName("enabled")]
        public bool Enabled { get; set; }

        /// <summary>Язык интерфейса: "ru" или "en".</summary>
        [JsonPropertyName("language")]
        public string Language { get; set; } = MobileUiStrings.Ru;

        /// <summary>Показывать экранную кнопку «Моб. UI».</summary>
        [JsonPropertyName("show_button")]
        public bool ShowButton { get; set; } = true;

        /// <summary>Высота строки списка в пикселях (размер предметов в меню).</summary>
        [JsonPropertyName("row_height")]
        public int RowHeight { get; set; } = 44;

        /// <summary>Пресет размера окна: 0 — компактное, 1 — обычное, 2 — широкое.</summary>
        [JsonPropertyName("window_preset")]
        public int WindowPreset { get; set; } = 1;

        [JsonIgnore]
        public string FilePath { get; private set; } = "";

        private static readonly JsonSerializerOptions WriteOptions = new JsonSerializerOptions
        {
            WriteIndented = true,
            Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping
        };

        public static MobileUiConfig Load()
        {
            string dir = "";

            try
            {
                dir = Settings.GlobalSettings.UltimaOnlineDirectory ?? "";
            }
            catch (Exception)
            {
                // настройки могут быть ещё не инициализированы — не критично
            }

            var config = new MobileUiConfig();

            try
            {
                config.FilePath = Path.Combine(dir, "mobileui.json");

                if (File.Exists(config.FilePath))
                {
                    var loaded = JsonSerializer.Deserialize<MobileUiConfig>(File.ReadAllText(config.FilePath));

                    if (loaded != null)
                    {
                        config.Enabled = loaded.Enabled;
                        config.ShowButton = loaded.ShowButton;
                        config.RowHeight = loaded.RowHeight > 0 ? loaded.RowHeight : 44;
                        config.WindowPreset = loaded.WindowPreset < 0 || loaded.WindowPreset > 2 ? 1 : loaded.WindowPreset;
                        config.Language = MobileUiStrings.Normalize(loaded.Language);
                    }
                }
                else
                {
                    // при первом запуске сразу создаём файл, чтобы его было видно и можно было править
                    config.Save();
                }
            }
            catch (Exception ex)
            {
                Log.Error($"[MobileUI] не удалось прочитать {config.FilePath}: {ex.Message}");
            }

            Log.Info($"[MobileUI] настройки: enabled={config.Enabled}, язык={config.Language}, файл={config.FilePath}");

            return config;
        }

        public void Save()
        {
            try
            {
                if (string.IsNullOrEmpty(FilePath))
                {
                    return;
                }

                File.WriteAllText(FilePath, JsonSerializer.Serialize(this, WriteOptions));
            }
            catch (Exception ex)
            {
                Log.Error($"[MobileUI] не удалось сохранить настройки: {ex.Message}");
            }
        }
    }
}
