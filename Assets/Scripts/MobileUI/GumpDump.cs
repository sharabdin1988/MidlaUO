// Диагностика для мобильного интерфейса.
//
// Серверные окна (ковка, все крафты, магазины, банк, кастомные меню шарда) приходят
// от сервера как layout-текст. Чтобы спроектировать мобильную отрисовку по реальным
// данным, а не по догадкам, этот модуль сохраняет каждое уникальное окно в файл.
//
// Файл: <persistentDataPath>/gumpdump.txt — на Android это
// /sdcard/Android/data/<пакет>/files/gumpdump.txt, то есть рядом с данными шарда.
//
// Свойства: дубликаты отбрасываются по (gumpId + layout), всего не больше MaxEntries
// записей за сессию, любые ошибки глотаются — диагностика не должна мешать игре.

using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEngine;

namespace ClassicUO.MobileUI
{
    public static class GumpDump
    {
        private const int MaxEntries = 400;

        private static readonly HashSet<string> _seen = new HashSet<string>();
        private static string _path;
        private static int _count;

        public static string FilePath
        {
            get
            {
                if (_path == null)
                {
                    try
                    {
                        _path = Path.Combine(Application.persistentDataPath, "gumpdump.txt");
                    }
                    catch (Exception)
                    {
                        _path = "gumpdump.txt";
                    }
                }

                return _path;
            }
        }

        public static int Count => _count;

        /// <summary>Записать серверное окно. Вызывается из обработчика пакета 0xB0.</summary>
        public static void Record(uint sender, uint gumpId, int x, int y, string layout, string[] lines)
        {
            try
            {
                if (_count >= MaxEntries)
                {
                    return;
                }

                string key = gumpId + "|" + (layout ?? string.Empty);

                if (!_seen.Add(key))
                {
                    return;
                }

                _count++;

                var sb = new StringBuilder();
                sb.Append("===== ").Append(DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"))
                  .Append("  gumpId=0x").Append(gumpId.ToString("X8"))
                  .Append("  sender=0x").Append(sender.ToString("X8"))
                  .Append("  pos=").Append(x).Append(',').Append(y)
                  .Append(" =====").AppendLine();

                sb.AppendLine("LAYOUT: " + (layout ?? string.Empty));

                if (lines != null)
                {
                    for (int i = 0; i < lines.Length; i++)
                    {
                        sb.Append("TEXT[").Append(i).Append("]: ").AppendLine(lines[i] ?? string.Empty);
                    }
                }

                sb.AppendLine();

                File.AppendAllText(FilePath, sb.ToString());
            }
            catch (Exception)
            {
                // молча: диагностика не должна ломать клиент
            }
        }
    }
}
