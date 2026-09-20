# MidlaUO — наш Android-клиент Ultima Online

Форк **[MobileUO/MobileUO](https://github.com/MobileUO/MobileUO)** (проект собран под
Unity **6000.3.8f1**, цель сборки — **Android**, язык C#; внутри лежат исходники
ClassicUO и встроенный ассистент — порт AssistUO).

**Зачем:** свой мобильный клиент для шарда **Middle-earth** (`middle-earth.ru`,
файловый сервер `d.midla.ru`), чтобы не зависеть от чужих релизов и держать
исправления, которые нужны именно нам (и, где получится, отдавать их в upstream
через PR).

> **Лицензия.** У upstream лицензия «Other» (см. `LICENSE`/README upstream),
> модуль ассистента — AGPL-3.0 с оговоркой, что собранный результат должен
> оставаться публично доступным. Поэтому **форк публичный** (это же даёт
> бесплатные минуты GitHub Actions для сборки).

---

## 1. Что будем править (наш патч-набор)

1. **verdata-анимации (`FileID 5/6`) не применяются вообще** — из-за этого на
   Middle-earth не рисовались ездовые дракон и белый медведь (AnimID 442/443) и
   плащ-крылья (AnimID 558): их графика есть только в `verdata.mul`.
   Правки: `Assets/Scripts/ClassicUO/src/IO/Resources/AnimationsLoader.cs`
   (при построении `DataIndex` учитывать патчи; читать блок из `Verdata.File` в
   `LoadDirectionGroup`; поправить `GetAnimationDimensions`), поле
   `AnimationDirection.IsVerdata` наконец начнёт использоваться.
   Подробности и доказательства: upstream issue
   [#108](https://github.com/MobileUO/MobileUO/issues/108).
2. **art-патч verdata применяется со сдвигом** — в
   `ClassicUO/src/IO/UOFileManager.cs` запись идёт в
   `Entries[vh.BlockID - 0x4000]` вместо `Entries[vh.BlockID]` (в актуальном
   ClassicUO уже правильно) → кастомный арт уезжает в land-тайлы.
3. **Рекордер макросов пишет битую команду для бега**: пишет
   `walk 'Left, Running'` (enum `Direction` — `[Flags]` c `Running = 0x80`),
   а парсер знает только 8 названий направлений → очередь пустая →
   `InvalidOperationException: Queue empty` и скрипт падает. Надо писать
   `run 'Left'` / `walk 'Left'`.
4. **Защита от падений в скриптах**: в `Commands.Walk/Run` нормализовать аргумент
   (обрезать всё после запятой) и не вызывать `Dequeue()` на пустой очереди;
   обернуть вызов обработчиков команд в try/catch, чтобы ошибка скрипта не
   вылетала исключением через таймер (`Timer.cs:286`).
5. Мелочи по вкусу: `CountStealthSteps` по умолчанию, наш значок/название,
   русская локализация и т.п.

## 2. Как это собирается (Unity на телефоне не запускается!)

В репозитории уже есть готовый конвейер: **`.github/workflows/build.yml`**
(`game-ci/unity-builder@v4`, Unity 6000.3.8f1, `targetPlatform: Android`,
запуск — push в ветку **`dev`**, плюс PR в `dev`). Сборка APK идёт на
GitHub-раннере, к нам приходит артефакт.

Что нужно для сборки:

| Секрет репозитория | Что это |
|---|---|
| `UNITY_LICENSE` (или `UNITY_EMAIL` + `UNITY_PASSWORD`) | бесплатный Unity Personal: аккаунт Unity + активация лицензии (`.alf` → `.ulf`, либо email/пароль) |
| `ANDROID_STAGING_KEYSTORE_BASE64` | наш ключ подписи APK в base64 |
| `ANDROID_STAGING_KEYSTORE_PASS` | пароль хранилища |
| `ANDROID_STAGING_KEYALIAS_NAME` | имя ключа |
| `ANDROID_STAGING_KEYALIAS_PASS` | пароль ключа |

Ключ подписи сгенерирован на телефоне (`~/.midlauo/ks.jks`, base64 —
`~/.midlauo/ks.jks.b64`); в репозиторий он не попадает — только в secrets GitHub
и в наш сейф секретов.

**Actions в форке по умолчанию выключены** — их надо один раз включить в
веб-интерфейсе (вкладка Actions → «I understand my workflows, go ahead and
enable them»).

## 3. Наш профиль сборки (имя и пакет)

Пакет и название задаёт `Assets/Editor/PreBuildScript/PreBuildScript.cs`, читая
`Assets/appsettings.<BUILD_ENV>.json`; CI передаёт `-BUILD_ENV Staging`
(то есть `net.mandaria.mobileuo` / `MandoUO`). Для себя добавляем
**`Assets/appsettings.Midla.json`**:

```json
{
  "AndroidPackageName": "net.midla.uo",
  "ProductName": "MidlaUO",
  "AndroidUseCustomKeystore": true
}
```

и собираем с `-BUILD_ENV Midla`.

Почему отдельный пакет: наш клиент ставится **рядом** с MandoUO и не ломает его,
но и не наследует его папку данных — после первого запуска надо скопировать
данные шарда:

```bash
cp -r /sdcard/Android/data/net.mandaria.mobileuo/files/midla.ru \
      /sdcard/Android/data/net.midla.uo/files/
# и, если правки verdata ещё не в сборке, прогнать вплавление патчей:
python3 ~/uo-verdata-merge/merge_verdata.py /sdcard/Android/data/net.midla.uo/files/midla.ru
```

## 4. Порядок работ

- [x] 20.09.2026 — форк создан, склонирован, изучен конвейер сборки.
- [ ] Первый прогон CI **на чистом дереве** — проверить сам конвейер (лицензия,
      keystore, артефакт), не смешивая с нашими правками.
- [ ] Добавить `appsettings.Midla.json` + `workflow_dispatch` в `build.yml`
      (чтобы запускать сборку вручную из любой ветки).
- [ ] Внести правки 1–4, собирать, ставить на телефон, проверять в игре.
- [ ] Лучшие из правок — отдельными PR в upstream (`MobileUO/MobileUO`).

## 5. Полезное

- Установка на телефон: `pm install -r /sdcard/Download/MidlaUO-<версия>.apk`
  (от root, либо обычным способом из файлового менеджера).
- Ошибки клиента при отладке: `logcat` (тег `Unity`) и журнал игры
  `<папка данных>/Data/Client/JournalLogs/*.txt`.
- Шпаргалка по шарду и по клиенту: `~/mando-uo.md` на устройстве.
