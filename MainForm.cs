using System;
using System.Drawing;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace HikFingerprintLab;

internal sealed class MainForm : Form
{
    private readonly FingerprintVerificationService _engine = new();
    private readonly Label _status;
    private readonly Label _enrollment;
    private readonly Label _stats;
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

    public MainForm()
    {
        Text = "HikFingerprint Lab V2 — DS-K1F820-F";
        ClientSize = new Size(825, 605);
        MinimumSize = new Size(745, 565);
        StartPosition = FormStartPosition.CenterScreen;
        BackColor = Background;
        ForeColor = TextColor;
        Font = new Font("Segoe UI", 10f);

        var root = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 6,
            Padding = new Padding(22),
            BackColor = Background
        };
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 76));
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 75));
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 112));
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 52));
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 75));
        root.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        Controls.Add(root);

        var header = new Panel { Dock = DockStyle.Fill, BackColor = Background };
        header.Controls.Add(new Label
        {
            Text = "HikFingerprint Lab  /  V2",
            Font = new Font("Segoe UI", 19f, FontStyle.Bold),
            ForeColor = TextColor, Location = new Point(0, 0), AutoSize = true
        });
        header.Controls.Add(new Label
        {
            Text = "Hikvision DS-K1F820-F    •    Windows .NET 8 x64    •    Local verification prototype",
            Font = new Font("Segoe UI", 9f),
            ForeColor = Secondary, Location = new Point(2, 43), AutoSize = true
        });
        root.Controls.Add(header, 0, 0);

        root.Controls.Add(new Label
        {
            Text = "LAB ONLY — Does NOT unlock Windows. Optional saved enrollment uses Windows-user DPAPI encryption; other software running as you may still decrypt it. No account password is stored.",
            ForeColor = Color.FromArgb(254, 209, 131),
            BackColor = PanelBackground, Dock = DockStyle.Fill,
            Padding = new Padding(12), TextAlign = ContentAlignment.MiddleLeft
        }, 0, 1);

        var statePanel = new Panel
        {
            BackColor = PanelBackground, Dock = DockStyle.Fill,
            Padding = new Padding(12)
        };
        _status = new Label
        {
            Text = "Status: Ready. Close iVMS-4200 and connect your scanner.",
            ForeColor = TextColor, Dock = DockStyle.Top, AutoEllipsis = true, Height = 28
        };
        _enrollment = new Label
        {
            Text = "Enrollment: None", ForeColor = Secondary,
            Dock = DockStyle.Top, Height = 28
        };
        _stats = new Label
        {
            Text = "This session: 0 matches  •  0 rejections  •  0 unexpected codes",
            ForeColor = Secondary, Dock = DockStyle.Top, Height = 28
        };
        // Dock order is reversed during layout.
        statePanel.Controls.Add(_stats);
        statePanel.Controls.Add(_enrollment);
        statePanel.Controls.Add(_status);
        root.Controls.Add(statePanel, 0, 2);

        _remember = new CheckBox
        {
            Text = "Remember enrollment for this Windows user (encrypted local file)",
            Checked = _engine.HasSavedEnrollment,
            ForeColor = TextColor, BackColor = Background,
            Dock = DockStyle.Fill, Padding = new Padding(2, 10, 0, 0), AutoSize = false
        };
        root.Controls.Add(_remember, 0, 3);

        var buttons = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill, BackColor = Background,
            Padding = new Padding(0, 11, 0, 6), WrapContents = false
        };
        _enrollButton = MakeButton("Enroll / Replace", Color.FromArgb(41, 118, 233), 171);
        _verifyButton = MakeButton("Verify Finger", Color.FromArgb(41, 136, 91), 158);
        _clearButton = MakeButton("Clear All Enrollment", Color.FromArgb(78, 86, 103), 194);
        buttons.Controls.Add(_enrollButton);
        buttons.Controls.Add(_verifyButton);
        buttons.Controls.Add(_clearButton);
        root.Controls.Add(buttons, 0, 4);

        _log = new TextBox
        {
            Dock = DockStyle.Fill, Multiline = true, ReadOnly = true,
            ScrollBars = ScrollBars.Vertical, BackColor = PanelBackground,
            ForeColor = TextColor, BorderStyle = BorderStyle.FixedSingle,
            Font = new Font("Consolas", 10f), WordWrap = true, TabStop = false
        };
        root.Controls.Add(_log, 0, 5);

        _enrollButton.Click += async (_, _) => await EnrollClickedAsync();
        _verifyButton.Click += async (_, _) => await VerifyClickedAsync();
        _clearButton.Click += (_, _) => ClearClicked();

        _timer = new System.Windows.Forms.Timer { Interval = 1000 };
        _timer.Tick += (_, _) => UpdateUi();
        _timer.Start();
        UpdateUi();
        Append("Ready. Uses FPModule SDK already installed with iVMS-4200.");
        Append("Check 'Remember enrollment' BEFORE enrolling to save it with Windows-user DPAPI encryption.");
        if (_engine.HasSavedEnrollment)
            Append("Encrypted enrollment file found for this Windows user. Verify Finger to test it.");
    }

    private static Button MakeButton(string title, Color color, int width) => new()
    {
        Text = title, BackColor = color, ForeColor = Color.White,
        FlatStyle = FlatStyle.Flat, Width = width, Height = 43,
        Margin = new Padding(0, 0, 12, 0), Cursor = Cursors.Hand
    };

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
        _enrollment.Text = enrolled
            ? (_engine.HasSavedEnrollment ? "Enrollment: Encrypted file for this Windows user"
                : "Enrollment: In memory for this session")
            : "Enrollment: None";
        _stats.Text = $"This session: {_engine.Matches} matches  •  {_engine.NonMatches} rejections  •  " +
            $"{_engine.UnexpectedResults} unexpected codes";
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

    private void SetStatus(string message) => _status.Text = "Status: " + message;

    private void Append(string message) =>
        _log.AppendText($"[{DateTime.Now:HH:mm:ss}] {message}{Environment.NewLine}");

    protected override void OnFormClosing(FormClosingEventArgs e)
    {
        if (_busy)
        {
            e.Cancel = true;
            MessageBox.Show(this,
                "A scan is running. Finish the scan or wait for the SDK timeout before closing.",
                "HikFingerprint Lab V2", MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }
        _timer.Stop();
        _timer.Dispose();
        _engine.Dispose();
        base.OnFormClosing(e);
    }
}
