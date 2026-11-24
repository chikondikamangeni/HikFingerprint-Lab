using System;
using System.Drawing;
using System.IO;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace HikFingerprintLab;

internal sealed class MainForm : Form
{
    private readonly FingerprintVerificationService _engine = new();
    private readonly Label _status;
    private readonly Label _enrollment;
    private readonly TextBox _log;
    private readonly CheckBox _remember;
    private readonly Button _enrollButton;
    private readonly Button _verifyButton;
    private readonly Button _clearButton;
    private readonly System.Windows.Forms.Timer _timer;
    private bool _busy;

    private static readonly Color Background = Color.FromArgb(16, 19, 27);
    private static readonly Color PanelBackground = Color.FromArgb(27, 32, 43);
    private static readonly Color TextColor = Color.FromArgb(237, 241, 247);
    private static readonly Color Secondary = Color.FromArgb(177, 188, 204);

    private readonly Label _quality;
    private readonly Label _matches;
    private readonly Label _rejects;
    private readonly Label _unexpected;
    private readonly FingerprintGraphic _graphic;
    private readonly Button _logToggle;
    private readonly Button _sdkButton;
    private readonly Button _testButton;
    private readonly TableLayoutPanel _root;
    private int? _lastQuality;
    private bool _logsVisible;

    public MainForm()
    {
        Text = "HikFingerprint Lab — v0.2.0";
        ClientSize = new Size(880, 760);
        MinimumSize = new Size(760, 720);
        StartPosition = FormStartPosition.CenterScreen;
        BackColor = Background;
        ForeColor = TextColor;
        Font = new Font("Segoe UI", 10f);
        Icon = System.Drawing.Icon.ExtractAssociatedIcon(Application.ExecutablePath);

        _root = new TableLayoutPanel
        {
            Dock = DockStyle.Fill, ColumnCount = 1, RowCount = 8,
            Padding = new Padding(24), BackColor = Background
        };
        _root.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        foreach (int h in new[] { 70, 65 }) _root.RowStyles.Add(new RowStyle(SizeType.Absolute, h));
        _root.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        foreach (int h in new[] { 58, 38, 112, 44, 0 }) _root.RowStyles.Add(new RowStyle(SizeType.Absolute, h));
        Controls.Add(_root);

        var header = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 2, RowCount = 2 };
        header.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        header.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 115));
        header.RowStyles.Add(new RowStyle(SizeType.Absolute, 38));
        header.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        header.Controls.Add(new Label { Text = "HikFingerprint Lab", Font = new Font("Segoe UI", 22f, FontStyle.Bold), ForeColor = TextColor, Dock = DockStyle.Fill }, 0, 0);
        header.Controls.Add(new Label { Text = "v0.2.0  /  LAB", ForeColor = Secondary, Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleRight }, 1, 0);
        var subtitle = new Label { Text = "DS-K1F820-F  •  Windows x64  •  Local biometric research", ForeColor = Secondary, Dock = DockStyle.Fill };
        header.Controls.Add(subtitle, 0, 1); header.SetColumnSpan(subtitle, 2);
        _root.Controls.Add(header, 0, 0);

        _root.Controls.Add(new Label
        {
            Text = "EXPERIMENTAL ONLY — Does not unlock Windows. Saved enrollment is optional and uses Windows-user DPAPI; other software running as you may decrypt it.",
            ForeColor = Color.FromArgb(254, 209, 131), BackColor = PanelBackground,
            Dock = DockStyle.Fill, Padding = new Padding(12, 6, 12, 6), TextAlign = ContentAlignment.MiddleLeft
        }, 0, 1);

        var hero = new TableLayoutPanel { Dock = DockStyle.Fill, BackColor = PanelBackground, ColumnCount = 1, RowCount = 4, Padding = new Padding(14, 4, 14, 8) };
        hero.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        hero.RowStyles.Add(new RowStyle(SizeType.Absolute, 33));
        hero.RowStyles.Add(new RowStyle(SizeType.Absolute, 42));
        hero.RowStyles.Add(new RowStyle(SizeType.Absolute, 25));
        _graphic = new FingerprintGraphic { Dock = DockStyle.Fill };
        hero.Controls.Add(_graphic, 0, 0);
        _status = new Label { Text = "Status: Ready for a scan", ForeColor = TextColor, Font = new Font("Segoe UI", 15f, FontStyle.Bold), Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleCenter, AutoEllipsis = true };
        hero.Controls.Add(_status, 0, 1);
        _enrollment = new Label { ForeColor = Secondary, Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleCenter };
        hero.Controls.Add(_enrollment, 0, 2);
        var hint = new Label { Text = "Close iVMS-4200 before starting. Follow the scan prompts.", ForeColor = Secondary, Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleCenter, Font = new Font("Segoe UI", 9f) };
        hero.Controls.Add(hint, 0, 3);
        _root.Controls.Add(hero, 0, 2);

        var buttons = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 3, RowCount = 1, Padding = new Padding(0, 6, 0, 1) };
        for (int i = 0; i < 3; i++) buttons.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f / 3));
        _enrollButton = MakeButton("Enroll / Replace", Color.FromArgb(37, 99, 235));
        _verifyButton = MakeButton("Verify Finger", Color.FromArgb(25, 115, 80));
        _clearButton = MakeButton("Clear Enrollment", Color.FromArgb(52, 62, 78));
        buttons.Controls.Add(_enrollButton, 0, 0); buttons.Controls.Add(_verifyButton, 1, 0); buttons.Controls.Add(_clearButton, 2, 0);
        _root.Controls.Add(buttons, 0, 3);
        _remember = new CheckBox { Text = "Remember enrollment for this Windows user (encrypted local file)", Checked = _engine.HasSavedEnrollment, ForeColor = TextColor, BackColor = Background, Dock = DockStyle.Fill };
        _root.Controls.Add(_remember, 0, 4);

        var metrics = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 4, RowCount = 1 };
        for (int i = 0; i < 4; i++) metrics.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25));
        metrics.Controls.Add(Metric("Matches", out _matches), 0, 0);
        metrics.Controls.Add(Metric("Rejections", out _rejects), 1, 0);
        metrics.Controls.Add(Metric("SDK quality", out _quality), 2, 0);
        metrics.Controls.Add(Metric("Other codes", out _unexpected), 3, 0);
        _root.Controls.Add(metrics, 0, 5);

        _logToggle = MakeButton("Show diagnostics  +", Color.FromArgb(34, 42, 56));
        _logToggle.TextAlign = ContentAlignment.MiddleLeft;
        var tools = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 3, RowCount = 1 };
        tools.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 40));
        tools.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 30));
        tools.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 30));
        _sdkButton = MakeButton("SDK location", Color.FromArgb(34, 42, 56));
        _testButton = MakeButton("Test scanner", Color.FromArgb(34, 42, 56));
        tools.Controls.Add(_logToggle, 0, 0);
        tools.Controls.Add(_sdkButton, 1, 0);
        tools.Controls.Add(_testButton, 2, 0);
        _root.Controls.Add(tools, 0, 6);
        _log = new TextBox { Dock = DockStyle.Fill, Multiline = true, ReadOnly = true, ScrollBars = ScrollBars.Vertical, BackColor = PanelBackground, ForeColor = TextColor, BorderStyle = BorderStyle.FixedSingle, Font = new Font("Consolas", 10f), WordWrap = true, TabStop = false, Visible = false };
        _root.Controls.Add(_log, 0, 7);
        _logToggle.Click += (_, _) => ToggleLogs();
        _sdkButton.Click += (_, _) => SelectSdkClicked();
        _testButton.Click += async (_, _) => await TestScannerAsync();
        _enrollButton.Click += async (_, _) => await EnrollClickedAsync();
        _verifyButton.Click += async (_, _) => await VerifyClickedAsync();
        _clearButton.Click += (_, _) => ClearClicked();
        _timer = new System.Windows.Forms.Timer { Interval = 100 };
        _timer.Tick += (_, _) => { _graphic.Advance(_busy); if (!_busy) UpdateUi(); };
        _timer.Start();
        UpdateUi();
        Append("Ready. Uses an existing, trusted Hikvision SDK installation.");
        ReportSdkLocation();
        Append("Check 'Remember enrollment' BEFORE enrolling to save it with Windows-user DPAPI encryption.");
        if (_engine.HasSavedEnrollment) Append("Encrypted enrollment file found for this Windows user. Verify Finger to test it.");
    }

    private Control Metric(string title, out Label value)
    {
        var panel = new TableLayoutPanel { Dock = DockStyle.Fill, BackColor = PanelBackground, Margin = new Padding(3), Padding = new Padding(9, 5, 9, 5), RowCount = 2, ColumnCount = 1 };
        panel.RowStyles.Add(new RowStyle(SizeType.Percent, 58)); panel.RowStyles.Add(new RowStyle(SizeType.Percent, 42));
        value = new Label { Text = "—", Dock = DockStyle.Fill, Font = new Font("Segoe UI", 19f, FontStyle.Bold), TextAlign = ContentAlignment.MiddleCenter, ForeColor = TextColor };
        panel.Controls.Add(value, 0, 0);
        panel.Controls.Add(new Label { Text = title, ForeColor = Secondary, Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleCenter, Font = new Font("Segoe UI", 9f) }, 0, 1);
        return panel;
    }

    private static Button MakeButton(string title, Color color)
    {
        var b = new Button { Text = title, BackColor = color, ForeColor = Color.White, FlatStyle = FlatStyle.Flat, Dock = DockStyle.Fill, Margin = new Padding(3), Cursor = Cursors.Hand, UseVisualStyleBackColor = false };
        b.FlatAppearance.BorderSize = 0;
        return b;
    }

    private void ToggleLogs()
    {
        _logsVisible = !_logsVisible;
        _log.Visible = _logsVisible;
        _logToggle.Text = _logsVisible ? "Hide diagnostics  −" : "Show diagnostics  +";
        _root.RowStyles[7].Height = _logsVisible ? Math.Min(150, Math.Max(90, ClientSize.Height / 5)) : 0;
    }

    private void ReportSdkLocation()
    {
        try { Append(FingerprintSdk.DescribeSdk()); }
        catch (Exception ex) { Append("SDK inspection failed: " + ex.Message); }
    }

    private void SelectSdkClicked()
    {
        if (_busy) return;
        if (!_logsVisible) ToggleLogs();
        ReportSdkLocation();
        if (!FingerprintSdk.CanChangeLocation)
        {
            MessageBox.Show(this, "The SDK is already loaded. Restart the app to select another installation.",
                "SDK location", MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }
        if (MessageBox.Show(this,
            "Only select the DLL from your own trusted, legitimately installed Hikvision software. " +
            "Native libraries execute code. Compatibility is not guaranteed. Selection applies to this app session only. Continue?",
            "SDK location", MessageBoxButtons.YesNo, MessageBoxIcon.Warning) != DialogResult.Yes) return;
        using var dialog = new OpenFileDialog
        {
            Title = "Select the installed Hikvision fingerprint SDK",
            Filter = "Hikvision fingerprint SDK (FPModule_SDK_x64.dll)|FPModule_SDK_x64.dll",
            FileName = FingerprintSdk.DllName,
            CheckFileExists = true, CheckPathExists = true, Multiselect = false
        };
        string? path = FingerprintSdk.SdkPath;
        if (path is not null) dialog.InitialDirectory = Path.GetDirectoryName(path)!;
        if (dialog.ShowDialog(this) != DialogResult.OK) return;
        try { FingerprintSdk.SelectSdk(dialog.FileName); ReportSdkLocation(); SetStatus("SDK location selected."); }
        catch (Exception ex) { Append("ERROR: " + ex.Message); SetStatus("SDK selection failed."); }
    }

    private async Task TestScannerAsync()
    {
        if (_busy) return;
        if (!_logsVisible) ToggleLogs();
        ReportSdkLocation();
        SetBusy(true);
        SetStatus("Testing scanner connection...");
        try
        {
            await Task.Run(() => FingerprintSdk.TestConnection(ReportFromWorker));
            SetStatus("Scanner connection successful.");
            Append("Scanner opened and configured successfully. No fingerprint collected.");
        }
        catch (Exception ex)
        {
            SetStatus("Scanner connection failed.");
            Append("ERROR: " + ex.Message);
        }
        finally { SetBusy(false); }
    }

    private async Task EnrollClickedAsync()
    {
        if (_busy) return;
        bool remember = _remember.Checked;
        if (_engine.HasSavedEnrollment && !remember &&
            MessageBox.Show(this,
                "This replaces your saved enrollment with a memory-only enrollment, deleting the saved file. Continue?",
                "Replace saved enrollment", MessageBoxButtons.YesNo, MessageBoxIcon.Warning) != DialogResult.Yes)
            return;

        SetBusy(true);
        SetStatus("Preparing enrollment...");
        Append("Enrollment starting. Keep sensor clear until prompted.");
        try
        {
            int quality = await Task.Run(() => _engine.Enroll(remember, ReportFromWorker));
            _lastQuality = quality;
            SetStatus("Enrollment complete.");
            Append($"Enrollment succeeded. Quality {quality}; " +
                (remember ? "saved encrypted for current Windows user." : "held in memory only."));
        }
        catch (Exception ex)
        {
            SetStatus("Enrollment failed; existing enrollment not intentionally replaced.");
            Append("ERROR: " + ex.Message);
        }
        finally
        {
            SetBusy(false);
        }
    }

    private async Task VerifyClickedAsync()
    {
        if (_busy || !_engine.HasEnrollment || _engine.CooldownRemaining > TimeSpan.Zero) return;
        SetBusy(true);
        SetStatus("Preparing verification...");
        Append("Verification starting. Test enrolled finger and a different finger.");
        try
        {
            var result = await Task.Run(() => _engine.Verify(ReportFromWorker));
            _lastQuality = result.Quality;
            if (result.MatchCode == 0)
            {
                SetStatus("SDK result: MATCH (0)");
                Append($"MATCH (0) • Quality {result.Quality}. Not proof of secure authentication.");
            }
            else if (result.MatchCode == 6)
            {
                SetStatus("SDK result: NO MATCH (6)");
                Append($"NO MATCH (6) • Quality {result.Quality}.");
                if (_engine.CooldownRemaining > TimeSpan.Zero)
                    Append("5 consecutive non-matches: 30-second test cooldown activated.");
            }
            else
            {
                SetStatus($"Unexpected SDK result {result.MatchCode}.");
                Append($"WARNING: Unexpected MatchTemplate code {result.MatchCode}.");
            }
        }
        catch (Exception ex)
        {
            SetStatus("Verification failed.");
            Append("ERROR: " + ex.Message);
        }
        finally
        {
            SetBusy(false);
        }
    }

    private void ClearClicked()
    {
        if (_busy || !_engine.HasEnrollment) return;
        if (MessageBox.Show(this,
            "Delete the encrypted local enrollment (if present) and clear the in-memory template?\n\n" +
            "This cannot guarantee erasure from SSD snapshots or backups.",
            "Clear all enrollment", MessageBoxButtons.YesNo, MessageBoxIcon.Warning)
            != DialogResult.Yes)
            return;
        try
        {
            _engine.ClearAll();
            _remember.Checked = false;
            _lastQuality = null;
            SetStatus("Enrollment cleared.");
            Append("Enrollment cleared: local saved file deleted and managed memory cleared (best effort).");
        }
        catch (Exception ex)
        {
            SetStatus("Unable to clear enrollment.");
            Append("ERROR: " + ex.Message);
        }
        UpdateUi();
    }

    private void ReportFromWorker(string message)
    {
        if (!IsHandleCreated || IsDisposed) return;
        try
        {
            BeginInvoke(new Action(() =>
            {
                if (IsDisposed) return;
                SetStatus(message);
                Append(message);
            }));
        }
        catch (InvalidOperationException) { /* closing */ }
    }

    private void UpdateUi()
    {
        bool enrolled = _engine.HasEnrollment;
        _enrollButton.Enabled = !_busy;
        _verifyButton.Enabled = !_busy && enrolled && _engine.CooldownRemaining <= TimeSpan.Zero;
        _clearButton.Enabled = !_busy && enrolled;
        _remember.Enabled = !_busy;
        _sdkButton.Enabled = !_busy;
        _testButton.Enabled = !_busy;
        _enrollment.Text = enrolled
            ? (_engine.HasSavedEnrollment ? "Enrollment: Encrypted file for this Windows user"
                : "Enrollment: In memory for this session")
            : "Enrollment: None";
        _matches.Text = _engine.Matches.ToString();
        _rejects.Text = _engine.NonMatches.ToString();
        _unexpected.Text = _engine.UnexpectedResults.ToString();
        _quality.Text = _lastQuality?.ToString() ?? "—";
        if (!_busy && _engine.CooldownRemaining > TimeSpan.Zero)
            _status.Text = $"Status: Test cooldown — {Math.Ceiling(_engine.CooldownRemaining.TotalSeconds)}s remaining";
        else if (!_busy && _status.Text.StartsWith("Status: Test cooldown", StringComparison.Ordinal))
            SetStatus("Ready for next verification.");
        UseWaitCursor = _busy;
    }

    private void SetBusy(bool value)
    {
        _busy = value;
        UpdateUi();
    }

    private void SetStatus(string message)
    {
        _status.Text = message;
        _graphic.Result = message.Contains("NO MATCH", StringComparison.Ordinal) || message.Contains("failed", StringComparison.OrdinalIgnoreCase) ? -1 : message.Contains("MATCH (0)", StringComparison.Ordinal) || message == "Enrollment complete." ? 1 : 0;
        _graphic.Invalidate();
    }

    private void Append(string message) =>
        _log.AppendText($"[{DateTime.Now:HH:mm:ss}] {message}{Environment.NewLine}");

    protected override void OnFormClosing(FormClosingEventArgs e)
    {
        if (_busy)
        {
            e.Cancel = true;
            MessageBox.Show(this,
                "A scan is running. Finish the scan or wait for the SDK timeout before closing.",
                "HikFingerprint Lab", MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }
        _timer.Stop();
        _timer.Dispose();
        _engine.Dispose();
        base.OnFormClosing(e);
    }
}
