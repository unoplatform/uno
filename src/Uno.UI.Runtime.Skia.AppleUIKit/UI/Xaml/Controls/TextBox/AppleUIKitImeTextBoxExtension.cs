#nullable enable

using System;
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
	private TextBox? _activeTextBox;

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

		_activeTextBox = textBox;

		if (this.Log().IsEnabled(LogLevel.Debug))
		{
			this.Log().Debug("IME session started (iOS)");
		}
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
			var selEnd = _activeTextBox.SelectionStart + _activeTextBox.SelectionLength;
			var caretRect = parsedText.GetRectForIndex(selEnd);
			var transform = _activeTextBox.TextBoxView.DisplayBlock.TransformToVisual(null);
			var caretPoint = transform.TransformPoint(new Point(caretRect.Left, caretRect.Top));
			var caretBottom = transform.TransformPoint(new Point(caretRect.Left, caretRect.Top + caretRect.Height));

			return new Rect(caretPoint.X, caretPoint.Y, 1, caretBottom.Y - caretPoint.Y);
		}

		return Rect.Empty;
	}
}
