#nullable enable

using System;
using System.Collections.Generic;
using System.Runtime.InteropServices.JavaScript;
using System.Threading;
using System.Threading.Tasks;
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
	private bool _restartPending;
	private IImeSessionHost? _host;
	private int _sessionGeneration;

	public bool IsComposing => _isComposing;

	public event EventHandler? CompositionStarted;
	public event EventHandler<ImeCompositionEventArgs>? CompositionUpdated;
	public event EventHandler<ImeCompositionEventArgs>? CompositionCompleted;
	public event EventHandler<ImePartialCompositionEventArgs>? CompositionPartiallyCommitted
	{
		add { }
		remove { }
	}
	public event EventHandler<ImeCompositionEventArgs>? CompositionCanceled;
	public event EventHandler? CompositionEnded;

	public void StartImeSession(IImeSessionHost host, ImeSessionActivation activation)
	{
		if (host is TextBoxCore { IsPassword: true })
		{
			return;
		}
		_host = host;
		_sessionGeneration++;
		if (_restartPending)
		{
			_restartPending = false;
			BrowserInvisibleTextBoxViewExtension.RestartComposition();
		}
	}

	public void UpdateImeSession(IImeSessionHost host, ImeSessionUpdate update)
	{
		if ((update & (
			ImeSessionUpdate.InputScope |
			ImeSessionUpdate.TextPrediction |
			ImeSessionUpdate.AcceptsReturn |
			ImeSessionUpdate.SpellCheck)) != 0)
		{
			host.TextBoxView?.UpdateProperties();
		}
	}

	public Task<IReadOnlyList<string>> GetLinguisticAlternativesAsync(string compositionText, CancellationToken cancellationToken)
	{
		cancellationToken.ThrowIfCancellationRequested();
		return Task.FromResult<IReadOnlyList<string>>(Array.Empty<string>());
	}

	public event EventHandler<ImeCandidateWindowBoundsChangedEventArgs>? CandidateWindowBoundsChanged
	{
		add { }
		remove { }
	}

	public void EndImeSession()
	{
		_restartPending = _isComposing;
		_host = null;
		_sessionGeneration++;
		BrowserInvisibleTextBoxViewExtension.InvalidateComposition();
		if (_isComposing)
		{
			_isComposing = false;
			CompositionEnded?.Invoke(this, EventArgs.Empty);
		}
	}

	private bool OwnsCompositionCallback(IntPtr handle)
		=> _host?.TextBoxView?.Host?.Owner == BrowserInvisibleTextBoxViewExtension.GetFocusedTextInputOwner(handle)
			&& _host is { CanAcceptTextInput: true };

	[JSExport]
	private static void OnCompositionStarted(IntPtr handle)
	{
		if (!Instance.OwnsCompositionCallback(handle))
		{
			BrowserInvisibleTextBoxViewExtension.InvalidateComposition();
			return;
		}
		Instance._isComposing = true;
		Instance.CompositionStarted?.Invoke(Instance, EventArgs.Empty);
	}

	[JSExport]
	private static void OnCompositionUpdated(IntPtr handle, string text, int cursorPosition)
	{
		if (!Instance._isComposing || !Instance.OwnsCompositionCallback(handle))
		{
			return;
		}
		Instance.CompositionUpdated?.Invoke(Instance, new ImeCompositionEventArgs(text, cursorPosition));
	}

	[JSExport]
	private static void OnCompositionCompleted(IntPtr handle, string text)
	{
		if (!Instance._isComposing || !Instance.OwnsCompositionCallback(handle))
		{
			return;
		}
		Instance._isComposing = false;
		var generation = Instance._sessionGeneration;
		Instance.CompositionCompleted?.Invoke(Instance, new ImeCompositionEventArgs(text));
		if (generation == Instance._sessionGeneration && !Instance._isComposing)
		{
			Instance.CompositionEnded?.Invoke(Instance, EventArgs.Empty);
		}
	}

	[JSExport]
	private static void OnCompositionEnded(IntPtr handle)
	{
		if (!Instance._isComposing || !Instance.OwnsCompositionCallback(handle))
		{
			return;
		}

		Instance._isComposing = false;
		Instance.CompositionEnded?.Invoke(Instance, EventArgs.Empty);
	}

	[JSExport]
	private static void OnCompositionCanceled(IntPtr handle)
	{
		if (!Instance._isComposing || !Instance.OwnsCompositionCallback(handle))
		{
			return;
		}

		Instance._isComposing = false;
		var generation = Instance._sessionGeneration;
		Instance.CompositionCanceled?.Invoke(Instance, new ImeCompositionEventArgs(string.Empty));
		if (generation == Instance._sessionGeneration && !Instance._isComposing)
		{
			Instance.CompositionEnded?.Invoke(Instance, EventArgs.Empty);
		}
	}
}
