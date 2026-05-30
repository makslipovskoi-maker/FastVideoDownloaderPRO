# Fast Video Downloader PRO

Windows-программа для быстрого скачивания видео по ссылке в максимальном качестве через `yt-dlp` и `FFmpeg`.

## Главное

- **Быстро скачать MP4** — старается скачать готовый совместимый MP4/H.264 без долгой перекодировки.
- **Скачать MAX качество** — скачивает лучшее доступное качество в MKV без потери качества.
- **Скачать H.264 MP4** — отдельная опция для создания совместимой копии, если файл не открывается стандартным плеером Windows.

Программа не обходит DRM, платный доступ, закрытые аккаунты или авторизацию. Используйте только для видео, которые принадлежат вам или на скачивание которых у вас есть разрешение.

## Как получить готовый EXE через GitHub

1. Откройте вкладку **Actions**.
2. Выберите workflow **Build Windows EXE**.
3. Нажмите **Run workflow** или дождитесь автоматической сборки после push.
4. После завершения скачайте artifact **FastVideoDownloaderPRO-win-x64-portable**.
5. Распакуйте ZIP и запустите:

```text
FastVideoDownloaderPRO.exe
```

В artifact уже автоматически добавляются:

```text
tools/yt-dlp.exe
tools/ffmpeg.exe
tools/ffprobe.exe
```

## Локальная сборка

```powershell
dotnet publish .\src\FastVideoDownloaderPRO\FastVideoDownloaderPRO.csproj -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true -p:EnableCompressionInSingleFile=true -o .\publish
```

## Структура portable-версии

```text
FastVideoDownloaderPRO.exe
tools/
  yt-dlp.exe
  ffmpeg.exe
  ffprobe.exe
downloads/
logs/
```

## Режимы

### Быстро скачать MP4

Используется формат:

```text
bestvideo[ext=mp4][vcodec^=avc1]+bestaudio[ext=m4a]/best[ext=mp4]/best
```

### Скачать MAX качество

Используется формат:

```text
bv*+ba/b
```

Результат сохраняется в MKV, потому что MKV безопаснее для AV1/VP9/Opus и сохраняет оригинальное качество.

### Скачать H.264 MP4

Сначала скачивается лучший источник. Затем программа проверяет файл через `ffprobe`:

- если файл уже H.264/AAC — делает быстрый remux без перекодировки;
- если файл VP9/AV1/HEVC — предлагает создать H.264-копию.

## Логи

Все логи сохраняются в папку `logs/` и доступны из интерфейса программы.