using System;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using UIKit;
using Uno.UI.Xaml.Controls.Extensions;

namespace Uno.WinUI.Runtime.Skia.AppleUIKit;

internal class InvisibleTextBoxViewExtension : IOverlayTextBoxViewExtension
{
	private readonly TextBoxView _view;
	private InvisibleUITextView? _nativeInput;

	public InvisibleTextBoxViewExtension(TextBoxView view)
	{
		_view = view;
	}

	private sealed class InvisibleUITextView : UITextField
	{
		private readonly TextBoxView _owner;
		private int _selectionChangeSuspended;

		public InvisibleUITextView(TextBoxView owner)
		{
			_owner = owner;
			BackgroundColor = UIColor.Clear;
			TextColor = UIColor.Clear;

			Text = owner.TextBox?.Text ?? string.Empty;
		}

		internal void SuspendSelectionChange() => _selectionChangeSuspended++;

		internal void ResumeSelectionChange() => _selectionChangeSuspended--;
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

		var textBox = _view.TextBox;

		_nativeInput = new InvisibleUITextView(_view);
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
		if (_nativeInput is not null)
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
		if (_nativeInput is not null)
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
		if (_nativeInput is not null)
		{
			try
			{
				_nativeInput.SuspendSelectionChange();
				_nativeInput.Text = text;
			}
			finally
			{
				_nativeInput.ResumeSelectionChange();
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

	public void UpdateNativeView() { }

	public void UpdateProperties() { }
}
