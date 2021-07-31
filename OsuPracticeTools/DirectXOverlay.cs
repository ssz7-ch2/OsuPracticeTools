using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using GameOverlay.Drawing;
using GameOverlay.Windows;
using System.Windows.Forms;
using Timer = System.Windows.Forms.Timer;
using System.Runtime.InteropServices;
using System.Diagnostics;

namespace OsuPracticeTools
{
	// copied and modified from https://github.com/michel-pi/GameOverlay.Net/blob/master/source/Examples/Example.cs
	public class DirectXOverlay : IDisposable
	{
		internal static DirectXOverlay Overlay;
		private static Timer _timer;
		private static int _elapsedMilliseconds = 0;

		private readonly GraphicsWindow _window;

		private SolidBrush _foreground;
		private SolidBrush _background;
		private Font _font;
		private readonly System.Drawing.Font _fontWidth = new("Segoe UI", 19.5f);
		private string _message;

		public DirectXOverlay(int left, int top, int width, int height, string message)
		{
			var gfx = new Graphics()
			{
				PerPrimitiveAntiAliasing = true,
				TextAntiAliasing = true
			};

			_message = message;

			_window = new GraphicsWindow(left, top, width, height, gfx)
			{
				FPS = 30,
				IsTopmost = true,
				IsVisible = true
			};

			_window.DestroyGraphics += _window_DestroyGraphics;
			_window.DrawGraphics += _window_DrawGraphics;
			_window.SetupGraphics += _window_SetupGraphics;
		}

		private void _window_SetupGraphics(object sender, SetupGraphicsEventArgs e)
		{
			var gfx = e.Graphics;

			_background = gfx.CreateSolidBrush(0, 0, 0, 200);
			_foreground = gfx.CreateSolidBrush(255, 255, 255, 240);
			_font = gfx.CreateFont("Segoe UI", 28);
		}

		private void _window_DestroyGraphics(object sender, DestroyGraphicsEventArgs e)
		{
			_background.Dispose();
			_foreground.Dispose();
			_font.Dispose();
		}

		private void _window_DrawGraphics(object sender, DrawGraphicsEventArgs e)
		{
			var gfx = e.Graphics;
			gfx.ClearScene();
			var size = TextRenderer.MeasureText(_message, _fontWidth);
			gfx.DrawTextWithBackground(_font, _foreground, _background, (_window.Width - size.Width) / 2, (_window.Height - size.Height) / 2, _message);

		}

		public void Run()
		{
			_window.Create();
			_window.Join();
		}

		public void Update(int left, int top, int width, int height, string message)
		{
			_window.Resize(width, height);
			_window.Move(left, top);
			_message = message;
		}

		public void Show()
		{
			if (!_window.IsVisible)
				_window.Show();
		}
		public void Hide()
		{
			if (_window.IsVisible)
				_window.Hide();
		}

		public bool IsRunning => _window.IsRunning;

		~DirectXOverlay()
		{
			Dispose(false);
		}

		#region IDisposable Support
		private bool disposedValue;

		protected virtual void Dispose(bool disposing)
		{
			if (!disposedValue)
			{
				_window.Dispose();

				disposedValue = true;
			}
		}

		public void Dispose()
		{
			Dispose(true);
			GC.SuppressFinalize(this);
		}
		#endregion

		public static void ShowMessage(string message)
		{
			var ptr = Program.OsuProcess.MainWindowHandle;
			var osuProcessRect = new Rect();
			GetWindowRect(ptr, ref osuProcessRect);
			if (Overlay is null)
			{
				Overlay = new DirectXOverlay(osuProcessRect.Left, osuProcessRect.Top, osuProcessRect.Right - osuProcessRect.Left, osuProcessRect.Bottom - osuProcessRect.Top, message);
			}

			if (!Overlay.IsRunning)
			{
				Thread thread = new(Overlay.Run);
				thread.Start();
			}
			else
			{
				Overlay.Update(osuProcessRect.Left, osuProcessRect.Top, osuProcessRect.Right - osuProcessRect.Left, osuProcessRect.Bottom - osuProcessRect.Top, message);
				Overlay.Show();
			}

			_timer?.Dispose();
			_timer = new Timer { Interval = 100 };
			_timer.Tick += CloseOverlay;
			_timer.Start();
			_elapsedMilliseconds = 0;
		}

		private static void CloseOverlay(object sender, EventArgs e)
		{
			_elapsedMilliseconds += _timer.Interval;
			if (_elapsedMilliseconds >= 1000)
			{
				Overlay.Hide();
				_elapsedMilliseconds = 0;
			}
		}

		[DllImport("user32.dll")]
		public static extern bool GetWindowRect(IntPtr hwnd, ref Rect rectangle);

		public struct Rect
		{
			public int Left { get; set; }
			public int Top { get; set; }
			public int Right { get; set; }
			public int Bottom { get; set; }
		}
	}
}
