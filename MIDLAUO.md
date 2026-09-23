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

## 7. История работ (хронология, каждый шаг)

Полная история — чтобы через месяц было видно, что и зачем делалось. Все коммиты —
в ветке `midla/fixes`, если не указано иное.

### 20.09.2026 — графика шарда не отображалась

- **Проблема**: MandoUO (клиент шарда Middle-earth) не показывал кастомную графику:
  эльфы выглядели дефолтными скинами, дракон-маунт, белый медведь и плащ-крылья не
  рисовались вообще.
- **Причина** (по исходникам): клиенты на ClassicUO не применяют патчи анимаций из
  `verdata.mul` — в `UOFileManager` они отбрасываются (`vh.FileID != 5 && != 6`), а
  `AnimationDirection.IsVerdata` в рендерере вообще никогда не присваивался.
- **Что сделали**: написали инструмент `merge_verdata.py`, который вплавляет патчи
  шарда в `.mul`/`.idx` (art, anim, gumpart, tiledata, skills). Клиент заработал
  сразу, без правок кода.

### 20.09.2026 — макросы ассистента

- **Проблема**: записанный макрос с шагами воспроизводился «чуть-чуть» и падал с
  ошибкой: рекордер писал `walk 'Left, Running'` (флаг бега внутри направления),
  такая команда не находила направление, очередь оставалась пустой и `Dequeue()`
  бросал `InvalidOperationException`.
- **Что сделали** (3 файла): рекордер пишет `run 'X'` / `walk 'X'`; добавлен
  `TryParseDirection` (терпит старый формат `"Left, Running"`); пустая очередь
  стала ошибкой скрипта, а не исключением; вызов обработчика обёрнут в try/catch.

### 21.09.2026 — фикс анимаций вердаты в самом клиенте

- **Что сделали**: `AnimationsLoader` строит карту патчей FileID 6 и применяет их к
  блокам `anim.mul`; `ReadMULAnimationFrames` читает такие блоки из `verdata.mul`;
  признак `IsVerdata` проброшен в рендерер. 2 файла, +110/−5.
- **Отправлено в апстрим**: `ClassicUO/ClassicUO#1938` → **смержен 23.09.2026**
  основателем проекта (`andreakarasho`), коммит `7335535`; CI зелёный на
  ubuntu/windows/macos. То есть фикс теперь в официальном ClassicUO.

### 21.09.2026 — форк MidlaUO и профиль сборки

- Форк `sharabdin1988/MidlaUO` от MobileUO `dev` (1.0.31): свой профиль сборки
  `Assets/appsettings.Midla.json` → пакет `net.midla.uo`, «MidlaUO» — ставится
  рядом с MandoUO и ничего не затирает.
- В MobileUO отправляли два PR: `#109` (макросы, остался открыт) и `#110`
  (анимации вердаты — закрыт как дубль, когда апстрим принял #1938). По просьбе
  владельца новых PR туда не делаем: MobileUO заброшен, живой клиент — MandoUO.

### 22–23.09.2026 — лицензия Unity и первая успешная сборка

- Ручная активация Personal на сайте Unity больше не работает (в их скрипте прямо
  написано, что offline-активация только для Enterprise/Industry).
- Рабочий путь: **Unity Hub** на ПК → «Get a free personal license» → файл
  `Unity_lic.ulf`. Его передали на телефон одноразовым HTTP-приёмником, залили в
  секреты (`UNITY_LICENSE`, `UNITY_EMAIL`, `UNITY_PASSWORD`) — содержимое нигде не
  печаталось.
- Первая успешная сборка: `MandoUO/MidlaUO 1.0.31+4`, затем `+5`.

### 23.09.2026 — почему пропадала одежда и как это решено

- Починили **`Verdata.Load()`** (`8629d4b`) — в ветке MobileUO этот вызов отсутствовал,
  поэтому `verdata.mul` вообще не читался и **ни один** патч шарда не применялся.
  В апстриме ClassicUO этот баг закрыли только в сентябре 2025 (PR #1813).
- После этого выяснилось: **собственное применение вердаты ломает отрисовку надетых
  вещей** (персонаж без одежды и в мире, и в кукле). Доказали, что данные тут не при
  чём: свежие данные шарда и наши вплавленные файлы совпадают (хеши, tiledata —
  1088 предметов, 0 различий, код клиентов отличается лишь нашими правками).
- **Рабочее решение**: держать данные со вплавленными патчами и `verdata.mul`
  отключённым. Проверено на устройстве: персонаж одет, графика шарда на месте.
- **Побочно**: клиент сам скачивает данные шарда при первом запуске (686 МБ) —
  ручное копирование не нужно.

### 23.09.2026 — ассистент подтверждён на устройстве

- Владелец записал макрос и воспроизвёл его: работает, ошибок нет. В профиле
  команды в правильном формате (`run 'Up'`, `walk 'South'`, без `Running` и запятых).
- Найдена механика записи: команды пишутся **в текстовое поле открытого окна
  ассистента**; если окно закрыто — теряются молча.

### 23.09.2026 — начало мобильного интерфейса

- Основа модуля `Assets/Scripts/MobileUI/`: свои настройки в `mobileui.json`
  (включён/выключен, язык), таблица строк русский/английский, экранная кнопка
  «Моб. UI» и панель с двумя переключателями.
- В штатный код — три однострочных хука (тип гумпа, восстановление кнопки из
  профиля, инициализация при загрузке мира).
- Дальше: сумка списком с параметрами предметов, рунбук, общий мобильный рендерер
  серверных окон (ковка, все крафты, магазины, банк).

### Инфраструктура, которую собрали по ходу

- `~/midlauo-ci/build-midlauo.py` — сборка одной командой: проверка секретов, запуск
  workflow, слежение, скачивание APK в `/sdcard/Download`.
- `~/midlauo-ci/set-secret.sh` — заливка секретов в форк без печати содержимого.
- `~/midlauo-ci/TEST-PLAN.md` — что и как проверять на устройстве.
- Сторож GitHub (`/data/adb/service.d/midlauo-watch.sh`) — уведомления на телефон о
  новых релизах клиента, коммитах и ответах в наших тредах.
- `Assets/Scripts/MobileUI/GumpDump.cs` — дамп серверных окон шарда в файл (сырьё для
  мобильного интерфейса).
