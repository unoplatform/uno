#nullable enable

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.UI.Xaml.Controls;
using Uno.Foundation.Logging;
using Uno.UI.Xaml.Controls.Extensions;
using Windows.Foundation;

namespace Uno.WinUI.Runtime.Skia.AppleUIKit.Controls;

/// <summary>
/// iOS Skia implementation of <see cref="IImeTextBoxExtension"/>.
/// Bridges UITextInput composition callbacks (SetMarkedText/InsertText/UnmarkText)
/// on the hidden UITextField/UITextView proxies to the managed TextBox composition
/// event lifecycle (Started → Updated → Completed → Ended).
/// </summary>
internal sealed class AppleUIKitImeTextBoxExtension : IImeTextBoxExtension
{
	internal static AppleUIKitImeTextBoxExtension Instance { get; } = new();

	private bool _isComposing;
	private string _lastComposingText = string.Empty;
	private IImeSessionHost? _activeTextBox;
	private Rect _lastCaretRect = Rect.Empty;

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
		if (host is PasswordBox)
		{
			return;
		}

		_activeTextBox = host;
		_lastCaretRect = Rect.Empty;

		if (this.Log().IsEnabled(LogLevel.Debug))
		{
			this.Log().Debug("IME session started (iOS)");
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
			host.TextBoxView?.Extension?.UpdateNativeView();
		}

		if ((update & (ImeSessionUpdate.CandidateWindowAlignment | ImeSessionUpdate.TextAndSelection)) != 0)
		{
			if ((update & ImeSessionUpdate.CandidateWindowAlignment) != 0)
			{
				_lastCaretRect = Rect.Empty;
			}
			var caretRect = GetCaretRect();
			if (caretRect != Rect.Empty && !caretRect.Equals(_lastCaretRect))
			{
				_lastCaretRect = caretRect;
				host.TextBoxView?.Extension?.UpdatePosition();
				host.TextBoxView?.Extension?.NotifyImePositionChanged();
			}
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
		if (_isComposing)
		{
			_isComposing = false;
			_lastComposingText = string.Empty;
			CompositionEnded?.Invoke(this, EventArgs.Empty);
		}

		_activeTextBox = null;
		_lastCaretRect = Rect.Empty;
	}

	/// <summary>
	/// Called from native view override when UITextInput.SetMarkedText is invoked.
	/// </summary>
	internal void OnSetMarkedText(string text)
	{
		bool wasComposing = _isComposing;

		if (text.Length > 0)
		{
			if (!wasComposing)
			{
				_isComposing = true;
				_lastComposingText = text;

				CompositionStarted?.Invoke(this, EventArgs.Empty);
				CompositionUpdated?.Invoke(this, new ImeCompositionEventArgs(text));

				if (this.Log().IsEnabled(LogLevel.Trace))
				{
					this.Log().Trace($"Composition started: '{text}'");
				}
			}
			else
			{
				_lastComposingText = text;
				CompositionUpdated?.Invoke(this, new ImeCompositionEventArgs(text));

				if (this.Log().IsEnabled(LogLevel.Trace))
				{
					this.Log().Trace($"Composition updated: '{text}'");
				}
			}
		}
		else if (wasComposing)
		{
			_isComposing = false;
			_lastComposingText = string.Empty;
			CompositionCanceled?.Invoke(this, new ImeCompositionEventArgs(string.Empty));
			CompositionEnded?.Invoke(this, EventArgs.Empty);

			if (this.Log().IsEnabled(LogLevel.Trace))
			{
				this.Log().Trace("Composition cancelled (empty marked text)");
			}
		}
	}

	/// <summary>
	/// Called from native view override when UITextInput.InsertText is invoked.
	/// </summary>
	internal void OnInsertText(string text)
	{
		bool wasComposing = _isComposing;

		if (!wasComposing)
		{
			CompositionStarted?.Invoke(this, EventArgs.Empty);
		}

		_isComposing = false;
		_lastComposingText = string.Empty;

		CompositionCompleted?.Invoke(this, new ImeCompositionEventArgs(text));
		CompositionEnded?.Invoke(this, EventArgs.Empty);

		if (this.Log().IsEnabled(LogLevel.Trace))
		{
			this.Log().Trace($"Composition committed: '{text}' (wasComposing: {wasComposing})");
		}
	}

	/// <summary>
	/// Called from native view override when UITextInput.UnmarkText is invoked.
	/// </summary>
	internal void OnUnmarkText()
	{
		if (_isComposing)
		{
			_isComposing = false;
			_lastComposingText = string.Empty;
			CompositionEnded?.Invoke(this, EventArgs.Empty);

			if (this.Log().IsEnabled(LogLevel.Trace))
			{
				this.Log().Trace("Composition ended (unmark)");
			}
		}
	}

	/// <summary>
	/// Returns the caret rectangle in view coordinates for candidate window positioning.
	/// On iOS, the system keyboard handles its own candidate bar, but this provides
	/// correct geometry for third-party keyboards or assistive input methods.
	/// </summary>
	internal Rect GetCaretRect()
	{
		if (_activeTextBox is { TextBoxView.DisplayBlock.ParsedText: { } parsedText, XamlRoot: { } })
		{
			var caret = _activeTextBox.IsBackwardSelection
				? _activeTextBox.SelectionStart
				: _activeTextBox.SelectionStart + _activeTextBox.SelectionLength;
			var caretRect = parsedText.GetRectForIndex(caret);
			var transform = _activeTextBox.TextBoxView.DisplayBlock.TransformToVisual(null);
			var candidateTop = _activeTextBox.DesiredCandidateWindowAlignment == CandidateWindowAlignment.BottomEdge
				? _activeTextBox.TextBoxView.DisplayBlock.ActualHeight
				: caretRect.Top;
			var candidateHeight = _activeTextBox.DesiredCandidateWindowAlignment == CandidateWindowAlignment.BottomEdge
				? 1
				: caretRect.Height;
			var caretPoint = transform.TransformPoint(new Point(caretRect.Left, candidateTop));
			var caretBottom = transform.TransformPoint(new Point(caretRect.Left, candidateTop + candidateHeight));

			return new Rect(caretPoint.X, caretPoint.Y, 1, caretBottom.Y - caretPoint.Y);
		}

		return Rect.Empty;
	}
}
