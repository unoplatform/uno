using System;
using Foundation;
using Microsoft.UI.Xaml.Controls;
using ObjCRuntime;
using UIKit;
using Uno.Extensions;
using Uno.UI.Extensions;
using Windows.System;

namespace Uno.WinUI.Runtime.Skia.AppleUIKit.Controls;

internal partial class MultilineInvisibleTextBoxView : UITextView, IInvisibleTextBoxView
{
	private readonly WeakReference<InvisibleTextBoxViewExtension> _textBoxViewExtension;
	private bool _settingTextFromManaged;
	private bool _settingSelectionFromManaged;

	public TextBoxView? Owner => TextBoxViewExtension?.Owner;

	public MultilineInvisibleTextBoxView(InvisibleTextBoxViewExtension textBoxView)
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
		Delegate = new MultilineInvisibleTextBoxDelegate(_textBoxViewExtension);
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

	internal void OnTextChanged()
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

	public void Select(int start, int length)
	{
		try
		{
			_settingSelectionFromManaged = true;
			SelectedTextRange = this.GetTextRange(start: start, end: start + length).GetHandle();
		}
		finally
		{
			_settingTextFromManaged = false;
		}
	}

	/// <summary>
	/// Workaround for https://github.com/unoplatform/uno/issues/9430
	/// </summary>
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
					textBoxView.Owner.TextBox?.OnSelectionChanged();
				}
			}
		}
	}

	public override void PressesBegan(NSSet<UIPress> presses, UIPressesEvent evt)
	{
		base.PressesBegan(presses, evt);

		if (Owner?.TextBox is { })
		{
			foreach (UIPress press in presses)
			{
				if (press.Key is not null)
				{
					var virtualKey = VirtualKeyHelper.FromKeyCode(press.Key.KeyCode);

					if (IsNavigationKey(virtualKey))
					{
						SyncSelectionToTextBox();
					}
				}
			}
		}
	}

	public override void PressesEnded(NSSet<UIPress> presses, UIPressesEvent evt)
	{
		base.PressesEnded(presses, evt);

		if (Owner?.TextBox is { })
		{
			foreach (UIPress press in presses)
			{
				if (press.Key is not null)
				{
					var virtualKey = VirtualKeyHelper.FromKeyCode(press.Key.KeyCode);

					if (IsNavigationKey(virtualKey))
					{
						SyncSelectionToTextBox();
					}
				}
			}
		}
	}

	private static bool IsNavigationKey(VirtualKey key)
	{
		return key switch
		{
			VirtualKey.Left or VirtualKey.Right or VirtualKey.Up or VirtualKey.Down or
			VirtualKey.Home or VirtualKey.End or
			VirtualKey.PageUp or VirtualKey.PageDown => true,
			_ => false
		};
	}

	private void SyncSelectionToTextBox()
	{
		if (TextBoxViewExtension?.Owner.TextBox is { } textBox && base.SelectedTextRange is { } range)
		{
			var selectedRange = this.SelectedRange;

			textBox.SelectionStart = (int)selectedRange.Location;
			textBox.SelectionLength = (int)selectedRange.Length;
		}
	}
}
