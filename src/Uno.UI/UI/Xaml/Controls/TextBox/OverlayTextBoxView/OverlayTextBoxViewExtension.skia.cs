#nullable enable

using System;
using System.Globalization;
using Uno.Disposables;
using Uno.UI.Extensions;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Point = Windows.Foundation.Point;
using Size = Windows.Foundation.Size;

namespace Uno.UI.Xaml.Controls.Extensions;
internal abstract class OverlayTextBoxViewExtension : IOverlayTextBoxViewExtension
{
	private readonly TextBoxView _owner;
	private readonly Func<TextBox, IOverlayTextBoxView> _textBoxViewFactory;
	private readonly SerialDisposable _textChangedDisposable = new SerialDisposable();
	private readonly SerialDisposable _pasteDisposable = new SerialDisposable();

	private ContentControl? _contentElement;
	private IOverlayTextBoxView? _textBoxView;
	private bool _processingTextChanged;
	private Point _lastPosition = new(-1, -1);
	private Size _lastSize = new(-1, -1);
	private PasswordRevealState _currentPasswordRevealState;
	private int? _selectionStartCache;
	private int? _selectionLengthCache;

	protected OverlayTextBoxViewExtension(TextBoxView owner, Func<TextBox, IOverlayTextBoxView> textBoxViewFactory)
	{
		_owner = owner ?? throw new ArgumentNullException(nameof(owner));
		_textBoxViewFactory = textBoxViewFactory ?? throw new ArgumentNullException(nameof(textBoxViewFactory));
	}

	public abstract bool IsOverlayLayerInitialized(XamlRoot xamlRoot);

	public void StartEntry()
	{
		if (_owner.TextBox is not { XamlRoot: { } xamlRoot } textBox)
		{
			// The parent TextBox must exist as source of properties.
			return;
		}

		_contentElement = textBox.ContentElement;

		EnsureTextBoxView(textBox);
		ObserveNativeTextChanges();
		ObserveNativePaste();
		_lastSize = new Size(-1, -1);
		_lastPosition = new Point(-1, -1);
		UpdateNativeView();
		SetNativeText(textBox.Text);

		_textBoxView!.AddToTextInputLayer(xamlRoot);
		InvalidateLayout();

		SetPasswordRevealState(_currentPasswordRevealState);

		_textBoxView.SetFocus();

		// Selection is now handled by native control
		if (_selectionStartCache != null && _selectionLengthCache != null)
		{
			Select(_selectionStartCache.Value, _selectionLengthCache.Value);
		}
		else
		{
			// Select end of the text
			var endIndex = textBox.Text.Length;
			Select(endIndex, 0);
		}
		_selectionStartCache = null;
		_selectionLengthCache = null;
	}

	public void EndEntry()
	{
		_textChangedDisposable.Disposable = null;
		_pasteDisposable.Disposable = null;
		if (_textBoxView is null ||
			!_textBoxView.IsDisplayed)
		{
			// No entry in progress
			return;
		}

		if (GetNativeText() is { } inputText)
		{
			_owner.UpdateTextFromNative(inputText);
		}

		_contentElement = null;

		if (_textBoxView is not null)
		{
			var selection = _textBoxView.Selection;
			(_selectionStartCache, _selectionLengthCache) = (selection.start, selection.length);
			_textBoxView.RemoveFromTextInputLayer();
		}
	}

	public void SetText(string text) => SetNativeText(text);

	public void UpdateNativeView()
	{
		if (_textBoxView is null || _owner.TextBox is not { } textBox)
		{
			// If the input widget does not exist, we don't need to update it.
			// The parent TextBox must exist as source of properties.
			return;
		}

		EnsureTextBoxView(textBox);
	}

	public void InvalidateLayout()
	{
		UpdateSize();
		UpdatePosition();
	}

	public void UpdateProperties()
	{
		if (_owner?.TextBox is { } textBox)
		{
			_textBoxView?.UpdateProperties(textBox);
		}
	}

	public void UpdateSize()
	{
		if (_contentElement is null ||
			_textBoxView is null ||
			!_textBoxView.IsDisplayed)
		{
			return;
		}

		var width = Math.Max(0, (int)(_contentElement.ActualWidth - _contentElement.Padding.Horizontal()));
		var height = Math.Max(0, (int)(_contentElement.ActualHeight - _contentElement.Padding.Vertical()));

		if (_lastSize.Width != width || _lastSize.Height != height)
		{
			_lastSize = new Size(width, height);
			_textBoxView.SetSize(_lastSize.Width, _lastSize.Height);
		}
	}

	public void UpdatePosition()
	{
		if (_contentElement is null || _textBoxView is not { IsDisplayed: true })
		{
			return;
		}

		var transformToRoot = _contentElement.TransformToVisual(Microsoft.UI.Xaml.Window.IShouldntUseCurrentWindow!.Content);
		var point = transformToRoot.TransformPoint(new Point(_contentElement.Padding.Left, _contentElement.Padding.Top));
		var pointX = _owner?.TextBox?.FlowDirection is FlowDirection.RightToLeft
			? (int)(point.X - _contentElement.RenderSize.Width)
			: (int)point.X;
		var pointY = (int)point.Y;

		if (_lastPosition.X != pointX || _lastPosition.Y != pointY)
		{
			_lastPosition = new Point(pointX, pointY);
			_textBoxView.SetPosition(pointX, pointY);
		}
	}

	public void SetPasswordRevealState(PasswordRevealState revealState)
	{
		if (_owner.TextBox is PasswordBox)
		{
			_textBoxView?.SetPasswordRevealState(revealState);
			_currentPasswordRevealState = revealState;
		}
	}

	public void Select(int start, int length)
	{
		if (_owner.TextBox is not { } textBox)
		{
			return;
		}

		EnsureTextBoxView(textBox);
		if (textBox.FocusState == FocusState.Unfocused)
		{
			// Native control can't handle selection until it is part of visual tree.
			// Use managed selection until then.
			_selectionStartCache = textBox.Text.Length >= start ? start : textBox.Text.Length;
			_selectionLengthCache = textBox.Text.Length >= start + length ? length : textBox.Text.Length - start;
		}
		else
		{
			_textBoxView!.Selection = (start, length);
		}
	}

	public int GetSelectionStart()
	{
		if (_owner.TextBox is not { } textBox)
		{
			return 0;
		}

		return textBox.FocusState == FocusState.Unfocused ?
			_selectionStartCache ?? 0 :
			_textBoxView?.Selection.start ?? 0;
	}

	public int GetSelectionLength()
	{
		if (_owner.TextBox is not { } textBox)
		{
			return 0;
		}

		return textBox.FocusState == FocusState.Unfocused ?
			_selectionLengthCache ?? 0 :
			_textBoxView?.Selection.length ?? 0;
	}

	public int GetSelectionStartBeforeKeyDown() => _textBoxView!.SelectionBeforeKeyDown.start;

	public int GetSelectionLengthBeforeKeyDown() => _textBoxView!.SelectionBeforeKeyDown.length;

	private void EnsureTextBoxView(TextBox textBox)
	{
		if (_textBoxView is null ||
			!_textBoxView.IsCompatible(textBox))
		{
			// The current TextBoxView is not compatible with the given TextBox state.
			// We need to create a new TextBoxView.
			var inputText = GetNativeText() ?? textBox.Text;
			_textBoxView = _textBoxViewFactory(textBox);
			SetNativeText(inputText ?? string.Empty);
		}

		_textBoxView.UpdateProperties(textBox);
	}

	private void ObserveNativeTextChanges()
	{
		_textChangedDisposable.Disposable = null;
		if (_textBoxView is not null)
		{
			_textChangedDisposable.Disposable = _textBoxView.ObserveTextChanges(NativeTextChanged);
		}
	}

	private void ObserveNativePaste()
	{
		_pasteDisposable.Disposable = null;
		if (_textBoxView is not null)
		{
			_textBoxView.Paste += NativePaste;
			_pasteDisposable.Disposable = Disposable.Create(() => _textBoxView.Paste -= NativePaste);
		}
	}

	private void NativePaste(object sender, TextControlPasteEventArgs e) => _owner.TextBox?.RaisePaste(e);

	private void NativeTextChanged(object? sender, EventArgs e)
	{
		// Avoid stack overflow as updating text from
		// shared code briefly sets empty string and causes
		// infinite loop
		if (_processingTextChanged)
		{
			return;
		}

		try
		{
			_processingTextChanged = true;
			_owner.UpdateTextFromNative(GetNativeText() ?? string.Empty);

		}
		finally
		{
			_processingTextChanged = false;
		}
	}

	private string? GetNativeText() => _textBoxView?.Text;

	private void SetNativeText(string text)
	{
		if (_textBoxView is null)
		{
			return;
		}

		// Avoid setting same text (as it raises WidgetTextChanged on GTK).
		if (_textBoxView.Text != text)
		{
			_textBoxView.Text = text;
		}
	}
}
