using System;
using System.Drawing;
using System.Windows.Forms;

namespace OsuPracticeTools
{
    public class SystemTray : ApplicationContext
    {
        private readonly NotifyIcon _trayIcon;

        public SystemTray()
        {
            var contextMenuStrip = new ContextMenuStrip();
            contextMenuStrip.Items.Add(new ToolStripMenuItem("Reload Hotkeys", null, Program.ReloadHotkeys));
            contextMenuStrip.Items.Add(new ToolStripMenuItem("Exit", null, Exit));


            _trayIcon = new NotifyIcon()
            {
                Icon = Icon.ExtractAssociatedIcon(Application.ExecutablePath),
                ContextMenuStrip = contextMenuStrip,
                Visible = true
            };
        }
        private void Exit(object sender, EventArgs e)
        {
            _trayIcon.Visible = false;

            Application.Exit();
        }
    }
}