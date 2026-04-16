using RifeZPhoneBridge.Host.Models;
using System.IO;

namespace RifeZPhoneBridge.App;

public sealed class TrayApplicationContext : ApplicationContext
{
    private readonly AppSettingsService _settingsService;
    private readonly BridgeAppSettings _settings;
    private readonly SimpleFileLogger _logger;
    private readonly BridgeAppController _controller;
    private readonly NotifyIcon _notifyIcon;
    private readonly ToolStripMenuItem _statusItem;
    private readonly ToolStripMenuItem _receiverItem;
    private readonly ToolStripMenuItem _initializeItem;
    private readonly ToolStripMenuItem _startItem;
    private readonly ToolStripMenuItem _stopItem;
    private readonly ToolStripMenuItem _reconnectItem;
    private readonly ToolStripMenuItem _openStatusItem;
    private readonly ToolStripMenuItem _startOnLoginItem;
    private readonly System.Windows.Forms.Timer _statusTimer;
    private readonly Icon _idleIcon;
    private readonly Icon _readyIcon;
    private readonly Icon _streamingIcon;
    private readonly Icon _faultedIcon;
    private readonly BridgeMetricsService _metrics;
    private readonly SynchronizationContext _uiContext;
    private readonly Control _uiInvoker;
    private readonly System.Windows.Forms.Timer _autoRestoreTimer;


    private StatusForm? _statusForm;
    private bool _commandInProgress;
    private DateTime _lastFaultBalloonUtc = DateTime.MinValue;
    private string? _lastFaultBalloonText;
    private bool _autoRestoreArmed;
    private bool _autoRestoreAttemptInFlight;

    public TrayApplicationContext()
    {

        _idleIcon = RifeZAudioBridge.App.Properties.Resources.idle;
        _readyIcon = RifeZAudioBridge.App.Properties.Resources.ready;
        _streamingIcon = RifeZAudioBridge.App.Properties.Resources.streaming;
        _faultedIcon = RifeZAudioBridge.App.Properties.Resources.faulted;


        _settingsService = new AppSettingsService();
        _settings = _settingsService.Load();
        _logger = new SimpleFileLogger(_settingsService.LogsPath);
        _metrics = new BridgeMetricsService();
        _controller = new BridgeAppController(_settingsService, _settings, _logger, _metrics);

        _uiInvoker = new Control();
        _uiInvoker.CreateControl();

        _controller.StateChanged += ControllerOnStateChanged;

        _statusItem = new ToolStripMenuItem("Status: Idle") { Enabled = false };
        _receiverItem = new ToolStripMenuItem("Receiver: none") { Enabled = false };

        _initializeItem = new ToolStripMenuItem("Initialize", null, async (_, _) => await RunCommandAsync(() => _controller.InitializeAsync()));
        _startItem = new ToolStripMenuItem("Start Streaming", null, async (_, _) => await RunCommandAsync(() => _controller.StartAsync()));
        _stopItem = new ToolStripMenuItem("Stop Streaming", null, async (_, _) =>
        {
            _autoRestoreArmed = false;
            _autoRestoreTimer.Stop();
            await RunCommandAsync(() => _controller.StopAsync());
        });
        _reconnectItem = new ToolStripMenuItem("Reconnect", null, async (_, _) =>
        {
            _autoRestoreArmed = true;
            _autoRestoreTimer.Start();
            await RunCommandAsync(() => _controller.ReconnectAsync());
        });
        _openStatusItem = new ToolStripMenuItem("Open Status", null, (_, _) => OpenStatusWindow());
        _uiContext = SynchronizationContext.Current ?? new WindowsFormsSynchronizationContext();
        _startOnLoginItem = new ToolStripMenuItem("Run at Windows startup")
        {
            Checked = StartupRegistrationService.IsEnabled(),
            CheckOnClick = true
        };
        _startOnLoginItem.Click += (_, _) => ToggleRunAtStartup();

        var menu = new ContextMenuStrip();
        menu.Items.Add(_statusItem);
        menu.Items.Add(_receiverItem);
        menu.Items.Add(new ToolStripSeparator());
        menu.Items.Add(_startItem);
        menu.Items.Add(_stopItem);
        menu.Items.Add(_reconnectItem);
        menu.Items.Add(_openStatusItem);
        menu.Items.Add(new ToolStripSeparator());
        menu.Items.Add(new ToolStripMenuItem("Open Logs Folder", null, (_, _) => OpenLogsFolder()));
        menu.Items.Add(_startOnLoginItem);
        menu.Items.Add(new ToolStripSeparator());
        menu.Items.Add(new ToolStripMenuItem("Exit", null, async (_, _) => await ExitAsync()));

        _notifyIcon = new NotifyIcon
        {
            Text = "RifeZ Audio Bridge",
            Visible = true,
            ContextMenuStrip = menu,
            Icon = _idleIcon
        };
        _notifyIcon.DoubleClick += (_, _) => OpenStatusWindow();

        _statusTimer = new System.Windows.Forms.Timer { Interval = 1000 };
        _statusTimer.Tick += (_, _) => RefreshStatus();
        _statusTimer.Start();

        _autoRestoreTimer = new System.Windows.Forms.Timer { Interval = 10000 };
        _autoRestoreTimer.Tick += async (_, _) => await AutoRestoreTickAsync();

        _logger.Info("Tray application started.");
        RefreshStatus();
        _ = WarmStartAsync();
    }

    private void UpdateTrayIcon(BridgeStreamState state)
    {
        _notifyIcon.Icon = state switch
        {
            BridgeStreamState.Streaming => _streamingIcon,
            BridgeStreamState.StreamConfigured => _readyIcon,
            BridgeStreamState.ControlConnected => _readyIcon,
            BridgeStreamState.ReceiverSelected => _readyIcon,
            BridgeStreamState.Discovering => _readyIcon,
            BridgeStreamState.WaitingForReceiver => _idleIcon,
            BridgeStreamState.Idle => _idleIcon,
            BridgeStreamState.Stopping => _idleIcon,
            BridgeStreamState.Faulted => _faultedIcon,
            _ => _idleIcon
        };
    }

    private async Task WarmStartAsync()
    {
        try
        {
            if (!_settings.AutoConnectOnLaunch)
                return;

            _autoRestoreArmed = true;
            _autoRestoreTimer.Start();

            // Short grace period so app/UI/services settle before first attempt.
            await Task.Delay(TimeSpan.FromSeconds(3));

            if (_settings.AutoStartStreaming)
            {
                await RunCommandAsync(() => _controller.StartAsync());
            }
            else
            {
                await RunCommandAsync(() => _controller.InitializeAsync());
            }
        }
        catch (Exception ex)
        {
            _logger.Error($"Warm start failed: {ex}");
            RefreshStatus();
        }
    }

    private async Task AutoRestoreTickAsync()
    {
        if (!_autoRestoreArmed || _autoRestoreAttemptInFlight || _commandInProgress)
            return;

        BridgeStatusSnapshot status = _controller.GetStatus();

        // Stop retrying once streaming is active.
        if (status.State == BridgeStreamState.Streaming)
        {
            _autoRestoreArmed = false;
            _autoRestoreTimer.Stop();
            return;
        }

        // Do not interfere while a transition is already happening.
        if (status.State is BridgeStreamState.Discovering or BridgeStreamState.Stopping)
            return;

        _autoRestoreAttemptInFlight = true;
        try
        {
            if (_settings.AutoStartStreaming)
            {
                await RunCommandAsync(() => _controller.StartAsync());
            }
            else
            {
                await RunCommandAsync(() => _controller.InitializeAsync());
            }
        }
        catch (Exception ex)
        {
            _logger.Error($"Auto-restore attempt failed: {ex}");
        }
        finally
        {
            _autoRestoreAttemptInFlight = false;
        }
    }

    private async Task RunCommandAsync(Func<Task> action)
    {
        if (_commandInProgress)
            return;

        _commandInProgress = true;
        UpdateMenuState();

        try
        {
            await action();
        }
        catch (InvalidOperationException ex) when (ex.Message.Contains("No receiver discovered", StringComparison.OrdinalIgnoreCase))
        {
            _logger.Info($"Receiver unavailable: {ex.Message}");
            // No balloon here. This is a normal environment condition.
        }
        catch (Exception ex)
        {
            _logger.Error(ex.ToString());
            ShowFaultBalloon(ex.Message);
        }
        finally
        {
            _commandInProgress = false;
            RefreshStatus();
        }
    }

    private void RefreshStatus()
    {
        if (_uiInvoker.IsDisposed)
            return;

        if (_uiInvoker.InvokeRequired)
        {
            _uiInvoker.BeginInvoke(new Action(RefreshStatus));
            return;
        }

        BridgeStatusSnapshot status = _controller.GetStatus();
        _statusItem.Text = $"Status: {status.State}";
        _receiverItem.Text = $"Receiver: {status.ReceiverHost ?? "none"}:{(status.ReceiverPort?.ToString() ?? "-")}";
        UpdateTrayIcon(status.State);
        UpdateMenuState(status);
        _statusForm?.UpdateStatus(status, _settings, _controller.GetMetrics());
    }

    private void ControllerOnStateChanged(BridgeRuntimeState state)
    {
        if (_uiInvoker.IsDisposed)
            return;

        if (_uiInvoker.InvokeRequired)
        {
            _uiInvoker.BeginInvoke(new Action(() => ControllerOnStateChangedOnUiThread(state)));
            return;
        }

        ControllerOnStateChangedOnUiThread(state);
    }

    private void ControllerOnStateChangedOnUiThread(BridgeRuntimeState state)
    {
        UpdateTrayIcon(state.State);
        _statusItem.Text = $"Status: {state.State}";
        _receiverItem.Text = $"Receiver: {state.ReceiverHost ?? "none"}:{(state.ReceiverPort?.ToString() ?? "-")}";

        UpdateMenuState(new BridgeStatusSnapshot(
            State: state.State,
            ReceiverHost: state.ReceiverHost,
            ReceiverPort: state.ReceiverPort,
            LastError: state.LastError,
            IsInitialized: state.State is not BridgeStreamState.Idle and not BridgeStreamState.WaitingForReceiver,
            IsStreaming: state.State == BridgeStreamState.Streaming));

        _statusForm?.UpdateStatus(_controller.GetStatus(), _settings, _controller.GetMetrics());

        if (state.State == BridgeStreamState.Faulted && !string.IsNullOrWhiteSpace(state.LastError))
        {
            ShowFaultBalloon(state.LastError);
        }
    }

    private void UpdateMenuState()
    {
        UpdateMenuState(_controller.GetStatus());
    }

    private void UpdateMenuState(BridgeStatusSnapshot status)
    {
        if (_commandInProgress)
        {
            _initializeItem.Enabled = false;
            _startItem.Enabled = false;
            _stopItem.Enabled = false;
            _reconnectItem.Enabled = false;
            _openStatusItem.Enabled = true;
            return;
        }

        _openStatusItem.Enabled = true;

        switch (status.State)
        {
            case BridgeStreamState.Idle:
            case BridgeStreamState.WaitingForReceiver:
            case BridgeStreamState.Faulted:
                _initializeItem.Enabled = true;
                _startItem.Enabled = true;
                _stopItem.Enabled = false;
                _reconnectItem.Enabled = true;
                break;

            case BridgeStreamState.Discovering:
            case BridgeStreamState.Stopping:
                _initializeItem.Enabled = false;
                _startItem.Enabled = false;
                _stopItem.Enabled = false;
                _reconnectItem.Enabled = false;
                break;

            case BridgeStreamState.StreamConfigured:
                _initializeItem.Enabled = false;
                _startItem.Enabled = true;
                _stopItem.Enabled = false;
                _reconnectItem.Enabled = true;
                break;

            case BridgeStreamState.Streaming:
                _initializeItem.Enabled = false;
                _startItem.Enabled = false;
                _stopItem.Enabled = true;
                _reconnectItem.Enabled = true;
                break;

            default:
                _initializeItem.Enabled = false;
                _startItem.Enabled = false;
                _stopItem.Enabled = false;
                _reconnectItem.Enabled = true;
                break;
        }
    }

    private void OpenStatusWindow()
    {
        if (_statusForm is null || _statusForm.IsDisposed)
        {
            _statusForm = new StatusForm();
            _statusForm.FormClosed += (_, _) => _statusForm = null;
        }

        _statusForm.UpdateStatus(_controller.GetStatus(), _settings, _controller.GetMetrics());
        if (!_statusForm.Visible)
        {
            _statusForm.Show();
        }
        else
        {
            _statusForm.BringToFront();
            _statusForm.Activate();
        }
    }

    private void ToggleRunAtStartup()
    {
        string exePath = Application.ExecutablePath;
        StartupRegistrationService.SetEnabled(_startOnLoginItem.Checked, exePath);
        _settings.RunAtWindowsStartup = _startOnLoginItem.Checked;
        _settingsService.Save(_settings);
    }

    private void OpenLogsFolder()
    {
        Directory.CreateDirectory(_settingsService.LogsPath);
        System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
        {
            FileName = _settingsService.LogsPath,
            UseShellExecute = true
        });
    }

    private void ShowBalloon(string title, string text, ToolTipIcon icon)
    {
        _notifyIcon.BalloonTipTitle = title;
        _notifyIcon.BalloonTipText = text;
        _notifyIcon.BalloonTipIcon = icon;
        _notifyIcon.ShowBalloonTip(2000);
    }

    private void ShowFaultBalloon(string message)
    {
        string normalized = message.Trim();

        var now = DateTime.UtcNow;
        bool sameAsLast = string.Equals(_lastFaultBalloonText, normalized, StringComparison.Ordinal);
        bool tooSoon = (now - _lastFaultBalloonUtc) < TimeSpan.FromSeconds(15);

        if (sameAsLast && tooSoon)
            return;

        _lastFaultBalloonText = normalized;
        _lastFaultBalloonUtc = now;

        ShowBalloon("RifeZ Audio Bridge", normalized, ToolTipIcon.None);
    }

    private async Task ExitAsync()
    {
        try
        {
            _statusTimer.Stop();
            _controller.StateChanged -= ControllerOnStateChanged;
            _notifyIcon.Visible = false;
            _autoRestoreTimer.Stop();
            _autoRestoreTimer.Dispose();

            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(2));

            try
            {
                await _controller.DisposeAsync().AsTask().WaitAsync(cts.Token);
            }
            catch (OperationCanceledException)
            {
                _logger.Error("Exit timed out while disposing controller. Forcing process exit.");
            }
            catch (TimeoutException)
            {
                _logger.Error("Exit timed out while disposing controller. Forcing process exit.");
            }
            catch (Exception ex)
            {
                _logger.Error($"Exit failed during controller dispose: {ex}");
            }

            _settingsService.Save(_settings);
        }
        catch (Exception ex)
        {
            _logger.Error($"Exit failed: {ex}");
        }
        finally
        {
            try { _notifyIcon.Dispose(); } catch { }
            try { _statusForm?.Close(); } catch { }
            try { _statusForm?.Dispose(); } catch { }
            try { _uiInvoker.Dispose(); } catch { }

            ExitThread();
            Application.Exit();
            Environment.Exit(0);
        }
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _statusTimer.Dispose();
            _notifyIcon.Dispose();
            _statusForm?.Dispose();
            _idleIcon.Dispose();
            _readyIcon.Dispose();
            _streamingIcon.Dispose();
            _faultedIcon.Dispose();
            _uiInvoker.Dispose();
        }

        base.Dispose(disposing);
    }
}