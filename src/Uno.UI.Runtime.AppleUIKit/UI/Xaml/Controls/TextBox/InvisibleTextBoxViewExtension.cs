using System;
using System.Diagnostics.CodeAnalysis;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using UIKit;
using Uno.Foundation.Logging;
using Uno.UI;
using Uno.UI.Hosting;
using Uno.UI.Runtime.AppleUIKit.Hosting;
using Uno.UI.Xaml.Controls;
using Uno.UI.Xaml.Controls.Extensions;
using Uno.UI.Xaml.Core;
using Uno.UI.Runtime.AppleUIKit.Controls;

namespace Uno.UI.Runtime.AppleUIKit;

internal class InvisibleTextBoxViewExtension : IOverlayTextBoxViewExtension, INativeTextLoupeExtension
{
	private readonly TextBoxView _owner;
	private readonly InvisibleTextBoxLoupe _loupe = new();
	private IInvisibleTextBoxView? _textBoxView;
	private XamlRoot? _textBoxViewRoot;
	private UIView? _keyboardDismissAccessory;

	// The other input of a username/password pair. iOS AutoFill fills a login form by making each of its fields the
	// first responder in turn, so both keep their proxy in the hierarchy while they are loaded, not only the focused one.
	private InvisibleTextBoxViewExtension? _autofillCompanion;

	public InvisibleTextBoxViewExtension(TextBoxView view)
	{
		_owner = view;
	}

	internal TextBoxView Owner => _owner;

	/// <summary>The invisible native text view (the first responder while the TextBox has focus), if created.</summary>
	internal UIView? NativeView => _textBoxView as UIView;

	private IImeSessionHost? ImeHost => _owner.Host as IImeSessionHost;

	private bool IsKeptForAutofill => _autofillCompanion is not null;

	// Reapplying the template of the input replaces its TextBoxView, and with it this extension.
	private bool IsCurrent => ReferenceEquals(_owner.Core?.TextBoxView, _owner);

	internal AppleUIKitImeTextBoxExtension? GetImeExtension(IInvisibleTextBoxView source)
	{
		if (ReferenceEquals(_textBoxView, source)
			&& ImeHost is { XamlRoot: { } xamlRoot } host
			&& ReferenceEquals(host.TextBoxView, _owner)
			&& ReferenceEquals(_textBoxViewRoot, xamlRoot))
		{
			return ImeSessionCoordinator.GetExtension(host) as AppleUIKitImeTextBoxExtension;
		}

		return null;
	}

	public bool IsOverlayLayerInitialized(XamlRoot xamlRoot) => true;

	public void StartEntry(bool suppressSoftwareKeyboard = false)
	{
		// StartEntry can be called twice without any EndEntry.
		// This happens when the managed TextBox receives Focus
		// with two different `FocusState`s (e.g, Programmatic and Keyboard/Pointer)
		// The proxy kept for autofill is also reused when its input gets the focus back.
		if (_textBoxView is not null)
		{
			_textBoxView.SetSoftwareKeyboardSuppressed(suppressSoftwareKeyboard);
			if (!_textBoxView.IsFirstResponder && ImeHost is { } focusedHost)
			{
				if (focusedHost.XamlRoot is { } xamlRoot)
				{
					// A kept proxy can have lost its layer in the meantime.
					AddViewToTextInputLayer(xamlRoot);
				}

				MakeFirstResponder(focusedHost);
			}

			CompleteTextInputLayerHandoff();
			return;
		}

		var host = ImeHost;
		if (host?.XamlRoot is null)
		{
			return;
		}

		AttachAutofillCompanion();
		EnsureTextBoxView(host);
		SetSoftKeyboardTheme();

		AddViewToTextInputLayer(host.XamlRoot);

		// A suppressed keyboard still needs the first responder: it is what hardware keyboards type into.
		_textBoxView.SetSoftwareKeyboardSuppressed(suppressSoftwareKeyboard);

		// change FirstResponder's View before removing the previous view to avoid flickering
		MakeFirstResponder(host);

		CompleteTextInputLayerHandoff();
	}

	// Selects once first responder: a secure text field re-inserts its text then, which moves the caret to the end.
	private void MakeFirstResponder(IImeSessionHost host)
	{
		_textBoxView?.BecomeFirstResponder();
		_textBoxView?.Select(host.SelectionStart, host.SelectionLength);
	}

	public void EndEntry()
	{
		_loupe.End();

		if (_textBoxView is null)
		{
			return;
		}

		if (IsKeptForAutofill)
		{
			// The proxy stays for autofill: it only gives up the keyboard, unless another text input takes it over.
			if (_textBoxView.IsFirstResponder && !IsTextInputFocusing())
			{
				_textBoxView.ResignFirstResponder();
			}

			InvalidateLayout();
			return;
		}

		RemoveViewFromTextInputLayer();
		_textBoxView = null;
		_textBoxViewRoot = null;
	}

	public void UpdateSize() => InvalidateLayout();

	public void UpdatePosition() => InvalidateLayout();

	public void NotifyImePositionChanged() => _textBoxView?.NotifyImePositionChanged();

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

	public void ReplaceText(int start, int length, string replacement)
	{
		if (GetNativeText() is not { } text)
		{
			return;
		}

		start = Math.Clamp(start, 0, text.Length);
		length = Math.Clamp(length, 0, text.Length - start);
		SetText(string.Concat(text.AsSpan(0, start), replacement, text.AsSpan(start + length)));
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
		// Property changes before focus must not create an unattached native responder.
		if (_textBoxView is not null && ImeHost is { } host)
		{
			EnsureTextBoxView(host);
			UpdateProperties();
		}
	}

	public void UpdateProperties()
	{
		if (_textBoxView is null || ImeHost is not { } host)
		{
			return;
		}

		_textBoxView.AutocapitalizationType = InputScopeHelper.ConvertInputScopeToCapitalization(host.InputScope);
		_textBoxView.KeyboardType = InputScopeHelper.ConvertInputScopeToKeyboardType(host.InputScope);

		// Apply the iOS 26 number-pad-popover opt-out after KeyboardType is set so the iPad +
		// numeric-keyboard gate is evaluated against the final value. Single-line only; the
		// popover is a UITextField behavior and does not apply to UITextView (multiline).
		if (_textBoxView is SinglelineInvisibleTextBoxView singleline)
		{
			singleline.TryDisableNumberPadPopover();
		}

		_textBoxView.SpellCheckingType = host.IsSpellCheckEnabled ? UITextSpellCheckingType.Yes : UITextSpellCheckingType.No;
		_textBoxView.AutocorrectionType =
			host.IsSpellCheckEnabled && host.IsTextPredictionEnabled
				? UITextAutocorrectionType.Yes
				: UITextAutocorrectionType.No;

		var inputReturnType = host is TextBoxCore core
			? TextBoxExtensions.GetInputReturnType(core.Owner)
			: host.AcceptsReturn
				? InputReturnType.Enter
				: InputReturnType.Done;
		_textBoxView.ReturnKeyType = inputReturnType.ToUIReturnKeyType();

		UpdateKeyboardAccessoryView(host);

		if (host.IsSpellCheckEnabled)
		{
			_textBoxView.AutocapitalizationType = UITextAutocapitalizationType.Sentences;
		}

		var isPassword = host is TextBoxCore { IsPassword: true };
		_textBoxView.SecureTextEntry = isPassword;

		// Lets iOS offer AutoFill (saved passwords, contact details) for the field. Only assigned when there is a
		// type, the native default being none.
		var contentType = IsAutofillUsername(host)
			? UITextContentType.Username
			: InputScopeHelper.ConvertInputScopeToTextContentType(host.InputScope, isPassword);
		if (contentType is not null)
		{
			_textBoxView.TextContentType = contentType;
		}

		SetSoftKeyboardTheme();

		// KeyboardType may have changed — re-evaluate the native view
		// position (anchored vs. off-screen).
		if (_textBoxView is UIView nativeView)
		{
			UpdateNativeViewFrame(nativeView);
		}
	}

	// Apple asks for the username type on a login's username, an e-mail address included (the keyboard still follows
	// the InputScope). Other input scopes keep their own type.
	private bool IsAutofillUsername(IImeSessionHost host)
		=> IsKeptForAutofill
			&& host is TextBoxCore { IsPassword: false }
			&& host.InputScope.GetFirstInputScopeNameValue() is InputScopeNameValue.Default
				or InputScopeNameValue.EmailSmtpAddress
				or InputScopeNameValue.EmailNameOrAddress;

	private void UpdateKeyboardAccessoryView(IImeSessionHost host)
	{
		if (_textBoxView is null)
		{
			return;
		}

		if (host is not TextBoxCore core
			|| !KeyboardDismissAccessory.IsSupported
			|| !TextBoxExtensions.GetShowKeyboardDismissButton(core.Owner))
		{
			_textBoxView.SetKeyboardAccessoryView(null);
			return;
		}

		// The toolbar is built on first opt-in and kept for the lifetime of the extension, so
		// re-focusing the same TextBox does not rebuild it.
		_keyboardDismissAccessory ??= KeyboardDismissAccessory.TryCreate(new WeakReference<InvisibleTextBoxViewExtension>(this));
		_textBoxView.SetKeyboardAccessoryView(_keyboardDismissAccessory);
	}

	/// <summary>
	/// Dismisses the soft keyboard, as the "Done" accessory button does. Resigning the native responder
	/// ends the native editing session, which unfocuses the managed control through the delegate's
	/// EditingEnded - the same path the Enter key takes on a single-line TextBox.
	/// </summary>
	internal void DismissKeyboard()
	{
		if (_textBoxView is { IsFirstResponder: true } view)
		{
			view.ResignFirstResponder();
		}
	}

	private void SetSoftKeyboardTheme()
	{
		if (_owner.Host?.Owner is not { } control || _textBoxView is null)
		{
			return;
		}

		if (control.ActualTheme == ElementTheme.Default)
		{
			_textBoxView.KeyboardAppearance = UIKeyboardAppearance.Default;
		}
		else if (control.ActualTheme == ElementTheme.Light)
		{
			_textBoxView.KeyboardAppearance = UIKeyboardAppearance.Light;
		}
		else if (control.ActualTheme == ElementTheme.Dark)
		{
			_textBoxView.KeyboardAppearance = UIKeyboardAppearance.Dark;
		}
	}

	[MemberNotNull(nameof(_textBoxView))]
	private void EnsureTextBoxView(IImeSessionHost host)
	{
		if (_textBoxView is null ||
			!_textBoxView.IsCompatible(host))
		{
			var previousView = _textBoxView;
			var wasFirstResponder = previousView?.IsFirstResponder == true;
			// RichEditBox can recreate the UIKit editor from a synchronous TextChanging handler,
			// before its native mirror has received the document mutation.
			var inputText = host is RichEditBox ? host.Text : GetNativeText() ?? host.Text;
			_textBoxView = CreateNativeView(host);
			_textBoxViewRoot = host.XamlRoot;
			if (_textBoxView is UIView nativeView)
			{
				nativeView.Alpha = 0.01f;
			}
			UpdateProperties();
			SetText(inputText ?? string.Empty);

			// Replaces a proxy that is in the hierarchy: the focused one, or one kept for autofill.
			if (previousView is UIView { Superview: not null } previousNativeView && host.XamlRoot is { } xamlRoot)
			{
				AddViewToTextInputLayer(xamlRoot);
				if (wasFirstResponder)
				{
					MakeFirstResponder(host);
				}

				previousNativeView.RemoveFromSuperview();
			}
		}
	}

	internal void SyncSelectionToTextBox(IInvisibleTextBoxView source)
	{
		if (ReferenceEquals(_textBoxView, source) && ImeHost is { } host)
		{
			var start = GetSelectionStart();
			var length = GetSelectionLength();
			host.SelectFromNative(start, length);
		}
	}

	// While a caret drag is running, UIKit's own selection updates are computed against the proxy's
	// system-font layout, which bears no relation to the Skia-rendered text. The managed side owns
	// the caret for the duration of the gesture.
	internal bool IsCaretDragActive => _owner?.Core?.IsCaretDragActive ?? false;

	internal bool ProcessCaretDragGesture(TextBoxCore.CaretDragPhase phase, Windows.Foundation.Point cumulativeOffset)
	{
		// Re-resolved on every callback: EndEntry() can clear _textBoxView without resigning first
		// responder, so a discarded proxy can still receive the gesture.
		if (_textBoxView?.Owner?.Core is not { } core || _textBoxView.IsComposing)
		{
			// The caller stops forwarding once declined, so its End would never reach an already-started drag.
			_owner.Core?.CancelCaretDrag();
			return false;
		}

		return core.ProcessCaretDragGesture(phase, cumulativeOffset);
	}

	internal void ProcessNativeTextInput(IInvisibleTextBoxView source, string? text)
	{
		if (!ReferenceEquals(_textBoxView, source))
		{
			return;
		}

		// A kept proxy that gives up the first responder can commit text (an autocorrection, marked text): it is dropped,
		// as for the proxies that go away, so the input keeps what was typed. AutoFill types into a first responder.
		if (IsKeptForAutofill && !source.IsFirstResponder)
		{
			if (ImeHost is { } keptHost)
			{
				SetText(keptHost.Text);
			}

			return;
		}

		// During IME composition, text updates are managed by the shared
		// TextBox.skia.cs composition handlers via IImeTextBoxExtension events.
		// Suppress the normal text processing path to prevent double processing.
		if (_textBoxView?.IsComposing == true)
		{
			return;
		}

		if (ImeHost is { } host)
		{
			var newSelectionStart = GetSelectionStart();
			host.UpdateTextFromNative(text ?? string.Empty, newSelectionStart, 0);
			if (text != host.Text)
			{
				SetText(host.Text);
			}
		}
	}

	private string? GetNativeText() => _textBoxView?.Text;

	private IInvisibleTextBoxView CreateNativeView(IImeSessionHost host) => !host.AcceptsReturn ?
		new SinglelineInvisibleTextBoxView(this) : new MultilineInvisibleTextBoxView(this);

	public void AddViewToTextInputLayer(XamlRoot xamlRoot)
	{
		if (_textBoxView is not UIView nativeView)
		{
			return;
		}

		if (GetOverlayLayer(xamlRoot) is { } layer && nativeView.Superview != layer)
		{
			layer.AddSubview(nativeView);

			UpdateNativeViewFrame(nativeView);
		}
	}

	public void RemoveViewFromTextInputLayer()
	{
		if (ImeHost?.XamlRoot is null || IsTextInputFocusing())
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

	// Once this proxy is the first responder, removes the ones of the previously focused inputs (kept until then to avoid
	// flickering the keyboard). The proxies kept for autofill stay, and go back to their input's bounds.
	private void CompleteTextInputLayerHandoff()
	{
		if ((_textBoxView as UIView)?.Superview is not { } layer)
		{
			return;
		}

		foreach (var view in layer.Subviews)
		{
			if (view is not IInvisibleTextBoxView proxy)
			{
				continue;
			}

			if (proxy.Owner?.Extension is InvisibleTextBoxViewExtension { IsKeptForAutofill: true } extension
				&& ReferenceEquals(extension._textBoxView, proxy))
			{
				if (extension.IsCurrent)
				{
					extension.InvalidateLayout();
					continue;
				}

				extension.ReleaseAutofillPair();
			}

			if (!ReferenceEquals(proxy, _textBoxView))
			{
				view.RemoveFromSuperview();
			}
		}
	}

	private void AttachAutofillCompanion()
	{
		if (IsKeptForAutofill
			|| FindAutofillCompanion() is not { } companion
			|| companion.ImeHost is not { XamlRoot: { } xamlRoot } companionHost)
		{
			return;
		}

		_autofillCompanion = companion;
		companion._autofillCompanion = this;
		SubscribeToUnloaded();
		companion.SubscribeToUnloaded();

		companion.EnsureTextBoxView(companionHost);
		companion.AddViewToTextInputLayer(xamlRoot);

		if (this.Log().IsEnabled(LogLevel.Debug))
		{
			this.Log().Debug($"AutoFill login pair: {Describe(_owner.Core?.Owner)} and {Describe(companion._owner.Core?.Owner)}.");
		}
	}

	// The username is the single-line text input right before the password one in the tab order.
	private InvisibleTextBoxViewExtension? FindAutofillCompanion()
	{
		if (ImeHost is not TextBoxCore { AcceptsReturn: false } core
			|| IsPin(core)
			|| VisualTree.GetFocusManagerForElement(core.Owner) is not { } focusManager)
		{
			return null;
		}

		var candidate = core.IsPassword
			? focusManager.GetPreviousTabStop(core.Owner)
			: focusManager.GetNextTabStop(core.Owner);

		if (candidate is not ITextBoxHost { Core: { AcceptsReturn: false } candidateCore }
			|| candidateCore.IsPassword == core.IsPassword
			|| IsPin(candidateCore)
			|| candidateCore.TextBoxView.Extension is not InvisibleTextBoxViewExtension extension)
		{
			if (core.IsPassword && this.Log().IsEnabled(LogLevel.Debug))
			{
				this.Log().Debug($"No AutoFill username for {Describe(core.Owner)}: its previous tab stop is {Describe(candidate)}.");
			}

			return null;
		}

		if (extension._autofillCompanion is { IsCurrent: false } replaced)
		{
			replaced.ReleaseAutofillPair();
		}

		return extension._autofillCompanion is null ? extension : null;
	}

	// A PIN isn't the password of a login.
	private static bool IsPin(TextBoxCore core)
		=> core.IsPassword && core.InputScope.GetFirstInputScopeNameValue() == InputScopeNameValue.NumericPin;

	private static string Describe(DependencyObject? element)
		=> element switch
		{
			null => "none",
			FrameworkElement { Name.Length: > 0 } named => $"{named.GetType().Name} '{named.Name}'",
			_ => element.GetType().Name,
		};

	private void SubscribeToUnloaded()
	{
		if (_owner.Core?.Owner is { } control)
		{
			control.Unloaded += OnAutofillInputUnloaded;
		}
	}

	// Removing the proxies with their text, once the login form goes away, is also when iOS can offer to save the password.
	private void OnAutofillInputUnloaded(object sender, RoutedEventArgs e) => ReleaseAutofillPair();

	private void ReleaseAutofillPair()
	{
		if (this.Log().IsEnabled(LogLevel.Debug))
		{
			this.Log().Debug($"AutoFill login pair released: {Describe(_owner.Core?.Owner)} and {Describe(_autofillCompanion?._owner.Core?.Owner)}.");
		}

		var companion = _autofillCompanion;
		ReleaseAutofillProxy();
		companion?.ReleaseAutofillProxy();
	}

	private void ReleaseAutofillProxy()
	{
		if (_owner.Core?.Owner is { } control)
		{
			control.Unloaded -= OnAutofillInputUnloaded;
		}

		_autofillCompanion = null;

		// The focused input keeps its proxy until EndEntry.
		if (_textBoxView is { IsFirstResponder: false } view)
		{
			(view as UIView)?.RemoveFromSuperview();
			_textBoxView = null;
			_textBoxViewRoot = null;
		}
	}

	private bool IsTextInputFocusing()
		=> ImeHost?.XamlRoot is { } xamlRoot && CouldBecomeFirstResponder(FocusManager.GetFocusingElement(xamlRoot) as FrameworkElement);

	private void UpdateNativeViewFrame(UIView nativeView)
	{
		var host = _textBoxView?.Owner?.Host?.Owner;
		var rect = host?.GetAbsoluteBoundsRect();
		// GetAbsoluteBoundsRect returns WinUI DIPs which map 1:1 to iOS
		// points.  Do NOT convert to physical pixels — UIView.Frame is in
		// points, not physical pixels.
		var x = rect?.X ?? 0;
		var y = rect?.Y ?? 0;
		var width = rect?.Width ?? 10;
		var height = rect?.Height ?? 10;

		// While another input is focused, a proxy kept for autofill sits at its input's bounds, within the window: iOS only
		// looks for the other fields of a login form on screen. VoiceOver reads the input from the accessibility tree instead.
		var isParkedForAutofill = IsKeptForAutofill && _textBoxView is { IsFirstResponder: false };
		if (IsKeptForAutofill)
		{
			nativeView.AccessibilityElementsHidden = isParkedForAutofill;
		}

		if (isParkedForAutofill && host?.XamlRoot?.Size is { } windowSize)
		{
			x = Math.Clamp(x, 0, Math.Max(0, windowSize.Width - width));
			y = Math.Clamp(y, 0, Math.Max(0, windowSize.Height - height));
		}

		// Only iPad shows a floating numeric keypad that needs an anchor
		// view. For all other cases we push the native view off-screen so
		// that the iOS autocorrect/suggestion bubble does not leak over
		// the Skia-rendered text.
		if (isParkedForAutofill || ShouldAnchorToTextBox())
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
		return element is ITextBoxHost ||
			element is RichEditBox ||
			element is AutoSuggestBox ||
			element is NumberBox;
	}

	public bool BeginLoupe(XamlRoot xamlRoot, Windows.Foundation.Point point, Windows.Foundation.Rect caretRect)
		=> GetOverlayLayer(xamlRoot) is { } layer && _loupe.Begin(layer, point, caretRect);

	public void MoveLoupe(Windows.Foundation.Point point, Windows.Foundation.Rect caretRect) => _loupe.Move(point, caretRect);

	public void EndLoupe() => _loupe.End();

	internal static UIView? GetOverlayLayer(XamlRoot xamlRoot) =>
		(XamlRootMap.GetHostForRoot(xamlRoot) as IAppleUIKitXamlRootHost)?.TextInputLayer;
}
