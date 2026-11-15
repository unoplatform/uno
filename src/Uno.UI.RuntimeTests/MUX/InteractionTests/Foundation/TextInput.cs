#nullable enable

using System;
using System.Collections.Generic;
using System.Globalization;
using Windows.System;
using Windows.UI.Input.Preview.Injection;

namespace Microsoft.Windows.Apps.Test.Foundation;

/// <summary>
/// Injects keystrokes written in MITA's SendKeys syntax: <c>{ENTER}</c>, <c>{SHIFT DOWN}</c>, <c>{a 3}</c>,
/// the <c>+</c> <c>^</c> <c>%</c> modifier prefixes (with <c>(...)</c> groups), <c>~</c> for Enter, and literal text.
/// </summary>
public static class TextInput
{
	private static readonly HashSet<VirtualKey> _heldKeys = new();

	private static readonly Dictionary<string, VirtualKey> _namedKeys = new(StringComparer.OrdinalIgnoreCase)
	{
		["ENTER"] = VirtualKey.Enter,
		["ESC"] = VirtualKey.Escape,
		["ESCAPE"] = VirtualKey.Escape,
		["TAB"] = VirtualKey.Tab,
		["UP"] = VirtualKey.Up,
		["DOWN"] = VirtualKey.Down,
		["LEFT"] = VirtualKey.Left,
		["RIGHT"] = VirtualKey.Right,
		["PGUP"] = VirtualKey.PageUp,
		["PGDN"] = VirtualKey.PageDown,
		["HOME"] = VirtualKey.Home,
		["END"] = VirtualKey.End,
		["SPACE"] = VirtualKey.Space,
		["BACKSPACE"] = VirtualKey.Back,
		["BS"] = VirtualKey.Back,
		["BKSP"] = VirtualKey.Back,
		["DELETE"] = VirtualKey.Delete,
		["DEL"] = VirtualKey.Delete,
		["INSERT"] = VirtualKey.Insert,
		["INS"] = VirtualKey.Insert,
		["APPS"] = VirtualKey.Application,
		["SHIFT"] = VirtualKey.Shift,
		["CONTROL"] = VirtualKey.Control,
		["CTRL"] = VirtualKey.Control,
		["ALT"] = VirtualKey.Menu,
		["MENU"] = VirtualKey.Menu,
		["WIN"] = VirtualKey.LeftWindows,
		["LWIN"] = VirtualKey.LeftWindows,
		["RWIN"] = VirtualKey.RightWindows,
		["GAMEPADA"] = VirtualKey.GamepadA,
		["GAMEPADB"] = VirtualKey.GamepadB,
		["GAMEPADDPADUP"] = VirtualKey.GamepadDPadUp,
		["GAMEPADDPADDOWN"] = VirtualKey.GamepadDPadDown,
		["GAMEPADDPADLEFT"] = VirtualKey.GamepadDPadLeft,
		["GAMEPADDPADRIGHT"] = VirtualKey.GamepadDPadRight,
	};

	public static void SendText(string text)
	{
		var keys = ToKeys(text);
		if (keys.Count > 0)
		{
			InteractionInput.InjectKeys(keys);
		}
	}

	internal static List<InjectedInputKeyboardInfo> ToKeys(string text)
	{
		List<InjectedInputKeyboardInfo> keys = new();
		var index = 0;
		Parse(text, ref index, keys, stopAtGroupEnd: false);
		return keys;
	}

	internal static void ReleaseHeldKeys()
	{
		if (_heldKeys.Count == 0)
		{
			return;
		}

		List<InjectedInputKeyboardInfo> keys = new();
		foreach (var key in _heldKeys)
		{
			keys.Add(KeyUp(key));
		}

		_heldKeys.Clear();
		InteractionInput.InjectKeys(keys);
	}

	private static void Parse(string text, ref int index, List<InjectedInputKeyboardInfo> keys, bool stopAtGroupEnd)
	{
		while (index < text.Length)
		{
			var c = text[index];
			switch (c)
			{
				case ')' when stopAtGroupEnd:
					index++;
					return;
				case '{':
					ParseBraced(text, ref index, keys);
					break;
				case '~':
					index++;
					Press(VirtualKey.Enter, keys);
					break;
				case '+' or '^' or '%':
					index++;
					ParseModified(ModifierFor(c), text, ref index, keys);
					break;
				default:
					index++;
					PressCharacter(c, keys);
					break;
			}
		}
	}

	private static void ParseModified(VirtualKey modifier, string text, ref int index, List<InjectedInputKeyboardInfo> keys)
	{
		keys.Add(KeyDown(modifier));
		if (index < text.Length && text[index] == '(')
		{
			index++;
			Parse(text, ref index, keys, stopAtGroupEnd: true);
		}
		else if (index < text.Length)
		{
			var single = ReadSingle(text, ref index);
			var singleIndex = 0;
			Parse(single, ref singleIndex, keys, stopAtGroupEnd: false);
		}

		keys.Add(KeyUp(modifier));
	}

	private static string ReadSingle(string text, ref int index)
	{
		if (text[index] is '+' or '^' or '%')
		{
			var start = index++;
			_ = ReadSingle(text, ref index);
			return text[start..index];
		}

		if (text[index] == '{')
		{
			var close = text.IndexOf('}', index + 2);
			var end = close < 0 ? text.Length : close + 1;
			var token = text[index..end];
			index = end;
			return token;
		}

		return text[index++].ToString();
	}

	private static void ParseBraced(string text, ref int index, List<InjectedInputKeyboardInfo> keys)
	{
		// "{}}" and "{{}" escape the braces themselves, so look for the closing brace past the first character.
		var close = text.IndexOf('}', index + 2);
		if (close < 0)
		{
			throw new FormatException($"Unterminated '{{' in key sequence '{text}'.");
		}

		var token = text.Substring(index + 1, close - index - 1);
		index = close + 1;

		var parts = token.Split(' ', 2, StringSplitOptions.RemoveEmptyEntries);
		var name = parts.Length > 0 ? parts[0] : token;
		var argument = parts.Length > 1 ? parts[1] : null;

		if (argument is not null && argument.Equals("DOWN", StringComparison.OrdinalIgnoreCase))
		{
			var key = ResolveKey(name, out _);
			keys.Add(KeyDown(key));
			_heldKeys.Add(key);
			return;
		}

		if (argument is not null && argument.Equals("UP", StringComparison.OrdinalIgnoreCase))
		{
			var key = ResolveKey(name, out _);
			keys.Add(KeyUp(key));
			_heldKeys.Remove(key);
			return;
		}

		var count = argument is not null ? int.Parse(argument, CultureInfo.InvariantCulture) : 1;
		for (var i = 0; i < count; i++)
		{
			if (_namedKeys.TryGetValue(name, out var named) || TryGetFunctionKey(name, out named))
			{
				Press(named, keys);
			}
			else if (name.Length == 1)
			{
				PressCharacter(name[0], keys);
			}
			else
			{
				throw new FormatException($"Unknown key '{{{token}}}'.");
			}
		}
	}

	private static VirtualKey ResolveKey(string name, out bool needsShift)
	{
		needsShift = false;
		if (_namedKeys.TryGetValue(name, out var named) || TryGetFunctionKey(name, out named))
		{
			return named;
		}

		if (name.Length == 1 && TryGetCharacterKey(name[0], out var key, out needsShift))
		{
			return key;
		}

		throw new FormatException($"Unknown key '{name}'.");
	}

	private static bool TryGetFunctionKey(string name, out VirtualKey key)
	{
		if (name.Length >= 2
			&& (name[0] == 'F' || name[0] == 'f')
			&& int.TryParse(name.AsSpan(1), NumberStyles.None, CultureInfo.InvariantCulture, out var number)
			&& number is >= 1 and <= 24)
		{
			key = VirtualKey.F1 + (number - 1);
			return true;
		}

		key = default;
		return false;
	}

	private static bool TryGetCharacterKey(char c, out VirtualKey key, out bool needsShift)
	{
		needsShift = false;
		if (c is >= 'a' and <= 'z')
		{
			key = VirtualKey.A + (c - 'a');
			return true;
		}

		if (c is >= 'A' and <= 'Z')
		{
			key = VirtualKey.A + (c - 'A');
			needsShift = true;
			return true;
		}

		if (c is >= '0' and <= '9')
		{
			key = VirtualKey.Number0 + (c - '0');
			return true;
		}

		if (c == ' ')
		{
			key = VirtualKey.Space;
			return true;
		}

		key = default;
		return false;
	}

	private static void PressCharacter(char c, List<InjectedInputKeyboardInfo> keys)
	{
		if (TryGetCharacterKey(c, out var key, out var needsShift))
		{
			if (needsShift && !_heldKeys.Contains(VirtualKey.Shift))
			{
				keys.Add(KeyDown(VirtualKey.Shift));
				Press(key, keys);
				keys.Add(KeyUp(VirtualKey.Shift));
			}
			else
			{
				Press(key, keys);
			}
		}
		else
		{
			keys.Add(new InjectedInputKeyboardInfo { ScanCode = c, KeyOptions = InjectedInputKeyOptions.Unicode });
			keys.Add(new InjectedInputKeyboardInfo { ScanCode = c, KeyOptions = InjectedInputKeyOptions.Unicode | InjectedInputKeyOptions.KeyUp });
		}
	}

	private static void Press(VirtualKey key, List<InjectedInputKeyboardInfo> keys)
	{
		keys.Add(KeyDown(key));
		keys.Add(KeyUp(key));
	}

	private static VirtualKey ModifierFor(char c) => c switch
	{
		'+' => VirtualKey.Shift,
		'^' => VirtualKey.Control,
		_ => VirtualKey.Menu,
	};

	private static InjectedInputKeyboardInfo KeyDown(VirtualKey key) => new() { VirtualKey = (ushort)key };

	private static InjectedInputKeyboardInfo KeyUp(VirtualKey key) => new() { VirtualKey = (ushort)key, KeyOptions = InjectedInputKeyOptions.KeyUp };
}

public class Keyboard
{
	private Keyboard()
	{
	}

	public static Keyboard Instance { get; } = new();

	public static int SendKeysDelay { get; set; }

	public void SendText(string text) => TextInput.SendText(text);

	public static string EscapeSpecialCharacters(string text)
	{
		var builder = new System.Text.StringBuilder(text.Length);
		foreach (var c in text)
		{
			if (c is '+' or '^' or '%' or '~' or '(' or ')' or '{' or '}' or '[' or ']')
			{
				builder.Append('{').Append(c).Append('}');
			}
			else
			{
				builder.Append(c);
			}
		}

		return builder.ToString();
	}
}
