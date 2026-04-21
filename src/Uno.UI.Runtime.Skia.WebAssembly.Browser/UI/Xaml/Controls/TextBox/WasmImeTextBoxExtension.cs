#nullable enable

using System;
using System.Runtime.InteropServices.JavaScript;
using Microsoft.UI.Xaml.Controls;
using Uno.UI.Xaml.Controls.Extensions;

namespace Uno.UI.Runtime.Skia;

/// <summary>
/// WASM browser implementation of <see cref="IImeTextBoxExtension"/>.
/// Bridges browser CompositionEvent APIs (compositionstart/compositionupdate/compositionend)
/// to the managed TextBox composition event lifecycle (Started → Updated → Completed → Ended).
/// </summary>
internal sealed partial class WasmImeTextBoxExtension : IImeTextBoxExtension
{
	internal static WasmImeTextBoxExtension Instance { get; } = new();

	private bool _isComposing;

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
	}

	public void EndImeSession()
	{
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
		Instance.CompositionStarted?.Invoke(Instance, EventArgs.Empty);
	}

	[JSExport]
	private static void OnCompositionUpdated(string text, int cursorPosition)
	{
		Instance.CompositionUpdated?.Invoke(Instance, new ImeCompositionEventArgs(text, cursorPosition));
	}

	[JSExport]
	private static void OnCompositionCompleted(string text)
	{
		Instance._isComposing = false;
		Instance.CompositionCompleted?.Invoke(Instance, new ImeCompositionEventArgs(text));
		Instance.CompositionEnded?.Invoke(Instance, EventArgs.Empty);
	}

	[JSExport]
	private static void OnCompositionEnded()
	{
		if (!Instance._isComposing)
		{
			return;
		}

		Instance._isComposing = false;
		Instance.CompositionEnded?.Invoke(Instance, EventArgs.Empty);
	}
}
