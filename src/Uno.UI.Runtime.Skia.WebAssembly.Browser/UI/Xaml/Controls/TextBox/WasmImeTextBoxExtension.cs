#nullable enable

using System;
using System.Runtime.InteropServices.JavaScript;
using Microsoft.UI.Xaml.Controls;
using Uno.UI.Xaml.Controls.Extensions;

namespace Uno.UI.Runtime.Skia;

/// <summary>
/// WASM browser implementation of <see cref="IImeTextBoxExtension"/>.
/// Bridges browser composition events to the managed TextBox composition event lifecycle
/// (Started → Updated → Completed → Ended). The browser applies the composition to the hidden input, whose
/// text is synced to the TextBox like any other input, so every event reports the text as already applied.
/// A composition is reported from its first text change, since an IME can mark existing text as composing.
/// </summary>
internal sealed partial class WasmImeTextBoxExtension : IImeTextBoxExtension
{
	internal static WasmImeTextBoxExtension Instance { get; } = new();

	private bool _isComposing;
	private bool _restartPending;

	public bool IsComposing => _isComposing;

	public event EventHandler? CompositionStarted;
	public event EventHandler<ImeCompositionEventArgs>? CompositionUpdated;
	public event EventHandler<ImeCompositionEventArgs>? CompositionCompleted;
	public event EventHandler? CompositionEnded;

	public void StartImeSession(TextBox textBox)
	{
		if (textBox is PasswordBox)
		{
			return;
		}
		if (_restartPending)
		{
			BrowserInvisibleTextBoxViewExtension.RestartComposition();
			_restartPending = false;
		}
	}

	public void EndImeSession()
	{
		_restartPending = _isComposing;
		BrowserInvisibleTextBoxViewExtension.InvalidateComposition();
		if (_isComposing)
		{
			_isComposing = false;
			CompositionEnded?.Invoke(this, EventArgs.Empty);
		}
	}

	[JSExport]
	private static void OnCompositionStarted()
	{
		Instance._isComposing = true;
		BrowserInvisibleTextBoxViewExtension.RunFromNative(static () => Instance.CompositionStarted?.Invoke(Instance, EventArgs.Empty));
	}

	[JSExport]
	private static void OnCompositionUpdated(string text, int cursorPosition, int compositionStart)
	{
		if (!Instance._isComposing)
		{
			return;
		}
		BrowserInvisibleTextBoxViewExtension.RunFromNative(() => Instance.CompositionUpdated?.Invoke(Instance, new ImeCompositionEventArgs(text, cursorPosition, textAlreadyApplied: true, compositionStart: compositionStart)));
	}

	[JSExport]
	private static void OnCompositionCompleted(string text)
	{
		if (!Instance._isComposing)
		{
			return;
		}
		Instance._isComposing = false;
		BrowserInvisibleTextBoxViewExtension.RunFromNative(() =>
		{
			Instance.CompositionCompleted?.Invoke(Instance, new ImeCompositionEventArgs(text, textAlreadyApplied: true));
			Instance.CompositionEnded?.Invoke(Instance, EventArgs.Empty);
		});
	}

	[JSExport]
	private static void OnCompositionEnded()
	{
		if (!Instance._isComposing)
		{
			return;
		}

		Instance._isComposing = false;
		BrowserInvisibleTextBoxViewExtension.RunFromNative(static () => Instance.CompositionEnded?.Invoke(Instance, EventArgs.Empty));
	}
}
