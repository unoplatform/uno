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
/// The hidden input applies the text itself (synced through the input event), so composition
/// events only carry the preedit and where it sits in the text, and a completion is only reported
/// once the committed text is in the input.
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

	public void StartImeSession(TextBoxCore core)
	{
		if (core.IsPassword)
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
	private static void OnCompositionUpdated(string text, int startIndex, bool textChangePending)
	{
		Instance.CompositionUpdated?.Invoke(Instance, new ImeCompositionEventArgs(text, textAlreadyApplied: true, startIndex: startIndex, textChangePending: textChangePending));
	}

	[JSExport]
	private static void OnCompositionCompleted(string text)
	{
		// A compositionend for a composition that a managed text change already ended has nothing left to commit.
		if (!Instance._isComposing)
		{
			return;
		}

		Instance._isComposing = false;
		Instance.CompositionCompleted?.Invoke(Instance, new ImeCompositionEventArgs(text, textAlreadyApplied: true));
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
