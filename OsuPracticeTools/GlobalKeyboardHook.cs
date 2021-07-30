using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace OsuPracticeTools
{
	// Copied and modified from https://www.codeproject.com/Articles/19004/A-Simple-C-Global-Low-Level-Keyboard-Hook
	public static class GlobalKeyboardHook
	{
		public delegate int KeyboardHookProc(int code, int wParam, ref KeyboardHookStruct lParam);

		public struct KeyboardHookStruct
		{
			public int vkCode;
			public int scanCode;
			public int flags;
			public int time;
			public int dwExtraInfo;
		}

		const int WH_KEYBOARD_LL = 13;
		const int WM_KEYDOWN = 0x100;
		const int WM_KEYUP = 0x101;
		const int WM_SYSKEYDOWN = 0x104;
		const int WM_SYSKEYUP = 0x105;

		private const int VK_SHIFT = 0x10;
		private const int VK_CONTROL = 0x11;
		private const int VK_MENU = 0x12;

		public static List<List<Keys>> HookedUpKeys { get; set; }= new();
		public static List<List<Keys>> HookedDownKeys { get; set; } = new();
		public static List<Keys> PressedKeys { get; set; } = new();

		private static bool _enabled = true;
		private static IntPtr _hookId = IntPtr.Zero;
		private static readonly KeyboardHookProc HookProcDelegate = HookProc;

		public static event EventHandler<List<Keys>> KeyDown;
		public static event EventHandler<List<Keys>> KeyUp;

		public static bool IsHookSetup { get; set; }

		public static void Hook()
		{
			if (!IsHookSetup)
			{
				var hInstance = LoadLibrary("User32");
				_hookId = SetWindowsHookEx(WH_KEYBOARD_LL, HookProcDelegate, hInstance, 0);
				IsHookSetup = true;
			}
		}

		public static void Unhook()
		{
			if (IsHookSetup)
			{
				UnhookWindowsHookEx(_hookId);
				IsHookSetup = false;
			}
		}

		public static int HookProc(int code, int wParam, ref KeyboardHookStruct lParam)
		{
			if (code >= 0)
			{
				var handled = false;
				var key = (Keys)lParam.vkCode;

				if (wParam is WM_KEYDOWN or WM_SYSKEYDOWN)
				{
					if (IsModifierDown(Keys.Alt))
					{
						if (!(key is >= Keys.ShiftKey and <= Keys.Menu || key is >= Keys.LShiftKey and <= Keys.RMenu ||
							  key is Keys.Control or Keys.Shift or Keys.Alt))
						{
							var modifiers = Keys.None;
							if (IsModifierDown(Keys.Control))
								modifiers |= Keys.Control;
							if (IsModifierDown(Keys.Shift))
								modifiers |= Keys.Shift;

							if (!PressedKeys.Contains(key))
								PressedKeys.Add(key);
							var keysWithModifiers = PressedKeys.ConvertAll(k => k | modifiers);
							if (KeyDown != null && HookedDownKeys.Any(keysWithModifiers.SequenceEqual))
							{
								KeyDown(null, keysWithModifiers);
							}

							if (PressedKeys.Count > 0 && HookedDownKeys.Any(k => k[0] == PressedKeys[0]))
								handled = true;
						}
					}

					_enabled = true;
				}
				else if (wParam is WM_KEYUP or WM_SYSKEYUP)
				{
					if (IsModifierDown(Keys.Alt))
					{
						if (!(key is >= Keys.ShiftKey and <= Keys.Menu || key is >= Keys.LShiftKey and <= Keys.RMenu ||
							  key is Keys.Control or Keys.Shift or Keys.Alt))
						{
							var modifiers = Keys.None;
							if (IsModifierDown(Keys.Control))
								modifiers |= Keys.Control;
							if (IsModifierDown(Keys.Shift))
								modifiers |= Keys.Shift;

							var keysWithModifiers = PressedKeys.ConvertAll(k => k | modifiers);
							if (_enabled && KeyUp != null && HookedUpKeys.Any(keysWithModifiers.SequenceEqual))
							{
								_enabled = false;
								KeyUp(null, keysWithModifiers);
							}

							if (PressedKeys.Count > 0 && HookedDownKeys.Any(k => k[0] == PressedKeys[0]))
								handled = true;
						}

						PressedKeys.Remove(key);
					}
					else
					{
						PressedKeys.Clear();
					}

					_enabled = false;
				}

				if (handled)
					return 1;
			}

			return CallNextHookEx(_hookId, code, wParam, ref lParam);
		}

		private static bool IsModifierDown(Keys modifier)
		{
			return modifier switch
			{
				Keys.Control => (GetKeyState(VK_CONTROL) & 0x8000) != 0,
				Keys.Shift => (GetKeyState(VK_SHIFT) & 0x8000) != 0,
				Keys.Alt => (GetKeyState(VK_MENU) & 0x8000) != 0,
				_ => false
			};
		}

		[DllImport("user32.dll")]
		private static extern IntPtr SetWindowsHookEx(int idHook, KeyboardHookProc callback, IntPtr hInstance, uint threadId);
		[DllImport("user32.dll")]
		private static extern bool UnhookWindowsHookEx(IntPtr hInstance);
		[DllImport("user32.dll")]
		private static extern int CallNextHookEx(IntPtr idHook, int nCode, int wParam, ref KeyboardHookStruct lParam);
		[DllImport("kernel32.dll")]
		private static extern IntPtr LoadLibrary(string lpFileName);
		[DllImport("user32.dll", CharSet = CharSet.Auto, ExactSpelling = true, CallingConvention = CallingConvention.Winapi)]
		private static extern short GetKeyState(int keyCode);
	}
}