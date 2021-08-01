using System;
using System.Diagnostics;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace OsuPracticeTools.Forms
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
            Shown += MessageForm_Shown;
        }

        private void MessageForm_Shown(object sender, EventArgs e)
        {
        }

        private static void CloseForm(object sender, EventArgs e)
        {
            _elapsedMilliseconds += _timer.Interval;
            if (_elapsedMilliseconds >= 1000)
            {
                MForm.Hide();
                _isOpen = false;
                _elapsedMilliseconds = 0;
                _timer.Dispose();
                SwitchToThisWindow(_prevForegroundWindow, true);
            }
        }

        public static void ShowMessage(string message)
        {
            MForm._messageLabel.Text = message;
            MForm.Width = MForm._messageLabel.Width;
            MForm.CenterToScreen();

            if (!_isOpen)
            {
                _prevForegroundWindow = GetForegroundWindow();

                MForm.Show();
                _isOpen = true;

                SetForegroundWindow(MForm.Handle);

                _timer?.Dispose();
                _timer = new Timer { Interval = 100 };
                _timer.Tick += CloseForm;
                _timer.Start();
            }

            _elapsedMilliseconds = 0;
        }

        [DllImport("user32.dll")]
        private static extern int SetForegroundWindow(IntPtr hwnd);
        [DllImport("User32.dll", SetLastError = true)]
        static extern void SwitchToThisWindow(IntPtr hWnd, bool fAltTab);
        [DllImport("user32.dll")]
        private static extern IntPtr GetForegroundWindow();
    }
}