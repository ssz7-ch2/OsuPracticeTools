using System;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace OsuPracticeTools.Core.GlobalSettings
{
    public sealed class MessageForm : Form
    {
        private static readonly MessageForm MForm = new();
        private readonly Label _messageLabel;
        private static Timer _timer;
        private static bool _isOpen;
        private static int _elapsedMilliseconds = 0;
        private static IntPtr _prevForegroundWindow;

        public MessageForm()
        {
            ShowInTaskbar = false;
            FormBorderStyle = FormBorderStyle.None;
            TopMost = true;
            BackColor = Color.Black;
            Opacity = 0.8;
            StartPosition = FormStartPosition.CenterScreen;
            _messageLabel = new Label
            {
                AutoSize = true,
                Padding = new Padding(10),
                TextAlign = ContentAlignment.MiddleCenter,
                Dock = DockStyle.Fill,
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 20)
            };
            Controls.Add(_messageLabel);
            Height = _messageLabel.Height;
        }

        private static void CloseForm(object sender, EventArgs e)
        {
            _elapsedMilliseconds += _timer.Interval;
            if (_elapsedMilliseconds >= 1500)
            {
                MForm.Hide();
                _isOpen = false;
                Cursor.Show();
                _elapsedMilliseconds = 0;
                _timer.Dispose();
                SwitchToThisWindow(_prevForegroundWindow, true);
            }
        }

        public static void ShowMessage(string message)
        {
            MForm._messageLabel.Text = message;
            MForm.Width = MForm._messageLabel.Width;
            MForm.BringToFront();
            CenterToOsu();

            if (!_isOpen)
            {
                _prevForegroundWindow = GetForegroundWindow();
                _isOpen = true;
                Cursor.Hide();

                MForm.Show();

                _timer?.Dispose();
                _timer = new Timer { Interval = 100 };
                _timer.Tick += CloseForm;
                _timer.Start();
            }

            if (SetForegroundWindow(MForm.Handle) == 0)
            {
                MForm.WindowState = FormWindowState.Minimized;
                MForm.Show();
                MForm.WindowState = FormWindowState.Normal;
            }

            _elapsedMilliseconds = 0;
        }

        private static void CenterToOsu()
        {
            if (IsIconic(Program.OsuProcess.MainWindowHandle))
            {
                MForm.CenterToScreen();
                return;
            }
            var osuRect = new Rect();
            GetWindowRect(Program.OsuProcess.MainWindowHandle, ref osuRect);
            MForm.Top = osuRect.Top + (osuRect.Bottom - osuRect.Top - MForm.Height) / 2;
            MForm.Left = osuRect.Left + (osuRect.Right - osuRect.Left - MForm.Width) / 2;
        }

        [DllImport("user32.dll")]
        private static extern int SetForegroundWindow(IntPtr hwnd);
        [DllImport("User32.dll", SetLastError = true)]
        static extern void SwitchToThisWindow(IntPtr hWnd, bool fAltTab);
        [DllImport("user32.dll")]
        private static extern IntPtr GetForegroundWindow();
        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        static extern bool IsIconic(IntPtr hWnd);
        [DllImport("user32.dll")]
        public static extern bool GetWindowRect(IntPtr hwnd, ref Rect rectangle);
        public struct Rect
        {
            public int Left { get; set; }
            public int Top { get; set; }
            public int Right { get; set; }
            public int Bottom { get; set; }
        }

        protected override CreateParams CreateParams
        {
            get
            {
                var Params = base.CreateParams;
                Params.ExStyle |= 0x80;

                return Params;
            }
        }
    }
}