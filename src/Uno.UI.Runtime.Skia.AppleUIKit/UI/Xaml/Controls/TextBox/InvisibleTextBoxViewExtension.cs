using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using UIKit;
using Uno.UI;
using Uno.UI.Hosting;
using Uno.UI.Runtime.Skia.AppleUIKit;
using Uno.UI.Runtime.Skia.AppleUIKit.Hosting;
using Uno.UI.Xaml.Controls;
using Uno.UI.Xaml.Controls.Extensions;
using Uno.WinUI.Runtime.Skia.AppleUIKit.Controls;

namespace Uno.WinUI.Runtime.Skia.AppleUIKit;

internal class InvisibleTextBoxViewExtension : IOverlayTextBoxViewExtension
{
	private readonly TextBoxView _owner;
	private UIView? _latestNativeView;
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
		// This happens when the managed TextBox receives Focus
		// with two different `FocusState`s (e.g, Programmatic and Keyboard/Pointer)
		if (_textBoxView is not null)
		{
			if (!_textBoxView.IsFirstResponder)
			{
				_textBoxView.BecomeFirstResponder();
			}
			return;
		}

		var textBox = _owner.TextBox;
		if (textBox is null || textBox.XamlRoot is null)
		{
			return;
		}

		EnsureTextBoxView(textBox);
		SetSoftKeyboardTheme();

		AddViewToTextInputLayer(textBox.XamlRoot);

		// change FirstResponder's View before removing the previous view to avoid flickering
		_textBoxView.BecomeFirstResponder();

		RemovePreviousViewFromTextInputLayer();

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
		if (_textBoxView is UIView nativeView)
		{
			UpdateNativeViewFrame(nativeView);
		}
	}

	public void SetText(string text)
	{
		if (_textBoxView is not null)
		{
			// In managed, we only use \r and convert all \n's to \r's to
			// match WinUI, so when copying from managed to native, we convert
			// \r to \n so that in the case of typing two newlines in a row,
			// we get \n\n from native and not \r\n (the first converted by
			// managed, the second was just typed
			// before conversion) which looks like a single newline.
			// cf. https://github.com/unoplatform/uno-private/issues/965
			text = text.Replace('\r', '\n');
			_textBoxView.SetTextNative(text);
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

	public void UpdateNativeView() => UpdateProperties();

	public void UpdateProperties()
	{
		if (_textBoxView is null || _owner.TextBox is not { } textBox)
		{
			return;
		}

		_textBoxView.AutocapitalizationType = InputScopeHelper.ConvertInputScopeToCapitalization(textBox.InputScope);
		_textBoxView.KeyboardType = InputScopeHelper.ConvertInputScopeToKeyboardType(textBox.InputScope);

		// Apply the iOS 26 number-pad-popover opt-out after KeyboardType is set so the iPad +
		// numeric-keyboard gate is evaluated against the final value. Single-line only; the
		// popover is a UITextField behavior and does not apply to UITextView (multiline).
		if (_textBoxView is SinglelineInvisibleTextBoxView singleline)
		{
			singleline.TryDisableNumberPadPopover();
		}

		_textBoxView.SpellCheckingType = textBox.IsSpellCheckEnabled ? UITextSpellCheckingType.Yes : UITextSpellCheckingType.No;
		_textBoxView.AutocorrectionType = textBox.IsSpellCheckEnabled ? UITextAutocorrectionType.Yes : UITextAutocorrectionType.No;

		var inputReturnType = TextBoxExtensions.GetInputReturnType(textBox);
		_textBoxView.ReturnKeyType = inputReturnType.ToUIReturnKeyType();

		if (textBox.IsSpellCheckEnabled)
		{
			_textBoxView.AutocapitalizationType = UITextAutocapitalizationType.Sentences;
		}

		_textBoxView.SecureTextEntry = textBox is PasswordBox;
		SetSoftKeyboardTheme();

		// KeyboardType may have changed — re-evaluate the native view
		// position (anchored vs. off-screen).
		if (_textBoxView is UIView nativeView)
		{
			UpdateNativeViewFrame(nativeView);
		}
	}

	private void SetSoftKeyboardTheme()
	{
		if (_owner.TextBox is not { } textBox || _textBoxView is null)
		{
			return;
		}

		if (textBox.ActualTheme == ElementTheme.Default)
		{
			_textBoxView.KeyboardAppearance = UIKeyboardAppearance.Default;
		}
		else if (textBox.ActualTheme == ElementTheme.Light)
		{
			_textBoxView.KeyboardAppearance = UIKeyboardAppearance.Light;
		}
		else if (textBox.ActualTheme == ElementTheme.Dark)
		{
			_textBoxView.KeyboardAppearance = UIKeyboardAppearance.Dark;
		}
	}

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
			if (_textBoxView is UIView nativeView)
			{
				nativeView.Alpha = 0.01f;
			}
			UpdateProperties();
			SetText(inputText ?? string.Empty);
		}
	}

	internal void SyncSelectionToTextBox()
	{
		if (_owner?.TextBox is { } textBox)
		{
			var start = GetSelectionStart();
			var length = GetSelectionLength();
			textBox.SelectInternal(start, length);
		}
	}

	internal void ProcessNativeTextInput(string? text)
	{
		// During IME composition, text updates are managed by the shared
		// TextBox.skia.cs composition handlers via IImeTextBoxExtension events.
		// Suppress the normal text processing path to prevent double processing.
		if (_textBoxView?.IsComposing == true)
		{
			return;
		}

		if (_owner?.TextBox is { } textBox)
		{
			var selectionStart = textBox.SelectionStart;
			var selectionLength = textBox.SelectionLength;

			var newSelectionStart = GetSelectionStart();
			textBox.SetPendingSelection(newSelectionStart, 0);
			var updatedText = textBox.ProcessTextInput(text);
			if (text != updatedText)
			{
				SetText(updatedText);
			}
		}
	}

	private string? GetNativeText() => _textBoxView?.Text;

	private IInvisibleTextBoxView CreateNativeView(TextBox textBox) => _owner?.TextBox?.AcceptsReturn != true ?
		new SinglelineInvisibleTextBoxView(this) : new MultilineInvisibleTextBoxView(this);

	public void AddViewToTextInputLayer(XamlRoot xamlRoot)
	{
		if (_textBoxView is not UIView nativeView)
		{
			return;
		}

		if (GetOverlayLayer(xamlRoot) is { } layer && nativeView.Superview != layer)
		{
			var view = layer.Subviews.LastOrDefault();

			// prevents adding the same native view multiple times. This should not happen very often.
			if ((view as IInvisibleTextBoxView)?.Owner?.TextBox != _textBoxView?.Owner?.TextBox)
			{
				_latestNativeView = view;
				layer.AddSubview(nativeView);

				UpdateNativeViewFrame(nativeView);
			}
		}
	}

	public void RemoveViewFromTextInputLayer()
	{
		var xamlRoot = _owner.TextBox?.XamlRoot;
		if (xamlRoot is null)
		{
			return;
		}

		var focusingView = FocusManager.GetFocusingElement(xamlRoot) as FrameworkElement;
		if (CouldBecomeFirstResponder(focusingView))
		{
			return;
		}

		if (_textBoxView is not UIView nativeView)
		{
			return;
		}

		if (nativeView.Superview is not null)
		{
			nativeView.RemoveFromSuperview();
		}
	}

	private void RemovePreviousViewFromTextInputLayer()
	{
		if (_latestNativeView is not UIView nativeView)
		{
			return;
		}

		if (nativeView.Superview is not null)
		{
			nativeView.RemoveFromSuperview();
			_latestNativeView = null;
		}
	}

	private void UpdateNativeViewFrame(UIView nativeView)
	{
		var textBox = _textBoxView?.Owner?.TextBox;
		var rect = textBox?.GetAbsoluteBoundsRect();
		// GetAbsoluteBoundsRect returns WinUI DIPs which map 1:1 to iOS
		// points.  Do NOT convert to physical pixels — UIView.Frame is in
		// points, not physical pixels.
		var x = rect?.X ?? 0;
		var y = rect?.Y ?? 0;
		var width = rect?.Width ?? 10;
		var height = rect?.Height ?? 10;

		// Only iPad shows a floating numeric keypad that needs an anchor
		// view. For all other cases we push the native view off-screen so
		// that the iOS autocorrect/suggestion bubble does not leak over
		// the Skia-rendered text.
		if (ShouldAnchorToTextBox())
		{
			nativeView.Frame = new CoreGraphics.CGRect(x, y, width, height);
		}
		else
		{
			nativeView.Frame = new CoreGraphics.CGRect(-1000 - width, -1000 - height, width, height);
		}
	}

	private bool ShouldAnchorToTextBox()
		=> _textBoxView is { } view && IsFloatingNumericKeypad(view.KeyboardType);

	// Returns true when the textfield is on iPad with a numeric keyboard type —
	// the configuration where iOS displays (or would display) the floating
	// number-pad popover. Used by TryDisableNumberPadPopover to decide whether
	// the setAllowsNumberPadPopover: opt-out is relevant for this field.
	internal static bool IsIPadNumericKeyboard(UIKeyboardType keyboardType)
	{
		if (UIDevice.CurrentDevice.UserInterfaceIdiom != UIUserInterfaceIdiom.Pad)
		{
			return false;
		}

		return keyboardType is
			UIKeyboardType.NumberPad or
			UIKeyboardType.DecimalPad or
			UIKeyboardType.NumbersAndPunctuation or
			UIKeyboardType.PhonePad;
	}

	// Single source of truth for "should we anchor/expand the caret rect so
	// iPad's floating numeric keypad positions adjacent to the full control?"
	// Also consumed by SinglelineInvisibleTextBoxView.GetFirstRectForRange /
	// GetCaretRectForPosition. Keep the two call sites in sync by routing
	// through this helper.
	// When FeatureConfiguration.TextBox.DisableNumberPadPopover is true the
	// popover is opted out via setAllowsNumberPadPopover:, so no anchoring or
	// frame adjustments are needed and we return false.
	internal static bool IsFloatingNumericKeypad(UIKeyboardType keyboardType)
	{
		if (global::Uno.UI.FeatureConfiguration.TextBox.DisableNumberPadPopover)
		{
			return false;
		}

		return IsIPadNumericKeyboard(keyboardType);
	}

	private static bool CouldBecomeFirstResponder(FrameworkElement? element)
	{
		return element is TextBox ||
		element is AutoSuggestBox ||
		element is NumberBox;
	}

	internal static UIView? GetOverlayLayer(XamlRoot xamlRoot) =>
		(XamlRootMap.GetHostForRoot(xamlRoot) as IAppleUIKitXamlRootHost)?.TextInputLayer;
}
