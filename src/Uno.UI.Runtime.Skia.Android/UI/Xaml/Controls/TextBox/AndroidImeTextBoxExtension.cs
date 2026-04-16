#nullable enable

using System;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Uno.Foundation.Logging;
using Uno.UI.Xaml.Controls.Extensions;

namespace Uno.UI.Runtime.Skia.Android;

/// <summary>
/// Android Skia implementation of <see cref="IImeTextBoxExtension"/>.
/// Bridges Android <see cref="TextInputConnection"/> composition state
/// (SetComposingText/CommitText/FinishComposingText) to the managed
/// TextBox composition event lifecycle (Started → Updated → Completed → Ended).
/// </summary>
/// <remarks>
/// Timing: The composition callback fires from <see cref="ObservableEditingState.EndBatchEdit"/>
/// which happens BEFORE <see cref="TextInputConnection.EndBatchEdit"/> calls
/// <c>ActiveTextBox.ProcessTextInput()</c>. This means TextBox.Text still has the
/// composing text when our callback runs, so <c>ReplaceCompositionText</c> in
/// <c>TextBox.skia.cs</c> works correctly. The subsequent <c>ProcessTextInput</c>
/// from <c>EndBatchEdit</c> sets the same text and is effectively a no-op.
/// </remarks>
internal sealed class AndroidImeTextBoxExtension : IImeTextBoxExtension
{
	private bool _isComposing;
	private int _lastComposingStart = -1;
	private int _lastComposingEnd = -1;
	private int _lastFullTextLength;
	private bool _sessionActive;
	private TextInputConnection? _subscribedConnection;

	public bool IsComposing => _isComposing;

	public event EventHandler? CompositionStarted;
	public event EventHandler<ImeCompositionEventArgs>? CompositionUpdated;
	public event EventHandler<ImeCompositionEventArgs>? CompositionCompleted;
	public event EventHandler? CompositionEnded;

	private static TextInputPlugin? Plugin => ApplicationActivity.RenderView?.TextInputPlugin;

	public void StartImeSession(TextBox textBox)
	{
		if (textBox is PasswordBox)
		{
			return;
		}

		_sessionActive = true;

		if (Plugin is { } plugin)
		{
			plugin.InputConnectionCreated -= OnInputConnectionCreated;
			plugin.InputConnectionCreated += OnInputConnectionCreated;

			SubscribeToConnection(plugin.ActiveInputConnection);
		}

		if (this.Log().IsEnabled(LogLevel.Debug))
		{
			this.Log().Debug("IME session started.");
		}
	}

	public void EndImeSession()
	{
		_sessionActive = false;

		UnsubscribeFromConnection();

		if (Plugin is { } plugin)
		{
			plugin.InputConnectionCreated -= OnInputConnectionCreated;
		}

		if (_isComposing)
		{
			_isComposing = false;
			_lastComposingStart = -1;
			_lastComposingEnd = -1;
			CompositionEnded?.Invoke(this, EventArgs.Empty);
		}
	}

	private void SubscribeToConnection(TextInputConnection? connection)
	{
		if (connection is null || connection == _subscribedConnection)
		{
			return;
		}

		UnsubscribeFromConnection();
		_subscribedConnection = connection;
		connection.CompositionStateChanged += OnCompositionStateChanged;
	}

	private void UnsubscribeFromConnection()
	{
		if (_subscribedConnection is { } old)
		{
			old.CompositionStateChanged -= OnCompositionStateChanged;
			_subscribedConnection = null;
		}
	}

	private void OnInputConnectionCreated(TextInputConnection newConnection)
	{
		if (_sessionActive)
		{
			SubscribeToConnection(newConnection);
		}
	}

	private void OnCompositionStateChanged(int composingStart, int composingEnd, string? composingText, string fullText, bool textChanged)
	{
		if (!_sessionActive)
		{
			return;
		}

		bool wasComposing = _isComposing;
		bool isNowComposing = composingStart >= 0 && composingEnd > composingStart;

		if (!wasComposing && isNowComposing)
		{
			// Don't treat passive autocorrect/spell-check compositions (composing region
			// set on existing text without any text change) as a real composition session.
			// Without this filter, the IME setting a composing region on pre-existing text
			// would set _isComposing=true on the TextBox, causing all subsequent key events
			// to be swallowed by the IsComposing check in OnKeyDown.
			if (!textChanged)
			{
				if (this.Log().IsEnabled(LogLevel.Trace))
				{
					this.Log().Trace($"Ignoring passive composition (no text change): [{composingStart}..{composingEnd}] '{composingText}'");
				}
				return;
			}

			// Transition: Idle → Composing
			_isComposing = true;
			_lastComposingStart = composingStart;
			_lastComposingEnd = composingEnd;
			_lastFullTextLength = fullText.Length;

			CompositionStarted?.Invoke(this, EventArgs.Empty);

			if (!string.IsNullOrEmpty(composingText))
			{
				CompositionUpdated?.Invoke(this, new ImeCompositionEventArgs(composingText, textAlreadyApplied: true));
			}

			if (this.Log().IsEnabled(LogLevel.Trace))
			{
				this.Log().Trace($"Composition started: [{composingStart}..{composingEnd}] '{composingText}'");
			}
		}
		else if (wasComposing && isNowComposing)
		{
			// Transition: Composing → Composing (preedit update)
			_lastComposingStart = composingStart;
			_lastComposingEnd = composingEnd;
			_lastFullTextLength = fullText.Length;

			if (!string.IsNullOrEmpty(composingText))
			{
				CompositionUpdated?.Invoke(this, new ImeCompositionEventArgs(composingText, textAlreadyApplied: true));
			}

			if (this.Log().IsEnabled(LogLevel.Trace))
			{
				this.Log().Trace($"Composition updated: [{composingStart}..{composingEnd}] '{composingText}'");
			}
		}
		else if (wasComposing && !isNowComposing)
		{
			// Transition: Composing → Idle (commit or cancel)
			_isComposing = false;

			// Compute the committed text. The old composing region was at
			// [_lastComposingStart.._lastComposingEnd). The text outside
			// that region is unchanged, so:
			//   nonComposingLength = _lastFullTextLength - oldComposingLength
			//   committedLength = fullText.Length - nonComposingLength
			var oldComposingLength = _lastComposingEnd - _lastComposingStart;
			var nonComposingLength = _lastFullTextLength - oldComposingLength;
			var committedLength = fullText.Length - nonComposingLength;

			if (committedLength > 0 && _lastComposingStart >= 0
				&& _lastComposingStart + committedLength <= fullText.Length)
			{
				var committedText = fullText.Substring(_lastComposingStart, committedLength);
				CompositionCompleted?.Invoke(this, new ImeCompositionEventArgs(committedText, textAlreadyApplied: true));

				if (this.Log().IsEnabled(LogLevel.Trace))
				{
					this.Log().Trace($"Composition committed: '{committedText}' at {_lastComposingStart}");
				}
			}
			else if (committedLength == 0)
			{
				// Composing region removed without replacement — cancel.
				if (this.Log().IsEnabled(LogLevel.Trace))
				{
					this.Log().Trace("Composition cancelled (no committed text)");
				}
			}

			_lastComposingStart = -1;
			_lastComposingEnd = -1;
			CompositionEnded?.Invoke(this, EventArgs.Empty);
		}
	}
}
