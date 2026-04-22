using System;
using CoreGraphics;
using Foundation;
using Microsoft.UI.Xaml.Controls;
using ObjCRuntime;
using UIKit;
using Uno.Extensions;
using Uno.UI.Extensions;
using Uno.UI.Xaml.Controls.Extensions;

namespace Uno.WinUI.Runtime.Skia.AppleUIKit.Controls;

internal partial class SinglelineInvisibleTextBoxView : UITextField, IInvisibleTextBoxView
{
	private readonly WeakReference<InvisibleTextBoxViewExtension> _textBoxViewExtension;
	private bool _settingTextFromManaged;
	private bool _settingSelectionFromManaged;

	public TextBoxView? Owner => TextBoxViewExtension?.Owner;

	public SinglelineInvisibleTextBoxView(InvisibleTextBoxViewExtension textBoxView)
	{
		if (textBoxView is null)
		{
			throw new ArgumentNullException(nameof(textBoxView));
		}

		_textBoxViewExtension = new WeakReference<InvisibleTextBoxViewExtension>(textBoxView);
		Alpha = 0;

		Initialize();
	}

	private void Initialize()
	{
		//Set native VerticalAlignment to top-aligned (default is center) to match Windows text placement
		base.VerticalAlignment = UIControlContentVerticalAlignment.Top;

		Delegate = new SinglelineInvisibleTextBoxDelegate(_textBoxViewExtension)
		{
			IsKeyboardHiddenOnEnter = true
		};
	}

	public bool IsCompatible(Microsoft.UI.Xaml.Controls.TextBox textBox) => !textBox.AcceptsReturn;

	// UIKit uses the first-responder's firstRect/caretRect (UITextInput) to
	// position the iPad floating numeric keypad. Returning the view's full
	// Bounds makes the keypad anchor adjacent to the whole NumberBox rather
	// than overlapping it when the field has non-default Padding or sits
	// near a screen edge. Gated by the same condition the extension uses
	// to anchor the native view (see InvisibleTextBoxViewExtension.
	// IsFloatingNumericKeypad) so other scenarios keep the managed caret
	// geometry used for IME candidate positioning.
	public override CGRect GetFirstRectForRange(UITextRange range)
	{
		if (InvisibleTextBoxViewExtension.IsFloatingNumericKeypad(KeyboardType))
		{
			return Bounds;
		}

		var caretRect = AppleUIKitImeTextBoxExtension.Instance.GetCaretRect();
		if (caretRect != Windows.Foundation.Rect.Empty && Superview is not null)
		{
			// GetCaretRect returns Uno logical coordinates (relative to the XamlRoot).
			// iOS expects the result in the view's own coordinate system, then uses the
			// view hierarchy to convert to screen coordinates for the candidate window.
			// Since our view is off-screen, convert from the superview's (window) coordinate
			// space into our local coordinate space.
			var windowRect = new CoreGraphics.CGRect(caretRect.X, caretRect.Y, caretRect.Width, caretRect.Height);
			return ConvertRectFromView(windowRect, Superview);
		}

		return base.GetFirstRectForRange(range);
	}

	public override CGRect GetCaretRectForPosition(UITextPosition? position)
		=> InvisibleTextBoxViewExtension.IsFloatingNumericKeypad(KeyboardType) ? Bounds : base.GetCaretRectForPosition(position);

	public override void Paste(NSObject? sender) => HandlePaste(() => base.Paste(sender));

	public override void PasteAndGo(NSObject? sender) => HandlePaste(() => base.PasteAndGo(sender));

	public override void PasteAndMatchStyle(NSObject? sender) => HandlePaste(() => base.PasteAndMatchStyle(sender));

	public override void PasteAndSearch(NSObject? sender) => HandlePaste(() => base.PasteAndSearch(sender));

#if !__TVOS__
	public override void Paste(NSItemProvider[] itemProviders) => HandlePaste(() => base.Paste(itemProviders));
#endif

	private void HandlePaste(Action baseAction)
	{
		var args = new TextControlPasteEventArgs();
		TextBoxViewExtension?.Owner.TextBox?.RaisePaste(args);
		if (!args.Handled)
		{
			baseAction.Invoke();
		}
	}

	public bool IsComposing => AppleUIKitImeTextBoxExtension.Instance.IsComposing;

	internal InvisibleTextBoxViewExtension TextBoxViewExtension => _textBoxViewExtension.GetTarget();

	public override string? Text
	{
		get => base.Text;
		set
		{
			// The native control will ignore a value of null and retain an empty string. We coalesce the null to prevent a spurious empty string getting bounced back via two-way binding.
			value ??= string.Empty;
			if (base.Text != value)
			{
				base.Text = value;
				OnTextChanged();
			}
		}
	}

	private void OnEditingChanged(object? sender, EventArgs e)
	{
		OnTextChanged();
	}

	private void OnTextChanged()
	{
		if (_settingTextFromManaged)
		{
			return;
		}

		if (_textBoxViewExtension?.GetTarget() is { } textBoxView)
		{
			textBoxView.ProcessNativeTextInput(Text);
		}
	}

	public void SetTextNative(string text)
	{
		try
		{
			_settingTextFromManaged = true;
			Text = text;
		}
		finally
		{
			_settingTextFromManaged = false;
		}
	}

	private void StartEditing()
	{
		this.EditingChanged += OnEditingChanged;
		this.EditingDidEnd += OnEditingChanged;
	}

	private void EndEditing()
	{
		this.EditingChanged -= OnEditingChanged;
		this.EditingDidEnd -= OnEditingChanged;
	}

	//Forces the secure UITextField to maintain its current value upon regaining focus
	public override bool BecomeFirstResponder()
	{
		var result = base.BecomeFirstResponder();

		if (SecureTextEntry)
		{
			var text = Text;
			SetTextNative(string.Empty); // Does not trigger TextChanged
			InsertText(text ?? ""); // Does not trigger Text setter
			OnTextChanged();
		}

		StartEditing();

		return result;
	}

	public override bool ResignFirstResponder()
	{
		var result = base.ResignFirstResponder();

		EndEditing();

		return result;
	}

	public void Select(int start, int length)
	{
		try
		{
			_settingSelectionFromManaged = true;
			SelectedTextRange = this.GetTextRange(start: start, end: start + length).GetHandle();
		}
		finally
		{
			_settingSelectionFromManaged = false;
		}
	}

	[Export("selectedTextRange")]
	public new IntPtr SelectedTextRange
	{
		get => NativeTextSelection.GetSelectedTextRange(SuperHandle);
		set
		{
			var textBoxView = TextBoxViewExtension;

			if (textBoxView != null && SelectedTextRange != value)
			{
				NativeTextSelection.SetSelectedTextRange(SuperHandle, value);
				if (!_settingSelectionFromManaged)
				{
					textBoxView.SyncSelectionToTextBox();
				}
			}
		}
	}

	#region IME Composition (UITextInput overrides)

	public override void SetMarkedText(string markedText, NSRange selectedRange)
	{
		markedText ??= string.Empty;
		AppleUIKitImeTextBoxExtension.Instance.OnSetMarkedText(markedText);
		base.SetMarkedText(markedText, selectedRange);
	}

	public new void InsertText(string text)
	{
		var wasComposing = AppleUIKitImeTextBoxExtension.Instance.IsComposing;
		base.InsertText(text);

		// Only fire composition events when completing an active IME composition
		// (SetMarkedText was called first). Regular native keystrokes and
		// BecomeFirstResponder's silent text restore should not trigger composition.
		if (wasComposing)
		{
			AppleUIKitImeTextBoxExtension.Instance.OnInsertText(text);
		}
	}

	public override void UnmarkText()
	{
		AppleUIKitImeTextBoxExtension.Instance.OnUnmarkText();
		base.UnmarkText();
	}


	#endregion
}
