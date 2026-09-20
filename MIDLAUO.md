# MidlaUO — наш Android-клиент Ultima Online

Форк **[MobileUO/MobileUO](https://github.com/MobileUO/MobileUO)** (Unity **6000.3.8f1**,
цель сборки — **Android**; внутри исходники ClassicUO и встроенный ассистент —
порт AssistUO). **https://github.com/sharabdin1988/MidlaUO**

**Зачем:** свой мобильный клиент для шарда **Middle-earth** (`middle-earth.ru`,
файловый сервер `d.midla.ru`), чтобы держать нужные нам исправления и не зависеть
от чужих релизов. Лицензия upstream — AGPLv3, поэтому форк публичный (это же даёт
бесплатные минуты GitHub Actions для сборки).

---

## 0. ВАЖНО: рабочая ветка — `dev`, а не `master`

В upstream **`master` — это древний снапшот 0.9.0**, а весь актуальный код и
релизы живут в ветке **`dev`** (там `Assets/Scripts/VersionNumber.txt` = `1.0.31`).
CI собирает APK именно из `dev` (`.github/workflows/build.yml`: триггер — push в `dev`).

Поэтому **вся наша работа — на базе `origin/dev`** (ветка `midla/fixes` создана из
`origin/dev`). Именно в `dev` есть официальный фикс 1.0.31 для `walk`/`run`
(«fixed inverted timer check … which was causing the commands to never fire off»:
`if (ScriptManager.LastWalk < DateTime.UtcNow)` → `>`), которого в `master` нет —
сборка из `master` дала бы клиент, где шаги из макроса вообще не срабатывают.

## 1. Что уже сделано в ветке `midla/fixes`

1. **Рекордер писал битую команду для бега** (`Assistant/Network/Handlers.cs`):
   `Direction` — это `[Flags]`-enum с `Running = 0x80`, поэтому шаг бегом
   записывался как `walk 'Left, Running'`; команда `walk` знала только 8 названий
   направлений, очередь оставалась пустой и падала на `Dequeue()`
   (`InvalidOperationException: Queue empty`) — отсюда «макрос чуть ходит и
   вываливается с ошибкой». Теперь пишется `run 'Left'` / `walk 'Left'`.
2. **`Commands.Walk`/`Run`**: новый `TryParseDirection` терпит старый формат
   (`"Left, Running"` — обрезает по запятой, игнорирует `running`); пустая очередь
   больше не роняет скрипт (`ScriptManager.Error` + `return true`).
3. **`Interpreter.ExecuteCommand`**: вызов обработчика команды обёрнут в try/catch —
   ошибка скрипта становится `RunTimeError`, а не вылетает исключением через
   таймер (`Timer.cs` вызывает `OnTick()` без защиты).

Синтаксис проверен компилятором Mono (см. §5) — 0 синтаксических ошибок.

## 2. Что ещё планируем

4. **verdata-анимации (`FileID 5/6`)** — не применяются вообще (`UOFileManager` их
   пропускает, `UOFileMul._patch` не используется, `AnimationDirection.IsVerdata`
   не выставляется); из-за этого не рисовались ездовые дракон/медведь и
   плащ-крылья шарда — их графика есть только в `verdata.mul`. Разбор:
   upstream issue [#108](https://github.com/MobileUO/MobileUO/issues/108).
5. **art-патч verdata** применяется по индексу `BlockID − 0x4000` вместо `BlockID`
   (в актуальном ClassicUO уже правильно) → кастомный арт уезжает в land-тайлы.
6. Мелочи: `CountStealthSteps` по умолчанию, иконка/название, локализация.

## 3. Как собирается APK

Unity Editor на телефоне не запускается, поэтому сборка — в GitHub Actions:
`.github/workflows/build.yml` (`game-ci/unity-builder@v4`, Unity 6000.3.8f1,
`targetPlatform: Android`). В форке добавлен **`workflow_dispatch`** — сборку можно
запускать вручную из любой ветки.

| Секрет репозитория | Что это | Статус |
|---|---|---|
| `ANDROID_STAGING_KEYSTORE_BASE64` | ключ подписи в base64 | ✅ прописан |
| `ANDROID_STAGING_KEYSTORE_PASS` | пароль хранилища | ✅ |
| `ANDROID_STAGING_KEYALIAS_NAME` | `midlauo` | ✅ |
| `ANDROID_STAGING_KEYALIAS_PASS` | пароль ключа | ✅ |
| `UNITY_LICENSE` (или `UNITY_EMAIL` + `UNITY_PASSWORD`) | бесплатный Unity Personal | ⏳ не задан |

Сам ключ — на устройстве (`~/.midlauo/ks.jks`, пароль `~/.midlauo/keystore.pass`)
и в age-сейфе. Actions в форке надо один раз включить в веб-интерфейсе.

## 4. Наш профиль сборки

`Assets/Editor/PreBuildScript/PreBuildScript.cs` читает
`Assets/appsettings.<BUILD_ENV>.json`; CI передаёт `-BUILD_ENV Staging`
(`net.mandaria.mobileuo` / «MandoUO»). Для себя — **`Assets/appsettings.Midla.json`**
(`-BUILD_ENV Midla`): пакет `net.midla.uo`, имя «MidlaUO». Наш клиент ставится
рядом с MandoUO; папку данных шарда надо скопировать:

```bash
cp -r /sdcard/Android/data/net.mandaria.mobileuo/files/midla.ru \
      /sdcard/Android/data/net.midla.uo/files/
# и, если правки verdata ещё не в сборке:
python3 ~/uo-verdata-merge/merge_verdata.py /sdcard/Android/data/net.midla.uo/files/midla.ru
```

## 5. Как проверить C# без Unity

```bash
deb /usr/bin/apt-get install -y --no-install-recommends mono-mcs
mkdir -p /data/debian/tmp/midla        # chroot НЕ видит /data/data
cp Assets/Scripts/Assistant/Scripts/{Commands,Interpreter}.cs \
   Assets/Scripts/Assistant/Network/Handlers.cs /data/debian/tmp/midla/
deb /usr/bin/mcs -unsafe -target:library -out:/tmp/x.dll /tmp/midla/*.cs 2>&1 \
  | grep -E "error CS(1[0-9]{3}|8[0-9]{3})"     # 0 = синтаксис чистый
rm -rf /data/debian/tmp/midla
```

Ошибки `CS0246`/`CS0234` («тип не найден») — норма: сборок Unity/ClassicUO рядом нет.

## 6. Порядок работ

- [x] Форк создан, изучен конвейер сборки; выяснено, что рабочая ветка — `dev`.
- [x] `appsettings.Midla.json`, `workflow_dispatch`, ключ подписи и его секреты.
- [x] Правки 1–3 (рекордер/макрос/защита скриптов) — ветка `midla/fixes` на базе `dev`.
- [ ] Первый прогон CI на чистой `dev` — проверить сам конвейер (лицензия → APK).
- [ ] Правки 4–5 (verdata), сборка, установка, проверка в игре.
- [ ] Лучшее — PR в upstream (их PR-ы целятся в `dev`).
