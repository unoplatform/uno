using Foundation;
using Uno.Extensions;
using System;
using System.Linq;
using UIKit;
using Windows.UI.Core;
using System.Threading.Tasks;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml;

namespace Uno.WinUI.Runtime.Skia.AppleUIKit.Controls;

internal partial class SinglelineInvisibleTextBoxDelegate : UITextFieldDelegate
{
	private readonly WeakReference<InvisibleTextBoxViewExtension> _textBoxViewExtension;

	public SinglelineInvisibleTextBoxDelegate(WeakReference<InvisibleTextBoxViewExtension> textBoxViewExtension)
	{
		_textBoxViewExtension = textBoxViewExtension ?? throw new ArgumentNullException(nameof(textBoxViewExtension));
	}

	public bool IsKeyboardHiddenOnEnter
	{
		get;
		set;
	}

	public override bool ShouldChangeCharacters(UITextField textField, NSRange range, string replacementString)
	{
		if (textField is SinglelineInvisibleTextBoxView textBoxView)
		{
			if (_textBoxViewExtension.GetTarget()?.Owner.TextBox is not TextBox textBox)
			{
				return false;
			}

			// Both IsReadOnly = true and IsTabStop = false can prevent editing
			if (textBox.IsReadOnly || !textBox.IsTabStop)
			{
				return false;
			}

			// TODO:MZ:
			//if (textBox.OnKey(replacementString.FirstOrDefault()))
			//{
			//	return false;
			//}

			if (textBox.MaxLength > 0)
			{
				// When replacing text from pasting (multiple characters at once)
				// we should only allow it (return true) when the new text length
				// is lower or equal to the allowed length (TextBox.MaxLength)
				var newLength = (textBoxView.Text?.Length ?? 0) + replacementString.Length - range.Length;
				return newLength <= textBox.MaxLength;
			};
		}

		return true;
	}

	public override bool ShouldReturn(UITextField textField)
	{
		if (IsKeyboardHiddenOnEnter)
		{
			_ = CoreDispatcher.Main.RunAsync(CoreDispatcherPriority.Normal,
				async () =>
				{
					// Delay losing focus to avoid concurrent interactions when transferring focus to another control. See 101152
					await Task.Delay(TimeSpan.FromMilliseconds(50));
					textField.ResignFirstResponder();
				});
		}

		var textBox = textField as SinglelineInvisibleTextBoxView;
		if (textBox != null)
		{
			// TODO:MZ:
			//if (_textBoxView.GetTarget()?.OnKey('\n') ?? false)
			//{
			//	return false;
			//}
		}

		return true;
	}

	/// <summary>
	/// Corresponds to a gain of focus
	/// </summary>
	public override void EditingStarted(UITextField textField)
	{
		if (_textBoxViewExtension.GetTarget()?.Owner.TextBox is TextBox textBox && textBox.FocusState == FocusState.Unfocused)
		{
			textBox.Focus(FocusState.Pointer);
		}
	}

	/// <summary>
	/// Corresponds to a loss of focus
	/// </summary>
	public override void EditingEnded(UITextField textField)
	{
		if (_textBoxViewExtension.GetTarget()?.Owner.TextBox is TextBox { FocusState: not FocusState.Unfocused } textBox)
		{
			textBox.Unfocus();
		}
	}
}
