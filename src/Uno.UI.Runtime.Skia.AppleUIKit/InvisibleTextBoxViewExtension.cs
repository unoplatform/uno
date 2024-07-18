using System;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using UIKit;
using Uno.UI.Runtime.Skia.AppleUIKit;
using Uno.UI.Xaml.Controls.Extensions;
using Uno.UI.Xaml.Core;
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

	public bool IsOverlayLayerInitialized(XamlRoot xamlRoot) => true;

	public void StartEntry()
	{
		// StartEntry can be called twice without any EndEntry.
		// So,  do nothing if we already have non-null _nativeEditText.
		// This happens when the managed TextBox receives Focus with two different `FocusState`s (e.g, Programmatic and Keyboard/Pointer)
		if (_nativeInput is not null)
		{
			return;
		}

		var textBox = _owner.TextBox;

		_nativeInput = new SinglelineInvisibleTextBoxView(_owner);
		//var relativeLayout = ApplicationActivity.Instance.RelativeLayout;
		//relativeLayout.AddView(_nativeInput);
		//_nativeInput.RequestFocus();

		//var start = textBox?.SelectionStart ?? 0;
		//var length = textBox?.SelectionLength ?? 0;
		//_nativeInput.SetSelection(start, start + length);
		//InvalidateLayout();

		//InputMethodManager imm = (InputMethodManager)ContextHelper.Current.GetSystemService(Context.InputMethodService)!;
		//imm.ShowSoftInput(_nativeEditText, ShowFlags.Implicit);
	}

	public void EndEntry()
	{
		if (_textBoxView is not null)
		{
			//InputMethodManager imm = (InputMethodManager)ContextHelper.Current.GetSystemService(Context.InputMethodService)!;
			//imm.HideSoftInputFromWindow(_nativeInput.WindowToken, HideSoftInputFlags.None);

			//ApplicationActivity.Instance.RelativeLayout.RemoveView(_nativeInput);
			//_nativeInput = null;
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
				_textBoxView.SuspendSelectionChange();
				_textBoxView.Text = text;
			}
			finally
			{
				_textBoxView.ResumeSelectionChange();
			}
		}
	}

	public int GetSelectionLength() => throw new NotImplementedException();
	public int GetSelectionLengthBeforeKeyDown() => throw new NotImplementedException();
	public int GetSelectionStart() => throw new NotImplementedException();
	public int GetSelectionStartBeforeKeyDown() => throw new NotImplementedException();
	public void Select(int start, int length)
	{
	}
	public void SetPasswordRevealState(PasswordRevealState passwordRevealState) { }

	public void UpdateNativeView()
	{
		if (_owner.TextBox is { } textBox)
		{
			EnsureTextBoxView(textBox);
		}
	}

	public void UpdateProperties() { }

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

	private IInvisibleTextBoxView CreateNativeView(TextBox textBox) =>
		!textBox.AcceptsReturn ?
			new SinglelineInvisibleTextBoxView(_owner) :
			new MultilineInvisibleTextBoxView(_owner);

	public void AddToTextInputLayer(XamlRoot xamlRoot)
	{
		if (GetOverlayLayer(xamlRoot) is { } layer && RootElement.Parent != layer)
		{
			layer.Children.Add(RootElement);
			DataObject.AddPastingHandler(RootElement, PasteHandler);
		}
	}

	public void RemoveFromTextInputLayer()
	{
		if (RootElement.Parent is WpfCanvas layer)
		{
			layer.Children.Remove(RootElement);
			DataObject.RemovePastingHandler(RootElement, PasteHandler);
		}
	}

	internal static UIView? GetOverlayLayer(XamlRoot xamlRoot) =>
		AppManager.XamlRootMap.GetHostForRoot(xamlRoot)?.NativeOverlayLayer;
}
