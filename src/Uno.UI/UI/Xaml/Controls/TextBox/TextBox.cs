#if IS_UNIT_TESTS || UNO_REFERENCE_API
#pragma warning disable CS0067, CS649
#endif

using System;
using System.Text;
using Uno.Extensions;
using Uno.UI.Common;
using Uno.UI.DataBinding;
using Uno.UI.Xaml.Input;
using Windows.Foundation;
using Windows.System;
using Windows.UI.Core;
using Windows.UI.Text;
using Microsoft.UI.Xaml.Automation;
using Microsoft.UI.Xaml.Automation.Peers;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Uno.Foundation.Logging;
using Uno.Disposables;
using Uno.UI.Helpers;
using Uno.UI.Xaml.Core;
using Uno.UI.Xaml.Media;
using Windows.ApplicationModel.DataTransfer;
using Uno.UI;
using DirectUI;

#if HAS_UNO_WINUI
using Microsoft.UI.Input;
using PointerDeviceType = Microsoft.UI.Input.PointerDeviceType;
using Uno.UI.Xaml.Controls;
#else
using PointerDeviceType = Windows.Devices.Input.PointerDeviceType;
#endif

namespace Microsoft.UI.Xaml.Controls
{
	public class TextBoxConstants
	{
		public const string HeaderContentPartName = "HeaderContentPresenter";
		public const string ContentElementPartName = "ContentElement";
		public const string PlaceHolderPartName = "PlaceholderTextContentPresenter";
		public const string DeleteButtonPartName = "DeleteButton";
		public const string ButtonVisibleStateName = "ButtonVisible";
		public const string ButtonCollapsedStateName = "ButtonCollapsed";
	}

	public partial class TextBox : Control, IFrameworkTemplatePoolAware
	{
		/// <summary>
		/// This is a workaround for the template pooling issue where we change IsChecked when the template is recycled.
		/// This prevents incorrect event raising but is not a "real" solution. Pooling could still cause issues.
		/// This workaround can be removed if pooling is removed. See https://github.com/unoplatform/uno/issues/12189
		/// </summary>
		private bool _suppressTextChanged;
		private bool _wasTemplateRecycled;

#pragma warning disable CS0067, CS0649
		private IFrameworkElement _placeHolder;
		private ContentControl _contentElement;
		private WeakReference<Button> _deleteButton;

		private Action _selectionHighlightColorChanged;
		private Action _foregroundBrushChanged;
		private IDisposable _selectionHighlightBrushChangedSubscription;
		private IDisposable _foregroundBrushChangedSubscription;
#pragma warning restore CS0067, CS0649

		private ContentPresenter _header;
		protected private bool _isButtonEnabled = true;
		protected private bool CanShowButton => !Text.IsNullOrEmpty() && FocusState != FocusState.Unfocused && !IsReadOnly && !AcceptsReturn && TextWrapping == TextWrapping.NoWrap;

		public event TextChangedEventHandler TextChanged;
		public event TypedEventHandler<TextBox, TextBoxTextChangingEventArgs> TextChanging;
		public event TypedEventHandler<TextBox, TextBoxBeforeTextChangingEventArgs> BeforeTextChanging;
		public event RoutedEventHandler SelectionChanged;

		public event TypedEventHandler<TextBox, TextBoxSelectionChangingEventArgs> SelectionChanging;

#if !IS_UNIT_TESTS
		/// <summary>
		/// Occurs when text is pasted into the control.
		/// </summary>
		public
#if __APPLE_UIKIT__
			new
#endif
			event TextControlPasteEventHandler Paste;

		internal void RaisePaste(TextControlPasteEventArgs args) => Paste?.Invoke(this, args);
#endif

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

		public TextBox()
		{
			this.RegisterParentChangedCallbackStrong(this, OnParentChanged);

			DefaultStyleKey = typeof(TextBox);
			SizeChanged += OnSizeChanged;

#if __SKIA__
			ActualThemeChanged += (_, _) =>
			{
				TextBoxView?.DisplayBlock.InvalidateInlines(false);
				TextBoxView?.UpdateTheme();
			};
			_timer.Tick += TimerOnTick;
			EnsureHistory();
#endif

			InitializePartial();
		}

		partial void InitializePartial();

		private protected override void OnLoaded()
		{
			base.OnLoaded();

#if __ANDROID__
			SetupTextBoxView();
#endif

			// This workaround is added in OnLoaded rather than OnApplyTemplate.
			// Apparently, sometimes (e.g, Material style), the TextBox style setters are executed after OnApplyTemplate
			// So, the style setters would override what the workaround does.
			// OnLoaded appears to be executed after both OnApplyTemplate and after the style setters, making sure the values set here are not modified after.
			if (_contentElement is ScrollViewer scrollViewer)
			{
#if __APPLE_UIKIT__
				// We disable scrolling because the inner ITextBoxView provides its own scrolling
				scrollViewer.HorizontalScrollMode = ScrollMode.Disabled;
				scrollViewer.VerticalScrollMode = ScrollMode.Disabled;
				scrollViewer.HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled;
				scrollViewer.VerticalScrollBarVisibility = ScrollBarVisibility.Disabled;
#else
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
#if __SKIA__
				if (!_isSkiaTextBox)
#endif
				{
					scrollViewer.HorizontalScrollMode = ScrollMode.Enabled; // The template sets this to Auto
					scrollViewer.VerticalScrollMode = ScrollMode.Enabled; // The template sets this to Auto
					scrollViewer.HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled; // The template sets this to Hidden
					scrollViewer.VerticalScrollBarVisibility = ScrollBarVisibility.Auto; // The template sets this to Hidden
				}

#if __WASM__
				scrollViewer.DisableSetFocusOnPopupByPointer = !IsPointerCaptureRequired;
#endif
#endif
			}
		}

		private protected override void OnUnloaded()
		{
			base.OnUnloaded();

			OnUnloadedPartial();
		}

		partial void OnUnloadedPartial();

		private void OnSizeChanged(object sender, SizeChangedEventArgs args)
		{
			UpdateButtonStates();
		}

		private void OnParentChanged(object instance, object key, DependencyObjectParentChangedEventArgs args) => UpdateFontPartial();

		private void InitializeProperties()
		{
			UpdatePlaceholderVisibility();
			UpdateButtonStates();
			OnInputScopeChanged(InputScope);
			OnMaxLengthChanged(MaxLength);
			OnAcceptsReturnChanged(AcceptsReturn);
			OnIsReadonlyChanged();
			OnForegroundColorChanged(null, Foreground);
			UpdateFontPartial();
			OnHeaderChanged();
			OnIsTextPredictionEnabledChanged(IsTextPredictionEnabled);
			OnSelectionHighlightColorChanged(null, SelectionHighlightColor);
			OnIsSpellCheckEnabledChanged(IsSpellCheckEnabled);
			OnTextAlignmentChanged(TextAlignment);
			OnTextWrappingChanged();
			OnFocusStateChanged(FocusState.Unfocused, FocusState, initial: true);
			OnTextCharacterCasingChanged(CharacterCasing);
			OnInputReturnTypeChanged(TextBoxExtensions.GetInputReturnType(this), initial: true);
			UpdateDescriptionVisibility(true);
			var buttonRef = _deleteButton?.GetTarget();

			if (buttonRef != null)
			{
				var thisRef = (this as IWeakReferenceProvider).WeakReference;
				buttonRef.Command = new DelegateCommand(() => (thisRef.Target as TextBox)?.DeleteButtonClick());
			}

			InitializePropertiesPartial();
		}

		protected override void OnGotFocus(RoutedEventArgs e) => StartBringIntoView(new BringIntoViewOptions
		{
			AnimationDesired = false
		});

		protected override void OnApplyTemplate()
		{
			base.OnApplyTemplate();

			// Ensures we don't keep a reference to a textBoxView that exists in a previous template
			_textBoxView = null;

			_placeHolder = GetTemplateChild(TextBoxConstants.PlaceHolderPartName) as IFrameworkElement;
			_contentElement = GetTemplateChild(TextBoxConstants.ContentElementPartName) as ContentControl;
			_header = GetTemplateChild(TextBoxConstants.HeaderContentPartName) as ContentPresenter;

			if (GetTemplateChild(TextBoxConstants.DeleteButtonPartName) is Button button)
			{
				_deleteButton = new WeakReference<Button>(button);
			}

			if (_contentElement is { })
			{
				_contentElement.SetProtectedCursor(Microsoft.UI.Input.InputSystemCursor.Create(Microsoft.UI.Input.InputSystemCursorShape.IBeam));
			}

			UpdateTextBoxView();
			InitializeProperties();
			UpdateVisualState();
		}

		partial void InitializePropertiesPartial();

		internal void OnInputReturnTypeChanged(InputReturnType inputReturnType, bool initial)
		{
			if (inputReturnType != InputReturnType.Default || !initial)
			{
				SetInputReturnTypePlatform(inputReturnType);
			}
		}

		partial void SetInputReturnTypePlatform(InputReturnType inputReturnType);

		#region Text DependencyProperty

		public string Text
		{
			get => (string)this.GetValue(TextProperty);
			set
			{
				if (value == null)
				{
#if HAS_UNO_WINUI
					value = string.Empty;
#else
					throw new ArgumentNullException();
#endif
				}

				this.SetValue(TextProperty, value);
			}
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

		public static DependencyProperty TextProperty { get; } =
			DependencyProperty.Register(
				"Text",
				typeof(string),
				typeof(TextBox),
				new FrameworkPropertyMetadata(
					defaultValue: string.Empty,
					options: FrameworkPropertyMetadataOptions.CoerceOnlyWhenChanged,
					propertyChangedCallback: (s, e) => ((TextBox)s)?.OnTextChanged(e),
					coerceValueCallback: (d, v, _) => ((TextBox)d)?.CoerceText(v)
				)
			);

		protected virtual void OnTextChanged(DependencyPropertyChangedEventArgs e)
		{
			_hasTextChangedThisFocusSession = true;

			RaiseTextChanging();

			if (!_isInputModifyingText
#if __SKIA__
				|| _isSkiaTextBox
#endif
				)
			{
				_textBoxView?.SetTextNative(Text);
			}

			UpdatePlaceholderVisibility();

			OnTextChangedPartial();

			// Update states after the text has changed, since we're
			// using selection values to compute SV scrolling.
			UpdateButtonStates();

			var focusManager = VisualTree.GetFocusManagerForElement(this);
			if (focusManager?.FocusedElement != this &&
				GetBindingExpression(TextProperty) is
				{
					ParentBinding:
					{
						IsXBind: false, // NOTE: we UpdateSource in OnTextChanged only when the binding is not an x:Bind. WinUI's generated code for x:Bind contains a simple LostFocus subscription and waits for the next LostFocus even when not focused, unlike regular Bindings.
						UpdateSourceTrigger: UpdateSourceTrigger.Default or UpdateSourceTrigger.LostFocus
					}
				} bindingExpression)
			{
				bindingExpression.UpdateSource(Text);
			}

			var isUserModifyingText = _isInputModifyingText | _isInputClearingText;
			_textChangedPendingCount++;
			_ = Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () => RaiseTextChanged(isUserModifyingText));
		}

		partial void OnTextChangedPartial();

		private void RaiseTextChanging()
		{
			if (!_isInvokingTextChanging)
			{
				try
				{
					_isInvokingTextChanging = true;
					TextChanging?.Invoke(this, new TextBoxTextChangingEventArgs());
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
					TextChanged?.Invoke(this, new TextChangedEventArgs(this, isUserModifyingText, _textChangedPendingCount > 0));
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

		private object CoerceText(object baseValue)
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
			else if (_isSkiaTextBox)
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

			var args = new TextBoxBeforeTextChangingEventArgs(baseString);
			BeforeTextChanging?.Invoke(this, args);
			if (args.Cancel)
			{
#if __SKIA__
				if (_isSkiaTextBox)
				{
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
				}
#endif
				return DependencyProperty.UnsetValue;
			}

			return baseString;
		}

		#endregion

		#region Description DependencyProperty

		public
#if __APPLE_UIKIT__
		new
#endif
		object Description
		{
			get => this.GetValue(DescriptionProperty);
			set => this.SetValue(DescriptionProperty, value);
		}

		public static DependencyProperty DescriptionProperty { get; } =
			DependencyProperty.Register(
				nameof(Description),
				typeof(object),
				typeof(TextBox),
				new FrameworkPropertyMetadata(
					defaultValue: null,
					propertyChangedCallback: (s, e) => ((TextBox)s)?.UpdateDescriptionVisibility(false)
				)
			);

		private void UpdateDescriptionVisibility(bool initialization)
		{
			if (initialization && Description == null)
			{
				// Avoid loading DescriptionPresenter element in template if not needed.
				return;
			}

			var descriptionPresenter = this.FindName("DescriptionPresenter") as ContentPresenter;
			if (descriptionPresenter != null)
			{
				descriptionPresenter.Visibility = Description != null ? Visibility.Visible : Visibility.Collapsed;
			}
		}
		#endregion

		protected override void OnFontSizeChanged(double oldValue, double newValue)
		{
			base.OnFontSizeChanged(oldValue, newValue);
			UpdateFontPartial();
		}

		protected override void OnFontFamilyChanged(FontFamily oldValue, FontFamily newValue)
		{
			base.OnFontFamilyChanged(oldValue, newValue);
			UpdateFontPartial();
		}

		protected override void OnFontStyleChanged(FontStyle oldValue, FontStyle newValue)
		{
			base.OnFontStyleChanged(oldValue, newValue);
			UpdateFontPartial();
		}

		private protected override void OnFontStretchChanged(FontStretch oldValue, FontStretch newValue)
		{
			base.OnFontStretchChanged(oldValue, newValue);
			UpdateFontPartial();
		}

		protected override void OnFontWeightChanged(FontWeight oldValue, FontWeight newValue)
		{
			base.OnFontWeightChanged(oldValue, newValue);
			UpdateFontPartial();
		}

		partial void UpdateFontPartial();

		protected override void OnForegroundColorChanged(Brush oldValue, Brush newValue)
		{
			_foregroundBrushChangedSubscription?.Dispose();
			_foregroundBrushChangedSubscription = Brush.SetupBrushChanged(newValue, ref _foregroundBrushChanged, () => OnForegroundColorChangedPartial(newValue));
		}

		partial void OnForegroundColorChangedPartial(Brush newValue);

		#region PlaceholderText DependencyProperty

		public string PlaceholderText
		{
			get => (string)this.GetValue(PlaceholderTextProperty);
			set => this.SetValue(PlaceholderTextProperty, value);
		}

		public static DependencyProperty PlaceholderTextProperty { get; } =
			DependencyProperty.Register(
				nameof(PlaceholderText),
				typeof(string),
				typeof(TextBox),
				new FrameworkPropertyMetadata(defaultValue: string.Empty, options: FrameworkPropertyMetadataOptions.AffectsMeasure)
			);

		#endregion

		#region SelectionHighlightColor DependencyProperty

		/// <summary>
		/// Gets or sets the brush used to highlight the selected text.
		/// </summary>
		public SolidColorBrush SelectionHighlightColor
		{
			get => (SolidColorBrush)GetValue(SelectionHighlightColorProperty);
			set => SetValue(SelectionHighlightColorProperty, value);
		}

		/// <summary>
		/// Identifies the SelectionHighlightColor dependency property.
		/// </summary>
		public static DependencyProperty SelectionHighlightColorProperty { get; } =
			DependencyProperty.Register(
				nameof(SelectionHighlightColor),
				typeof(SolidColorBrush),
				typeof(TextBox),
				new FrameworkPropertyMetadata(
					DefaultBrushes.SelectionHighlightColor,
					propertyChangedCallback: (s, e) => ((TextBox)s)?.OnSelectionHighlightColorChanged((SolidColorBrush)e.OldValue, (SolidColorBrush)e.NewValue)));

		private void OnSelectionHighlightColorChanged(SolidColorBrush oldBrush, SolidColorBrush newBrush)
		{
			oldBrush ??= DefaultBrushes.SelectionHighlightColor;
			newBrush ??= DefaultBrushes.SelectionHighlightColor;

			_selectionHighlightBrushChangedSubscription?.Dispose();
			_selectionHighlightBrushChangedSubscription = Brush.SetupBrushChanged(newBrush, ref _selectionHighlightColorChanged, () => OnSelectionHighlightColorChangedPartial(newBrush));
		}

		partial void OnSelectionHighlightColorChangedPartial(SolidColorBrush brush);

		#endregion

		#region PlaceholderForeground DependencyProperty

		/// <summary>
		/// Gets or sets a brush that describes the color of placeholder text.
		/// </summary>
		public Brush PlaceholderForeground
		{
			get => (Brush)GetValue(PlaceholderForegroundProperty);
			set => SetValue(PlaceholderForegroundProperty, value);
		}

		/// <summary>
		/// Identifies the PlaceholderForeground dependency property.
		/// </summary>
		public static DependencyProperty PlaceholderForegroundProperty { get; } =
			DependencyProperty.Register(
				nameof(PlaceholderForeground),
				typeof(Brush),
				typeof(TextBox),
				new FrameworkPropertyMetadata(default(Brush)));

		#endregion

		#region InputScope DependencyProperty

		public InputScope InputScope
		{
			get => (InputScope)this.GetValue(InputScopeProperty);
			set => this.SetValue(InputScopeProperty, value);
		}

		public static DependencyProperty InputScopeProperty { get; } =
			DependencyProperty.Register(
				"InputScope",
				typeof(InputScope),
				typeof(TextBox),
				new FrameworkPropertyMetadata(
					defaultValue: new InputScope()
					{
						Names =
						{
							new InputScopeName
							{
								NameValue = InputScopeNameValue.Default
							}
						}
					},
					propertyChangedCallback: (s, e) => ((TextBox)s)?.OnInputScopeChanged((InputScope)e.NewValue)
				)
			);

		private void OnInputScopeChanged(InputScope newValue) => OnInputScopeChangedPartial(newValue);
		partial void OnInputScopeChangedPartial(InputScope newValue);

		#endregion

		#region MaxLength DependencyProperty

		public int MaxLength
		{
			get => (int)this.GetValue(MaxLengthProperty);
			set => this.SetValue(MaxLengthProperty, value);
		}

		public static DependencyProperty MaxLengthProperty { get; } =
			DependencyProperty.Register(
				"MaxLength",
				typeof(int),
				typeof(TextBox),
				new FrameworkPropertyMetadata(
					defaultValue: 0,
					propertyChangedCallback: (s, e) => ((TextBox)s)?.OnMaxLengthChanged((int)e.NewValue)
				)
			);

		private void OnMaxLengthChanged(int newValue) => OnMaxLengthChangedPartial(newValue);

		partial void OnMaxLengthChangedPartial(int newValue);

		#endregion

		#region AcceptsReturn DependencyProperty

		public bool AcceptsReturn
		{
			get => (bool)this.GetValue(AcceptsReturnProperty);
			set => this.SetValue(AcceptsReturnProperty, value);
		}

		public static DependencyProperty AcceptsReturnProperty { get; } =
			DependencyProperty.Register(
				"AcceptsReturn",
				typeof(bool),
				typeof(TextBox),
				new FrameworkPropertyMetadata(
					defaultValue: false,
					propertyChangedCallback: (s, e) => ((TextBox)s)?.OnAcceptsReturnChanged((bool)e.NewValue)
				)
			);

		private void OnAcceptsReturnChanged(bool newValue)
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

		partial void OnAcceptsReturnChangedPartial(bool newValue);

		#endregion

		#region TextWrapping DependencyProperty
		public TextWrapping TextWrapping
		{
			get => (TextWrapping)this.GetValue(TextWrappingProperty);
			set => this.SetValue(TextWrappingProperty, value);
		}

		public static DependencyProperty TextWrappingProperty { get; } =
			DependencyProperty.Register(
				nameof(TextWrapping),
				typeof(TextWrapping),
				typeof(TextBox),
				new FrameworkPropertyMetadata(
					defaultValue: TextWrapping.NoWrap,
					options: FrameworkPropertyMetadataOptions.AffectsMeasure,
					propertyChangedCallback: (s, e) => ((TextBox)s)?.OnTextWrappingChanged())
				);

		private void OnTextWrappingChanged()
		{
			OnTextWrappingChangedPartial();
			UpdateButtonStates();
		}

		partial void OnTextWrappingChangedPartial();

		#endregion
#if SUPPORTS_RTL
		internal override void OnPropertyChanged2(DependencyPropertyChangedEventArgs args)
		{
			base.OnPropertyChanged2(args);
			if (args.Property == FrameworkElement.FlowDirectionProperty)
			{
				OnFlowDirectionChangedPartial();
			}
		}

		partial void OnFlowDirectionChangedPartial();
#endif

#if __APPLE_UIKIT__ || IS_UNIT_TESTS || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__
		[Uno.NotImplemented("__APPLE_UIKIT__", "IS_UNIT_TESTS", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__")]
#endif
		public CharacterCasing CharacterCasing
		{
			get => (CharacterCasing)this.GetValue(CharacterCasingProperty);
			set => this.SetValue(CharacterCasingProperty, value);
		}

#if __APPLE_UIKIT__ || IS_UNIT_TESTS || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__
		[Uno.NotImplemented("__APPLE_UIKIT__", "IS_UNIT_TESTS", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__")]
#endif
		public static DependencyProperty CharacterCasingProperty { get; } =
			DependencyProperty.Register(
				nameof(CharacterCasing),
				typeof(CharacterCasing),
				typeof(TextBox),
				new FrameworkPropertyMetadata(
						defaultValue: CharacterCasing.Normal,
						propertyChangedCallback: (s, e) => ((TextBox)s)?.OnTextCharacterCasingChanged((CharacterCasing)e.NewValue))
				);

		private void OnTextCharacterCasingChanged(CharacterCasing newValue)
		{
			OnTextCharacterCasingChangedPartial(newValue);
		}

		partial void OnTextCharacterCasingChangedPartial(CharacterCasing newValue);

		#region IsReadOnly DependencyProperty

		public bool IsReadOnly
		{
			get => (bool)GetValue(IsReadOnlyProperty);
			set => SetValue(IsReadOnlyProperty, value);
		}

		public static DependencyProperty IsReadOnlyProperty { get; } =
			DependencyProperty.Register(
				"IsReadOnly",
				typeof(bool),
				typeof(TextBox),
				new FrameworkPropertyMetadata(
					false,
					propertyChangedCallback: (s, e) => ((TextBox)s)?.OnIsReadonlyChanged()
				)
			);

		private void OnIsReadonlyChanged()
		{
			OnIsReadonlyChangedPartial();
			UpdateButtonStates();
		}

		partial void OnIsReadonlyChangedPartial();

		#endregion

		#region Header DependencyProperties

		public object Header
		{
			get => (object)GetValue(HeaderProperty);
			set => SetValue(HeaderProperty, value);
		}

		public static DependencyProperty HeaderProperty { get; } =
			DependencyProperty.Register(
				nameof(Header),
				typeof(object),
				typeof(TextBox),
				new FrameworkPropertyMetadata(
					defaultValue: null,
					options: FrameworkPropertyMetadataOptions.AffectsMeasure,
					propertyChangedCallback: (s, e) => ((TextBox)s)?.OnHeaderChanged()
				)
			);

		public DataTemplate HeaderTemplate
		{
			get => (DataTemplate)GetValue(HeaderTemplateProperty);
			set => SetValue(HeaderTemplateProperty, value);
		}

		public static DependencyProperty HeaderTemplateProperty { get; } =
			DependencyProperty.Register(
				nameof(HeaderTemplate),
				typeof(DataTemplate),
				typeof(TextBox),
				new FrameworkPropertyMetadata(
					defaultValue: null,
					options: FrameworkPropertyMetadataOptions.ValueDoesNotInheritDataContext | FrameworkPropertyMetadataOptions.AffectsMeasure,
					propertyChangedCallback: (s, e) => ((TextBox)s)?.OnHeaderChanged()
				)
			);

		private void OnHeaderChanged()
		{
			var headerVisibility = (Header != null || HeaderTemplate != null) ? Visibility.Visible : Visibility.Collapsed;

			if (_header != null)
			{
				_header.Visibility = headerVisibility;
			}
		}

		#endregion

		#region IsSpellCheckEnabled DependencyProperty

		public bool IsSpellCheckEnabled
		{
			get => (bool)this.GetValue(IsSpellCheckEnabledProperty);
			set => this.SetValue(IsSpellCheckEnabledProperty, value);
		}

		public static DependencyProperty IsSpellCheckEnabledProperty { get; } =
			DependencyProperty.Register(
				"IsSpellCheckEnabled",
				typeof(bool),
				typeof(TextBox),
				new FrameworkPropertyMetadata(
					defaultValue: true,
					propertyChangedCallback: (s, e) => ((TextBox)s)?.OnIsSpellCheckEnabledChanged((bool)e.NewValue)
				)
			);

		private void OnIsSpellCheckEnabledChanged(bool newValue) => OnIsSpellCheckEnabledChangedPartial(newValue);

		partial void OnIsSpellCheckEnabledChangedPartial(bool newValue);

		#endregion

		#region IsTextPredictionEnabled DependencyProperty

		[Uno.NotImplemented]
		public bool IsTextPredictionEnabled
		{
			get => (bool)this.GetValue(IsTextPredictionEnabledProperty);
			set => this.SetValue(IsTextPredictionEnabledProperty, value);
		}

		[Uno.NotImplemented]
		public static DependencyProperty IsTextPredictionEnabledProperty { get; } =
			DependencyProperty.Register(
				"IsTextPredictionEnabled",
				typeof(bool),
				typeof(TextBox),
				new FrameworkPropertyMetadata(
					defaultValue: true,
					propertyChangedCallback: (s, e) => ((TextBox)s)?.OnIsTextPredictionEnabledChanged((bool)e.NewValue)
				)
			);

		private void OnIsTextPredictionEnabledChanged(bool newValue) => OnIsTextPredictionEnabledChangedPartial(newValue);

		partial void OnIsTextPredictionEnabledChangedPartial(bool newValue);

		#endregion

		#region TextAlignment DependencyProperty

#if __ANDROID__
		public new TextAlignment TextAlignment
#else
		public TextAlignment TextAlignment
#endif
		{
			get { return (TextAlignment)GetValue(TextAlignmentProperty); }
			set { SetValue(TextAlignmentProperty, value); }
		}

		public static DependencyProperty TextAlignmentProperty { get; } =
			DependencyProperty.Register(
				nameof(TextAlignment),
				typeof(TextAlignment),
				typeof(TextBox),
				new FrameworkPropertyMetadata(
					TextAlignment.Left,
					FrameworkPropertyMetadataOptions.AffectsMeasure,
					(s, e) => ((TextBox)s)?.OnTextAlignmentChanged((TextAlignment)e.NewValue)));


		private void OnTextAlignmentChanged(TextAlignment newValue) => OnTextAlignmentChangedPartial(newValue);

		partial void OnTextAlignmentChangedPartial(TextAlignment newValue);

		#endregion

		public string SelectedText
		{
			get => ((string)this.GetValue(TextProperty)).Substring(SelectionStart, SelectionLength);
			set
			{
				if (value == null)
				{
					throw new ArgumentNullException();
				}

				var actual = (string)this.GetValue(TextProperty);
				actual = actual.Remove(SelectionStart, SelectionLength);
				actual = actual.Insert(SelectionStart, value);

				this.SetValue(TextProperty, actual);

				SelectionLength = value.Length;
			}
		}

		private protected override void OnIsTabStopChanged(bool oldValue, bool newValue)
		{
			base.OnIsTabStopChanged(oldValue, newValue);
			OnIsTabStopChangedPartial();
		}

		partial void OnIsTabStopChangedPartial();

		internal override void UpdateFocusState(FocusState focusState)
		{
			var oldValue = FocusState;
			base.UpdateFocusState(focusState);
			if (oldValue != focusState)
			{
				OnFocusStateChanged(oldValue, focusState, initial: false);
			}
		}

		private void OnFocusStateChanged(FocusState oldValue, FocusState newValue, bool initial)
		{
			OnFocusStateChangedPartial(newValue, initial);

			if (!initial && newValue == FocusState.Unfocused && _hasTextChangedThisFocusSession)
			{
				if (!_wasTemplateRecycled &&
					GetBindingExpression(TextProperty) is { ParentBinding.UpdateSourceTrigger: UpdateSourceTrigger.LostFocus or UpdateSourceTrigger.Default } bindingExpression)
				{
					// Manually update Source when losing focus because TextProperty's default UpdateSourceTrigger is Explicit
					bindingExpression.UpdateSource(Text);
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

		partial void OnFocusStateChangedPartial(FocusState focusState, bool initial);

		protected override void OnVisibilityChanged(Visibility oldValue, Visibility newValue)
		{
			base.OnVisibilityChanged(oldValue, newValue);
			if (newValue == Visibility.Visible)
			{
				UpdateVisualState();
			}
			else
			{
				_isPointerOver = false;
			}
		}

		protected override void OnPointerEntered(PointerRoutedEventArgs e)
		{
			base.OnPointerEntered(e);
			_isPointerOver = true;
			UpdateVisualState();
		}

		protected override void OnPointerExited(PointerRoutedEventArgs e)
		{
			base.OnPointerExited(e);
			_isPointerOver = false;
			UpdateVisualState();
		}

		protected override void OnPointerCaptureLost(PointerRoutedEventArgs e)
		{
			base.OnPointerCaptureLost(e);
			_isPointerOver = false;
			UpdateVisualState();
			OnPointerCaptureLostPartial(e);
		}

		protected override void OnPointerPressed(PointerRoutedEventArgs args)
		{
			base.OnPointerPressed(args);

			bool isPointerCaptureRequired =
#if __WASM__
				IsPointerCaptureRequired;
#else
				true;
#endif

			if (ShouldFocusOnPointerPressed(args)) // UWP Captures if the pointer is not Touch
			{
				var wasFocused = FocusState != FocusState.Unfocused;
				if (isPointerCaptureRequired)
				{
					if (CapturePointer(args.Pointer))
					{
						Focus(FocusState.Pointer);
					}
				}
				else
				{
					Focus(FocusState.Pointer);
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

		partial void OnPointerPressedPartial(PointerRoutedEventArgs args);

		partial void OnPointerReleasedPartial(PointerRoutedEventArgs args, bool wasFocused);

		partial void OnPointerCaptureLostPartial(PointerRoutedEventArgs e);

		/// <inheritdoc />
		protected override void OnPointerReleased(PointerRoutedEventArgs args)
		{
			base.OnPointerReleased(args);

			bool wasFocused = FocusState != FocusState.Unfocused;
			if (!ShouldFocusOnPointerPressed(args))
			{
				Focus(FocusState.Pointer);
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

		protected override void OnTapped(TappedRoutedEventArgs e)
		{
			base.OnTapped(e);

			OnTappedPartial();
		}

		partial void OnTappedPartial();

		/// <inheritdoc />
		protected override void OnKeyDown(KeyRoutedEventArgs args)
		{
			OnKeyDownPartial(args);

			var modifiers = CoreImports.Input_GetKeyboardModifiers();
			if (!args.Handled && KeyboardAcceleratorUtility.IsKeyValidForAccelerators(args.Key, KeyboardAcceleratorUtility.MapVirtualKeyModifiersToIntegersModifiers(modifiers)))
			{
				bool shouldNotImpedeTextInput = KeyboardAcceleratorUtility.TextInputHasPriorityForKey(
					args.Key,
					modifiers.HasFlag(VirtualKeyModifiers.Control),
					modifiers.HasFlag(VirtualKeyModifiers.Menu));
				args.HandledShouldNotImpedeTextInput = shouldNotImpedeTextInput;
			}
		}

		partial void OnKeyDownPartial(KeyRoutedEventArgs args);

#if !__SKIA__
		partial void OnKeyDownPartial(KeyRoutedEventArgs args) => OnKeyDownInternal(args);
#endif

		private void OnKeyDownInternal(KeyRoutedEventArgs args)
		{
			base.OnKeyDown(args);


			// On skia, sometimes SelectionStart is updated to a new value before KeyDown is fired, so
			// we need to get selectionStart from another source on Skia.
#if __SKIA__
			var selectionStart = TextBoxView.SelectionBeforeKeyDown.start;
#else
			var selectionStart = SelectionStart;
#endif

			// Note: On windows only keys that are "moving the cursor" are handled
			//		 AND ** only KeyDown ** is handled (not KeyUp)
			switch (args.Key)
			{
				case VirtualKey.Up:
					if (AcceptsReturn)
					{
						args.Handled = true;
					}
					break;
				case VirtualKey.Down:
					if (selectionStart != Text.Length)
					{
						SelectionStart = Text.Length;
						args.Handled = true;
					}
					if (AcceptsReturn)
					{
						args.Handled = true;
					}
					break;
				case VirtualKey.Left:
					if (selectionStart != 0)
					{
						args.Handled = true;
					}
					break;
				case VirtualKey.Right:
					if (selectionStart != Text.Length)
					{
						args.Handled = true;
					}
					break;
				case VirtualKey.Home:
				case VirtualKey.End:
					args.Handled = true;
					break;
			}

#if __WASM__
			if (args.Handled)
			{
				// Marking the routed event as Handled makes the browser call preventDefault() for key events.
				// This is a problem as it breaks the browser caret navigation within the input.
				((IHtmlHandleableRoutedEventArgs)args).HandledResult &= ~HtmlEventDispatchResult.PreventDefault;
			}
#endif
		}

		protected virtual void UpdateButtonStates()
		{
			if (this.Log().IsEnabled(LogLevel.Debug))
			{
				this.Log().LogDebug(nameof(UpdateButtonStates));
			}

			var changed = false;
			// Minimum width for TextBox with DeleteButton visible is 5em.
			if (CanShowButton && _isButtonEnabled && ActualWidth > FontSize * 5)
			{
				changed |= VisualStateManager.GoToState(this, TextBoxConstants.ButtonVisibleStateName, true);
			}
			else
			{
				changed |= VisualStateManager.GoToState(this, TextBoxConstants.ButtonCollapsedStateName, true);
			}

#if __SKIA__
			_deleteButtonVisibilityChangedSinceLastUpdateScrolling |= changed;

			DispatchUpdateScrolling();
#endif
		}


#if __SKIA__
		bool _pendingUpdateScrolling;

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

		partial void OnDeleteButtonClickPartial();

		internal void OnSelectionChanged() => SelectionChanged?.Invoke(this, new RoutedEventArgs(this));

		public void OnTemplateRecycled()
		{
			_suppressTextChanged = true;
			Text = string.Empty;
			_wasTemplateRecycled = true;
		}

		protected override AutomationPeer OnCreateAutomationPeer() => new TextBoxAutomationPeer(this);

		public override string GetAccessibilityInnerText() => Text;

		// TODO: Remove as a breaking change for Uno 6
		// Also, make OnVerticalContentAlignmentChanged private protected.
		protected override void OnVerticalContentAlignmentChanged(VerticalAlignment oldVerticalContentAlignment, VerticalAlignment newVerticalContentAlignment) { }

		public void Select(int start, int length)
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

			var textBoxSelectionChangingEventArgs = new TextBoxSelectionChangingEventArgs(start, length);
			SelectionChanging?.Invoke(this, textBoxSelectionChangingEventArgs);
			if (!textBoxSelectionChangingEventArgs.Cancel || textBoxSelectionChangingEventArgs.SelectionStart + textBoxSelectionChangingEventArgs.SelectionLength > Text.Length)
			{
				SelectPartial(start, length);
				OnSelectionChanged();
			}
		}

		public void SelectAll() => SelectAllPartial();

		partial void SelectPartial(int start, int length);

		partial void SelectAllPartial();

		public void PasteFromClipboard()
		{
			_ = Dispatcher.RunAsync(CoreDispatcherPriority.High, async () =>
			{
				var content = Clipboard.GetContent();
				string clipboardText;
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
			});
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
			RaisePaste(new TextControlPasteEventArgs());
#endif
		}

		partial void PasteFromClipboardPartial(string adjustedClipboardText, int selectionStart, string newText);

		/// <summary>
		/// Copies the selected content to the OS clipboard.
		/// </summary>
		public void CopySelectionToClipboard()
		{
			if (SelectionLength > 0)
			{
				var text = SelectedText;
				var dataPackage = new DataPackage();
				dataPackage.SetText(text);
				Clipboard.SetContent(dataPackage);
			}
		}

		/// <summary>
		/// Moves the selected content to the OS clipboard and removes it from the text control.
		/// </summary>
		public void CutSelectionToClipboard()
		{
			if (IsReadOnly)
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

		partial void CutSelectionToClipboardPartial();

		internal override bool CanHaveChildren() => true;

		internal override void UpdateThemeBindings(Data.ResourceUpdateReason updateReason)
		{
			base.UpdateThemeBindings(updateReason);

			UpdateKeyboardThemePartial();
		}

		partial void UpdateKeyboardThemePartial();

		private protected override void OnIsEnabledChanged(IsEnabledChangedEventArgs e)
		{
			base.OnIsEnabledChanged(e);
			UpdateVisualState();
			OnIsEnabledChangedPartial(e);
		}

		partial void OnIsEnabledChangedPartial(IsEnabledChangedEventArgs e);

		private bool ShouldFocusOnPointerPressed(PointerRoutedEventArgs args) =>
			// For mouse and pen, the TextBox should focus on pointer press
			// (and then capture pointer to make sure to handle the whol down->move->up sequence).
			// For touch we wait for the release to focus (avoid flickering in case of cancel due to scroll for instance).
			args.Pointer.PointerDeviceType != PointerDeviceType.Touch;
	}
}
