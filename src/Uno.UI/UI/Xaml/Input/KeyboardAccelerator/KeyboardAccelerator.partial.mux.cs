// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// MUX Reference dxaml\xcp\dxaml\lib\KeyboardAccelerator_Partial.cpp, tag winui3/release/1.5.3, commit 685d2bf

using System.Collections.Generic;
using System.Globalization;
using System.Text.RegularExpressions;
using DirectUI;
using Windows.UI.Xaml.Controls;
using Windows.Foundation;
using Windows.System;

namespace Windows.UI.Xaml.Input;

partial class KeyboardAccelerator
{
	internal static bool RaiseKeyboardAcceleratorInvoked(
		KeyboardAccelerator pNativeAccelerator,
		DependencyObject pElement)
	{
		var spPeerDO = pNativeAccelerator;

		if (spPeerDO is null)
		{
			// There is no need to fire the event if there is no peer, since no one is listening
			return false;
		}

		var spElementPeerDO = pElement;

		KeyboardAccelerator spAccelerator = spPeerDO;

		KeyboardAcceleratorInvokedEventArgs spArgs = new(spElementPeerDO, pNativeAccelerator);
		pNativeAccelerator.Invoked?.Invoke(pNativeAccelerator, spArgs);
		var pIsHandled = spArgs.Handled;

		// If not handled, raise an event on parent element to give it a chance to handle the event.
		// This will enable controls which don't have automated invoked action, to handle the event. e.g Pivot
		if (!pIsHandled)
		{
			pIsHandled = UIElement.RaiseKeyboardAcceleratorInvokedStatic(pElement, spArgs);
		}

		return pIsHandled;
	}

	internal static string GetStringRepresentationForUIElement(UIElement uiElement)
	{
		// We don't want to bother doing anything if we've never actually set a keyboard accelerator,
		// so we'll just return null unless we have.
		// UNO TODO if (!uiElement.GetHandle().CheckOnDemandProperty(KnownPropertyIndex.UIElement_KeyboardAccelerators).IsNull())
		if (uiElement.KeyboardAccelerators.Count != 0)
		{
			IList<KeyboardAccelerator> keyboardAccelerators;
			int keyboardAcceleratorCount;

			keyboardAccelerators = uiElement.KeyboardAccelerators;
			keyboardAcceleratorCount = keyboardAccelerators.Count;

			if (keyboardAcceleratorCount > 0)
			{
				var keyboardAcceleratorStringRepresentation = keyboardAccelerators[0];
				return keyboardAcceleratorStringRepresentation.GetStringRepresentation();
			}
		}

		return null;
	}

	internal string GetStringRepresentation()
	{
		var key = Key;
		var modifiers = Modifiers;
		var stringRepresentationLocal = "";

		if ((modifiers & VirtualKeyModifiers.Control) != 0)
		{
			ConcatVirtualKey(VirtualKey.Control, ref stringRepresentationLocal);
		}

		if ((modifiers & VirtualKeyModifiers.Menu) != 0)
		{
			ConcatVirtualKey(VirtualKey.Menu, ref stringRepresentationLocal);
		}

		if ((modifiers & VirtualKeyModifiers.Windows) != 0)
		{
			ConcatVirtualKey(VirtualKey.LeftWindows, ref stringRepresentationLocal);
		}

		if ((modifiers & VirtualKeyModifiers.Shift) != 0)
		{
			ConcatVirtualKey(VirtualKey.Shift, ref stringRepresentationLocal);
		}

		ConcatVirtualKey(key, ref stringRepresentationLocal);

		return stringRepresentationLocal;
	}

	internal void ConcatVirtualKey(VirtualKey key, ref string keyboardAcceleratorString)
	{
		var keyName = DXamlCore.Current.GetLocalizedResourceString(GetResourceStringIdFromVirtualKey(key));

		if (string.IsNullOrEmpty(keyboardAcceleratorString))
		{
			// If this is the first key string we've accounted for, then
			// we can just set the keyboard accelerator string equal to it.
			keyboardAcceleratorString = keyName;
		}
		else
		{
			// Otherwise, if we already had an existing keyboard accelerator string,
			// then we'll use the formatting string to join strings together
			// to combine it with the new key string.
			string joiningFormatString = DXamlCore.Current.GetLocalizedResourceString("KEYBOARD_ACCELERATOR_TEXT_JOIN");

			// TODO Uno specific: Using C# style format string instead of C++ one.
			static string ConvertCppFormatStringToCSharp(string cppFormatString)
			{
				// This regex finds C++ style format specifiers like %s, %d, etc.
				Regex regex = CppFormatStringRegex();

				int index = 0;
				string csharpFormatString = regex.Replace(cppFormatString, match => $"{{{index++}}}");

				return csharpFormatString;
			}

			joiningFormatString = ConvertCppFormatStringToCSharp(joiningFormatString);

			keyboardAcceleratorString = string.Format(CultureInfo.InvariantCulture, joiningFormatString, keyboardAcceleratorString, keyName);
		}
	}

	[GeneratedRegex("%[sd]")]
	private static partial Regex CppFormatStringRegex();

	private static string GetResourceStringIdFromVirtualKey(VirtualKey key)
	{
		switch (key)
		{
			case VirtualKey.A: return "TEXT_VK_A";
			case VirtualKey.Accept: return "TEXT_VK_ACCEPT";
			case VirtualKey.Add: return "TEXT_VK_ADD";
			case VirtualKey.Application: return "TEXT_VK_APPLICATION";
			case VirtualKey.B: return "TEXT_VK_B";
			case VirtualKey.Back: return "TEXT_VK_BACK";
			case VirtualKey.C: return "TEXT_VK_C";
			case VirtualKey.Cancel: return "TEXT_VK_CANCEL";
			case VirtualKey.CapitalLock: return "TEXT_VK_CAPITALLOCK";
			case VirtualKey.Clear: return "TEXT_VK_CLEAR";
			case VirtualKey.Control: return "TEXT_VK_CONTROL";
			case VirtualKey.Convert: return "TEXT_VK_CONVERT";
			case VirtualKey.D: return "TEXT_VK_D";
			case VirtualKey.Decimal: return "TEXT_VK_DECIMAL";
			case VirtualKey.Delete: return "TEXT_VK_DELETE";
			case VirtualKey.Divide: return "TEXT_VK_DIVIDE";
			case VirtualKey.Down: return "TEXT_VK_DOWN";
			case VirtualKey.E: return "TEXT_VK_E";
			case VirtualKey.End: return "TEXT_VK_END";
			case VirtualKey.Enter: return "TEXT_VK_ENTER";
			case VirtualKey.Escape: return "TEXT_VK_ESCAPE";
			case VirtualKey.Execute: return "TEXT_VK_EXECUTE";
			case VirtualKey.F: return "TEXT_VK_F";
			case VirtualKey.F1: return "TEXT_VK_F1";
			case VirtualKey.F10: return "TEXT_VK_F10";
			case VirtualKey.F11: return "TEXT_VK_F11";
			case VirtualKey.F12: return "TEXT_VK_F12";
			case VirtualKey.F13: return "TEXT_VK_F13";
			case VirtualKey.F14: return "TEXT_VK_F14";
			case VirtualKey.F15: return "TEXT_VK_F15";
			case VirtualKey.F16: return "TEXT_VK_F16";
			case VirtualKey.F17: return "TEXT_VK_F17";
			case VirtualKey.F18: return "TEXT_VK_F18";
			case VirtualKey.F19: return "TEXT_VK_F19";
			case VirtualKey.F2: return "TEXT_VK_F2";
			case VirtualKey.F20: return "TEXT_VK_F20";
			case VirtualKey.F21: return "TEXT_VK_F21";
			case VirtualKey.F22: return "TEXT_VK_F22";
			case VirtualKey.F23: return "TEXT_VK_F23";
			case VirtualKey.F24: return "TEXT_VK_F24";
			case VirtualKey.F3: return "TEXT_VK_F3";
			case VirtualKey.F4: return "TEXT_VK_F4";
			case VirtualKey.F5: return "TEXT_VK_F5";
			case VirtualKey.F6: return "TEXT_VK_F6";
			case VirtualKey.F7: return "TEXT_VK_F7";
			case VirtualKey.F8: return "TEXT_VK_F8";
			case VirtualKey.F9: return "TEXT_VK_F9";
			case VirtualKey.Favorites: return "TEXT_VK_FAVORITES";
			case VirtualKey.Final: return "TEXT_VK_FINAL";
			case VirtualKey.G: return "TEXT_VK_G";
			case VirtualKey.GamepadA: return "TEXT_VK_GAMEPADA";
			case VirtualKey.GamepadB: return "TEXT_VK_GAMEPADB";
			case VirtualKey.GamepadDPadDown: return "TEXT_VK_GAMEPADDPADDOWN";
			case VirtualKey.GamepadDPadLeft: return "TEXT_VK_GAMEPADDPADLEFT";
			case VirtualKey.GamepadDPadRight: return "TEXT_VK_GAMEPADDPADRIGHT";
			case VirtualKey.GamepadDPadUp: return "TEXT_VK_GAMEPADDPADUP";
			case VirtualKey.GamepadLeftShoulder: return "TEXT_VK_GAMEPADLEFTSHOULDER";
			case VirtualKey.GamepadLeftThumbstickButton: return "TEXT_VK_GAMEPADLEFTTHUMBSTICKBUTTON";
			case VirtualKey.GamepadLeftThumbstickDown: return "TEXT_VK_GAMEPADLEFTTHUMBSTICKDOWN";
			case VirtualKey.GamepadLeftThumbstickLeft: return "TEXT_VK_GAMEPADLEFTTHUMBSTICKLEFT";
			case VirtualKey.GamepadLeftThumbstickRight: return "TEXT_VK_GAMEPADLEFTTHUMBSTICKRIGHT";
			case VirtualKey.GamepadLeftThumbstickUp: return "TEXT_VK_GAMEPADLEFTTHUMBSTICKUP";
			case VirtualKey.GamepadLeftTrigger: return "TEXT_VK_GAMEPADLEFTTRIGGER";
			case VirtualKey.GamepadMenu: return "TEXT_VK_GAMEPADMENU";
			case VirtualKey.GamepadRightShoulder: return "TEXT_VK_GAMEPADRIGHTSHOULDER";
			case VirtualKey.GamepadRightThumbstickButton: return "TEXT_VK_GAMEPADRIGHTTHUMBSTICKBUTTON";
			case VirtualKey.GamepadRightThumbstickDown: return "TEXT_VK_GAMEPADRIGHTTHUMBSTICKDOWN";
			case VirtualKey.GamepadRightThumbstickLeft: return "TEXT_VK_GAMEPADRIGHTTHUMBSTICKLEFT";
			case VirtualKey.GamepadRightThumbstickRight: return "TEXT_VK_GAMEPADRIGHTTHUMBSTICKRIGHT";
			case VirtualKey.GamepadRightThumbstickUp: return "TEXT_VK_GAMEPADRIGHTTHUMBSTICKUP";
			case VirtualKey.GamepadRightTrigger: return "TEXT_VK_GAMEPADRIGHTTRIGGER";
			case VirtualKey.GamepadView: return "TEXT_VK_GAMEPADVIEW";
			case VirtualKey.GamepadX: return "TEXT_VK_GAMEPADX";
			case VirtualKey.GamepadY: return "TEXT_VK_GAMEPADY";
			case VirtualKey.GoBack: return "TEXT_VK_GOBACK";
			case VirtualKey.GoForward: return "TEXT_VK_GOFORWARD";
			case VirtualKey.GoHome: return "TEXT_VK_GOHOME";
			case VirtualKey.H: return "TEXT_VK_H";
			case VirtualKey.Hangul: return "TEXT_VK_HANGUL";
			case VirtualKey.Hanja: return "TEXT_VK_HANJA";
			case VirtualKey.Help: return "TEXT_VK_HELP";
			case VirtualKey.Home: return "TEXT_VK_HOME";
			case VirtualKey.I: return "TEXT_VK_I";
			case VirtualKey.ImeOn: return "TEXT_VK_IMEON";
			case VirtualKey.ImeOff: return "TEXT_VK_IMEOFF";
			case VirtualKey.Insert: return "TEXT_VK_INSERT";
			case VirtualKey.J: return "TEXT_VK_J";
			case VirtualKey.Junja: return "TEXT_VK_JUNJA";
			case VirtualKey.K: return "TEXT_VK_K";
			case VirtualKey.L: return "TEXT_VK_L";
			case VirtualKey.Left: return "TEXT_VK_LEFT";
			case VirtualKey.LeftButton: return "TEXT_VK_LEFTBUTTON";
			case VirtualKey.LeftControl: return "TEXT_VK_CONTROL";
			case VirtualKey.LeftMenu: return "TEXT_VK_MENU";
			case VirtualKey.LeftShift: return "TEXT_VK_SHIFT";
			case VirtualKey.LeftWindows: return "TEXT_VK_WINDOWS";
			case VirtualKey.M: return "TEXT_VK_M";
			case VirtualKey.Menu: return "TEXT_VK_MENU";
			case VirtualKey.MiddleButton: return "TEXT_VK_MIDDLEBUTTON";
			case VirtualKey.ModeChange: return "TEXT_VK_MODECHANGE";
			case VirtualKey.Multiply: return "TEXT_VK_MULTIPLY";
			case VirtualKey.N: return "TEXT_VK_N";
			case VirtualKey.NavigationAccept: return "TEXT_VK_NAVIGATIONACCEPT";
			case VirtualKey.NavigationCancel: return "TEXT_VK_NAVIGATIONCANCEL";
			case VirtualKey.NavigationDown: return "TEXT_VK_NAVIGATIONDOWN";
			case VirtualKey.NavigationLeft: return "TEXT_VK_NAVIGATIONLEFT";
			case VirtualKey.NavigationMenu: return "TEXT_VK_NAVIGATIONMENU";
			case VirtualKey.NavigationRight: return "TEXT_VK_NAVIGATIONRIGHT";
			case VirtualKey.NavigationUp: return "TEXT_VK_NAVIGATIONUP";
			case VirtualKey.NavigationView: return "TEXT_VK_NAVIGATIONVIEW";
			case VirtualKey.NonConvert: return "TEXT_VK_NONCONVERT";
			case VirtualKey.None: return "TEXT_VK_NONE";
			case VirtualKey.Number0: return "TEXT_VK_NUMBER0";
			case VirtualKey.Number1: return "TEXT_VK_NUMBER1";
			case VirtualKey.Number2: return "TEXT_VK_NUMBER2";
			case VirtualKey.Number3: return "TEXT_VK_NUMBER3";
			case VirtualKey.Number4: return "TEXT_VK_NUMBER4";
			case VirtualKey.Number5: return "TEXT_VK_NUMBER5";
			case VirtualKey.Number6: return "TEXT_VK_NUMBER6";
			case VirtualKey.Number7: return "TEXT_VK_NUMBER7";
			case VirtualKey.Number8: return "TEXT_VK_NUMBER8";
			case VirtualKey.Number9: return "TEXT_VK_NUMBER9";
			case VirtualKey.NumberKeyLock: return "TEXT_VK_NUMBERKEYLOCK";
			case VirtualKey.NumberPad0: return "TEXT_VK_NUMBERPAD0";
			case VirtualKey.NumberPad1: return "TEXT_VK_NUMBERPAD1";
			case VirtualKey.NumberPad2: return "TEXT_VK_NUMBERPAD2";
			case VirtualKey.NumberPad3: return "TEXT_VK_NUMBERPAD3";
			case VirtualKey.NumberPad4: return "TEXT_VK_NUMBERPAD4";
			case VirtualKey.NumberPad5: return "TEXT_VK_NUMBERPAD5";
			case VirtualKey.NumberPad6: return "TEXT_VK_NUMBERPAD6";
			case VirtualKey.NumberPad7: return "TEXT_VK_NUMBERPAD7";
			case VirtualKey.NumberPad8: return "TEXT_VK_NUMBERPAD8";
			case VirtualKey.NumberPad9: return "TEXT_VK_NUMBERPAD9";
			case VirtualKey.O: return "TEXT_VK_O";
			case VirtualKey.P: return "TEXT_VK_P";
			case VirtualKey.PageDown: return "TEXT_VK_PAGEDOWN";
			case VirtualKey.PageUp: return "TEXT_VK_PAGEUP";
			case VirtualKey.Pause: return "TEXT_VK_PAUSE";
			case VirtualKey.Print: return "TEXT_VK_PRINT";
			case VirtualKey.Q: return "TEXT_VK_Q";
			case VirtualKey.R: return "TEXT_VK_R";
			case VirtualKey.Refresh: return "TEXT_VK_REFRESH";
			case VirtualKey.Right: return "TEXT_VK_RIGHT";
			case VirtualKey.RightButton: return "TEXT_VK_RIGHTBUTTON";
			case VirtualKey.RightControl: return "TEXT_VK_CONTROL";
			case VirtualKey.RightMenu: return "TEXT_VK_MENU";
			case VirtualKey.RightShift: return "TEXT_VK_SHIFT";
			case VirtualKey.RightWindows: return "TEXT_VK_WINDOWS";
			case VirtualKey.S: return "TEXT_VK_S";
			case VirtualKey.Scroll: return "TEXT_VK_SCROLL";
			case VirtualKey.Search: return "TEXT_VK_SEARCH";
			case VirtualKey.Select: return "TEXT_VK_SELECT";
			case VirtualKey.Separator: return "TEXT_VK_SEPARATOR";
			case VirtualKey.Shift: return "TEXT_VK_SHIFT";
			case VirtualKey.Sleep: return "TEXT_VK_SLEEP";
			case VirtualKey.Snapshot: return "TEXT_VK_SNAPSHOT";
			case VirtualKey.Space: return "TEXT_VK_SPACE";
			case VirtualKey.Stop: return "TEXT_VK_STOP";
			case VirtualKey.Subtract: return "TEXT_VK_SUBTRACT";
			case VirtualKey.T: return "TEXT_VK_T";
			case VirtualKey.Tab: return "TEXT_VK_TAB";
			case VirtualKey.U: return "TEXT_VK_U";
			case VirtualKey.Up: return "TEXT_VK_UP";
			case VirtualKey.V: return "TEXT_VK_V";
			case VirtualKey.W: return "TEXT_VK_W";
			case VirtualKey.X: return "TEXT_VK_X";
			case VirtualKey.XButton1: return "TEXT_VK_XBUTTON1";
			case VirtualKey.XButton2: return "TEXT_VK_XBUTTON2";
			case VirtualKey.Y: return "TEXT_VK_Y";
			case VirtualKey.Z: return "TEXT_VK_Z";
			default: return "TEXT_VK_NONE";
		}
	}

	internal static void SetToolTip(
		KeyboardAccelerator pNativeAccelerator,
		DependencyObject pParentElement)
	{
		//Retrive the string representing accelerator defined on element.
		string stringRepresentation = pNativeAccelerator.GetStringRepresentation();

		var textValue = PropertyValue.CreateString(stringRepresentation);

		var pToolTipTargetDO = (DependencyObject)pParentElement.GetValue(UIElement.KeyboardAcceleratorPlacementTargetProperty);

		if (pToolTipTargetDO is not null)
		{
			pParentElement = pToolTipTargetDO;
		}
		var spParentDO = pParentElement;

		ToolTipService.SetKeyboardAcceleratorToolTip(spParentDO, textValue);
	}
}
