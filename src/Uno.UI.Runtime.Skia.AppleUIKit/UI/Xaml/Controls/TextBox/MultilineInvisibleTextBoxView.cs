using System;
using Foundation;
using Microsoft.UI.Xaml.Controls;
using ObjCRuntime;
using UIKit;
using Uno.Extensions;

namespace Uno.WinUI.Runtime.Skia.AppleUIKit.Controls;

internal partial class MultilineInvisibleTextBoxView : UITextView, IInvisibleTextBoxView
{
	private readonly WeakReference<InvisibleTextBoxViewExtension> _textBoxViewExtension;
	private MultilineInvisibleTextBoxDelegate? _delegate;

	public MultilineInvisibleTextBoxView(InvisibleTextBoxViewExtension textBoxView)
	{
		if (textBoxView is null)
		{
			throw new ArgumentNullException(nameof(textBoxView));
		}

		_textBoxViewExtension = new WeakReference<InvisibleTextBoxViewExtension>(textBoxView);

		Initialize();
	}

	private void Initialize()
	{
		Delegate = _delegate = new MultilineInvisibleTextBoxDelegate(_textBoxViewExtension);
		BackgroundColor = UIColor.Clear;
		TextContainer.LineFragmentPadding = 0;

		// Reset the default margin of 8px at the top
		TextContainerInset = new UIEdgeInsets();
	}

	public bool IsCompatible(Microsoft.UI.Xaml.Controls.TextBox textBox) => textBox.AcceptsReturn;

	public override void Paste(NSObject? sender) => HandlePaste(() => base.Paste(sender));

	public override void PasteAndGo(NSObject? sender) => HandlePaste(() => base.PasteAndGo(sender));

	public override void PasteAndMatchStyle(NSObject? sender) => HandlePaste(() => base.PasteAndMatchStyle(sender));

	public override void PasteAndSearch(NSObject? sender) => HandlePaste(() => base.PasteAndSearch(sender));

	public override void Paste(NSItemProvider[] itemProviders) => HandlePaste(() => base.Paste(itemProviders));

	private void HandlePaste(Action baseAction)
	{
		var args = new TextControlPasteEventArgs();
		TextBoxViewExtension?.Owner.TextBox?.RaisePaste(args);
		if (!args.Handled)
		{
			baseAction.Invoke();
		}
	}

	internal InvisibleTextBoxViewExtension TextBoxViewExtension => _textBoxViewExtension.GetTarget();

	public override string? Text
	{
		get
		{
			return base.Text;
		}
		set
		{
			// The native control will ignore a value of null and retain an empty string. We coalesce the null to prevent a spurious empty string getting bounced back via two-way binding.
			value = value ?? string.Empty;
			if (base.Text != value)
			{
				base.Text = value;
				OnTextChanged();
			}
		}
	}

	public void SetTextNative(string text) => Text = text;

	internal void OnTextChanged()
	{
		if (_textBoxViewExtension?.GetTarget() is { } textBoxView)
		{
			textBoxView.ProcessNativeTextInput(Text);
		}
	}

	public void Select(int start, int length)
		=> SelectedTextRange = this.GetTextRange(start: start, end: start + length).GetHandle();

	/// <summary>
	/// Workaround for https://github.com/unoplatform/uno/issues/9430
	/// </summary>
	[Export("selectedTextRange")]
	public new IntPtr SelectedTextRange
	{
		get
		{
			return SinglelineInvisibleTextBoxView.IntPtr_objc_msgSendSuper(SuperHandle, Selector.GetHandle("selectedTextRange"));
		}
		set
		{
			var textBoxView = TextBoxViewExtension;

			if (textBoxView != null && SelectedTextRange != value)
			{
				SinglelineInvisibleTextBoxView.void_objc_msgSendSuper(SuperHandle, Selector.GetHandle("setSelectedTextRange:"), value);
				textBoxView.Owner.TextBox?.OnSelectionChanged();
			}
		}
	}
}
