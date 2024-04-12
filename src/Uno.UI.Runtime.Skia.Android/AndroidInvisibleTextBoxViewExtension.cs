using System.Runtime.InteropServices.JavaScript;
using Android.Content;
using Android.Graphics;
using Android.Runtime;
using Android.Text;
using Android.Text.Method;
using Android.Views;
using Android.Views.InputMethods;
using Android.Widget;
using Java.Interop;
using Java.Lang;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Uno.UI.Xaml.Controls.Extensions;

using Rect = Windows.Foundation.Rect;

namespace Uno.UI.Runtime.Skia;

internal partial class AndroidInvisibleTextBoxViewExtension : IOverlayTextBoxViewExtension
{
	private sealed class InvisibleEditText : EditText
	{
		private readonly TextBoxView _owner;
		private int _selectionChangeSuspended;

		internal void SuspendSelectionChange()
			=> _selectionChangeSuspended++;

		internal void ResumeSelectionChange()
			=> _selectionChangeSuspended--;

		public InvisibleEditText(TextBoxView owner) : base(ContextHelper.Current)
		{
			SetBackgroundColor(Color.Transparent);
			SetTextColor(Color.Transparent);
			SetHighlightColor(Color.Transparent);
			SetPadding(0, 0, 0, 0);
			SetCursorVisible(false);
			if (owner.IsPasswordBox)
			{
				InputType = InputTypes.TextVariationPassword;
			}
			else
			{
				InputType = InputTypes.TextFlagNoSuggestions;
			}

			Text = owner.TextBox?.Text ?? string.Empty;
			_owner = owner;
		}

		public override bool OnKeyDown([GeneratedEnum] Keycode keyCode, KeyEvent? e)
		{
			return true;
		}

		protected override void OnTextChanged(ICharSequence? text, int start, int lengthBefore, int lengthAfter)
		{
			base.OnTextChanged(text, start, lengthBefore, lengthAfter);

			if (_selectionChangeSuspended > 0)
			{
				return;
			}

			if (_owner?.TextBox is { } textBox)
			{
				var selectionStart = textBox.SelectionStart;
				var selectionLength = textBox.SelectionLength;

				var selectionEnd = selectionStart + selectionLength;
				var distanceFromEnd = lengthBefore - selectionEnd;

				var newSelectionStart = lengthAfter - distanceFromEnd;
				textBox.SetPendingSelection(newSelectionStart, 0);
				textBox.Text = text?.ToString() ?? string.Empty;
			}
		}
	}

	private readonly TextBoxView _view;
	private InvisibleEditText? _nativeEditText;

	public AndroidInvisibleTextBoxViewExtension(TextBoxView view)
	{
		_view = view;
	}

	// TODO: When native EditText changes its text, we should sync it with managed one.

	public bool IsOverlayLayerInitialized(XamlRoot xamlRoot) => true;

	public void StartEntry()
	{
		// StartEntry can be called twice without any EndEntry.
		// So,  do nothing if we already have non-null _nativeEditText.
		// This happens when the managed TextBox receives Focus with two different `FocusState`s (e.g, Programmatic and Keyboard/Pointer)
		if (_nativeEditText is not null)
		{
			return;
		}

		var textBox = _view.TextBox;

		_nativeEditText = new InvisibleEditText(_view);
		var relativeLayout = ApplicationActivity.Instance.RelativeLayout;
		relativeLayout.AddView(_nativeEditText);
		_nativeEditText.RequestFocus();

		var start = textBox?.SelectionStart ?? 0;
		var length = textBox?.SelectionLength ?? 0;
		_nativeEditText.SetSelection(start, start + length);
		InvalidateLayout();

		InputMethodManager imm = (InputMethodManager)ContextHelper.Current.GetSystemService(Context.InputMethodService)!;
		imm.ShowSoftInput(_nativeEditText, ShowFlags.Implicit);
	}

	public void EndEntry()
	{
		if (_nativeEditText is not null)
		{
			InputMethodManager imm = (InputMethodManager)ContextHelper.Current.GetSystemService(Context.InputMethodService)!;
			imm.HideSoftInputFromWindow(_nativeEditText.WindowToken, HideSoftInputFlags.None);

			ApplicationActivity.Instance.RelativeLayout.RemoveView(_nativeEditText);
			_nativeEditText = null;
		}
	}

	public void UpdateSize()
		=> InvalidateLayout();

	public void UpdatePosition()
		=> InvalidateLayout();

	public void InvalidateLayout()
	{
		if (_nativeEditText is not null)
		{
			var width = _view.DisplayBlock.ActualWidth;
			var height = _view.DisplayBlock.ActualHeight;

			var position = _view.DisplayBlock.TransformToVisual(null).TransformPoint(default);
			var rect = new Rect(position.X, position.Y, width, height);
			var physical = rect.LogicalToPhysicalPixels();
			_nativeEditText.Layout(
				(int)physical.Left,
				(int)physical.Top,
				(int)physical.Right,
				(int)physical.Bottom
			);
		}
	}

	public void SetText(string text)
	{
		if (_nativeEditText is not null)
		{
			try
			{
				_nativeEditText.SuspendSelectionChange();
				_nativeEditText.Text = text;
			}
			finally
			{
				_nativeEditText.ResumeSelectionChange();
			}
		}
	}

	public void Select(int start, int length)
	{
		_nativeEditText?.SetSelection(start, start + length);
	}

	public void UpdateNativeView() { }
	public void SetPasswordRevealState(PasswordRevealState passwordRevealState) { }
	public void UpdateProperties() { }
	public int GetSelectionStart() => 0;
	public int GetSelectionLength() => 0;
	public int GetSelectionStartBeforeKeyDown() => 0;
	public int GetSelectionLengthBeforeKeyDown() => 0;
}

//internal class KeyListener : IKeyListener
//{
//	public InputTypes InputType => throw new System.NotImplementedException();

//	public nint Handle => throw new System.NotImplementedException();

//	public int JniIdentityHashCode => throw new System.NotImplementedException();

//	public JniObjectReference PeerReference => throw new System.NotImplementedException();

//	public JniPeerMembers JniPeerMembers => throw new System.NotImplementedException();

//	public JniManagedPeerStates JniManagedPeerState => throw new System.NotImplementedException();

//	public void ClearMetaKeyState(View? view, IEditable? content, [GeneratedEnum] MetaKeyStates states) => throw new System.NotImplementedException();
//	public void Dispose() => throw new System.NotImplementedException();
//	public void Disposed() => throw new System.NotImplementedException();
//	public void DisposeUnlessReferenced() => throw new System.NotImplementedException();
//	public void Finalized() => throw new System.NotImplementedException();
//	public bool OnKeyDown(View? view, IEditable? text, [GeneratedEnum] Keycode keyCode, KeyEvent? e) => throw new System.NotImplementedException();
//	public bool OnKeyOther(View? view, IEditable? text, KeyEvent? e) => throw new System.NotImplementedException();
//	public bool OnKeyUp(View? view, IEditable? text, [GeneratedEnum] Keycode keyCode, KeyEvent? e) => throw new System.NotImplementedException();
//	public void SetJniIdentityHashCode(int value) => throw new System.NotImplementedException();
//	public void SetJniManagedPeerState(JniManagedPeerStates value) => throw new System.NotImplementedException();
//	public void SetPeerReference(JniObjectReference reference) => throw new System.NotImplementedException();
//	public void UnregisterFromRuntime() => throw new System.NotImplementedException();
//}
