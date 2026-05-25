#nullable enable

using System;
using Windows.Win32;
using Windows.Win32.Foundation;
using Windows.Win32.UI.Input.Ime;
using Microsoft.UI.Xaml.Controls;
using Uno.UI.Hosting;
using Uno.UI.NativeElementHosting;
using Uno.UI.Xaml.Controls.Extensions;

namespace Uno.UI.Runtime.Skia.Win32;

/// <summary>
/// Win32 IMM32-based implementation of <see cref="IImeTextBoxExtension"/>.
/// Handles WM_IME_STARTCOMPOSITION, WM_IME_COMPOSITION, and WM_IME_ENDCOMPOSITION
/// messages to provide IME support for TextBox on Win32 Skia.
/// </summary>
internal sealed class Win32ImeTextBoxExtension : IImeTextBoxExtension
{
	internal static Win32ImeTextBoxExtension Instance { get; } = new();

	private HWND _hwnd;
	private bool _isComposing;

	private Win32ImeTextBoxExtension()
	{
	}

	public bool IsComposing => _isComposing;

	public event EventHandler? CompositionStarted;
	public event EventHandler<ImeCompositionEventArgs>? CompositionUpdated;
	public event EventHandler<ImeCompositionEventArgs>? CompositionCompleted;
	public event EventHandler? CompositionEnded;

	public void StartImeSession(TextBox textBox)
	{
		var wrapper = (Win32WindowWrapper)XamlRootMap.GetHostForRoot(textBox.XamlRoot!)!;
		_hwnd = (HWND)((Win32NativeWindow)wrapper.NativeWindow!).Hwnd;
	}

	public void EndImeSession()
	{
		if (_isComposing)
		{
			// Tell the IME to commit the active composition and close its windows
			var himc = PInvoke.ImmGetContext(_hwnd);
			if (!himc.IsNull)
			{
				PInvoke.ImmNotifyIME(himc, NOTIFY_IME_ACTION.NI_COMPOSITIONSTR, NOTIFY_IME_INDEX.CPS_COMPLETE, 0);
				PInvoke.ImmReleaseContext(_hwnd, himc);
			}

			_isComposing = false;
			CompositionEnded?.Invoke(this, EventArgs.Empty);
		}

		_hwnd = HWND.Null;
	}

	/// <summary>
	/// Called from WndProc when WM_IME_STARTCOMPOSITION is received.
	/// </summary>
	internal void OnWmImeStartComposition()
	{
		_isComposing = true;
		CompositionStarted?.Invoke(this, EventArgs.Empty);
	}

	/// <summary>
	/// Called from WndProc when WM_IME_COMPOSITION is received.
	/// </summary>
	internal unsafe void OnWmImeComposition(LPARAM lParam)
	{
		var himc = PInvoke.ImmGetContext(_hwnd);
		if (himc.IsNull)
		{
			return;
		}

		try
		{
			var flags = (IME_COMPOSITION_STRING)(uint)lParam.Value;

			if (flags.HasFlag(IME_COMPOSITION_STRING.GCS_RESULTSTR))
			{
				var text = GetCompositionString(himc, IME_COMPOSITION_STRING.GCS_RESULTSTR);
				if (text is not null)
				{
					CompositionCompleted?.Invoke(this, new ImeCompositionEventArgs(text));
				}
			}

			if (flags.HasFlag(IME_COMPOSITION_STRING.GCS_COMPSTR))
			{
				var text = GetCompositionString(himc, IME_COMPOSITION_STRING.GCS_COMPSTR);
				if (!string.IsNullOrEmpty(text))
				{
					var cursorPos = PInvoke.ImmGetCompositionString(himc, IME_COMPOSITION_STRING.GCS_CURSORPOS, null, 0);
					var resolvedLen = GetResolvedLength(himc, text.Length);
					CompositionUpdated?.Invoke(this, new ImeCompositionEventArgs(text, cursorPos, resolvedLen));
				}
			}
		}
		finally
		{
			PInvoke.ImmReleaseContext(_hwnd, himc);
		}
	}

	/// <summary>
	/// Called from WndProc when WM_IME_ENDCOMPOSITION is received.
	/// </summary>
	internal void OnWmImeEndComposition()
	{
		if (!_isComposing)
		{
			return;
		}

		_isComposing = false;
		CompositionEnded?.Invoke(this, EventArgs.Empty);
	}

	/// <summary>
	/// Counts leading characters in the composition that are already resolved
	/// (ATTR_TARGET_CONVERTED or ATTR_FIXEDCONVERTED) using GCS_COMPATTR.
	/// </summary>
	private static unsafe int GetResolvedLength(HIMC himc, int compositionLength)
	{
		var byteLen = PInvoke.ImmGetCompositionString(himc, IME_COMPOSITION_STRING.GCS_COMPATTR, null, 0);
		if (byteLen <= 0)
		{
			return 0;
		}

		var attrs = stackalloc byte[byteLen];
		var result = PInvoke.ImmGetCompositionString(himc, IME_COMPOSITION_STRING.GCS_COMPATTR, attrs, (uint)byteLen);
		if (result <= 0)
		{
			return 0;
		}

		// ATTR_INPUT = 0, ATTR_TARGET_CONVERTED = 1, ATTR_CONVERTED = 2,
		// ATTR_TARGET_NOTCONVERTED = 3, ATTR_INPUT_ERROR = 4, ATTR_FIXEDCONVERTED = 5
		// Count leading non-input characters (already converted/resolved).
		var count = Math.Min(result, compositionLength);
		for (var i = 0; i < count; i++)
		{
			if (attrs[i] == 0) // ATTR_INPUT
			{
				return i;
			}
		}

		return count;
	}

	private static unsafe string? GetCompositionString(HIMC himc, IME_COMPOSITION_STRING dwIndex)
	{
		var byteLen = PInvoke.ImmGetCompositionString(himc, dwIndex, null, 0);
		if (byteLen <= 0)
		{
			return dwIndex == IME_COMPOSITION_STRING.GCS_COMPSTR ? string.Empty : null;
		}

		var buffer = stackalloc byte[byteLen];
		var result = PInvoke.ImmGetCompositionString(himc, dwIndex, buffer, (uint)byteLen);
		if (result <= 0)
		{
			return null;
		}

		return new string((char*)buffer, 0, result / sizeof(char));
	}
}
