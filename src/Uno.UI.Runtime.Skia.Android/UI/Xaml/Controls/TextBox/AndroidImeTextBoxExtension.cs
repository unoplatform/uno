#nullable enable

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Uno.Foundation.Logging;
using Uno.UI.Xaml.Controls.Extensions;

namespace Uno.UI.Runtime.Skia.Android;

/// <summary>
/// Android Skia implementation of <see cref="IImeTextBoxExtension"/>.
/// Bridges Android <see cref="TextInputConnection"/> text and composition state to the active
/// <see cref="IImeSessionHost"/>.
/// </summary>
/// <remarks>
/// Composition bookkeeping precedes native text synchronization; completion follows it so the
/// committed text belongs to the composition's undo group and is visible to completion handlers.
/// </remarks>
internal sealed class AndroidImeTextBoxExtension : IImeTextBoxExtension
{
	private bool _isComposing;
	private int _lastComposingStart = -1;
	private int _lastComposingEnd = -1;
	private int _lastFullTextLength;
	private bool _sessionActive;
	private TextInputConnection? _subscribedConnection;
	private IImeSessionHost? _activeHost;
	private int _sessionVersion;
	private TextBox? _observedTextBox;
	private long _readOnlyChangedToken;
	private long _enabledChangedToken;
	private long _tabStopChangedToken;

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

	private static TextInputPlugin? Plugin => ApplicationActivity.RenderView?.TextInputPlugin;

	public void StartImeSession(IImeSessionHost host, ImeSessionActivation activation)
	{
		if (host is TextBoxCore { IsPassword: true })
		{
			return;
		}

		_sessionActive = true;
		_activeHost = host;
		_sessionVersion++;

		try
		{
			ObserveInputAvailability(host);
			if (Plugin is { } plugin)
			{
				plugin.InputConnectionCreated -= OnInputConnectionCreated;
				plugin.InputConnectionCreated += OnInputConnectionCreated;

				plugin.StartImeSession(host, activation);
				SubscribeToConnection(plugin.ActiveInputConnection);
			}
		}
		catch
		{
			_sessionActive = false;
			_activeHost = null;
			StopObservingInputAvailability();
			UnsubscribeFromConnection();
			if (Plugin is { } plugin)
			{
				plugin.InputConnectionCreated -= OnInputConnectionCreated;
			}
			throw;
		}

		if (this.Log().IsEnabled(LogLevel.Debug))
		{
			this.Log().Debug("IME session started.");
		}
	}

	public void UpdateImeSession(IImeSessionHost host, ImeSessionUpdate update)
	{
		if (ReferenceEquals(_activeHost, host) && !TextInputPlugin.CanAcceptTextInput(host) && _isComposing)
		{
			ResetComposition();
			CompositionEnded?.Invoke(this, EventArgs.Empty);
		}

		Plugin?.UpdateImeSession(host, update);
	}

	private void ObserveInputAvailability(IImeSessionHost host)
	{
		StopObservingInputAvailability();
		if (host is TextBoxCore { Owner: TextBox textBox })
		{
			_observedTextBox = textBox;
			_readOnlyChangedToken = textBox.RegisterPropertyChangedCallback(TextBox.IsReadOnlyProperty, OnInputAvailabilityChanged);
			_enabledChangedToken = textBox.RegisterPropertyChangedCallback(Control.IsEnabledProperty, OnInputAvailabilityChanged);
			_tabStopChangedToken = textBox.RegisterPropertyChangedCallback(Control.IsTabStopProperty, OnInputAvailabilityChanged);
		}
	}

	private void StopObservingInputAvailability()
	{
		if (_observedTextBox is { } textBox)
		{
			textBox.UnregisterPropertyChangedCallback(TextBox.IsReadOnlyProperty, _readOnlyChangedToken);
			textBox.UnregisterPropertyChangedCallback(Control.IsEnabledProperty, _enabledChangedToken);
			textBox.UnregisterPropertyChangedCallback(Control.IsTabStopProperty, _tabStopChangedToken);
			_observedTextBox = null;
		}
	}

	private void OnInputAvailabilityChanged(DependencyObject sender, DependencyProperty property)
	{
		if (_activeHost is { } host)
		{
			UpdateImeSession(host, ImeSessionUpdate.TextAndSelection);
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
		_sessionActive = false;
		_sessionVersion++;

		StopObservingInputAvailability();
		UnsubscribeFromConnection();

		if (Plugin is { } plugin)
		{
			plugin.InputConnectionCreated -= OnInputConnectionCreated;
			if (_activeHost is { } host)
			{
				plugin.EndImeSession(host);
			}
		}
		_activeHost = null;

		if (_isComposing)
		{
			ResetComposition();
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

	private void OnInputConnectionCreated(object? sender, TextInputConnectionCreatedEventArgs args)
	{
		if (_sessionActive)
		{
			SubscribeToConnection(args.Connection);
			if (_isComposing && args.Connection.GetComposingRange() is null)
			{
				ResetComposition();
				CompositionEnded?.Invoke(this, EventArgs.Empty);
			}
		}
	}

	private bool IsCurrentSession(object? connection, int version)
		=> _sessionActive
			&& _sessionVersion == version
			&& ReferenceEquals(connection, _subscribedConnection)
			&& ReferenceEquals(_subscribedConnection?.ActiveHost, _activeHost)
			&& TextInputPlugin.CanAcceptTextInput(_activeHost)
			&& !TextInputPlugin.IsSupersededByFocusedHost(_activeHost);

	private void ResetComposition()
	{
		_isComposing = false;
		_lastComposingStart = -1;
		_lastComposingEnd = -1;
		_lastFullTextLength = 0;
	}

	private void OnCompositionStateChanged(object? sender, TextInputCompositionEventArgs args)
	{
		var sessionVersion = _sessionVersion;
		if (!IsCurrentSession(sender, sessionVersion))
		{
			return;
		}

		bool wasComposing = _isComposing;
		bool isNowComposing = args.IsComposing;
		var composingStart = args.ComposingStart;
		var composingEnd = args.ComposingEnd;
		var composingText = args.CompositionText;
		var fullText = args.Text;

		if (args.TextApplied && isNowComposing)
		{
			_lastComposingStart = composingStart;
			_lastComposingEnd = composingEnd;
			_lastFullTextLength = fullText.Length;
			return;
		}

		if (!args.TextApplied && !wasComposing && isNowComposing)
		{
			// Don't treat passive autocorrect/spell-check compositions (composing region
			// set on existing text without any text change) as a real composition session.
			// Without this filter, the IME setting a composing region on pre-existing text
			// would set _isComposing=true on the TextBox, causing all subsequent key events
			// to be swallowed by the IsComposing check in OnKeyDown.
			if (!args.TextChanged)
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

			if (IsCurrentSession(sender, sessionVersion) && _isComposing)
			{
				CompositionUpdated?.Invoke(this, new ImeCompositionEventArgs(composingText, textAlreadyApplied: true));
			}

			if (this.Log().IsEnabled(LogLevel.Trace))
			{
				this.Log().Trace($"Composition started: [{composingStart}..{composingEnd}] '{composingText}'");
			}
		}
		else if (!args.TextApplied && wasComposing && isNowComposing)
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
			// Compute the committed text. The old composing region was at
			// [_lastComposingStart.._lastComposingEnd). The text outside
			// that region is unchanged, so:
			//   nonComposingLength = _lastFullTextLength - oldComposingLength
			//   committedLength = fullText.Length - nonComposingLength
			var oldComposingLength = _lastComposingEnd - _lastComposingStart;
			var nonComposingLength = _lastFullTextLength - oldComposingLength;
			var committedLength = fullText.Length - nonComposingLength;

			if (!args.TextApplied)
			{
				// The final native replacement must consume the same external-change guard as a preedit.
				if (args.TextChanged && committedLength >= 0 && _lastComposingStart >= 0
					&& _lastComposingStart + committedLength <= fullText.Length)
				{
					CompositionUpdated?.Invoke(this, new ImeCompositionEventArgs(
						fullText.Substring(_lastComposingStart, committedLength),
						textAlreadyApplied: true));
				}
				return;
			}

			var committedStart = _lastComposingStart;
			ResetComposition();
			if (committedLength > 0 && committedStart >= 0
				&& committedStart + committedLength <= fullText.Length)
			{
				var committedText = fullText.Substring(committedStart, committedLength);
				CompositionCompleted?.Invoke(this, new ImeCompositionEventArgs(committedText, textAlreadyApplied: true));

				if (this.Log().IsEnabled(LogLevel.Trace))
				{
					this.Log().Trace($"Composition committed: '{committedText}' at {committedStart}");
				}
			}
			else if (committedLength == 0)
			{
				// Composing region removed without replacement — cancel.
				CompositionCanceled?.Invoke(
					this,
					new ImeCompositionEventArgs(string.Empty, textAlreadyApplied: true));
				if (this.Log().IsEnabled(LogLevel.Trace))
				{
					this.Log().Trace("Composition cancelled (no committed text)");
				}
			}

			if (IsCurrentSession(sender, sessionVersion) && !_isComposing)
			{
				CompositionEnded?.Invoke(this, EventArgs.Empty);
			}
		}
	}
}
