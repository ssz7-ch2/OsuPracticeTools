using System;
using System.Drawing;
using System.Windows.Forms;

namespace OsuPracticeTools.Forms
{
    public sealed class MessageForm : Form
    {
        private static readonly MessageForm MForm = new();
        private readonly Label _messageLabel;
        private Timer _timer;
        private static bool _isOpen;
        private static int _elapsedMilliseconds = 0;
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
            _timer?.Dispose();
            _timer = new Timer{ Interval = 100 };
            _timer.Tick += CloseForm;
            _timer.Start();
        }

        private void CloseForm(object sender, EventArgs e)
        {
            _elapsedMilliseconds += _timer.Interval;
            if (_elapsedMilliseconds >= 1000)
            {
                Hide();
                _isOpen = false;
                _elapsedMilliseconds = 0;
            }
        }

        public static void ShowMessage(string message)
        {
            MForm._messageLabel.Text = message;
            MForm.Width = MForm._messageLabel.Width;
            MForm.BringToFront();
            if (!_isOpen)
            {
                MForm.Show();
                _isOpen = true;
            }
            _elapsedMilliseconds = 0;
        }

        protected override CreateParams CreateParams
        {
            get
            {
                var baseParams = base.CreateParams;

                const int WS_EX_NOACTIVATE = 0x08000000;
                const int WS_EX_TOOLWINDOW = 0x00000080;
                baseParams.ExStyle |= (int)(WS_EX_NOACTIVATE | WS_EX_TOOLWINDOW);

                return baseParams;
            }
        }

        protected override bool ShowWithoutActivation => true;
    }
}
