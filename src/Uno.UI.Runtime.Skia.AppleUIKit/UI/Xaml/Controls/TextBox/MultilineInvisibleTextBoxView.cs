//using System;
//using CoreGraphics;
//using Foundation;
//using Microsoft.UI.Xaml;
//using Microsoft.UI.Xaml.Controls;
//using Microsoft.UI.Xaml.Media;
//using ObjCRuntime;
//using UIKit;
//using Uno.Extensions;
//using Uno.UI.Extensions;
//using Windows.UI.Core;

//namespace Uno.WinUI.Runtime.Skia.AppleUIKit.Controls;

//public partial class MultilineInvisibleTextBoxView : UITextView, IInvisibleTextBoxView
//{
//	private MultilineInvisibleTextBoxDelegate _delegate;
//	private readonly WeakReference<TextBoxView> _textBoxView;
//	private WeakReference<Uno.UI.Controls.Window> _window;

//	public MultilineInvisibleTextBoxView(TextBoxView textBoxView)
//	{
//		_textBoxView = new WeakReference<TextBox>(textBoxView);
//		_window = new WeakReference<Uno.UI.Controls.Window>(textBox.FindFirstParent<Uno.UI.Controls.Window>());
//		Initialize();
//	}

//	public override void Paste(NSObject sender) => HandlePaste(() => base.Paste(sender));

//	public override void PasteAndGo(NSObject sender) => HandlePaste(() => base.PasteAndGo(sender));

//	public override void PasteAndMatchStyle(NSObject sender) => HandlePaste(() => base.PasteAndMatchStyle(sender));

//	public override void PasteAndSearch(NSObject sender) => HandlePaste(() => base.PasteAndSearch(sender));

//	public override void Paste(NSItemProvider[] itemProviders) => HandlePaste(() => base.Paste(itemProviders));

//	private void HandlePaste(Action baseAction)
//	{
//		var args = new TextControlPasteEventArgs();
//		var textBox = _textBoxView.GetTarget();
//		textBox?.RaisePaste(args);
//		if (!args.Handled)
//		{
//			baseAction.Invoke();
//		}
//	}

//	private void Initialize()
//	{
//		Delegate = _delegate = new MultilineInvisibleTextBoxDelegate(_textBoxView);
//		BackgroundColor = UIColor.Clear;
//		TextContainer.LineFragmentPadding = 0;

//		// Reset the default margin of 8px at the top
//		TextContainerInset = new UIEdgeInsets();
//	}

//	public override string Text
//	{
//		get
//		{
//			return base.Text;
//		}
//		set
//		{
//			// The native control will ignore a value of null and retain an empty string. We coalesce the null to prevent a spurious empty string getting bounced back via two-way binding.
//			value = value ?? string.Empty;
//			if (base.Text != value)
//			{
//				base.Text = value;
//				OnTextChanged();
//			}
//		}
//	}

//	public void SetTextNative(string text) => Text = text;

//	internal void OnTextChanged()
//	{
//		var textBox = _textBoxView?.GetTarget();
//		if (textBox != null)
//		{
//			var text = textBox.ProcessTextInput(Text);
//			SetTextNative(text);
//		}

//		SetNeedsLayout();
//	}

//	/// <summary>
//	/// Workaround for https://github.com/unoplatform/uno/issues/9430
//	/// </summary>
//	[Export("selectedTextRange")]
//	public new IntPtr SelectedTextRange
//	{
//		get
//		{
//			return SinglelineInvisibleTextBoxView.IntPtr_objc_msgSendSuper(SuperHandle, Selector.GetHandle("selectedTextRange"));
//		}
//		set
//		{
//			if (SelectedTextRange != value)
//			{
//				SinglelineInvisibleTextBoxView.void_objc_msgSendSuper(SuperHandle, Selector.GetHandle("setSelectedTextRange:"), value);
//				_textBoxView.GetTarget()?.OnSelectionChanged();
//			}
//		}
//	}

//	public void Select(int start, int length)
//		=> SelectedTextRange = this.GetTextRange(start: start, end: start + length).GetHandle();
//}
