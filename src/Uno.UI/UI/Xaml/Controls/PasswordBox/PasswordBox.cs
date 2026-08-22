using System;
using DirectUI;
using Microsoft.UI.Xaml.Automation.Peers;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Uno;
using Uno.Disposables;
using Uno.Extensions;
using Uno.UI.Xaml.Media;
using Windows.System;
using Windows.UI.Text;

namespace Microsoft.UI.Xaml.Controls
{
	public partial class PasswordBox : Control, IFrameworkTemplatePoolAware
	{
		// On Windows, ● is used as password character.
		// However, this character can't be retrieved on Android (doesn't exist in any system font) and on some browser/OS combinations.
		// We use • instead, which is already the one normally used by Android and all the major browsers.
		// See https://github.com/mozilla/gecko-dev/blob/1d4c27f9f166ce6e967fb0e8c8d6e0795dbbd12e/widget/android/nsLookAndFeel.cpp#L441
		internal static readonly string DefaultPasswordChar = OperatingSystem.IsAndroid() || OperatingSystem.IsBrowser() ? "•" : "●";

		private protected bool _isButtonEnabled = true;

		public event RoutedEventHandler PasswordChanged;

		public const string RevealButtonPartName = "RevealButton";
		private ButtonBase _revealButton;
		private readonly SerialDisposable _revealButtonSubscription = new SerialDisposable();
		private bool UseIsPasswordEnabledProperty => this.IsDependencyPropertySet(IsPasswordRevealButtonEnabledProperty) && !this.IsDependencyPropertySet(PasswordRevealModeProperty);

		public PasswordBox()
		{
			_core = new TextBoxCore(this);

			DefaultStyleKey = typeof(PasswordBox);

			_core.Initialize();
		}

#if !IS_UNIT_TESTS
		/// <summary>
		/// Occurs when content is pasted into the control.
		/// </summary>
		public event TextControlPasteEventHandler Paste;

		internal void RaisePaste(TextControlPasteEventArgs args) => Paste?.Invoke(this, args);
#endif

		public void SelectAll() => _core.SelectAll();

		/// <summary>
		/// Copies content from the OS clipboard into the text control.
		/// </summary>
		public void PasteFromClipboard() => _core.PasteFromClipboard();

		private protected override void OnLoaded()
		{
			base.OnLoaded();

			_core.OnLoadedCore();

			RegisterSetPasswordScope();
			UpdateDescriptionVisibility(true);
		}

		private protected override void OnUnloaded()
		{
			base.OnUnloaded();

			_core.OnUnloadedCore();

			_revealButtonSubscription.Disposable = null;
		}

		protected override void OnGotFocus(RoutedEventArgs e) => _core.OnGotFocusCore();

		protected override void OnApplyTemplate()
		{
			base.OnApplyTemplate();

			_core.ApplyTemplate();
		}

		private void RegisterSetPasswordScope()
		{
			_revealButton = this.GetTemplateChild(RevealButtonPartName) as ButtonBase;

			if (_revealButton != null)
			{
				var beginReveal = new PointerEventHandler(BeginReveal);
				var endReveal = new PointerEventHandler(EndReveal);

				// Button will handle Pressed and Released, so we have subscribe to handled events too
				_revealButton.AddHandler(PointerPressedEvent, beginReveal, handledEventsToo: true);
				_revealButton.AddHandler(PointerReleasedEvent, endReveal, handledEventsToo: true);
				_revealButton.AddHandler(PointerExitedEvent, endReveal, handledEventsToo: true);
				_revealButton.AddHandler(PointerCanceledEvent, endReveal, handledEventsToo: true);
				_revealButton.AddHandler(PointerCaptureLostEvent, endReveal, handledEventsToo: true);

				_revealButtonSubscription.Disposable = Disposable.Create(() =>
				{
					_revealButton.RemoveHandler(PointerPressedEvent, beginReveal);
					_revealButton.RemoveHandler(PointerReleasedEvent, endReveal);
					_revealButton.RemoveHandler(PointerExitedEvent, endReveal);
					_revealButton.RemoveHandler(PointerCanceledEvent, endReveal);
					_revealButton.RemoveHandler(PointerCaptureLostEvent, endReveal);
				});
			}

			CheckRevealModeForScope();
		}

		private void BeginReveal(object sender, PointerRoutedEventArgs e)
		{
			SetPasswordRevealState(PasswordRevealState.Revealed);
		}

		private void EndReveal(object sender, PointerRoutedEventArgs e)
		{
			SetPasswordRevealState(PasswordRevealState.Obscured);
			EndRevealPartial();
		}

		partial void EndRevealPartial();

		partial void SetPasswordRevealState(PasswordRevealState state);

		#region Password DependencyProperty

		public string Password
		{
			get { return (string)this.GetValue(PasswordProperty); }
			set { this.SetValue(PasswordProperty, value); }
		}

		public static DependencyProperty PasswordProperty { get; } =
			DependencyProperty.Register(
				"Password",
				typeof(string),
				typeof(PasswordBox),
				new FrameworkPropertyMetadata(
					defaultValue: string.Empty,
					options: FrameworkPropertyMetadataOptions.CoerceOnlyWhenChanged,
					propertyChangedCallback: (s, e) => ((PasswordBox)s)?.OnPasswordChanged(e),
					coerceValueCallback: (d, v, _) => ((PasswordBox)d)?._core.CoerceText(v)
				)
			);

		private void OnPasswordChanged(DependencyPropertyChangedEventArgs e)
		{
			_core.OnTextChangedCore((string)e.OldValue, (string)e.NewValue);

			OnPasswordChangedPartial(e);

			if (Password.IsNullOrEmpty() &&
				((PasswordRevealMode == PasswordRevealMode.Peek) || (UseIsPasswordEnabledProperty && IsPasswordRevealButtonEnabled)))
			{
				_isButtonEnabled = true;
			}
		}

		partial void OnPasswordChangedPartial(DependencyPropertyChangedEventArgs e);

		#endregion

		[NotImplemented("__IOS__", "__TVOS__", "IS_UNIT_TESTS", "__WASM__")]
		public string PasswordChar
		{
			get => (string)this.GetValue(PasswordCharProperty);
			set => this.SetValue(PasswordCharProperty, value);
		}

		[NotImplemented("__IOS__", "__TVOS__", "IS_UNIT_TESTS", "__WASM__")]
		public static DependencyProperty PasswordCharProperty { get; } =
			DependencyProperty.Register(
				nameof(PasswordChar),
				typeof(string),
				typeof(PasswordBox),
				new FrameworkPropertyMetadata(
					DefaultPasswordChar,
					propertyChangedCallback: (s, e) => ((PasswordBox)s)?.OnPasswordCharChanged(e)));

		private void OnPasswordCharChanged(DependencyPropertyChangedEventArgs e)
		{
			OnPasswordCharChangedPartial(e);
		}

		partial void OnPasswordCharChangedPartial(DependencyPropertyChangedEventArgs e);

		#region Description DependencyProperty

		public object Description
		{
			get => this.GetValue(DescriptionProperty);
			set => this.SetValue(DescriptionProperty, value);
		}

		public static DependencyProperty DescriptionProperty { get; } =
			DependencyProperty.Register(
				nameof(Description),
				typeof(object),
				typeof(PasswordBox),
				new FrameworkPropertyMetadata(
					defaultValue: null,
					propertyChangedCallback: (s, e) => (s as PasswordBox)?.UpdateDescriptionVisibility(false)));

		#endregion

		#region Header DependencyProperty

		public object Header
		{
			get => this.GetValue(HeaderProperty);
			set => this.SetValue(HeaderProperty, value);
		}

		public static DependencyProperty HeaderProperty { get; } =
			DependencyProperty.Register(
				nameof(Header),
				typeof(object),
				typeof(PasswordBox),
				new FrameworkPropertyMetadata(
					defaultValue: null,
					options: FrameworkPropertyMetadataOptions.AffectsMeasure,
					propertyChangedCallback: (s, e) => ((PasswordBox)s)?._core.OnHeaderChanged()
				)
			);

		#endregion

		#region HeaderTemplate DependencyProperty

		public DataTemplate HeaderTemplate
		{
			get => (DataTemplate)this.GetValue(HeaderTemplateProperty);
			set => this.SetValue(HeaderTemplateProperty, value);
		}

		public static DependencyProperty HeaderTemplateProperty { get; } =
			DependencyProperty.Register(
				nameof(HeaderTemplate),
				typeof(DataTemplate),
				typeof(PasswordBox),
				new FrameworkPropertyMetadata(
					defaultValue: null,
					options: FrameworkPropertyMetadataOptions.ValueDoesNotInheritDataContext | FrameworkPropertyMetadataOptions.AffectsMeasure,
					propertyChangedCallback: (s, e) => ((PasswordBox)s)?._core.OnHeaderChanged()
				)
			);

		#endregion

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
				typeof(PasswordBox),
				new FrameworkPropertyMetadata(defaultValue: string.Empty, options: FrameworkPropertyMetadataOptions.AffectsMeasure)
			);

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
				typeof(PasswordBox),
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
					propertyChangedCallback: (s, e) => ((PasswordBox)s)?._core.OnInputScopeChanged((InputScope)e.NewValue)
				)
			);

		#endregion

		#region MaxLength DependencyProperty

		public int MaxLength
		{
			get => (int)this.GetValue(MaxLengthProperty);
			set => this.SetValue(MaxLengthProperty, value);
		}

		public static DependencyProperty MaxLengthProperty { get; } =
			DependencyProperty.Register(
				nameof(MaxLength),
				typeof(int),
				typeof(PasswordBox),
				new FrameworkPropertyMetadata(
					defaultValue: 0,
					propertyChangedCallback: (s, e) => ((PasswordBox)s)?._core.OnMaxLengthChanged((int)e.NewValue)
				)
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
				typeof(PasswordBox),
				new FrameworkPropertyMetadata(
					DefaultBrushes.SelectionHighlightColor,
					propertyChangedCallback: (s, e) => ((PasswordBox)s)?._core.OnSelectionHighlightColorChanged((SolidColorBrush)e.OldValue, (SolidColorBrush)e.NewValue)));

		#endregion

		#region IsPasswordRevealButtonEnabled DependencyProperty
		public bool IsPasswordRevealButtonEnabled
		{
			get => (bool)this.GetValue(IsPasswordRevealButtonEnabledProperty);
			set => this.SetValue(IsPasswordRevealButtonEnabledProperty, value);
		}

		public static global::Microsoft.UI.Xaml.DependencyProperty IsPasswordRevealButtonEnabledProperty { get; } =
			DependencyProperty.Register(
				nameof(IsPasswordRevealButtonEnabled),
				typeof(bool),
				typeof(PasswordBox),
				new FrameworkPropertyMetadata(
					defaultValue: true,
					propertyChangedCallback: (s, e) => ((PasswordBox)s)?.OnIsPasswordRevealButtonEnabledChanged(e)
				)
			);

		private void OnIsPasswordRevealButtonEnabledChanged(DependencyPropertyChangedEventArgs e)
		{
			CheckRevealModeForScope();
			OnIsPasswordRevealButtonEnabledChangedPartial(e);
		}

		partial void OnIsPasswordRevealButtonEnabledChangedPartial(DependencyPropertyChangedEventArgs e);
		#endregion

		#region PasswordRevealMode DependencyProperty
		public PasswordRevealMode PasswordRevealMode
		{
			get => (PasswordRevealMode)this.GetValue(PasswordRevealModeProperty);
			set => this.SetValue(PasswordRevealModeProperty, value);
		}

		public static global::Microsoft.UI.Xaml.DependencyProperty PasswordRevealModeProperty { get; } =
			DependencyProperty.Register(
				nameof(PasswordRevealMode),
				typeof(PasswordRevealMode),
				typeof(PasswordBox),
				new FrameworkPropertyMetadata(
					defaultValue: PasswordRevealMode.Peek,
					propertyChangedCallback: (s, e) => ((PasswordBox)s)?.OnPasswordRevealModeChanged(e)
				)
			);

		private void OnPasswordRevealModeChanged(DependencyPropertyChangedEventArgs e)
		{
			CheckRevealModeForScope();
		}

		private void CheckRevealModeForScope()
		{
			// Only use IsPasswordRevealButtonEnabled if it is set and PasswordRevealMode is not
			if (UseIsPasswordEnabledProperty)
			{
				SetPasswordRevealState(PasswordRevealState.Obscured);
			}
			else
			{
				switch (PasswordRevealMode)
				{
					case PasswordRevealMode.Visible:
						SetPasswordRevealState(PasswordRevealState.Revealed);
						break;
					case PasswordRevealMode.Hidden:
					case PasswordRevealMode.Peek:
					default:
						SetPasswordRevealState(PasswordRevealState.Obscured);
						break;
				}
			}
		}
		#endregion

#if SUPPORTS_RTL
		internal override void OnPropertyChanged2(DependencyPropertyChangedEventArgs args)
		{
			base.OnPropertyChanged2(args);
			if (args.Property == FrameworkElement.FlowDirectionProperty)
			{
				_core.OnFlowDirectionChanged();
			}
		}
#endif

		protected override void OnFontSizeChanged(double oldValue, double newValue)
		{
			base.OnFontSizeChanged(oldValue, newValue);
			_core.UpdateFont();
		}

		protected override void OnFontFamilyChanged(FontFamily oldValue, FontFamily newValue)
		{
			base.OnFontFamilyChanged(oldValue, newValue);
			_core.UpdateFont();
		}

		protected override void OnFontStyleChanged(FontStyle oldValue, FontStyle newValue)
		{
			base.OnFontStyleChanged(oldValue, newValue);
			_core.UpdateFont();
		}

		private protected override void OnFontStretchChanged(FontStretch oldValue, FontStretch newValue)
		{
			base.OnFontStretchChanged(oldValue, newValue);
			_core.UpdateFont();
		}

		protected override void OnFontWeightChanged(FontWeight oldValue, FontWeight newValue)
		{
			base.OnFontWeightChanged(oldValue, newValue);
			_core.UpdateFont();
		}

		protected override void OnForegroundColorChanged(Brush oldValue, Brush newValue)
			=> _core.OnForegroundColorChanged(newValue);

		internal override void UpdateFocusState(FocusState focusState)
		{
			var oldValue = FocusState;
			base.UpdateFocusState(focusState);
			if (oldValue != focusState)
			{
				_core.OnFocusStateChanged(oldValue, focusState, initial: false);
			}

			OnRevealButtonFocusStateChanged(oldValue, focusState);
		}

		private void OnRevealButtonFocusStateChanged(FocusState oldValue, FocusState newValue)
		{
			if (oldValue == newValue) { return; }

			if (oldValue == FocusState.Unfocused)
			{
				if (UseIsPasswordEnabledProperty)
				{
					_isButtonEnabled = IsPasswordRevealButtonEnabled;

					if (_isButtonEnabled)
					{
						VisualStateManager.GoToState(this, TextBoxConstants.ButtonVisibleStateName, true);
					}
					else
					{
						VisualStateManager.GoToState(this, TextBoxConstants.ButtonCollapsedStateName, true);
					}
				}
				else
				{
					if (PasswordRevealMode == PasswordRevealMode.Peek && Password.IsNullOrEmpty())
					{
						_isButtonEnabled = true;
					}
					else
					{
						_isButtonEnabled = false;
					}

					VisualStateManager.GoToState(this, TextBoxConstants.ButtonCollapsedStateName, true);
				}
			}
		}

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
			_core.OnPointerCaptureLost(e);
		}

		protected override void OnPointerPressed(PointerRoutedEventArgs args)
		{
			base.OnPointerPressed(args);

			_core.OnPointerPressed(args);
		}

		protected override void OnPointerReleased(PointerRoutedEventArgs args)
		{
			base.OnPointerReleased(args);

			_core.OnPointerReleased(args);
		}

		protected override void OnKeyDown(KeyRoutedEventArgs args)
		{
			base.OnKeyDown(args);

			_core.OnKeyDown(args);
		}

		protected override void OnCharacterReceived(CharacterReceivedRoutedEventArgs e)
		{
			base.OnCharacterReceived(e);

			_core.OnCharacterReceived(e);
		}

		private protected override void OnPostKeyDown(KeyRoutedEventArgs args)
		{
			_core.OnPostKeyDown(args);

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

		protected virtual void UpdateButtonStates() => _core.UpdateButtonStatesCore();

		internal override void UpdateVisualState(bool useTransitions = true)
			=> _core.UpdateVisualStateCore(useTransitions);

		internal override string GetPlainText()
		{
			// Header or placeholder only — never the password.
			if (Header is not null)
			{
				var plainText = FrameworkElement.GetStringFromObject(Header);
				if (!string.IsNullOrEmpty(plainText))
				{
					return plainText;
				}
			}

			return PlaceholderText ?? string.Empty;
		}

		void IFrameworkTemplatePoolAware.OnTemplateRecycled()
		{
			_core.OnTemplateRecycled();
		}

		protected override AutomationPeer OnCreateAutomationPeer()
		{
			return new PasswordBoxAutomationPeer(this);
		}

		public override string GetAccessibilityInnerText()
		{
			// We don't want to reveal the password
			return null;
		}

		internal override bool CanHaveChildren() => true;

		private protected override void OnIsEnabledChanged(IsEnabledChangedEventArgs e)
		{
			base.OnIsEnabledChanged(e);
			UpdateVisualState();
		}

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
	}
}
