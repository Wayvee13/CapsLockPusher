using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace CapsLockPusher;

internal static class Program
{
    [STAThread]
    private static void Main()
    {
        ApplicationConfiguration.Initialize();
        Application.Run(new MainForm());
    }
}

public sealed class MainForm : Form
{
    private const byte VK_CAPITAL = 0x14;
    private const uint KEYEVENTF_KEYUP = 0x0002;

    private readonly Timer _pressTimer;
    private readonly Timer _uiTimer;
    private readonly NotifyIcon _trayIcon;

    private readonly Label _statusBadge;
    private readonly Label _statusTitle;
    private readonly Label _lastPressValue;
    private readonly Label _nextPressValue;
    private readonly Label _intervalValue;
    private readonly Label _hintLabel;
    private readonly Button _toggleButton;
    private readonly Button _pressNowButton;
    private readonly NumericUpDown _intervalInput;
    private readonly CheckBox _startMinimizedBox;
    private readonly ProgressBar _progress;

    private bool _isRunning = true;
    private DateTime _lastPressAt = DateTime.MinValue;
    private DateTime _nextPressAt;
    private int IntervalSeconds => Math.Max(5, (int)_intervalInput.Value);

    public MainForm()
    {
        Text = "CapsLock Pusher";
        Width = 760;
        Height = 520;
        MinimumSize = new Size(720, 500);
        StartPosition = FormStartPosition.CenterScreen;
        BackColor = Color.FromArgb(8, 11, 19);
        ForeColor = Color.White;
        Font = new Font("Segoe UI", 10);
        Icon = LoadAppIcon();

        _pressTimer = new Timer();
        _pressTimer.Tick += (_, _) => PressCapsLockAndUpdate();

        _uiTimer = new Timer { Interval = 500 };
        _uiTimer.Tick += (_, _) => RefreshStatusUi();
        _uiTimer.Start();

        _nextPressAt = DateTime.Now.AddSeconds(60);

        _trayIcon = new NotifyIcon
        {
            Icon = Icon,
            Text = "CapsLock Pusher",
            Visible = true,
            ContextMenuStrip = BuildTrayMenu()
        };
        _trayIcon.DoubleClick += (_, _) => ShowFromTray();

        var root = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            Padding = new Padding(24),
            ColumnCount = 1,
            RowCount = 4,
            BackColor = BackColor
        };
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 110));
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 160));
        root.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 72));
        Controls.Add(root);

        var header = new GradientPanel
        {
            Dock = DockStyle.Fill,
            Radius = 26,
            StartColor = Color.FromArgb(20, 30, 58),
            EndColor = Color.FromArgb(12, 17, 30),
            BorderColor = Color.FromArgb(60, 91, 190),
            Padding = new Padding(22)
        };
        root.Controls.Add(header, 0, 0);

        var title = new Label
        {
            Text = "CapsLock Pusher",
            Font = new Font("Segoe UI", 24, FontStyle.Bold),
            AutoSize = true,
            Location = new Point(22, 18)
        };
        header.Controls.Add(title);

        var subtitle = new Label
        {
            Text = "Presses CapsLock every selected interval for report scripts",
            Font = new Font("Segoe UI", 10),
            ForeColor = Color.FromArgb(150, 164, 190),
            AutoSize = true,
            Location = new Point(26, 62)
        };
        header.Controls.Add(subtitle);

        _statusBadge = new Label
        {
            Text = "ACTIVE",
            TextAlign = ContentAlignment.MiddleCenter,
            Font = new Font("Segoe UI", 10, FontStyle.Bold),
            ForeColor = Color.FromArgb(134, 239, 172),
            BackColor = Color.FromArgb(22, 101, 52),
            Width = 110,
            Height = 34,
            Location = new Point(570, 26)
        };
        header.Controls.Add(_statusBadge);

        var infoGrid = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 3,
            RowCount = 1,
            Padding = new Padding(0, 18, 0, 10),
            BackColor = BackColor
        };
        infoGrid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 33.33f));
        infoGrid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 33.33f));
        infoGrid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 33.33f));
        root.Controls.Add(infoGrid, 0, 1);

        _statusTitle = new Label();
        var card1 = CreateInfoCard("Status", _statusTitle, "Timer is running");
        _lastPressValue = new Label();
        var card2 = CreateInfoCard("Last CapsLock", _lastPressValue, "Waiting for first press");
        _nextPressValue = new Label();
        var card3 = CreateInfoCard("Next press", _nextPressValue, "Countdown");

        infoGrid.Controls.Add(card1, 0, 0);
        infoGrid.Controls.Add(card2, 1, 0);
        infoGrid.Controls.Add(card3, 2, 0);

        var settingsCard = new GradientPanel
        {
            Dock = DockStyle.Fill,
            Radius = 24,
            StartColor = Color.FromArgb(13, 18, 30),
            EndColor = Color.FromArgb(10, 13, 22),
            BorderColor = Color.FromArgb(39, 51, 75),
            Padding = new Padding(24)
        };
        root.Controls.Add(settingsCard, 0, 2);

        var settingsTitle = new Label
        {
            Text = "Control Panel",
            Font = new Font("Segoe UI", 18, FontStyle.Bold),
            AutoSize = true,
            Location = new Point(24, 22)
        };
        settingsCard.Controls.Add(settingsTitle);

        var intervalLabel = new Label
        {
            Text = "Interval, seconds",
            ForeColor = Color.FromArgb(148, 163, 184),
            AutoSize = true,
            Location = new Point(28, 78)
        };
        settingsCard.Controls.Add(intervalLabel);

        _intervalInput = new NumericUpDown
        {
            Minimum = 5,
            Maximum = 3600,
            Value = 60,
            Increment = 5,
            Width = 120,
            Height = 34,
            Location = new Point(30, 104),
            BackColor = Color.FromArgb(15, 23, 42),
            ForeColor = Color.White,
            BorderStyle = BorderStyle.FixedSingle
        };
        _intervalInput.ValueChanged += (_, _) => ResetTimer();
        settingsCard.Controls.Add(_intervalInput);

        _intervalValue = new Label
        {
            Text = "Every 60 seconds",
            Font = new Font("Segoe UI", 11, FontStyle.Bold),
            ForeColor = Color.FromArgb(147, 197, 253),
            AutoSize = true,
            Location = new Point(170, 108)
        };
        settingsCard.Controls.Add(_intervalValue);

        _startMinimizedBox = new CheckBox
        {
            Text = "Minimize to tray when closing",
            Checked = true,
            ForeColor = Color.FromArgb(203, 213, 225),
            AutoSize = true,
            Location = new Point(30, 154)
        };
        settingsCard.Controls.Add(_startMinimizedBox);

        _progress = new ProgressBar
        {
            Width = 410,
            Height = 18,
            Location = new Point(250, 158),
            Style = ProgressBarStyle.Continuous
        };
        settingsCard.Controls.Add(_progress);

        _hintLabel = new Label
        {
            Text = "The app stays in tray. Right-click tray icon to control it.",
            ForeColor = Color.FromArgb(148, 163, 184),
            AutoSize = true,
            Location = new Point(30, 205)
        };
        settingsCard.Controls.Add(_hintLabel);

        var buttons = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            FlowDirection = FlowDirection.RightToLeft,
            Padding = new Padding(0, 14, 0, 0),
            BackColor = BackColor
        };
        root.Controls.Add(buttons, 0, 3);

        _toggleButton = CreateButton("Stop", Color.FromArgb(220, 38, 38));
        _toggleButton.Click += (_, _) => ToggleRunning();
        buttons.Controls.Add(_toggleButton);

        _pressNowButton = CreateButton("Press now", Color.FromArgb(37, 99, 235));
        _pressNowButton.Click += (_, _) => PressCapsLockAndUpdate();
        buttons.Controls.Add(_pressNowButton);

        StartTimer();
        RefreshStatusUi();
    }

    private ContextMenuStrip BuildTrayMenu()
    {
        var menu = new ContextMenuStrip();

        var show = new ToolStripMenuItem("Open window");
        show.Click += (_, _) => ShowFromTray();

        var toggle = new ToolStripMenuItem("Start / Stop");
        toggle.Click += (_, _) => ToggleRunning();

        var press = new ToolStripMenuItem("Press CapsLock now");
        press.Click += (_, _) => PressCapsLockAndUpdate();

        var exit = new ToolStripMenuItem("Exit");
        exit.Click += (_, _) =>
        {
            _trayIcon.Visible = false;
            Application.Exit();
        };

        menu.Items.Add(show);
        menu.Items.Add(toggle);
        menu.Items.Add(press);
        menu.Items.Add(new ToolStripSeparator());
        menu.Items.Add(exit);
        return menu;
    }

    private Icon LoadAppIcon()
    {
        try
        {
            string path = System.IO.Path.Combine(AppContext.BaseDirectory, "app.ico");
            if (System.IO.File.Exists(path)) return new Icon(path);
        }
        catch { }

        return SystemIcons.Application;
    }

    private GradientPanel CreateInfoCard(string label, Label value, string sub)
    {
        var panel = new GradientPanel
        {
            Dock = DockStyle.Fill,
            Margin = new Padding(0, 0, 14, 0),
            Radius = 22,
            StartColor = Color.FromArgb(13, 18, 30),
            EndColor = Color.FromArgb(10, 13, 22),
            BorderColor = Color.FromArgb(39, 51, 75),
            Padding = new Padding(18)
        };

        var title = new Label
        {
            Text = label,
            ForeColor = Color.FromArgb(148, 163, 184),
            AutoSize = true,
            Location = new Point(18, 18)
        };
        panel.Controls.Add(title);

        value.Text = "—";
        value.Font = new Font("Segoe UI", 18, FontStyle.Bold);
        value.ForeColor = Color.White;
        value.AutoSize = true;
        value.Location = new Point(18, 52);
        panel.Controls.Add(value);

        var subtitle = new Label
        {
            Text = sub,
            ForeColor = Color.FromArgb(100, 116, 139),
            AutoSize = true,
            Location = new Point(20, 96)
        };
        panel.Controls.Add(subtitle);

        return panel;
    }

    private Button CreateButton(string text, Color color)
    {
        return new Button
        {
            Text = text,
            Width = 150,
            Height = 44,
            Margin = new Padding(12, 0, 0, 0),
            BackColor = color,
            ForeColor = Color.White,
            Font = new Font("Segoe UI", 11, FontStyle.Bold),
            FlatStyle = FlatStyle.Flat
        };
    }

    private void ToggleRunning()
    {
        if (_isRunning) StopTimer();
        else StartTimer();

        RefreshStatusUi();
    }

    private void StartTimer()
    {
        _isRunning = true;
        _pressTimer.Interval = IntervalSeconds * 1000;
        _pressTimer.Start();
        _nextPressAt = DateTime.Now.AddSeconds(IntervalSeconds);
        _trayIcon.Text = "CapsLock Pusher — active";
    }

    private void StopTimer()
    {
        _isRunning = false;
        _pressTimer.Stop();
        _trayIcon.Text = "CapsLock Pusher — stopped";
    }

    private void ResetTimer()
    {
        _intervalValue.Text = $"Every {IntervalSeconds} seconds";
        if (_isRunning)
        {
            _pressTimer.Stop();
            _pressTimer.Interval = IntervalSeconds * 1000;
            _pressTimer.Start();
            _nextPressAt = DateTime.Now.AddSeconds(IntervalSeconds);
        }
    }

    private void PressCapsLockAndUpdate()
    {
        PressCapsLock();
        _lastPressAt = DateTime.Now;
        _nextPressAt = DateTime.Now.AddSeconds(IntervalSeconds);
        RefreshStatusUi();
    }

    private void RefreshStatusUi()
    {
        _statusTitle.Text = _isRunning ? "Active" : "Stopped";
        _statusTitle.ForeColor = _isRunning ? Color.FromArgb(134, 239, 172) : Color.FromArgb(252, 165, 165);

        _statusBadge.Text = _isRunning ? "ACTIVE" : "STOPPED";
        _statusBadge.ForeColor = _isRunning ? Color.FromArgb(134, 239, 172) : Color.FromArgb(252, 165, 165);
        _statusBadge.BackColor = _isRunning ? Color.FromArgb(22, 101, 52) : Color.FromArgb(127, 29, 29);

        _toggleButton.Text = _isRunning ? "Stop" : "Start";
        _toggleButton.BackColor = _isRunning ? Color.FromArgb(220, 38, 38) : Color.FromArgb(22, 163, 74);

        _lastPressValue.Text = _lastPressAt == DateTime.MinValue ? "—" : _lastPressAt.ToString("HH:mm:ss");

        if (_isRunning)
        {
            var remain = _nextPressAt - DateTime.Now;
            if (remain.TotalSeconds < 0) remain = TimeSpan.Zero;
            _nextPressValue.Text = $"{Math.Ceiling(remain.TotalSeconds)}s";

            var total = IntervalSeconds;
            var left = Math.Max(0, Math.Min(total, remain.TotalSeconds));
            var done = (int)Math.Round((1 - left / total) * 100);
            _progress.Value = Math.Max(0, Math.Min(100, done));
        }
        else
        {
            _nextPressValue.Text = "Paused";
            _progress.Value = 0;
        }
    }

    private void ShowFromTray()
    {
        Show();
        WindowState = FormWindowState.Normal;
        Activate();
    }

    protected override void OnFormClosing(FormClosingEventArgs e)
    {
        if (_startMinimizedBox.Checked && e.CloseReason == CloseReason.UserClosing)
        {
            e.Cancel = true;
            Hide();
            _trayIcon.ShowBalloonTip(1500, "CapsLock Pusher", "Still running in tray.", ToolTipIcon.Info);
            return;
        }

        base.OnFormClosing(e);
    }

    private static void PressCapsLock()
    {
        var inputs = new INPUT[]
        {
            new INPUT
            {
                type = 1,
                U = new InputUnion
                {
                    ki = new KEYBDINPUT
                    {
                        wVk = VK_CAPITAL,
                        wScan = 0,
                        dwFlags = 0,
                        time = 0,
                        dwExtraInfo = IntPtr.Zero
                    }
                }
            },
            new INPUT
            {
                type = 1,
                U = new InputUnion
                {
                    ki = new KEYBDINPUT
                    {
                        wVk = VK_CAPITAL,
                        wScan = 0,
                        dwFlags = KEYEVENTF_KEYUP,
                        time = 0,
                        dwExtraInfo = IntPtr.Zero
                    }
                }
            }
        };

        SendInput((uint)inputs.Length, inputs, Marshal.SizeOf(typeof(INPUT)));
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct INPUT
    {
        public uint type;
        public InputUnion U;
    }

    [StructLayout(LayoutKind.Explicit)]
    private struct InputUnion
    {
        [FieldOffset(0)] public KEYBDINPUT ki;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct KEYBDINPUT
    {
        public ushort wVk;
        public ushort wScan;
        public uint dwFlags;
        public uint time;
        public IntPtr dwExtraInfo;
    }

    [DllImport("user32.dll", SetLastError = true)]
    private static extern uint SendInput(uint nInputs, INPUT[] pInputs, int cbSize);
}

public sealed class GradientPanel : Panel
{
    public Color StartColor { get; set; } = Color.FromArgb(15, 23, 42);
    public Color EndColor { get; set; } = Color.FromArgb(2, 6, 23);
    public Color BorderColor { get; set; } = Color.FromArgb(51, 65, 85);
    public int Radius { get; set; } = 20;

    protected override void OnPaint(PaintEventArgs e)
    {
        base.OnPaint(e);
        e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;

        var rect = new Rectangle(0, 0, Width - 1, Height - 1);
        using var path = RoundedRect(rect, Radius);
        using var brush = new LinearGradientBrush(rect, StartColor, EndColor, LinearGradientMode.Vertical);
        using var pen = new Pen(BorderColor, 1);

        e.Graphics.FillPath(brush, path);
        e.Graphics.DrawPath(pen, path);
    }

    private static GraphicsPath RoundedRect(Rectangle bounds, int radius)
    {
        int diameter = radius * 2;
        var path = new GraphicsPath();

        path.AddArc(bounds.X, bounds.Y, diameter, diameter, 180, 90);
        path.AddArc(bounds.Right - diameter, bounds.Y, diameter, diameter, 270, 90);
        path.AddArc(bounds.Right - diameter, bounds.Bottom - diameter, diameter, diameter, 0, 90);
        path.AddArc(bounds.X, bounds.Bottom - diameter, diameter, diameter, 90, 90);
        path.CloseFigure();

        return path;
    }
}
