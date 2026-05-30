using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace FastVideoDownloaderPRO;

public sealed class MainWindow : Window
{
    private readonly TextBox _urlBox = new();
    private readonly TextBox _logBox = new();
    private readonly TextBlock _statusText = new();
    private readonly ProgressBar _progress = new();
    private readonly ComboBox _fragmentsBox = new();
    private readonly Button _analyzeButton = new();
    private readonly Button _fastButton = new();
    private readonly Button _maxButton = new();
    private readonly Button _h264Button = new();
    private readonly Button _cancelButton = new();
    private CancellationTokenSource? _cts;
    private string _currentLogPath = string.Empty;

    private string AppRoot => AppContext.BaseDirectory;
    private string ToolsDir => Path.Combine(AppRoot, "tools");
    private string DownloadsDir => Path.Combine(AppRoot, "downloads");
    private string LogsDir => Path.Combine(AppRoot, "logs");

    public MainWindow()
    {
        Title = "Fast Video Downloader PRO";
        Width = 1180;
        Height = 780;
        MinWidth = 980;
        MinHeight = 680;
        Background = Brush("#0F172A");
        Foreground = Brush("#E5E7EB");
        FontFamily = new FontFamily("Segoe UI");

        Directory.CreateDirectory(ToolsDir);
        Directory.CreateDirectory(DownloadsDir);
        Directory.CreateDirectory(LogsDir);

        Content = BuildUi();
        Loaded += (_, _) => OnLoaded();
    }

    private UIElement BuildUi()
    {
        var root = new Grid { Margin = new Thickness(14) };
        root.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        root.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        root.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        root.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });

        var title = new TextBlock
        {
            Text = "Fast Video Downloader PRO",
            FontSize = 28,
            FontWeight = FontWeights.Bold,
            Foreground = Brush("#F8FAFC"),
            Margin = new Thickness(0, 0, 0, 6)
        };

        var subtitle = new TextBlock
        {
            Text = "Вставь ссылку и нажми кнопку. Быстрый режим не перекодирует видео, поэтому работает быстрее.",
            Foreground = Brush("#CBD5E1"),
            TextWrapping = TextWrapping.Wrap,
            Margin = new Thickness(0, 0, 0, 10)
        };

        _urlBox.Text = "https://rutube.ru/shorts/d98d7b791037d13b4eed88eeff2fef66/";
        _urlBox.Height = 42;
        _urlBox.FontSize = 15;
        _urlBox.VerticalContentAlignment = VerticalAlignment.Center;
        _urlBox.Margin = new Thickness(0, 0, 0, 10);

        var topPanel = new StackPanel();
        topPanel.Children.Add(title);
        topPanel.Children.Add(subtitle);
        topPanel.Children.Add(_urlBox);
        Grid.SetRow(topPanel, 0);
        root.Children.Add(Card(topPanel));

        _analyzeButton.Content = "Анализировать";
        _fastButton.Content = "Быстро скачать MP4";
        _maxButton.Content = "Скачать MAX качество";
        _h264Button.Content = "Скачать H.264 MP4";
        _cancelButton.Content = "Стоп";
        _cancelButton.IsEnabled = false;

        StyleButton(_analyzeButton, "#93C5FD");
        StyleButton(_fastButton, "#86EFAC");
        StyleButton(_maxButton, "#FACC15");
        StyleButton(_h264Button, "#FDBA74");
        StyleButton(_cancelButton, "#FCA5A5");

        _analyzeButton.Click += async (_, _) => await AnalyzeAsync();
        _fastButton.Click += async (_, _) => await DownloadAsync(DownloadMode.FastMp4);
        _maxButton.Click += async (_, _) => await DownloadAsync(DownloadMode.MaxQuality);
        _h264Button.Click += async (_, _) => await DownloadAsync(DownloadMode.CompatibleH264);
        _cancelButton.Click += (_, _) => _cts?.Cancel();

        _fragmentsBox.Width = 70;
        foreach (var item in new[] { "4", "8", "16", "32" }) _fragmentsBox.Items.Add(item);
        _fragmentsBox.SelectedItem = "16";

        var installButton = NewButton("Установить tools", "#C4B5FD");
        installButton.Click += async (_, _) => await InstallToolsAsync();
        var updateButton = NewButton("Обновить yt-dlp", "#C4B5FD");
        updateButton.Click += async (_, _) => await UpdateYtDlpAsync();
        var openDownloadsButton = NewButton("Открыть downloads", "#CBD5E1");
        openDownloadsButton.Click += (_, _) => OpenFolder(DownloadsDir);
        var openLogsButton = NewButton("Открыть logs", "#CBD5E1");
        openLogsButton.Click += (_, _) => OpenFolder(LogsDir);
        var openToolsButton = NewButton("Открыть tools", "#CBD5E1");
        openToolsButton.Click += (_, _) => OpenFolder(ToolsDir);

        var controls = new WrapPanel { Margin = new Thickness(0) };
        controls.Children.Add(_analyzeButton);
        controls.Children.Add(_fastButton);
        controls.Children.Add(_maxButton);
        controls.Children.Add(_h264Button);
        controls.Children.Add(_cancelButton);
        controls.Children.Add(new TextBlock { Text = "  Потоки:", VerticalAlignment = VerticalAlignment.Center, Foreground = Brush("#CBD5E1") });
        controls.Children.Add(_fragmentsBox);
        controls.Children.Add(installButton);
        controls.Children.Add(updateButton);
        controls.Children.Add(openDownloadsButton);
        controls.Children.Add(openLogsButton);
        controls.Children.Add(openToolsButton);

        Grid.SetRow(controls, 1);
        root.Children.Add(Card(controls));

        var statusPanel = new StackPanel();
        _statusText.Text = "Готово к работе.";
        _statusText.FontWeight = FontWeights.SemiBold;
        _statusText.Foreground = Brush("#F8FAFC");
        _progress.Minimum = 0;
        _progress.Maximum = 100;
        _progress.Height = 18;
        _progress.Margin = new Thickness(0, 8, 0, 0);
        statusPanel.Children.Add(_statusText);
        statusPanel.Children.Add(_progress);
        Grid.SetRow(statusPanel, 2);
        root.Children.Add(Card(statusPanel));

        _logBox.AcceptsReturn = true;
        _logBox.AcceptsTab = true;
        _logBox.IsReadOnly = true;
        _logBox.VerticalScrollBarVisibility = ScrollBarVisibility.Auto;
        _logBox.HorizontalScrollBarVisibility = ScrollBarVisibility.Auto;
        _logBox.FontFamily = new FontFamily("Consolas");
        _logBox.FontSize = 13;
        _logBox.Background = Brush("#020617");
        _logBox.Foreground = Brush("#E5E7EB");
        _logBox.BorderBrush = Brush("#334155");
        Grid.SetRow(_logBox, 3);
        root.Children.Add(Card(_logBox));

        return root;
    }

    private void OnLoaded()
    {
        Log("Папка программы: " + AppRoot);
        Log("Папка tools: " + ToolsDir);
        Log(ToolsStatus());
        Status(ToolsStatus());
    }

    private async Task AnalyzeAsync()
    {
        await RunUiTaskAsync("analyze", async token =>
        {
            var yt = RequireTool("yt-dlp.exe");
            var url = RequireUrl();
            Status("Анализирую доступные форматы...");
            var args = new List<string> { "--no-playlist", "--force-ipv4", "-F", url };
            var result = await RunProcessAsync(yt, args, token);
            if (result.ExitCode != 0) throw new InvalidOperationException("yt-dlp не смог проанализировать ссылку.");
            Status("Анализ завершён. Форматы показаны в логе.");
        });
    }

    private async Task DownloadAsync(DownloadMode mode)
    {
        await RunUiTaskAsync(mode.ToString(), async token =>
        {
            var yt = RequireTool("yt-dlp.exe");
            var ffmpeg = RequireTool("ffmpeg.exe");
            var url = RequireUrl();
            var started = DateTime.UtcNow.AddSeconds(-2);

            var args = BuildYtDlpArgs(mode, url, ffmpeg);
            Status(mode switch
            {
                DownloadMode.FastMp4 => "Быстро скачиваю MP4 без перекодировки...",
                DownloadMode.MaxQuality => "Скачиваю максимальное качество в MKV без перекодировки...",
                _ => "Скачиваю исходник для H.264 MP4..."
            });

            var result = await RunProcessAsync(yt, args, token);
            if (result.ExitCode != 0) throw new InvalidOperationException("Скачивание завершилось с ошибкой. Смотри лог.");

            var file = FindNewestVideo(started);
            if (file is null) throw new InvalidOperationException("Скачивание завершилось, но итоговый файл не найден.");
            Log("OUTPUT: " + file);

            if (mode == DownloadMode.CompatibleH264)
            {
                var output = await ConvertToH264Async(file, token);
                Status("Готово: " + output);
                OpenFile(output);
            }
            else
            {
                Status("Готово: " + file);
                OpenFile(file);
            }
        });
    }

    private List<string> BuildYtDlpArgs(DownloadMode mode, string url, string ffmpeg)
    {
        var fragments = _fragmentsBox.SelectedItem?.ToString() ?? "16";
        var ffmpegDir = Path.GetDirectoryName(ffmpeg)!;
        var suffix = mode switch
        {
            DownloadMode.FastMp4 => "_FAST",
            DownloadMode.MaxQuality => "_MAX",
            _ => "_SOURCE"
        };
        var output = Path.Combine(DownloadsDir, $"%(title).200B [%(id)s]{suffix}.%(ext)s");
        var format = mode == DownloadMode.FastMp4
            ? "bestvideo[ext=mp4][vcodec^=avc1]+bestaudio[ext=m4a]/best[ext=mp4]/best"
            : "bv*+ba/b";
        var merge = mode == DownloadMode.FastMp4 ? "mp4" : "mkv";

        return new List<string>
        {
            "--no-playlist", "--windows-filenames", "--newline", "--progress",
            "--concurrent-fragments", fragments,
            "--retries", "10", "--fragment-retries", "10", "--force-ipv4",
            "--user-agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 Chrome/120 Safari/537.36",
            "--referer", "https://rutube.ru/",
            "--ffmpeg-location", ffmpegDir,
            "-f", format,
            "--merge-output-format", merge,
            "-o", output,
            url
        };
    }

    private async Task<string> ConvertToH264Async(string input, CancellationToken token)
    {
        var ffmpeg = RequireTool("ffmpeg.exe");
        var output = Path.Combine(Path.GetDirectoryName(input)!, Path.GetFileNameWithoutExtension(input) + "_H264.mp4");
        Status("Создаю H.264 MP4. Это уже перекодировка, она может идти несколько минут...");
        var args = new List<string>
        {
            "-y", "-hide_banner", "-err_detect", "ignore_err", "-i", input,
            "-map", "0:v:0", "-map", "0:a:0?", "-sn", "-dn",
            "-vf", "scale=trunc(iw/2)*2:trunc(ih/2)*2",
            "-c:v", "libx264", "-preset", "veryfast", "-crf", "22", "-pix_fmt", "yuv420p",
            "-c:a", "aac", "-b:a", "192k", "-movflags", "+faststart", output
        };
        var result = await RunProcessAsync(ffmpeg, args, token);
        if (result.ExitCode != 0) throw new InvalidOperationException("FFmpeg не смог создать H.264 MP4.");
        return output;
    }

    private async Task InstallToolsAsync()
    {
        await RunUiTaskAsync("install_tools", async token =>
        {
            Status("Устанавливаю yt-dlp и FFmpeg через winget...");
            var args = new List<string>
            {
                "/c",
                "winget install -e --id yt-dlp.yt-dlp --accept-package-agreements --accept-source-agreements && winget install -e --id Gyan.FFmpeg --accept-package-agreements --accept-source-agreements"
            };
            var result = await RunProcessAsync("cmd.exe", args, token);
            if (result.ExitCode != 0) throw new InvalidOperationException("winget не смог установить инструменты. Установи yt-dlp и FFmpeg вручную или повтори попытку.");
            Status("Инструменты установлены. Перезапусти программу или нажми скачивание ещё раз.");
        });
    }

    private async Task UpdateYtDlpAsync()
    {
        await RunUiTaskAsync("update_ytdlp", async token =>
        {
            var yt = RequireTool("yt-dlp.exe");
            Status("Обновляю yt-dlp...");
            var result = await RunProcessAsync(yt, new List<string> { "-U" }, token);
            if (result.ExitCode != 0) throw new InvalidOperationException("yt-dlp не обновился.");
            Status("yt-dlp обновлён.");
        });
    }

    private async Task RunUiTaskAsync(string name, Func<CancellationToken, Task> action)
    {
        try
        {
            SetBusy(true);
            _progress.Value = 0;
            StartLog(name);
            _cts = new CancellationTokenSource();
            await action(_cts.Token);
        }
        catch (OperationCanceledException)
        {
            Status("Остановлено пользователем.");
            Log("Canceled.");
        }
        catch (Exception ex)
        {
            Status("Ошибка: " + ex.Message);
            Log("ERROR: " + ex);
            MessageBox.Show(ex.Message, "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
        }
        finally
        {
            SetBusy(false);
            _cts?.Dispose();
            _cts = null;
        }
    }

    private async Task<ProcResult> RunProcessAsync(string file, IReadOnlyList<string> args, CancellationToken token)
    {
        Log("RUN: " + file + " " + string.Join(" ", args.Select(QuoteForLog)));
        var psi = new ProcessStartInfo
        {
            FileName = file,
            UseShellExecute = false,
            CreateNoWindow = true,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            StandardOutputEncoding = Encoding.UTF8,
            StandardErrorEncoding = Encoding.UTF8,
            WorkingDirectory = AppRoot
        };
        foreach (var arg in args) psi.ArgumentList.Add(arg);

        using var process = new Process { StartInfo = psi, EnableRaisingEvents = true };
        var output = new StringBuilder();
        var error = new StringBuilder();
        process.OutputDataReceived += (_, e) => { if (e.Data != null) Dispatcher.Invoke(() => { output.AppendLine(e.Data); Log(e.Data); ParseProgress(e.Data); }); };
        process.ErrorDataReceived += (_, e) => { if (e.Data != null) Dispatcher.Invoke(() => { error.AppendLine(e.Data); Log("[err] " + e.Data); ParseProgress(e.Data); }); };
        process.Start();
        process.BeginOutputReadLine();
        process.BeginErrorReadLine();
        await process.WaitForExitAsync(token);
        Log("EXIT CODE: " + process.ExitCode);
        return new ProcResult(process.ExitCode, output.ToString(), error.ToString());
    }

    private void ParseProgress(string line)
    {
        var match = Regex.Match(line, @"(?<p>\d+(?:\.\d+)?)%");
        if (match.Success && double.TryParse(match.Groups["p"].Value, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out var p))
        {
            _progress.Value = Math.Clamp(p, 0, 100);
        }
        if (line.Contains("[download]", StringComparison.OrdinalIgnoreCase) || line.Contains("Merging", StringComparison.OrdinalIgnoreCase))
            _statusText.Text = line;
    }

    private void SetBusy(bool busy)
    {
        _analyzeButton.IsEnabled = !busy;
        _fastButton.IsEnabled = !busy;
        _maxButton.IsEnabled = !busy;
        _h264Button.IsEnabled = !busy;
        _cancelButton.IsEnabled = busy;
    }

    private void StartLog(string name)
    {
        Directory.CreateDirectory(LogsDir);
        _currentLogPath = Path.Combine(LogsDir, DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss_") + name + ".log");
        _logBox.Clear();
        Log("Log: " + _currentLogPath);
    }

    private void Log(string text)
    {
        _logBox.AppendText(text + Environment.NewLine);
        _logBox.ScrollToEnd();
        if (!string.IsNullOrWhiteSpace(_currentLogPath)) File.AppendAllText(_currentLogPath, text + Environment.NewLine, Encoding.UTF8);
    }

    private void Status(string text) => _statusText.Text = text;

    private string RequireUrl()
    {
        var url = _urlBox.Text.Trim();
        if (string.IsNullOrWhiteSpace(url)) throw new InvalidOperationException("Вставь ссылку на видео.");
        if (!Uri.TryCreate(url, UriKind.Absolute, out _)) throw new InvalidOperationException("Ссылка выглядит неверно.");
        return url;
    }

    private string RequireTool(string fileName)
    {
        var path = FindTool(fileName);
        if (path is null) throw new FileNotFoundException($"Не найден {fileName}. Нажми 'Установить tools' или положи файл в папку tools.");
        return path;
    }

    private string? FindTool(string fileName)
    {
        var candidates = new List<string>
        {
            Path.Combine(ToolsDir, fileName),
            Path.Combine(AppRoot, fileName),
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Microsoft", "WinGet", "Links", fileName),
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Microsoft", "WindowsApps", fileName)
        };
        foreach (var candidate in candidates) if (File.Exists(candidate)) return candidate;
        var path = Environment.GetEnvironmentVariable("PATH") ?? string.Empty;
        foreach (var dir in path.Split(Path.PathSeparator, StringSplitOptions.RemoveEmptyEntries))
        {
            try
            {
                var candidate = Path.Combine(dir.Trim(), fileName);
                if (File.Exists(candidate)) return candidate;
            }
            catch { }
        }
        return null;
    }

    private string ToolsStatus()
    {
        return "yt-dlp: " + (FindTool("yt-dlp.exe") ?? "НЕ НАЙДЕН") + Environment.NewLine +
               "ffmpeg: " + (FindTool("ffmpeg.exe") ?? "НЕ НАЙДЕН") + Environment.NewLine +
               "ffprobe: " + (FindTool("ffprobe.exe") ?? "НЕ НАЙДЕН") + Environment.NewLine +
               "downloads: " + DownloadsDir;
    }

    private string? FindNewestVideo(DateTime sinceUtc)
    {
        return Directory.EnumerateFiles(DownloadsDir)
            .Select(x => new FileInfo(x))
            .Where(x => x.LastWriteTimeUtc >= sinceUtc && x.Length > 100_000)
            .Where(x => new[] { ".mp4", ".mkv", ".webm", ".m4v", ".mov" }.Contains(x.Extension.ToLowerInvariant()))
            .OrderByDescending(x => x.LastWriteTimeUtc)
            .FirstOrDefault()?.FullName;
    }

    private static string QuoteForLog(string arg) => arg.Contains(' ') ? "\"" + arg + "\"" : arg;
    private static Brush Brush(string color) => new SolidColorBrush((Color)ColorConverter.ConvertFromString(color));

    private static Border Card(UIElement child) => new()
    {
        Child = child,
        Background = Brush("#111827"),
        BorderBrush = Brush("#334155"),
        BorderThickness = new Thickness(1),
        CornerRadius = new CornerRadius(16),
        Padding = new Thickness(14),
        Margin = new Thickness(0, 0, 0, 12)
    };

    private static Button NewButton(string text, string color)
    {
        var b = new Button { Content = text };
        StyleButton(b, color);
        return b;
    }

    private static void StyleButton(Button button, string color)
    {
        button.Margin = new Thickness(4);
        button.Padding = new Thickness(12, 8, 12, 8);
        button.FontWeight = FontWeights.SemiBold;
        button.Background = Brush(color);
        button.Foreground = Brushes.Black;
        button.BorderThickness = new Thickness(0);
        button.Cursor = System.Windows.Input.Cursors.Hand;
    }

    private static void OpenFolder(string path)
    {
        Directory.CreateDirectory(path);
        Process.Start(new ProcessStartInfo { FileName = "explorer.exe", Arguments = $"\"{path}\"", UseShellExecute = true });
    }

    private static void OpenFile(string path)
    {
        if (File.Exists(path)) Process.Start(new ProcessStartInfo { FileName = "explorer.exe", Arguments = $"/select,\"{path}\"", UseShellExecute = true });
    }

    private enum DownloadMode { FastMp4, MaxQuality, CompatibleH264 }
    private sealed record ProcResult(int ExitCode, string StdOut, string StdErr);
}
