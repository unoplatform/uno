#nullable enable

using System;
using Windows.System;
using Windows.UI.Core;

namespace Windows.UI.Input.Preview.Injection;

/// <summary>
/// Describes a single simulated keyboard event passed to
/// <see cref="InputInjector"/>.<c>InjectKeyboardInput</c>.
/// </summary>
public partial class InjectedInputKeyboardInfo
{
	/// <summary>
	/// The virtual key reported by <see cref="InjectedInputKeyboardInfo"/>
	/// when <see cref="InjectedInputKeyOptions.Unicode"/> is used, matching Windows.
	/// </summary>
	private const ushort UnicodeVirtualKey = 255;

	/// <summary>
	/// Gets or sets the options of the simulated keyboard input.
	/// </summary>
	public InjectedInputKeyOptions KeyOptions { get; set; }

	/// <summary>
	/// Gets or sets the hardware scan code of the simulated keyboard input.
	/// </summary>
	/// <remarks>
	/// When <see cref="InjectedInputKeyOptions.Unicode"/> is set, this carries a UTF-16 code unit
	/// instead of a scan code. Uno Platform echoes the value into
	/// <see cref="Windows.UI.Core.CorePhysicalKeyStatus.ScanCode"/> when
	/// <see cref="InjectedInputKeyOptions.ScanCode"/> is set, but always identifies the key from
	/// <see cref="VirtualKey"/> — scan-code-driven key identification requires an OS keyboard layout,
	/// which is not available on the Skia targets.
	/// </remarks>
	public ushort ScanCode { get; set; }

	/// <summary>
	/// Gets or sets the virtual key code of the simulated keyboard input.
	/// </summary>
	/// <remarks>
	/// This is a raw Win32 virtual-key code, not a <see cref="Windows.System.VirtualKey"/> value,
	/// although the two share the same numeric values. It must be 0 when
	/// <see cref="InjectedInputKeyOptions.Unicode"/> is set.
	/// </remarks>
	public ushort VirtualKey { get; set; }

	internal bool IsKeyUp => KeyOptions.HasFlag(InjectedInputKeyOptions.KeyUp);

	private bool IsUnicode => KeyOptions.HasFlag(InjectedInputKeyOptions.Unicode);

	internal void Validate(int index)
	{
		if (IsUnicode && VirtualKey != 0)
		{
			throw new ArgumentException(
				$"{nameof(VirtualKey)} must be 0 when {nameof(InjectedInputKeyOptions)}.{nameof(InjectedInputKeyOptions.Unicode)} is set (entry {index}).",
				"input");
		}
	}

	internal KeyEventArgs ToEventArgs(VirtualKeyModifiers modifiers, bool capsLock, bool wasKeyDown)
	{
		var isUp = IsKeyUp;
		var key = IsUnicode ? (VirtualKey)UnicodeVirtualKey : (VirtualKey)VirtualKey;

		// A key-up never carries a character: Windows pairs WM_CHAR with the key press, and
		// InputManager only raises CharacterReceived on the down pass.
		char? unicodeKey = (isUp, IsUnicode) switch
		{
			(true, _) => null,
			(false, true) => (char)ScanCode,
			(false, false) => InjectedInputKeyboardCharacterMap.Map(VirtualKey, modifiers, capsLock),
		};

		var keyStatus = new CorePhysicalKeyStatus
		{
			// Windows discards the supplied scan code unless the ScanCode option is set.
			ScanCode = KeyOptions.HasFlag(InjectedInputKeyOptions.ScanCode) ? ScanCode : 0u,
			RepeatCount = 1,
			IsExtendedKey = KeyOptions.HasFlag(InjectedInputKeyOptions.ExtendedKey),
			IsKeyReleased = isUp,
			WasKeyDown = isUp || wasKeyDown,
			IsMenuKeyDown = modifiers.HasFlag(VirtualKeyModifiers.Menu),
		};

		return new KeyEventArgs("keyboard", key, modifiers, keyStatus, unicodeKey);
	}
}
