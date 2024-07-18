using System;
using System.Diagnostics.CodeAnalysis;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using UIKit;
using Uno.UI.Extensions;
using Uno.UI.Runtime.Skia.AppleUIKit;
using Uno.UI.Xaml.Controls.Extensions;
using Uno.WinUI.Runtime.Skia.AppleUIKit.Controls;

namespace Uno.WinUI.Runtime.Skia.AppleUIKit;

internal class InvisibleTextBoxViewExtension : IOverlayTextBoxViewExtension
{
	private readonly TextBoxView _owner;
	private IInvisibleTextBoxView? _textBoxView;

	public InvisibleTextBoxViewExtension(TextBoxView view)
	{
		_owner = view;
	}

	internal TextBoxView Owner => _owner;

	public bool IsOverlayLayerInitialized(XamlRoot xamlRoot) => true;

	public void StartEntry()
	{
		// StartEntry can be called twice without any EndEntry.
		// So,  do nothing if we already have non-null _nativeEditText.
		// This happens when the managed TextBox receives Focus with two different `FocusState`s (e.g, Programmatic and Keyboard/Pointer)
		if (_textBoxView is not null)
		{
			return;
		}

		var textBox = _owner.TextBox;
		if (textBox is null || textBox.XamlRoot is null)
		{
			return;
		}

		EnsureTextBoxView(textBox);
		AddViewToTextInputLayer(textBox.XamlRoot);

		_textBoxView.BecomeFirstResponder();

		var start = textBox?.SelectionStart ?? 0;
		var length = textBox?.SelectionLength ?? 0;
		_textBoxView.Select(start, length);
	}

	public void EndEntry()
	{
		if (_textBoxView is not null)
		{
			RemoveViewFromTextInputLayer();
			_textBoxView = null;
		}
	}

	public void UpdateSize() => InvalidateLayout();

	public void UpdatePosition() => InvalidateLayout();

	public void InvalidateLayout()
	{
		if (_textBoxView is not null)
		{
			//var width = _view.DisplayBlock.ActualWidth;
			//var height = _view.DisplayBlock.ActualHeight;

			//var position = _view.DisplayBlock.TransformToVisual(null).TransformPoint(default);
			//var rect = new Rect(position.X, position.Y, width, height);
			//var physical = rect.LogicalToPhysicalPixels();
			//_nativeInput.Layout(
			//	(int)physical.Left,
			//	(int)physical.Top,
			//	(int)physical.Right,
			//	(int)physical.Bottom
			//);
		}
	}

	public void SetText(string text)
	{
		if (_textBoxView is not null)
		{
			try
			{
				//_textBoxView.SuspendSelectionChange();
				_textBoxView.Text = text;
			}
			finally
			{
				//_textBoxView.ResumeSelectionChange();
			}
		}
	}

	public int GetSelectionLength()
	{
		if (_textBoxView?.SelectedTextRange == null)
		{
			return 0;
		}

		return (int)_textBoxView.GetOffsetFromPosition(
			_textBoxView.SelectedTextRange.Start,
			_textBoxView.SelectedTextRange.End
		);
	}

	public int GetSelectionLengthBeforeKeyDown() => GetSelectionLength();

	public int GetSelectionStart()
	{
		if (_textBoxView?.SelectedTextRange == null || _textBoxView?.BeginningOfDocument == null)
		{
			return 0;
		}

		return (int)_textBoxView.GetOffsetFromPosition(
			_textBoxView.BeginningOfDocument,
			_textBoxView.SelectedTextRange.Start
		);
	}

	public int GetSelectionStartBeforeKeyDown() => GetSelectionStart();

	public void Select(int start, int length) => _textBoxView?.Select(start, length);

	public void SetPasswordRevealState(PasswordRevealState passwordRevealState) { }

	public void UpdateNativeView()
	{
		if (_owner.TextBox is { } textBox)
		{
			EnsureTextBoxView(textBox);
		}
	}

	public void UpdateProperties() { }

	[MemberNotNull(nameof(_textBoxView))]
	private void EnsureTextBoxView(TextBox textBox)
	{
		if (_textBoxView is null ||
			!_textBoxView.IsCompatible(textBox))
		{
			// The current TextBoxView is not compatible with the given TextBox state.
			// We need to create a new TextBoxView.
			var inputText = GetNativeText() ?? textBox.Text;
			_textBoxView = CreateNativeView(textBox);
			SetNativeText(inputText ?? string.Empty);
		}
	}

	internal void ProcessNativeTextInput(string? text)
	{
		if (_owner?.TextBox is { } textBox)
		{
			var selectionStart = textBox.SelectionStart;
			var selectionLength = textBox.SelectionLength;

			var newSelectionStart = GetSelectionStart();
			textBox.SetPendingSelection(newSelectionStart, 0);
			var updatedText = textBox.ProcessTextInput(text);
			if (text != updatedText)
			{
				SetNativeText(updatedText);
			}
		}
	}

	private string? GetNativeText() => _textBoxView?.Text;

	private void SetNativeText(string text)
	{
		if (_textBoxView is null)
		{
			return;
		}

		if (_textBoxView.Text != text)
		{
			_textBoxView.Text = text;
		}
	}

	private IInvisibleTextBoxView CreateNativeView(TextBox textBox) => new SinglelineInvisibleTextBoxView(this);

	public void AddViewToTextInputLayer(XamlRoot xamlRoot)
	{
		if (_textBoxView is not UIView nativeView)
		{
			return;
		}

		if (GetOverlayLayer(xamlRoot) is { } layer && nativeView.Superview != layer)
		{
			layer.AddSubview(nativeView);
		}
	}

	public void RemoveViewFromTextInputLayer()
	{
		if (_textBoxView is not UIView nativeView)
		{
			return;
		}

		if (nativeView.Superview is not null)
		{
			nativeView.RemoveFromSuperview();
		}
	}

	internal static UIView? GetOverlayLayer(XamlRoot xamlRoot) =>
		AppManager.XamlRootMap.GetHostForRoot(xamlRoot)?.TextInputLayer;
}
