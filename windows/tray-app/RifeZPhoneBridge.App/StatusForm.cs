using System.Drawing.Drawing2D;
using System.Globalization;
using System.Windows.Forms.DataVisualization.Charting;
using RifeZPhoneBridge.Host.Models;

namespace RifeZPhoneBridge.App;

public sealed class StatusForm : Form
{
    private readonly Label _stateValue;
    private readonly Label _receiverValue;
    private readonly Label _uptimeValue;
    private readonly Label _throughputValue;
    private readonly Label _framesValue;
    private readonly Label _reconnectsValue;
    private readonly Label _faultsValue;
    private readonly Label _audioValue;
    private readonly Label _startupValue;
    private readonly TextBox _lastErrorBox;
    private readonly ListBox _sessionTimelineList;

    private readonly Chart _throughputChart;
    private readonly Chart _fpsChart;

    public StatusForm()
    {
        Text = "RifeZ Audio Bridge Dashboard";
        Icon = RifeZAudioBridge.App.Properties.Resources.ready;
        Width = 1280;
        Height = 820;
        WindowState = FormWindowState.Maximized;
        StartPosition = FormStartPosition.CenterScreen;
        MinimumSize = new Size(1100, 720);
        Font = new Font("Segoe UI", 10f);
        BackColor = SystemColors.Control;
        ForeColor = SystemColors.ControlText;

        var root = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 4,
            Padding = new Padding(16),
            BackColor = SystemColors.Control
        };
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 88));
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 190));
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 260));
        root.RowStyles.Add(new RowStyle(SizeType.Percent, 100));

        Controls.Add(root);

        var header = BuildHeaderPanel();
        root.Controls.Add(header, 0, 0);

        var kpiGrid = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 3,
            RowCount = 2,
            Margin = new Padding(0, 12, 0, 12)
        };
        kpiGrid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 33.33f));
        kpiGrid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 33.33f));
        kpiGrid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 33.33f));
        kpiGrid.RowStyles.Add(new RowStyle(SizeType.Percent, 50));
        kpiGrid.RowStyles.Add(new RowStyle(SizeType.Percent, 50));

        _stateValue = CreateValueLabel(fontSize: 14f);
        _receiverValue = CreateValueLabel(fontSize: 14f);
        _uptimeValue = CreateValueLabel(fontSize: 14f);
        _throughputValue = CreateValueLabel(fontSize: 14f);
        _framesValue = CreateValueLabel(fontSize: 14f);
        _reconnectsValue = CreateValueLabel(fontSize: 14f);

        kpiGrid.Controls.Add(CreateKpiCard("State", _stateValue), 0, 0);
        kpiGrid.Controls.Add(CreateKpiCard("Receiver", _receiverValue), 1, 0);
        kpiGrid.Controls.Add(CreateKpiCard("Uptime", _uptimeValue), 2, 0);
        kpiGrid.Controls.Add(CreateKpiCard("Throughput", _throughputValue), 0, 1);
        kpiGrid.Controls.Add(CreateKpiCard("Frames Sent", _framesValue), 1, 1);
        kpiGrid.Controls.Add(CreateKpiCard("Reconnects", _reconnectsValue), 2, 1);

        root.Controls.Add(kpiGrid, 0, 1);

        _throughputChart = CreateMetricChart("KB/s");
        _fpsChart = CreateMetricChart("FPS");

        var graphRow = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 2,
            RowCount = 1,
            Margin = new Padding(0, 0, 0, 12)
        };
        graphRow.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
        graphRow.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));

        graphRow.Controls.Add(CreateContainer("Throughput Trend", _throughputChart), 0, 0);
        graphRow.Controls.Add(CreateContainer("Send Cadence (FPS)", _fpsChart), 1, 0);

        root.Controls.Add(graphRow, 0, 2);

        var details = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 2,
            RowCount = 1
        };
        details.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
        details.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));

        _faultsValue = CreateValueLabel(fontSize: 18f);
        _audioValue = CreateValueLabel(multiline: true);
        _startupValue = CreateValueLabel(multiline: true);

        var left = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 3
        };
        left.RowStyles.Add(new RowStyle(SizeType.Absolute, 100));
        left.RowStyles.Add(new RowStyle(SizeType.Absolute, 130));
        left.RowStyles.Add(new RowStyle(SizeType.Percent, 100));

        left.Controls.Add(CreateCard("Fault Count", _faultsValue), 0, 0);
        left.Controls.Add(CreateCard("Audio Configuration", _audioValue), 0, 1);

        _lastErrorBox = new TextBox
        {
            Multiline = true,
            ReadOnly = true,
            Dock = DockStyle.Fill,
            ScrollBars = ScrollBars.Vertical,
            BackColor = Color.White,
            BorderStyle = BorderStyle.FixedSingle,
            Font = new Font("Consolas", 10.5f)
        };
        left.Controls.Add(CreateContainer("Last Error", _lastErrorBox), 0, 2);

        var right = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 3
        };
        right.RowStyles.Add(new RowStyle(SizeType.Absolute, 150));
        right.RowStyles.Add(new RowStyle(SizeType.Absolute, 170));
        right.RowStyles.Add(new RowStyle(SizeType.Percent, 100));

        right.Controls.Add(CreateCard("Session Timings", _startupValue), 0, 0);

        _sessionTimelineList = new ListBox
        {
            Dock = DockStyle.Fill,
            Font = new Font("Consolas", 9.5f),
            IntegralHeight = false
        };
        right.Controls.Add(CreateContainer("Session Timeline", _sessionTimelineList), 0, 1);

        var roadmap = new Label
        {
            Dock = DockStyle.Fill,
            TextAlign = ContentAlignment.TopLeft,
            Padding = new Padding(10),
            Font = new Font("Segoe UI", 11f),
            Text =
                "OEM roadmap placeholder\r\n\r\n" +
                "• Single receiver active today\r\n" +
                "• Future multi-endpoint node list\r\n" +
                "• Planned roles: FL / FR / C / LFE / RL / RR\r\n" +
                "• 2.0 / 4.1 / 5.1 layout support\r\n" +
                "• Endpoint capability reporting",
            AutoSize = false
        };
        right.Controls.Add(CreateContainer("Endpoint / Layout Roadmap", roadmap), 0, 2);

        details.Controls.Add(left, 0, 0);
        details.Controls.Add(right, 1, 0);

        root.Controls.Add(details, 0, 3);
    }

    public void UpdateStatus(
        BridgeStatusSnapshot status,
        BridgeAppSettings settings,
        BridgeMetricsSnapshot metrics)
    {
        if (IsDisposed)
            return;

        if (InvokeRequired)
        {
            BeginInvoke(new Action(() => UpdateStatus(status, settings, metrics)));
            return;
        }

        _stateValue.Text = status.State.ToString();
        _receiverValue.Text = status.ReceiverHost is null
            ? "No receiver"
            : $"{status.ReceiverHost}:{(status.ReceiverPort?.ToString() ?? "-")}";
        _uptimeValue.Text = metrics.Uptime == TimeSpan.Zero ? "00:00:00" : metrics.Uptime.ToString(@"hh\:mm\:ss");
        _throughputValue.Text = $"{metrics.KilobytesPerSecond:F1} KB/s";
        _framesValue.Text = metrics.FramesSent.ToString("#,0", CultureInfo.InvariantCulture);
        _reconnectsValue.Text = metrics.ReconnectCount.ToString("N0");
        _faultsValue.Text = metrics.FaultCount.ToString();

        _audioValue.Text =
            $"Input Kind: {settings.InputKind}\r\n" +
            $"Sample Rate: {settings.SampleRate} Hz\r\n" +
            $"Channels: {settings.Channels}\r\n" +
            $"Frame Samples: {settings.FrameSamples}\r\n" +
            $"Startup Burst Frames: {settings.StartupBurstFrames}";

        _startupValue.Text =
            $"Discover → Select: {FormatMs(metrics.DiscoveryDurationMs)}\r\n" +
            $"Select → Control: {FormatMs(metrics.ReceiverSelectionDurationMs)}\r\n" +
            $"Control → Config: {FormatMs(metrics.ControlConnectDurationMs)}\r\n" +
            $"Config → Streaming: {FormatMs(metrics.StreamingTransitionDurationMs)}\r\n" +
            $"Total Bring-up: {FormatMs(metrics.TotalBringUpDurationMs)}";

        _lastErrorBox.Text = metrics.LastError ?? status.LastError ?? "NONE";

        UpdateChart(_throughputChart, metrics.ThroughputHistory, Color.FromArgb(0, 120, 215));
        UpdateChart(_fpsChart, metrics.FpsHistory, Color.FromArgb(0, 153, 51));

        _sessionTimelineList.BeginUpdate();
        _sessionTimelineList.Items.Clear();

        foreach (var evt in metrics.SessionEvents.Reverse())
        {
            string details = string.IsNullOrWhiteSpace(evt.Details) ? "" : $" | {evt.Details}";
            _sessionTimelineList.Items.Add($"{evt.TimestampUtc:HH:mm:ss.fff}  {evt.EventName}{details}");
        }

        if (_sessionTimelineList.Items.Count > 0)
        {
            _sessionTimelineList.TopIndex = 0;
            _sessionTimelineList.SelectedIndex = 0;
            _sessionTimelineList.ClearSelected();
        }

        _sessionTimelineList.EndUpdate();
    }

    private static string FormatMs(double? value) =>
        value.HasValue ? $"{value.Value:F0} ms" : "—";

    private Control BuildHeaderPanel()
    {
        var panel = new Panel
        {
            Dock = DockStyle.Fill,
            BackColor = Color.White,
            Padding = new Padding(18, 14, 18, 14),
            Margin = new Padding(0)
        };

        panel.Paint += (_, e) => DrawBorder(panel, e);

        var title = new Label
        {
            Dock = DockStyle.Top,
            Height = 34,
            Font = new Font("Segoe UI Semibold", 20f),
            Text = "RifeZ Audio Bridge",
            TextAlign = ContentAlignment.MiddleLeft
        };

        var subtitle = new Label
        {
            Dock = DockStyle.Fill,
            Font = new Font("Segoe UI", 10.5f),
            ForeColor = Color.FromArgb(90, 90, 90),
            Text = "Windows companion app for Wi-Fi audio endpoint sessions",
            TextAlign = ContentAlignment.TopLeft
        };

        panel.Controls.Add(subtitle);
        panel.Controls.Add(title);
        return panel;
    }

    private Control CreateKpiCard(string title, Control valueControl)
    {
        var panel = new Panel
        {
            Dock = DockStyle.Fill,
            BackColor = Color.White,
            Margin = new Padding(6),
            Padding = new Padding(14, 12, 14, 12)
        };

        panel.Paint += (_, e) => DrawBorder(panel, e);

        var layout = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 2,
            BackColor = Color.White,
            Margin = new Padding(0),
            Padding = new Padding(0)
        };
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 24));
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 30));

        var titleLabel = new Label
        {
            Dock = DockStyle.Fill,
            Text = title,
            Font = new Font("Segoe UI Semibold", 10f),
            ForeColor = Color.FromArgb(85, 85, 85),
            TextAlign = ContentAlignment.MiddleLeft
        };

        valueControl.Dock = DockStyle.Fill;

        layout.Controls.Add(titleLabel, 0, 0);
        layout.Controls.Add(valueControl, 0, 1);

        panel.Controls.Add(layout);
        return panel;
    }

    private Control CreateCard(string title, Control valueControl)
    {
        var panel = new Panel
        {
            Dock = DockStyle.Fill,
            BackColor = Color.White,
            Margin = new Padding(6),
            Padding = new Padding(16)
        };

        panel.Paint += (_, e) => DrawBorder(panel, e);

        var layout = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 2,
            BackColor = Color.White,
            Margin = new Padding(0),
            Padding = new Padding(0)
        };
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 28));
        layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));

        var titleLabel = new Label
        {
            Dock = DockStyle.Fill,
            Text = title,
            Font = new Font("Segoe UI Semibold", 11f),
            ForeColor = Color.FromArgb(85, 85, 85),
            TextAlign = ContentAlignment.MiddleLeft
        };

        valueControl.Dock = DockStyle.Fill;

        layout.Controls.Add(titleLabel, 0, 0);
        layout.Controls.Add(valueControl, 0, 1);

        panel.Controls.Add(layout);
        return panel;
    }

    private Control CreateContainer(string title, Control content)
    {
        var panel = new Panel
        {
            Dock = DockStyle.Fill,
            BackColor = Color.White,
            Margin = new Padding(6),
            Padding = new Padding(16)
        };

        panel.Paint += (_, e) => DrawBorder(panel, e);

        var titleLabel = new Label
        {
            Dock = DockStyle.Top,
            Height = 26,
            Text = title,
            Font = new Font("Segoe UI Semibold", 11f),
            ForeColor = Color.FromArgb(85, 85, 85)
        };

        content.Dock = DockStyle.Fill;

        panel.Controls.Add(content);
        panel.Controls.Add(titleLabel);
        return panel;
    }

    private Chart CreateMetricChart(string yAxisTitle)
    {
        var chart = new Chart
        {
            Dock = DockStyle.Fill,
            BackColor = Color.White,
            Palette = ChartColorPalette.None,
            BorderlineWidth = 0
        };

        var area = new ChartArea("Main")
        {
            BackColor = Color.White
        };

        area.AxisX.Title = "Seconds Ago";
        area.AxisX.TitleFont = new Font("Segoe UI", 9f);
        area.AxisX.LabelStyle.Font = new Font("Segoe UI", 8.5f);
        area.AxisX.LabelStyle.ForeColor = Color.FromArgb(95, 95, 95);
        area.AxisX.MajorGrid.LineColor = Color.FromArgb(235, 235, 235);
        area.AxisX.LineColor = Color.FromArgb(180, 180, 180);
        area.AxisX.Minimum = 0;
        area.AxisX.Maximum = 60;
        area.AxisX.Interval = 10;
        area.AxisX.IsReversed = true;

        area.AxisY.Title = yAxisTitle;
        area.AxisY.TitleFont = new Font("Segoe UI", 9f);
        area.AxisY.LabelStyle.Font = new Font("Segoe UI", 8.5f);
        area.AxisY.LabelStyle.ForeColor = Color.FromArgb(95, 95, 95);
        area.AxisY.MajorGrid.LineColor = Color.FromArgb(235, 235, 235);
        area.AxisY.LineColor = Color.FromArgb(180, 180, 180);

        chart.ChartAreas.Add(area);

        var series = new Series("Series")
        {
            ChartType = SeriesChartType.Line,
            BorderWidth = 3,
            XValueType = ChartValueType.Double,
            YValueType = ChartValueType.Double,
            IsVisibleInLegend = false,
            MarkerStyle = MarkerStyle.None
        };

        chart.Series.Add(series);
        return chart;
    }

    private void UpdateChart(Chart chart, IReadOnlyList<MetricPoint> points, Color color)
    {
        if (chart.Series.Count == 0)
            return;

        var series = chart.Series["Series"];
        series.Points.Clear();
        series.Color = color;

        foreach (var point in points)
        {
            series.Points.AddXY(point.SecondsAgo, point.Value);
        }

        if (chart.ChartAreas.Count == 0)
            return;

        var area = chart.ChartAreas["Main"];

        if (points.Count == 0)
        {
            area.AxisY.Minimum = double.NaN;
            area.AxisY.Maximum = double.NaN;
            return;
        }

        double min = points.Min(p => p.Value);
        double max = points.Max(p => p.Value);

        if (Math.Abs(max - min) < 0.001)
        {
            double pad = Math.Max(1.0, max * 0.05);
            area.AxisY.Minimum = Math.Max(0, min - pad);
            area.AxisY.Maximum = max + pad;
            return;
        }

        double range = max - min;
        double padding = Math.Max(range * 0.15, 0.5);

        area.AxisY.Minimum = Math.Max(0, min - padding);
        area.AxisY.Maximum = max + padding;
    }

    private Label CreateValueLabel(bool multiline = false, float? fontSize = null)
    {
        float resolvedSize = fontSize ?? (multiline ? 11f : 18f);

        return new Label
        {
            AutoSize = false,
            Dock = DockStyle.Fill,
            TextAlign = multiline ? ContentAlignment.TopLeft : ContentAlignment.MiddleLeft,
            Font = new Font("Segoe UI Semibold", resolvedSize),
            ForeColor = Color.Black,
            BackColor = Color.White
        };
    }

    private void InitializeComponent()
    {
        System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(StatusForm));
        SuspendLayout();
        // 
        // StatusForm
        // 
        ClientSize = new Size(284, 261);
        Icon = (Icon)resources.GetObject("$this.Icon");
        Name = "StatusForm";
        Load += StatusForm_Load;
        ResumeLayout(false);

    }

    private static void DrawBorder(Control control, PaintEventArgs e)
    {
        var rect = new Rectangle(0, 0, control.Width - 1, control.Height - 1);
        using var pen = new Pen(Color.FromArgb(210, 210, 210));
        e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
        e.Graphics.DrawRectangle(pen, rect);
    }

    private void StatusForm_Load(object sender, EventArgs e)
    {
        InitializeComponent();
    }
}