#nullable enable

using Windows.System;

namespace Windows.UI.Input.Preview.Injection;

/// <summary>
/// Produces the character an injected key press would type, using an invariant US layout.
/// </summary>
/// <remarks>
/// Windows derives this from the active keyboard layout via the OS message loop. Uno Platform has no
/// layout table on the Skia targets, so injected sequences are resolved against a fixed US layout to
/// keep them identical on every target. Callers needing exact text should use
/// <see cref="InjectedInputKeyOptions.Unicode"/> instead.
/// </remarks>
internal static class InjectedInputKeyboardCharacterMap
{
	// VK codes without a Windows.System.VirtualKey name.
	private const ushort VkOem1 = 0xBA;      // ;:
	private const ushort VkOemPlus = 0xBB;   // =+
	private const ushort VkOemComma = 0xBC;  // ,<
	private const ushort VkOemMinus = 0xBD;  // -_
	private const ushort VkOemPeriod = 0xBE; // .>
	private const ushort VkOem2 = 0xBF;      // /?
	private const ushort VkOem3 = 0xC0;      // `~
	private const ushort VkOem4 = 0xDB;      // [{
	private const ushort VkOem5 = 0xDC;      // \|
	private const ushort VkOem6 = 0xDD;      // ]}
	private const ushort VkOem7 = 0xDE;      // '"

	/// <summary>
	/// Gets the character produced by a key press, or <c>null</c> when the key types nothing.
	/// </summary>
	/// <param name="virtualKey">The Win32 virtual-key code of the pressed key.</param>
	/// <param name="modifiers">The modifier keys currently held.</param>
	/// <param name="capsLock">Whether Caps Lock is currently latched.</param>
	/// <returns>The typed character, or <c>null</c>.</returns>
	public static char? Map(ushort virtualKey, VirtualKeyModifiers modifiers, bool capsLock)
	{
		// Tab is never a typed character: it drives focus navigation, and inserting '\t' would both
		// break that and diverge from the Win32 host, which filters it explicitly.
		if (virtualKey == (ushort)VirtualKey.Tab)
		{
			return null;
		}

		var control = modifiers.HasFlag(VirtualKeyModifiers.Control);
		var menu = modifiers.HasFlag(VirtualKeyModifiers.Menu);

		// AltGr (Ctrl+Alt) types real characters on non-Apple layouts; every other shortcut
		// modifier suppresses the character, which is why an injected Ctrl+A raises no
		// CharacterReceived on Windows.
		var isAltGr = control && menu;
		if (!isAltGr && (control || menu || modifiers.HasFlag(VirtualKeyModifiers.Windows)))
		{
			return null;
		}

		var shift = modifiers.HasFlag(VirtualKeyModifiers.Shift);

		if (virtualKey is >= (ushort)VirtualKey.A and <= (ushort)VirtualKey.Z)
		{
			var upper = shift ^ capsLock;
			return upper ? (char)virtualKey : char.ToLowerInvariant((char)virtualKey);
		}

		if (virtualKey is >= (ushort)VirtualKey.Number0 and <= (ushort)VirtualKey.Number9)
		{
			return shift
				? ")!@#$%^&*("[virtualKey - (ushort)VirtualKey.Number0]
				: (char)virtualKey;
		}

		if (virtualKey is >= (ushort)VirtualKey.NumberPad0 and <= (ushort)VirtualKey.NumberPad9)
		{
			return (char)('0' + (virtualKey - (ushort)VirtualKey.NumberPad0));
		}

		return virtualKey switch
		{
			(ushort)VirtualKey.Space => ' ',
			// TextBox converts '\n' to '\r' itself; emit the Win32 host's '\r' directly.
			(ushort)VirtualKey.Enter => '\r',
			(ushort)VirtualKey.Multiply => '*',
			(ushort)VirtualKey.Add => '+',
			(ushort)VirtualKey.Subtract => '-',
			(ushort)VirtualKey.Decimal => '.',
			(ushort)VirtualKey.Divide => '/',
			VkOem1 => shift ? ':' : ';',
			VkOemPlus => shift ? '+' : '=',
			VkOemComma => shift ? '<' : ',',
			VkOemMinus => shift ? '_' : '-',
			VkOemPeriod => shift ? '>' : '.',
			VkOem2 => shift ? '?' : '/',
			VkOem3 => shift ? '~' : '`',
			VkOem4 => shift ? '{' : '[',
			VkOem5 => shift ? '|' : '\\',
			VkOem6 => shift ? '}' : ']',
			VkOem7 => shift ? '"' : '\'',
			_ => null,
		};
	}
}
