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

using Microsoft.UI.Input;
using PointerDeviceType = Microsoft.UI.Input.PointerDeviceType;
using Uno.UI.Xaml.Controls;
using System.Linq;
#if __SKIA__
using Microsoft.UI.Xaml.Internal;
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
		protected private bool _isButtonEnabled = true;

		public event TextChangedEventHandler TextChanged;
		public event TypedEventHandler<TextBox, TextBoxTextChangingEventArgs> TextChanging;
		public event TypedEventHandler<TextBox, TextBoxBeforeTextChangingEventArgs> BeforeTextChanging;
		public event RoutedEventHandler SelectionChanged;

		public event TypedEventHandler<TextBox, TextBoxSelectionChangingEventArgs> SelectionChanging;

#if __SKIA__
		public event ContextMenuOpeningEventHandler ContextMenuOpening;
#endif

#if !IS_UNIT_TESTS
		/// <summary>
		/// Occurs when text is pasted into the control.
		/// </summary>
		public
			event TextControlPasteEventHandler Paste;

		internal void RaisePaste(TextControlPasteEventArgs args) => Paste?.Invoke(this, args);
#endif


		public TextBox()
		{
			_core = new TextBoxCore(this);

			DefaultStyleKey = typeof(TextBox);

			_core.Initialize();

			InitializePartial();
		}

		partial void InitializePartial();

		partial void OnLoadedPartial();

		private protected override void OnLoaded()
		{
			base.OnLoaded();

			OnLoadedPartial();

			_core.OnLoadedCore();
		}

		private protected override void OnUnloaded()
		{
			base.OnUnloaded();

			_core.OnUnloadedCore();
		}

		protected override void OnGotFocus(RoutedEventArgs e) => _core.OnGotFocusCore();

		protected override void OnApplyTemplate()
		{
			base.OnApplyTemplate();

			_core.ApplyTemplate();

#if __SKIA__
			UpdateHighContrastBackgroundOverride();
#endif
		}

		internal void OnInputReturnTypeChanged(InputReturnType inputReturnType, bool initial)
			=> _core.OnInputReturnTypeChanged(inputReturnType, initial);

		#region Text DependencyProperty

		public string Text
		{
			get => (string)this.GetValue(TextProperty);
			set
			{
				if (value == null)
				{
					value = string.Empty;
				}

				this.SetValue(TextProperty, value);
			}
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
					coerceValueCallback: (d, v, _) => ((TextBox)d)?._core.CoerceText(v)
				)
			);

		protected virtual void OnTextChanged(DependencyPropertyChangedEventArgs e)
			=> _core.OnTextChangedCore((string)e.OldValue, (string)e.NewValue);





		#endregion

		#region Description DependencyProperty

		public
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
					propertyChangedCallback: (s, e) => ((TextBox)s)?._core.UpdateDescriptionVisibility(false)
				)
			);

		#endregion

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

		internal string ProcessTextInput(string newText) => _core.ProcessTextInput(newText);

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
					propertyChangedCallback: (s, e) => ((TextBox)s)?._core.OnSelectionHighlightColorChanged((SolidColorBrush)e.OldValue, (SolidColorBrush)e.NewValue)));



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
					propertyChangedCallback: (s, e) => ((TextBox)s)?._core.OnInputScopeChanged((InputScope)e.NewValue)
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
				"MaxLength",
				typeof(int),
				typeof(TextBox),
				new FrameworkPropertyMetadata(
					defaultValue: 0,
					propertyChangedCallback: (s, e) => ((TextBox)s)?._core.OnMaxLengthChanged((int)e.NewValue)
				)
			);



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
					propertyChangedCallback: (s, e) => ((TextBox)s)?._core.OnAcceptsReturnChanged((bool)e.NewValue)
				)
			);



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
					propertyChangedCallback: (s, e) => ((TextBox)s)?._core.OnTextWrappingChanged())
				);



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

#if IS_UNIT_TESTS || __SKIA__ || __NETSTD_REFERENCE__
		[Uno.NotImplemented("IS_UNIT_TESTS", "__SKIA__", "__NETSTD_REFERENCE__")]
#endif
		public CharacterCasing CharacterCasing
		{
			get => (CharacterCasing)this.GetValue(CharacterCasingProperty);
			set => this.SetValue(CharacterCasingProperty, value);
		}

#if IS_UNIT_TESTS || __SKIA__ || __NETSTD_REFERENCE__
		[Uno.NotImplemented("IS_UNIT_TESTS", "__SKIA__", "__NETSTD_REFERENCE__")]
#endif
		public static DependencyProperty CharacterCasingProperty { get; } =
			DependencyProperty.Register(
				nameof(CharacterCasing),
				typeof(CharacterCasing),
				typeof(TextBox),
				new FrameworkPropertyMetadata(
						defaultValue: CharacterCasing.Normal,
						propertyChangedCallback: (s, e) => ((TextBox)s)?._core.OnTextCharacterCasingChanged((CharacterCasing)e.NewValue))
				);



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
					propertyChangedCallback: (s, e) => ((TextBox)s)?._core.OnIsReadonlyChanged()
				)
			);



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
					propertyChangedCallback: (s, e) => ((TextBox)s)?._core.OnHeaderChanged()
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
					propertyChangedCallback: (s, e) => ((TextBox)s)?._core.OnHeaderChanged()
				)
			);


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
					propertyChangedCallback: (s, e) => ((TextBox)s)?._core.OnIsSpellCheckEnabledChanged((bool)e.NewValue)
				)
			);



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
					propertyChangedCallback: (s, e) => ((TextBox)s)?._core.OnIsTextPredictionEnabledChanged((bool)e.NewValue)
				)
			);



		#endregion

		#region TextAlignment DependencyProperty

		public TextAlignment TextAlignment
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
					(s, e) => ((TextBox)s)?._core.OnTextAlignmentChanged((TextAlignment)e.NewValue)));




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
				_core.OnFocusStateChanged(oldValue, focusState, initial: false);
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




		/// <inheritdoc />
		protected override void OnPointerReleased(PointerRoutedEventArgs args)
		{
			base.OnPointerReleased(args);

			_core.OnPointerReleased(args);
		}

		protected override void OnTapped(TappedRoutedEventArgs e)
		{
			base.OnTapped(e);

			OnTappedPartial();
		}

		partial void OnTappedPartial();


		protected override void OnKeyDown(KeyRoutedEventArgs args)
		{
			base.OnKeyDown(args);

			_core.OnKeyDown(args);
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

#if !__SKIA__
#endif

		protected virtual void UpdateButtonStates() => _core.UpdateButtonStatesCore();






		internal void OnSelectionChanged() => SelectionChanged?.Invoke(this, new RoutedEventArgs(this));

		void IFrameworkTemplatePoolAware.OnTemplateRecycled()
		{
			_core.OnTemplateRecycled();
		}

		protected override AutomationPeer OnCreateAutomationPeer() => new TextBoxAutomationPeer(this);

		public override string GetAccessibilityInnerText() => Text;

		public void Select(int start, int length) => _core.Select(start, length);

		public void SelectAll() => _core.SelectAll();

		public void PasteFromClipboard() => _core.PasteFromClipboard();



		/// <summary>
		/// Copies the selected content to the OS clipboard.
		/// </summary>
		public void CopySelectionToClipboard() => _core.CopySelectionToClipboard();

		/// <summary>
		/// Moves the selected content to the OS clipboard and removes it from the text control.
		/// </summary>
		public void CutSelectionToClipboard() => _core.CutSelectionToClipboard();

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
	}
}
