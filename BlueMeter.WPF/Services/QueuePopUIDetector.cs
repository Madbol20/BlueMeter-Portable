using System;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using BlueMeter.WPF.Logging;
using Tesseract;

namespace BlueMeter.WPF.Services;

/// <summary>
/// Detects queue pop by capturing the game window and using OCR to find Cancel/Confirm button text.
/// Works even when the game window is minimized or in the background.
/// 100% local - no external communication, only uses Windows OCR and screen capture.
/// </summary>
public interface IQueuePopUIDetector : IDisposable
{
    /// <summary>
    /// Start monitoring the game window for queue pop UI
    /// </summary>
    void Start();

    /// <summary>
    /// Stop monitoring
    /// </summary>
    void Stop();

    /// <summary>
    /// Whether the detector is currently running
    /// </summary>
    bool IsRunning { get; }
}

public sealed class QueuePopUIDetector : IQueuePopUIDetector
{
    private readonly ILogger<QueuePopUIDetector> _logger;
    private readonly ISoundPlayerService _soundPlayerService;

    // Game process names to monitor
    private static readonly string[] GameProcessNames =
    {
        "star",           // Original game
        "BPSR_STEAM",     // Steam version
        "BPSR_EPIC",      // Epic version
        "BPSR"            // Generic version
    };

    // Blacklist - if these are found, it's NOT a queue pop (e.g., "Challenge" button)
    private static readonly string[] BlacklistPatterns =
    {
        "challenge", "challen", "challe", "match", "single", "dual", "team"
    };

    // P/Invoke declarations for screen capture
    [DllImport("user32.dll")]
    private static extern IntPtr GetWindowRect(IntPtr hWnd, ref RECT rect);

    [DllImport("user32.dll")]
    private static extern IntPtr GetDC(IntPtr hWnd);

    [DllImport("user32.dll")]
    private static extern int ReleaseDC(IntPtr hWnd, IntPtr hDC);

    [DllImport("gdi32.dll")]
    private static extern IntPtr CreateCompatibleDC(IntPtr hDC);

    [DllImport("gdi32.dll")]
    private static extern IntPtr CreateCompatibleBitmap(IntPtr hDC, int nWidth, int nHeight);

    [DllImport("gdi32.dll")]
    private static extern IntPtr SelectObject(IntPtr hDC, IntPtr hObject);

    [DllImport("gdi32.dll")]
    private static extern bool BitBlt(IntPtr hdcDest, int xDest, int yDest, int wDest, int hDest,
        IntPtr hdcSource, int xSrc, int ySrc, CopyPixelOperation rop);

    [DllImport("user32.dll")]
    private static extern bool PrintWindow(IntPtr hWnd, IntPtr hdcBlt, uint nFlags);

    private const uint PW_RENDERFULLCONTENT = 0x00000002;

    [DllImport("gdi32.dll")]
    private static extern bool DeleteDC(IntPtr hDC);

    [DllImport("gdi32.dll")]
    private static extern bool DeleteObject(IntPtr hObject);

    [StructLayout(LayoutKind.Sequential)]
    private struct RECT
    {
        public int Left;
        public int Top;
        public int Right;
        public int Bottom;
    }

    private Timer? _pollingTimer;
    private bool _isRunning;
    private bool _disposed;
    private DateTime _lastAlertTime = DateTime.MinValue;
    private readonly TimeSpan _alertCooldown = TimeSpan.FromSeconds(10); // Cooldown between alerts
    private TesseractEngine? _ocrEngine;
    private readonly SemaphoreSlim _ocrLock = new(1, 1); // Ensure only one OCR operation at a time

    public bool IsRunning => _isRunning;

    public QueuePopUIDetector(
        ILogger<QueuePopUIDetector> logger,
        ISoundPlayerService soundPlayerService)
    {
        _logger = logger;
        _soundPlayerService = soundPlayerService;
    }

    public void Start()
    {
        if (_disposed)
            throw new ObjectDisposedException(nameof(QueuePopUIDetector));

        if (_isRunning)
        {
            _logger.LogWarning("QueuePopUIDetector already running");
            return;
        }

        // Initialize Tesseract OCR engine
        try
        {
            var tessDataPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "tessdata");

            if (!Directory.Exists(tessDataPath))
            {
                _logger.LogError("[OCR] tessdata directory not found at: {Path}", tessDataPath);
                _logger.LogError("[OCR] Please download language files from: https://github.com/tesseract-ocr/tessdata");
                return;
            }

            // Try to initialize with English language data
            _ocrEngine = new TesseractEngine(tessDataPath, "eng", EngineMode.Default);
            _logger.LogInformation("[OCR] Tesseract OCR engine initialized successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[OCR] Failed to initialize Tesseract engine. Make sure tessdata/eng.traineddata exists");
            return;
        }

        _isRunning = true;

        // Poll every 500ms for queue pop UI
        _pollingTimer = new Timer(
            _ => CheckForQueuePopUI(),
            null,
            TimeSpan.Zero,
            TimeSpan.FromMilliseconds(500));

        _logger.LogInformation(WpfLogEvents.QueueDetector,
            "QueuePopUIDetector started - monitoring via screen capture + OCR");
    }

    public void Stop()
    {
        if (!_isRunning)
            return;

        _isRunning = false;
        _pollingTimer?.Dispose();
        _pollingTimer = null;

        _logger.LogInformation(WpfLogEvents.QueueDetector, "QueuePopUIDetector stopped");
    }

    private void CheckForQueuePopUI()
    {
        if (!_isRunning || _ocrEngine == null)
            return;

        try
        {
            // Find game process
            var gameProcess = FindGameProcess();
            if (gameProcess == null)
                return;

            // Get main window handle
            var mainWindowHandle = gameProcess.MainWindowHandle;
            if (mainWindowHandle == IntPtr.Zero)
                return;

            // Capture screenshot of game window
            using var screenshot = CaptureWindow(mainWindowHandle);
            if (screenshot == null)
                return;

            // Run OCR on screenshot
            var hasQueuePop = Task.Run(async () => await DetectQueuePopTextAsync(screenshot)).Result;

            if (hasQueuePop)
            {
                // Check cooldown to prevent duplicate alerts
                if (DateTime.Now - _lastAlertTime < _alertCooldown)
                    return;

                _lastAlertTime = DateTime.Now;
                OnQueuePopDetected();
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "[OCR] Error checking for queue pop UI");
        }
    }

    private Process? FindGameProcess()
    {
        foreach (var processName in GameProcessNames)
        {
            var processes = Process.GetProcessesByName(processName);
            if (processes.Length > 0)
                return processes[0];
        }

        return null;
    }

    private Bitmap? CaptureWindow(IntPtr hWnd)
    {
        try
        {
            var rect = new RECT();
            GetWindowRect(hWnd, ref rect);

            int fullWidth = rect.Right - rect.Left;
            int fullHeight = rect.Bottom - rect.Top;

            if (fullWidth <= 0 || fullHeight <= 0)
                return null;

            // First, capture the full window using PrintWindow (works even when minimized/covered)
            var hdcSrc = GetDC(hWnd);
            var hdcDest = CreateCompatibleDC(hdcSrc);
            var hBitmap = CreateCompatibleBitmap(hdcSrc, fullWidth, fullHeight);
            var hOld = SelectObject(hdcDest, hBitmap);

            // Use PrintWindow instead of BitBlt - this captures even minimized/background windows
            bool success = PrintWindow(hWnd, hdcDest, PW_RENDERFULLCONTENT);

            SelectObject(hdcDest, hOld);
            DeleteDC(hdcDest);
            ReleaseDC(hWnd, hdcSrc);

            if (!success)
            {
                DeleteObject(hBitmap);
                return null;
            }

            var fullBitmap = Image.FromHbitmap(hBitmap);
            DeleteObject(hBitmap);

            // Now crop to bottom area (where Cancel/Confirm buttons appear)
            // Capture 100% width x 70% height from the bottom (include ALL buttons!)
            int captureWidth = fullWidth;
            int captureHeight = (int)(fullHeight * 0.7);
            int captureX = 0;                        // Full width - no horizontal cropping
            int captureY = (int)(fullHeight * 0.3);  // Start at 30% from top (bottom 70%)

            var croppedBitmap = new Bitmap(captureWidth, captureHeight);
            using (var g = Graphics.FromImage(croppedBitmap))
            {
                g.DrawImage(fullBitmap,
                    new Rectangle(0, 0, captureWidth, captureHeight),
                    new Rectangle(captureX, captureY, captureWidth, captureHeight),
                    GraphicsUnit.Pixel);
            }

            fullBitmap.Dispose();

            return croppedBitmap;
        }
        catch
        {
            return null;
        }
    }

    private async Task<bool> DetectQueuePopTextAsync(Bitmap screenshot)
    {
        // Use semaphore to ensure only one OCR operation at a time (Tesseract is not thread-safe)
        if (!await _ocrLock.WaitAsync(0)) // Try to acquire immediately, don't block
            return false;

        try
        {
            return await Task.Run(() =>
            {
                try
                {
                    if (_ocrEngine == null)
                        return false;

                    // Save bitmap to memory stream and process with Tesseract
                    using var ms = new MemoryStream();
                    screenshot.Save(ms, System.Drawing.Imaging.ImageFormat.Png);
                    ms.Position = 0;

                    using var img = Pix.LoadFromMemory(ms.ToArray());
                    using var page = _ocrEngine.Process(img);

                    var allText = page.GetText()?.ToLowerInvariant() ?? string.Empty;

                    // Check for blacklisted patterns first (Challenge button, etc.)
                    bool foundBlacklist = BlacklistPatterns.Any(pattern => allText.Contains(pattern));
                    if (foundBlacklist)
                        return false;

                    // Detect queue pop by player card layout pattern
                    // Queue pop shows 5 player cards with "Lv.60"
                    int playerCardCount = System.Text.RegularExpressions.Regex.Matches(allText, @"lv\.?\s*\d+").Count;

                    // Queue pop = 3+ player cards (OCR doesn't always catch all 5)
                    bool isQueuePop = playerCardCount >= 3;

                    if (isQueuePop)
                    {
                        _logger.LogInformation("[OCR] Queue pop detected! Player cards: {Count}", playerCardCount);
                    }

                    return isQueuePop;
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "[OCR] Error during text recognition");
                    return false;
                }
            });
        }
        finally
        {
            _ocrLock.Release();
        }
    }

    private void OnQueuePopDetected()
    {
        try
        {
            _logger.LogInformation(WpfLogEvents.QueueDetector,
                "★★★ QUEUE POP DETECTED via OCR! ★★★ Playing alert sound...");

            _soundPlayerService.PlayQueuePopSound();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to play queue pop alert sound");
        }
    }

    public void Dispose()
    {
        if (_disposed)
            return;

        _disposed = true;
        Stop();
        _ocrEngine?.Dispose();
        _ocrLock.Dispose();

        _logger.LogDebug("QueuePopUIDetector disposed");
    }
}
