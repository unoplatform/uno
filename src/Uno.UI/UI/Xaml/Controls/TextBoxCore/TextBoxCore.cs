#nullable enable

using System;
using System.Linq;
using Microsoft.UI.Input;
using Microsoft.UI.Xaml.Automation;
using Microsoft.UI.Xaml.Automation.Peers;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Uno.Extensions;
using Uno.Foundation.Logging;
using Uno.UI.Common;
using Uno.UI.Xaml.Controls;
using Uno.UI.Xaml.Core;
using Uno.UI.Xaml.Media;
using Windows.ApplicationModel.DataTransfer;
using Windows.System;
using Windows.UI.Core;
#if __SKIA__
using Microsoft.UI.Xaml.Internal;
#endif

namespace Microsoft.UI.Xaml.Controls;

/// <summary>
/// Shared text-input implementation owned by <see cref="TextBox"/> and <see cref="PasswordBox"/>.
/// See <see cref="ITextBoxHost"/> for why this is composed rather than inherited.
/// </summary>
internal sealed partial class TextBoxCore
{
	private readonly ITextBoxHost _host;

	internal TextBoxCore(ITextBoxHost host) => _host = host;

	/// <summary>
	/// Mirrors <c>CTextBoxBase::IsEmpty</c>, which WinUI leaves pure-virtual and each control answers
	/// from its own text property.
	/// </summary>
	internal bool IsEmpty => string.IsNullOrEmpty(_host.TextValue);

	/// <summary>
	/// The hosting control, for the platform runtimes that need framework state (XamlRoot, focus) alongside
	/// the core's own selection and view state.
	/// </summary>
	internal Control Owner => _host.Owner;

	// Qualified: `using Windows.System` brings in a same-named type.
	private Microsoft.UI.Dispatching.DispatcherQueue DispatcherQueue => _host.Owner.DispatcherQueue;

	// Shims that let the moved implementation compile unchanged: host state the core must ask for, and
	// framework API it cannot inherit because it is not a UIElement. Private by default — ITextBoxHost is
	// the seam. The `internal` ones are those TextBoxView reads, since it is driven by the core.
	internal string Text
	{
		get => _host.TextValue;
		set => _host.TextValue = value;
	}

	internal bool IsReadOnly => _host.IsReadOnly;

	internal bool AcceptsReturn => _host.AcceptsReturn;

	internal TextWrapping TextWrapping => _host.TextWrapping;

	internal bool IsSpellCheckEnabled => _host.IsSpellCheckEnabled;

	internal char PasswordChar => _host.PasswordChar;

	internal bool IsPassword => _host.IsPassword;

	internal bool IsPasswordRevealed => _host.IsPasswordRevealed;

	internal int MaxLength => _host.MaxLength;

	internal InputScope InputScope => _host.InputScope;

	internal TextAlignment TextAlignment => _host.TextAlignment;

	internal bool IsTextAlignmentExplicitlySet => _host.IsTextAlignmentExplicitlySet;

#if !IS_UNIT_TESTS
	// The native overlay raises this on the control's behalf, so it needs a way through the engine.
	internal void RaisePaste(TextControlPasteEventArgs args) => _host.RaisePaste(args);
#endif

	internal CharacterCasing CharacterCasing => _host.CharacterCasing;

	private bool IsTextPredictionEnabled => _host.IsTextPredictionEnabled;

	private SolidColorBrush SelectionHighlightColor => _host.SelectionHighlightColor;

	private object? Header => _host.Header;

	private DataTemplate? HeaderTemplate => _host.HeaderTemplate;

	private object? Description => _host.Description;

	internal string? PlaceholderText => _host.PlaceholderText;

	private FlyoutBase? SelectionFlyout => _host.SelectionFlyout;

#if __SKIA__
	private bool CanPasteClipboardContent
	{
		get => _host.CanPasteClipboardContent;
		set => _host.CanPasteClipboardContent = value;
	}
#endif

	private string SelectedText => Text.Substring(SelectionStart, SelectionLength);

	private bool IsButtonEnabled => _host.IsButtonEnabled;

	private void UpdateButtonStates() => _host.UpdateButtonStates();

	private void UpdateVisualState(bool useTransitions = true) => _host.UpdateVisualState(useTransitions);

	private FocusState FocusState => _host.Owner.FocusState;

	private Brush Foreground => _host.Owner.Foreground;

	internal FlowDirection FlowDirection => _host.Owner.FlowDirection;

	private bool HasPointerCapture => _host.Owner.HasPointerCapture;

	private bool IsLoaded => _host.Owner.IsLoaded;

	private bool IsFocused => _host.Owner.IsFocused;

	private void StartBringIntoView(BringIntoViewOptions options) => _host.Owner.StartBringIntoView(options);

	private FlyoutBase ContextFlyout
	{
		get => _host.Owner.ContextFlyout;
		set => _host.Owner.ContextFlyout = value;
	}

	private bool CapturePointer(Pointer value) => _host.Owner.CapturePointer(value);

	private void ReleasePointerCaptures() => _host.Owner.ReleasePointerCaptures();

	private GeneralTransform TransformToVisual(UIElement visual) => _host.Owner.TransformToVisual(visual);

	// Platform hooks, implemented in TextBoxCore.Input.cs.
	partial void OnUnloadedPartial();
	partial void SetInputReturnTypePlatform(InputReturnType inputReturnType);
	partial void OnTextChangedPartial();
	partial void UpdateFontPartial();
	partial void OnForegroundColorChangedPartial(Brush newValue);
	partial void OnSelectionHighlightColorChangedPartial(SolidColorBrush brush);
	partial void OnInputScopeChangedPartial(InputScope newValue);
	partial void OnMaxLengthChangedPartial(int newValue);
	partial void OnTextWrappingChangedPartial();
	partial void OnFlowDirectionChangedPartial();
	partial void OnIsReadonlyChangedPartial();
	partial void OnIsSpellCheckEnabledChangedPartial(bool newValue);
	partial void OnIsTextPredictionEnabledChangedPartial(bool newValue);
	partial void OnTextAlignmentChangedPartial(TextAlignment newValue);
	partial void OnFocusStateChangedPartial(FocusState focusState, bool initial);
	partial void OnPointerPressedPartial(PointerRoutedEventArgs args);
	partial void OnPointerReleasedPartial(PointerRoutedEventArgs args, bool wasFocused);
	partial void OnPointerCaptureLostPartial(PointerRoutedEventArgs e);
	partial void OnKeyDownPartial(KeyRoutedEventArgs args);
	partial void SelectPartial(int start, int length);
	partial void SelectAllPartial();
	partial void PasteFromClipboardPartial(string adjustedClipboardText, int selectionStart, string newText);
	partial void CutSelectionToClipboardPartial();
	partial void OnAcceptsReturnChangedPartial(bool newValue);
	partial void OnTextCharacterCasingChangedPartial(CharacterCasing newValue);
	partial void OnDeleteButtonClickPartial();

#if __SKIA__
	private bool _pendingUpdateScrolling;
#endif
	/// <summary>
	/// This is a workaround for the template pooling issue where we change IsChecked when the template is recycled.
	/// This prevents incorrect event raising but is not a "real" solution. Pooling could still cause issues.
	/// This workaround can be removed if pooling is removed. See https://github.com/unoplatform/uno/issues/12189
	/// </summary>
	private bool _suppressTextChanged;
	private bool _wasTemplateRecycled;

	// Template parts and brush subscriptions: null until OnApplyTemplate resolves them.
#pragma warning disable CS0067, CS0649
	private IFrameworkElement? _placeHolder;
	private ContentControl? _contentElement;
	private WeakReference<Button>? _deleteButton;

	private Action? _selectionHighlightColorChanged;
	private Action? _foregroundBrushChanged;
	private IDisposable? _selectionHighlightBrushChangedSubscription;
	private IDisposable? _foregroundBrushChangedSubscription;
#pragma warning restore CS0067, CS0649

	private ContentPresenter? _header;

	private bool CanShowButton => !IsEmpty && FocusState != FocusState.Unfocused && !IsReadOnly && !AcceptsReturn && TextWrapping == TextWrapping.NoWrap;

	/// <summary>
	/// Set when <see cref="TextChanged"/> event is being raised, to ensure modifications by handlers don't trigger an infinite loop.
	/// </summary>
	private bool _isInvokingTextChanged;
	/// <summary>
	/// Set when <see cref="TextChanging"/> event is being raised, to ensure modifications by handlers don't trigger an infinite loop.
	/// </summary>
	private bool _isInvokingTextChanging;
	/// <summary>
	/// Set when the <see cref="Text"/> property is being modified by user input.
	/// </summary>
	private bool _isInputModifyingText;
	/// <summary>
	/// Set when the <see cref="Text"/> property is being cleared via delete button.
	/// </summary>
	private bool _isInputClearingText;
	/// <summary>
	/// Indicates how many TextChanged events are pending. This is needed for AutoSuggestBox, which needs to
	/// respond only to the last TextChange event, not all of them.
	/// </summary>
	private int _textChangedPendingCount;
	/// <summary>
	/// True if Text has changed while the TextBox has had focus, false otherwise
	///
	/// This flag is checked to avoid pushing a value to a two-way binding if no edits have occurred, per UWP's behavior.
	/// </summary>
	private bool _hasTextChangedThisFocusSession;

	private bool _forceFocusedVisualState;

	internal void Initialize()
	{
		var owner = _host.Owner;

		owner.RegisterParentChangedCallbackStrong(this, OnParentChanged);
		owner.SizeChanged += OnSizeChanged;

#if __SKIA__
		owner.ActualThemeChanged += (_, _) =>
		{
			TextBoxView?.DisplayBlock.InvalidateInlines(false);
			TextBoxView?.UpdateTheme();
		};
		_timer.Tick += TimerOnTick;
		EnsureHistory();
#endif
	}

	internal void OnLoadedCore()
	{
		// This workaround is added in OnLoaded rather than OnApplyTemplate.
		// Apparently, sometimes (e.g, Material style), the TextBox style setters are executed after OnApplyTemplate
		// So, the style setters would override what the workaround does.
		// OnLoaded appears to be executed after both OnApplyTemplate and after the style setters, making sure the values set here are not modified after.
		if (_contentElement is ScrollViewer scrollViewer)
		{
			// The template of TextBox contains the following:
			/*
				HorizontalScrollBarVisibility="{TemplateBinding ScrollViewer.HorizontalScrollBarVisibility}"
				HorizontalScrollMode="{TemplateBinding ScrollViewer.HorizontalScrollMode}"
				VerticalScrollBarVisibility="{TemplateBinding ScrollViewer.VerticalScrollBarVisibility}"
				VerticalScrollMode="{TemplateBinding ScrollViewer.VerticalScrollMode}"
			 */
			// Historically, TemplateBinding for attached DPs wasn't supported, and TextBox worked perfectly fine.
			// When support for TemplateBinding for attached DPs was added, TextBox broke (test: TextBox_AutoGrow_Vertically_Wrapping_Test) because of
			// change in the values of these properties. The following code serves as a workaround to set the values to what they used to be
			// before the support for TemplateBinding for attached DPs.
#if !__SKIA__
			scrollViewer.HorizontalScrollMode = ScrollMode.Enabled; // The template sets this to Auto
			scrollViewer.VerticalScrollMode = ScrollMode.Enabled; // The template sets this to Auto
			scrollViewer.HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled; // The template sets this to Hidden
			scrollViewer.VerticalScrollBarVisibility = ScrollBarVisibility.Auto; // The template sets this to Hidden
#endif

		}
	}

	internal void OnUnloadedCore() => OnUnloadedPartial();

	internal void OnTemplateRecycled()
	{
		_suppressTextChanged = true;
		Text = string.Empty;
		_wasTemplateRecycled = true;
	}

	internal void OnPostKeyDown(KeyRoutedEventArgs args) => OnKeyDownSkia(args);

	internal void OnPointerPressed(PointerRoutedEventArgs args)
	{
		bool isPointerCaptureRequired =
			true;

		if (ShouldFocusOnPointerPressed(args)) // UWP Captures if the pointer is not Touch
		{
			var wasFocused = FocusState != FocusState.Unfocused;
			if (isPointerCaptureRequired)
			{
				if (CapturePointer(args.Pointer))
				{
					_host.Owner.Focus(FocusState.Pointer);
				}
			}
			else
			{
				_host.Owner.Focus(FocusState.Pointer);
			}

#if __SKIA__
			if (wasFocused)
			{
				// See comment in OnPointerReleased for why we do this
				_textBoxNotificationsSingleton?.OnFocused(this);
			}
#endif
		}

		args.Handled = true;

		OnPointerPressedPartial(args);
	}

	internal void OnPointerReleased(PointerRoutedEventArgs args)
	{
		bool wasFocused = FocusState != FocusState.Unfocused;
		if (!ShouldFocusOnPointerPressed(args))
		{
			// Ported from: TextBoxBase.cpp OnPointerReleased
			// Don't take focus if the context flyout is open.
#if __SKIA__
			if (!TextControlFlyoutHelper.IsOpen(ContextFlyout))
#endif
			{
				_host.Owner.Focus(FocusState.Pointer);
			}
#if __SKIA__
			if (wasFocused)
			{
				// We already call UpdateFocusState in TextBoxView when focus changes, but this is not enough.
				// UpdateFocusState should be called here even if the TextBox was already focused.
				// This is to support re-showing the keyboard when clicking on an already-focused TextBox.
				// For example:
				// 1. User taps on TextBox and it gained focus and soft keyboard was shown.
				// 2. User hides the keyboard, but TextBox is still focused.
				// 3. User taps on TextBox again. In this case, we want to call UpdateFocusState so that the soft keyboard is re-shown again.
				//
				// This approach feels hacky though and may not handle programmatic focus properly, i.e, when programmatic focus is requested on an already-focused TextBox. This is a niche case though.
				_textBoxNotificationsSingleton?.OnFocused(this);
			}
#endif
		}

		args.Handled = true;

		OnPointerReleasedPartial(args, wasFocused);
	}

	// Entry points for hooks whose caller stayed on the control.
	internal void UpdateFont() => UpdateFontPartial();

	internal void OnFlowDirectionChanged() => OnFlowDirectionChangedPartial();

	internal void OnKeyDown(KeyRoutedEventArgs args) => OnKeyDownPartial(args);

	internal void OnPointerCaptureLost(PointerRoutedEventArgs e) => OnPointerCaptureLostPartial(e);

	internal void Select(int start, int length)
	{
		if (start < 0)
		{
			throw new ArgumentException($"'{start}' cannot be negative.", nameof(start));
		}

		if (length < 0)
		{
			throw new ArgumentException($"'{length}' cannot be negative.", nameof(length));
		}

		// TODO: Test and adjust (if needed) this logic for surrogate pairs.

		var textLength = Text.Length;

		if (start >= textLength)
		{
			start = textLength;
			length = 0;
		}
		else if (start + length > textLength)
		{
			length = textLength - start;
		}

#if __SKIA__
		_pendingSelection = null;
#endif

		if (SelectionStart == start && SelectionLength == length)
		{
			return;
		}

		if (_host.RaiseSelectionChanging(start, length))
		{
			SelectPartial(start, length);
			_host.RaiseSelectionChanged();
			SelectionChanged?.Invoke(this, new RoutedEventArgs(_host.Owner));
		}
	}

	/// <summary>
	/// Raised alongside the control's own selection-changed event, for the platform runtimes that track
	/// selection through the engine instead of a control-typed event.
	/// </summary>
	internal event EventHandler<RoutedEventArgs>? SelectionChanged;

	internal void SelectAll() => SelectAllPartial();

	internal void OnForegroundColorChanged(Brush newValue)
	{
		_foregroundBrushChangedSubscription?.Dispose();
		_foregroundBrushChangedSubscription = Brush.SetupBrushChanged(newValue, ref _foregroundBrushChanged, () => OnForegroundColorChangedPartial(newValue));
	}

	internal void PasteFromClipboard()
	{
		_ = _host.Owner.Dispatcher.RunAsync(CoreDispatcherPriority.High, async () =>
		{
			var content = Clipboard.GetContent();
			string clipboardText;
			if (content.AvailableFormats.Contains(StandardDataFormats.Text))
			{
				try
				{
					clipboardText = await content.GetTextAsync();
					PasteFromClipboard(clipboardText);
				}
				catch (InvalidOperationException e)
				{
					if (this.Log().IsEnabled(LogLevel.Debug))
					{
						this.Log().Debug("TextBox.PasteFromClipboard failed during DataPackageView.GetTextAsync: " + e);
					}
				}
			}
		});
	}

	internal void CopySelectionToClipboard()
	{
		if (IsPassword)
		{
			return;
		}

		if (SelectionLength > 0)
		{
			var text = SelectedText;
			var dataPackage = new DataPackage();
			dataPackage.SetText(text);
			Clipboard.SetContent(dataPackage);
		}
	}

	internal void CutSelectionToClipboard()
	{
		if (IsReadOnly || IsPassword)
		{
			return;
		}

		CopySelectionToClipboard();
		CutSelectionToClipboardPartial();
#if __SKIA__
		try
		{
			_suppressCurrentlyTyping = true;
#else
		{
#endif
			Text = Text.Remove(SelectionStart, SelectionLength);
		}
#if __SKIA__
		finally
		{
			_suppressCurrentlyTyping = false;
		}
#endif
	}

	internal void OnTextChangedCore(string? oldValue, string? newValue)
	{
		_hasTextChangedThisFocusSession = true;

		RaiseTextChanging();

#if !__SKIA__
		if (!_isInputModifyingText)
#endif
		{
			_textBoxView?.SetTextNative(Text);
		}

		UpdatePlaceholderVisibility();

		OnTextChangedPartial();

		_host.RaiseValueAutomationEvents(oldValue, newValue);

		// Update states after the text has changed, since we're
		// using selection values to compute SV scrolling.
		UpdateButtonStates();

		_host.UpdateValueBindingSourceOnValueChanged();

		var isUserModifyingText = _isInputModifyingText | _isInputClearingText;
		_textChangedPendingCount++;
		_ = _host.Owner.Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () => RaiseTextChanged(isUserModifyingText));
	}

	internal void UpdateButtonStatesCore()
	{
		if (this.Log().IsEnabled(LogLevel.Debug))
		{
			this.Log().LogDebug(nameof(UpdateButtonStates));
		}

		var changed = false;
		// Minimum width for TextBox with DeleteButton visible is 5em.
		if (CanShowButton && IsButtonEnabled && _host.Owner.ActualWidth > _host.Owner.FontSize * 5)
		{
			changed |= VisualStateManager.GoToState(_host.Owner, TextBoxConstants.ButtonVisibleStateName, true);
		}
		else
		{
			changed |= VisualStateManager.GoToState(_host.Owner, TextBoxConstants.ButtonCollapsedStateName, true);
		}

#if __SKIA__
		DispatchUpdateScrolling();
#endif
	}

	internal void OnGotFocusCore()
	{
		_forceFocusedVisualState = false;
		_host.Owner.StartBringIntoView(new BringIntoViewOptions
		{
			AnimationDesired = false
		});
	}

	internal void ApplyTemplate()
	{
		var owner = _host.Owner;

		// Ensures we don't keep a reference to a textBoxView that exists in a previous template
		_textBoxView = null;

		_placeHolder = owner.GetTemplateChild(TextBoxConstants.PlaceHolderPartName) as IFrameworkElement;
		_contentElement = owner.GetTemplateChild(TextBoxConstants.ContentElementPartName) as ContentControl;
		_header = owner.GetTemplateChild(TextBoxConstants.HeaderContentPartName) as ContentPresenter;

		if (owner.GetTemplateChild(TextBoxConstants.DeleteButtonPartName) is Button button)
		{
			_deleteButton = new WeakReference<Button>(button);
		}

		if (_contentElement is { })
		{
			_contentElement.SetProtectedCursor(InputSystemCursor.Create(InputSystemCursorShape.IBeam));
		}

		UpdateTextBoxView();
		InitializeProperties();
		UpdateVisualState();
	}

	partial void InitializePropertiesPartial();

	internal void UpdateVisualStateCore(bool useTransitions = true)
	{
		var owner = _host.Owner;
		var focusManager = VisualTree.GetFocusManagerForElement(owner);
		// CommonStates & FocusStates are combined
		//
		// NOTES: Pressed state is the same as Focused
		//        PointerFocused state is the same as Focused
		if (!owner.IsEnabled)
		{
			VisualStateManager.GoToState(owner, "Disabled", true);
		}
		else if (_forceFocusedVisualState || (FocusState != FocusState.Unfocused && focusManager!.IsPluginFocused()))
		{
			VisualStateManager.GoToState(owner, "Focused", true);
		}
		else if (_host.IsPointerOver)
		{
			VisualStateManager.GoToState(owner, "PointerOver", true);
		}
		else
		{
			VisualStateManager.GoToState(owner, "Normal", true);
		}
	}

	internal void OnInputReturnTypeChanged(InputReturnType inputReturnType, bool initial)
	{
		if (inputReturnType != InputReturnType.Default || !initial)
		{
			SetInputReturnTypePlatform(inputReturnType);
		}
	}

	private void OnSizeChanged(object sender, SizeChangedEventArgs args)
	{
		UpdateButtonStates();
	}

	private void OnParentChanged(object instance, object? key, DependencyObjectParentChangedEventArgs args) => UpdateFontPartial();

	private void InitializeProperties()
	{
		UpdatePlaceholderVisibility();
		UpdateButtonStates();
		OnInputScopeChanged(InputScope);
		OnMaxLengthChanged(MaxLength);
		OnAcceptsReturnChanged(AcceptsReturn);
		OnIsReadonlyChanged();
		OnForegroundColorChanged(Foreground);
		UpdateFontPartial();
		OnHeaderChanged();
		OnIsTextPredictionEnabledChanged(IsTextPredictionEnabled);
		OnSelectionHighlightColorChanged(null, SelectionHighlightColor);
		OnIsSpellCheckEnabledChanged(IsSpellCheckEnabled);
		OnTextAlignmentChanged(TextAlignment);
		OnTextWrappingChanged();
		OnFocusStateChanged(FocusState.Unfocused, FocusState, initial: true);
		OnTextCharacterCasingChanged(CharacterCasing);
		OnInputReturnTypeChanged(_host.InputReturnType, initial: true);
		UpdateDescriptionVisibility(true);
		var buttonRef = _deleteButton?.GetTarget();

		if (buttonRef != null)
		{
			var coreRef = new WeakReference<TextBoxCore>(this);
			buttonRef.Command = new DelegateCommand(() =>
			{
				if (coreRef.TryGetTarget(out var core))
				{
					core.DeleteButtonClick();
				}
			});
		}

		InitializePropertiesPartial();
	}

	private static string GetFirstLine(string value)
	{
		for (int i = 0; i < value.Length; i++)
		{
			var c = value[i];
			if (c == '\r' || c == '\n')
			{
				return value.Substring(0, i);
			}
		}

		return value;
	}

	private void RaiseTextChanging()
	{
		if (!_isInvokingTextChanging)
		{
			try
			{
				_isInvokingTextChanging = true;
				_host.RaiseValueChanging();
			}
			finally
			{
				_isInvokingTextChanging = false;
			}
		}
	}

	/// <summary>
	/// This is called asynchronously after the UI changes in line with WinUI.
	/// Note that no further native text box view text modification should
	/// be performed in this method to avoid potential race conditions
	/// (see #6289)
	/// </summary>
	private void RaiseTextChanged(bool isUserModifyingText)
	{
		_textChangedPendingCount--;
		if (_isInvokingTextChanged)
		{
			return;
		}

		try
		{
			_isInvokingTextChanged = true;
			if (!_suppressTextChanged) // This workaround can be removed if pooling is removed. See https://github.com/unoplatform/uno/issues/12189
			{
				_host.RaiseValueChanged(isUserModifyingText, _textChangedPendingCount > 0);
			}
		}
		finally
		{
			_isInvokingTextChanged = false;
			_suppressTextChanged = false;
		}
	}

	private void UpdatePlaceholderVisibility()
	{
		if (_placeHolder != null)
		{
			_placeHolder.Visibility = Text.IsNullOrEmpty() ? Visibility.Visible : Visibility.Collapsed;
		}
	}

	internal object CoerceText(object baseValue)
	{
		if (!(baseValue is string baseString))
		{
			return ""; //Pushing null to the binding resets the text. (Setting null to the Text property directly throws an exception.)
		}

		if (MaxLength > 0 && baseString.Length > MaxLength)
		{
			// Reject the new string if it's longer than the MaxLength
#if __SKIA__
			_pendingSelection = null;
#endif
			return DependencyProperty.UnsetValue;
		}

		if (!AcceptsReturn)
		{
			baseString = GetFirstLine(baseString);
		}
#if __SKIA__
		else
		{
			// WinUI replaces all \n's and and \r\n's by \r. This is annoying because
			// the _pendingSelection uses indices before this removal.
			// On UIKit targets we use invisible overlay and replacing newlines would break the sync between
			// the native input and the managed representation.
			baseString = RemoveLF(baseString);
		}

		// make sure this coercion doesn't cause the pending selection to be out of range
		if (_pendingSelection is { } selection2)
		{
			var start = Math.Min(selection2.start, baseString.Length);
			var end = Math.Min(selection2.start + selection2.length, baseString.Length);
			_pendingSelection = (start, end - start);
		}
#endif

		if (_host.RaiseBeforeValueChanging(baseString))
		{
#if __SKIA__
			// On WinUI, when a selection is canceled, the TextBox invokes a bunch of weird
			// SelectionChanging events followed by a bunch of matching SelectionChanged.
			// Probing for the value of SelectionStart and SelectionLength during these SelectionChanging
			// events will give incorrect transient values and the SelectionChanged events will end up
			// with the selection where it started (before the text change). Also, the direction of
			// of the selection will be reset, i.e. if the selection end was "at the start", then it won't be
			// so anymore.
			// In Uno, we choose a simpler sequence. We just reset the selection direction (like WinUI) and
			// we don't invoke any selection change events (since selection was in fact not changed).
			_pendingSelection = (SelectionStart, SelectionLength);
#endif
			return DependencyProperty.UnsetValue;
		}

		return baseString;
	}

	internal void UpdateDescriptionVisibility(bool initialization)
	{
		if (initialization && Description == null)
		{
			// Avoid loading DescriptionPresenter element in template if not needed.
			return;
		}

		var descriptionPresenter = _host.Owner.FindName("DescriptionPresenter") as ContentPresenter;
		if (descriptionPresenter != null)
		{
			descriptionPresenter.Visibility = Description != null ? Visibility.Visible : Visibility.Collapsed;
		}
	}

	internal void OnSelectionHighlightColorChanged(SolidColorBrush? oldBrush, SolidColorBrush? newBrush)
	{
		oldBrush ??= DefaultBrushes.SelectionHighlightColor;
		newBrush ??= DefaultBrushes.SelectionHighlightColor;

		_selectionHighlightBrushChangedSubscription?.Dispose();
		_selectionHighlightBrushChangedSubscription = Brush.SetupBrushChanged(newBrush, ref _selectionHighlightColorChanged, () => OnSelectionHighlightColorChangedPartial(newBrush));
	}

	internal void OnInputScopeChanged(InputScope newValue) => OnInputScopeChangedPartial(newValue);

	internal void OnMaxLengthChanged(int newValue) => OnMaxLengthChangedPartial(newValue);

	internal void OnAcceptsReturnChanged(bool newValue)
	{
		if (!newValue)
		{
			var text = Text;
			var singleLineText = GetFirstLine(text);
			if (text != singleLineText)
			{
				Text = singleLineText;
			}
		}

		OnAcceptsReturnChangedPartial(newValue);
		UpdateButtonStates();
	}

	internal void OnTextWrappingChanged()
	{
		OnTextWrappingChangedPartial();
		UpdateButtonStates();
	}

	internal void OnTextCharacterCasingChanged(CharacterCasing newValue)
	{
		OnTextCharacterCasingChangedPartial(newValue);
	}

	internal void OnIsReadonlyChanged()
	{
		OnIsReadonlyChangedPartial();
		UpdateButtonStates();
	}

	internal void OnHeaderChanged()
	{
		var headerVisibility = (Header != null || HeaderTemplate != null) ? Visibility.Visible : Visibility.Collapsed;

		if (_header != null)
		{
			_header.Visibility = headerVisibility;
		}
	}

	internal void OnIsSpellCheckEnabledChanged(bool newValue) => OnIsSpellCheckEnabledChangedPartial(newValue);

	internal void OnIsTextPredictionEnabledChanged(bool newValue) => OnIsTextPredictionEnabledChangedPartial(newValue);

	internal void OnTextAlignmentChanged(TextAlignment newValue) => OnTextAlignmentChangedPartial(newValue);

	internal void OnFocusStateChanged(FocusState oldValue, FocusState newValue, bool initial)
	{
		OnFocusStateChangedPartial(newValue, initial);

		if (_forceFocusedVisualState && newValue == FocusState.Unfocused)
		{
			// Context flyout is taking focus - skip binding updates and keep
			// _hasTextChangedThisFocusSession. Deferred until actual focus loss.
			UpdateVisualState();
			return;
		}

		if (!initial && newValue == FocusState.Unfocused && _hasTextChangedThisFocusSession)
		{
			if (!_wasTemplateRecycled)
			{
				_host.UpdateValueBindingSourceOnLostFocus();
			}

			_wasTemplateRecycled = false;
		}

		UpdateButtonStates();

		if (newValue == FocusState.Unfocused)
		{
			_hasTextChangedThisFocusSession = false;
		}

		UpdateVisualState();
	}

#if __SKIA__
	private void DispatchUpdateScrolling()
	{
		if (!_pendingUpdateScrolling)
		{
			_pendingUpdateScrolling = true;

			// We may be pushing scrolling updates too often
			// when pushing keystrokes programmatically.
			DispatcherQueue.TryEnqueue(() =>
			{
				_pendingUpdateScrolling = false;

				UpdateScrolling();
			});
		}
	}
#endif

	/// <summary>
	/// Respond to text input from user interaction.
	/// </summary>
	/// <param name="newText">The most recent version of the text from the input field.</param>
	/// <returns>The value of the <see cref="Text"/> property, which may have been modified programmatically.</returns>
	internal string ProcessTextInput(string newText)
	{
		var isCurrentlyModifying = _isInputModifyingText;

		try
		{
			_isInputModifyingText = true;
			var oldText = Text;
			Text = newText;

#if __SKIA__
			if (_pendingSelection is { } selection && Text == oldText)
			{
				// OnTextChanged won't fire, so we immediately change the selection.
				// Note how we check that Text (after assignment) == oldText and
				// not oldText == newText. This is because CoerceText can make it so that
				// newText != oldText but Text (after assignment) == oldText
				SelectInternal(selection.start, selection.length);
			}
#endif
		}
		finally
		{
			if (!isCurrentlyModifying)
			{
				// The all to ProcessTextInput may be recursing, we only want to restore
				// the state on the last one.
				_isInputModifyingText = false;
			}
		}

		return Text; //This may have been modified by BeforeTextChanging, TextChanging, DP callback, etc
	}

	private void DeleteButtonClick()
	{
		try
		{
			_isInputClearingText = true;

			Text = string.Empty;
			OnDeleteButtonClickPartial();
		}
		finally
		{
			_isInputClearingText = false;
		}
	}

	/// <summary>
	/// Copies content from the OS clipboard into the text control.
	/// </summary>
	internal void PasteFromClipboard(string clipboardText)
	{
		if (IsReadOnly)
		{
			return;
		}

		var selectionStart = SelectionStart;
		var selectionLength = SelectionLength;
		var currentText = Text;
		var adjustedClipboardText = clipboardText;

		if (selectionLength > 0)
		{
			currentText = currentText.Remove(selectionStart, selectionLength);
		}

		if (MaxLength > 0)
		{
			var clipboardRangeToBePasted = Math.Max(0, Math.Min(clipboardText.Length, MaxLength - currentText.Length));
			adjustedClipboardText = clipboardText[..clipboardRangeToBePasted];
		}

		currentText = currentText.Insert(selectionStart, adjustedClipboardText);
		PasteFromClipboardPartial(adjustedClipboardText, selectionStart, currentText);

#if __SKIA__
		try
		{
			_clearHistoryOnTextChanged = false;
			_suppressCurrentlyTyping = true;
#else
		{
#endif
			ProcessTextInput(currentText);
		}
#if __SKIA__
		finally
		{
			_suppressCurrentlyTyping = false;
			_clearHistoryOnTextChanged = true;
			if (Text.IsNullOrEmpty())
			{
				// On WinUI, the caret never has thumbs if there is no text
				CaretMode = CaretDisplayMode.ThumblessCaretShowing;
			}
		}
#endif

#if !IS_UNIT_TESTS
		_host.RaisePaste(new TextControlPasteEventArgs());
#endif
	}

	private bool ShouldFocusOnPointerPressed(PointerRoutedEventArgs args) =>
		// For mouse and pen, the TextBox should focus on pointer press
		// (and then capture pointer to make sure to handle the whol down->move->up sequence).
		// For touch we wait for the release to focus (avoid flickering in case of cancel due to scroll for instance).
		args.Pointer.PointerDeviceType != PointerDeviceType.Touch;

}
