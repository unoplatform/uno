// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

// MUX Reference controls/test/testinfra/MUXTestInfra/Common/KeyboardHelper.cs, commit fee1e8acc

using Common;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

using Microsoft.Windows.Apps.Test.Automation;
using Microsoft.Windows.Apps.Test.Foundation;
using Microsoft.Windows.Apps.Test.Foundation.Controls;
using Microsoft.Windows.Apps.Test.Foundation.Patterns;
using Microsoft.Windows.Apps.Test.Foundation.Waiters;

namespace Microsoft.UI.Xaml.Tests.MUXControls.InteractionTests.Common
{
	public enum Key
	{
		Enter,
		Escape,
		Tab,
		Up,
		Down,
		Left,
		Right,
		PageUp,
		PageDown,
		Home,
		End,
		Space,
		Backspace,
		F10,
		F4,
		F6,
		F2,
		R,
		a,
		c,
		j,
		m,
		n,
		r,
		s,
		t,
		v,
		x,
	}

	[Flags]
	public enum ModifierKey
	{
		None = 0,
		Shift = 1,
		Control = 2,
		Alt = 4,
		Windows = 8
	}

	public class KeyboardHelper
	{
		private static Dictionary<Key, string> keyToKeyStringDictionary = new Dictionary<Key, string>()
		{
			{ Key.Enter, "{ENTER}" },
			{ Key.Escape, "{ESC}" },
			{ Key.Tab, "{TAB}" },
			{ Key.Up, "{UP}" },
			{ Key.Down, "{DOWN}" },
			{ Key.Left, "{LEFT}" },
			{ Key.Right, "{RIGHT}" },
			{ Key.PageUp, "{PGUP}" },
			{ Key.PageDown, "{PGDN}" },
			{ Key.Home, "{HOME}" },
			{ Key.End, "{END}" },
			{ Key.Space, "{SPACE}" },
			{ Key.Backspace, "{BACKSPACE}" },
			{ Key.F10, "{F10}" },
			{ Key.F4, "{F4}" },
			{ Key.F6, "{F6}" },
			{ Key.F2, "{F2}" },
			{ Key.R, "{R}" },
			{ Key.a, "{a}" },
			{ Key.c, "{c}" },
			{ Key.j, "{j}" },
			{ Key.m, "{m}" },
			{ Key.n, "{n}" },
			{ Key.r, "{r}" },
			{ Key.s, "{s}" },
			{ Key.t, "{t}" },
			{ Key.v, "{v}" },
			{ Key.x, "{x}" },
		};

		private static string ApplyModifierKey(string keyStrokes, ModifierKey key)
		{
			string returnString = keyStrokes;
			if ((key & ModifierKey.Alt) == ModifierKey.Alt)
			{
				string formatString = "{{ALT DOWN}}{0}{{ALT UP}}";
				returnString = string.Format(formatString, returnString);
			}

			if ((key & ModifierKey.Shift) == ModifierKey.Shift)
			{
				string formatString = "{{SHIFT DOWN}}{0}{{SHIFT UP}}";
				returnString = string.Format(formatString, returnString);
			}

			if ((key & ModifierKey.Control) == ModifierKey.Control)
			{
				string formatString = "{{CONTROL DOWN}}{0}{{CONTROL UP}}";
				returnString = string.Format(formatString, returnString);
			}

			if ((key & ModifierKey.Windows) == ModifierKey.Windows)
			{
				string formatString = "{{WIN DOWN}}{0}{{WIN UP}}";
				returnString = string.Format(formatString, returnString);
			}

			return returnString;
		}

		public static async Task PressKey(Key key, ModifierKey modifierKey = ModifierKey.None, uint numPresses = 1, bool skipWait = false)
		{
			string keystrokes = string.Empty;

			for (int i = 0; i < numPresses; i++)
			{
				keystrokes += keyToKeyStringDictionary[key];
			}

			keystrokes = ApplyModifierKey(keystrokes, modifierKey);

			Log.Comment("Send text '{0}'.", keystrokes);
			TextInput.SendText(keystrokes);
			if (!skipWait)
			{
				await Wait.ForIdle();
			}
		}

		public static async Task PressKeySequence(Key[] keys)
		{
			string keystrokes = string.Empty;

			foreach (var key in keys)
			{
				keystrokes += keyToKeyStringDictionary[key];
			}

			Log.Comment("Send text '{0}'.", keystrokes);
			TextInput.SendText(keystrokes);
			await Wait.ForIdle();
		}

		public static string GetPressDownModifierKeyStroke(ModifierKey modifierKey)
		{
			string keystrokes = string.Empty;

			if ((modifierKey & ModifierKey.Alt) == ModifierKey.Alt)
			{
				keystrokes += "{ALT DOWN}";
			}

			if ((modifierKey & ModifierKey.Shift) == ModifierKey.Shift)
			{
				keystrokes += "{SHIFT DOWN}";
			}

			if ((modifierKey & ModifierKey.Control) == ModifierKey.Control)
			{
				keystrokes += "{CONTROL DOWN}";
			}

			if ((modifierKey & ModifierKey.Windows) == ModifierKey.Windows)
			{
				keystrokes += "{WIN DOWN}";
			}
			return keystrokes;
		}

		public static async Task PressDownModifierKey(ModifierKey modifierKey, bool silent = false, bool waitForIdle = true)
		{
			var keystrokes = GetPressDownModifierKeyStroke(modifierKey);
			if (keystrokes != string.Empty)
			{
				if (!silent)
				{
					Log.Comment("Send text '{0}'.", keystrokes);
				}
				TextInput.SendText(keystrokes);
				if (waitForIdle)
				{
					await Wait.ForIdle();
				}
			}
		}

		public static string GetReleaseModifierKeyStroke(ModifierKey modifierKey)
		{
			string keystrokes = string.Empty;

			if ((modifierKey & ModifierKey.Alt) == ModifierKey.Alt)
			{
				keystrokes += "{ALT UP}";
			}

			if ((modifierKey & ModifierKey.Shift) == ModifierKey.Shift)
			{
				keystrokes += "{SHIFT UP}";
			}

			if ((modifierKey & ModifierKey.Control) == ModifierKey.Control)
			{
				keystrokes += "{CONTROL UP}";
			}

			if ((modifierKey & ModifierKey.Windows) == ModifierKey.Windows)
			{
				keystrokes += "{WIN UP}";
			}
			return keystrokes;
		}

		public static async Task ReleaseModifierKey(ModifierKey modifierKey, bool silent = false, bool waitForIdle = true)
		{
			var keystrokes = GetReleaseModifierKeyStroke(modifierKey);
			if (keystrokes != string.Empty)
			{
				if (!silent)
				{
					Log.Comment("Send text '{0}'.", keystrokes);
				}
				TextInput.SendText(keystrokes);
				if (waitForIdle)
				{
					await Wait.ForIdle();
				}
			}
		}

		public static async Task PressKey(UIObject obj, Key key, ModifierKey modifierKey = ModifierKey.None, uint numPresses = 1, bool useDebugMode = false)
		{
			using (var waiter = GetWaiterForKeyEvent(obj, key))
			{
				if (useDebugMode)
				{
					Log.Comment("PressKey uses waiter: {0}.", waiter != null);
				}

				if (waiter != null)
				{
					for (int i = 0; i < numPresses; i++)
					{
						string keystrokes = ApplyModifierKey(keyToKeyStringDictionary[key], modifierKey);

						Log.Comment("Send text '{0}'.", keystrokes);
						obj.SendKeys(keystrokes);

						await waiter.Wait();
						waiter.Reset();
						await Wait.ForIdle();
					}
				}
				else
				{
					string keystrokes = string.Empty;

					for (int i = 0; i < numPresses; i++)
					{
						keystrokes += keyToKeyStringDictionary[key];
					}

					keystrokes = ApplyModifierKey(keystrokes, modifierKey);

					Log.Comment("Send text '{0}'.", keystrokes);

					try
					{
						obj.SendKeys(keystrokes);
					}
					catch (Exception e)
					{
						if (useDebugMode)
						{
							Log.Comment("UIObject.SendKey threw exception: {0}", e.ToString());
							Log.Comment("Send text (2)."); // Attempting to send text again to check if it succeeds this time.
							obj.SendKeys(keystrokes);
							Log.Comment("Text sent (2).");
						}

						throw;
					}

					if (useDebugMode)
					{
						Log.Comment("Text sent.");
					}

					await Wait.ForIdle();
				}
			}

			if (useDebugMode)
			{
				Log.Comment("Exiting PressKey.");
			}
		}

		public static async Task HoldKeyMilliSeconds(Key key, uint milliseconds, ModifierKey modifierKey = ModifierKey.None, bool useDebugMode = false)
		{
			if (useDebugMode)
			{
				Log.Comment("Holding down key: {0} for {1} milliseconds.", key, milliseconds);
			}
			// Remove starting and ending curly bracket.
			var cleanedName = keyToKeyStringDictionary[key].Substring(1).Replace("}", "");

			string beginKeyStroke = GetPressDownModifierKeyStroke(modifierKey) + "{" + cleanedName + " DOWN}";
			string endKeyStroke = "{" + cleanedName + " UP}" + GetReleaseModifierKeyStroke(modifierKey);

			TextInput.SendText(beginKeyStroke);

			await Wait.ForMilliseconds(milliseconds);

			TextInput.SendText(endKeyStroke);

			if (useDebugMode)
			{
				Log.Comment($"Pressed key for {milliseconds} milliseconds.");
			}

			await Wait.ForIdle();

			if (useDebugMode)
			{
				Log.Comment("Exiting HoldKeyForMilliSeconds.");
			}
		}

		public static async Task EnterText(Edit edit, string s, bool useKeyboard = false)
		{
			Log.Comment("Enter text '{0}' into the '{1}' text box.", s, string.IsNullOrWhiteSpace(edit.AutomationId) ? edit.Name : edit.AutomationId);
			await FocusHelper.SetFocus(edit);

			using (var waiter = new ValueChangedEventWaiter(edit, s))
			{
				if (useKeyboard)
				{
					edit.SendKeys(s);
				}
				else
				{
					edit.SetValue(s);
				}

				await waiter.Wait();
			}

			await Wait.ForIdle();
		}

		private static Waiter GetWaiterForKeyEvent(UIObject obj, Key key)
		{
			Waiter waiter = null;

			if (obj is Button)
			{
				var button = obj as Button;
				if (key == Key.Enter)
				{
					waiter = button.GetInvokedWaiter();
				}
			}
			else if (obj is ComboBox)
			{
				var comboBox = obj as ComboBox;
				if (key == Key.Enter)
				{
					waiter = (comboBox.ExpandCollapseState == ExpandCollapseState.Collapsed ? comboBox.GetExpandedWaiter() : null);
				}
				else if (key == Key.Escape)
				{
					waiter = (comboBox.ExpandCollapseState == ExpandCollapseState.Expanded ? comboBox.GetCollapsedWaiter() : null);
				}
			}
#if COLORPICKER_INCLUDED
			else if (obj is ColorSpectrum)
			{
				var colorSpectrum = obj as ColorSpectrum;
				if (key == Key.Left || key == Key.Right || key == Key.Up || key == Key.Down)
				{
					waiter = colorSpectrum.GetColorChangedWaiter();
				}
			}
			else if (obj is ColorPickerSlider)
			{
				var colorPickerSlider = obj as ColorPickerSlider;
				if (key == Key.Left || key == Key.Right)
				{
					waiter = colorPickerSlider.GetColorChangedWaiter();
				}
			}
#endif

			return waiter;
		}
	}
}
