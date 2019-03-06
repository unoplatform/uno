#if NET461 || __WASM__ || __MACOS__
#pragma warning disable CS0067, CS649
#endif

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Windows.Input;
using Uno.Extensions;
using Uno.UI.Common;
using Windows.UI.Text;
using Windows.UI.Xaml.Automation;
using Windows.UI.Xaml.Automation.Peers;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;

namespace Windows.UI.Xaml.Controls
{
	public class TextBoxConstants
	{
		public const string HeaderContentPartName = "HeaderContentPresenter";
		public const string ContentElementPartName = "ContentElement";
		public const string PlaceHolderPartName = "PlaceholderTextContentPresenter";
		public const string DeleteButtonPartName = "DeleteButton";
	}

	public partial class TextBox : Control, IFrameworkTemplatePoolAware
	{
		private const string ButtonVisibleStateName = "ButtonVisible";
		private const string ButtonCollapsedStateName = "ButtonCollapsed";

#pragma warning disable CS0067, CS0649
		private IFrameworkElement _placeHolder;
		private ContentControl _contentElement;
		private WeakReference<Button> _deleteButton;
#pragma warning restore CS0067, CS0649

		private ContentPresenter _header;
		private bool _isPassword;

		public event TextChangedEventHandler TextChanged;
		public event RoutedEventHandler SelectionChanged;

		public TextBox()
		{
			_isPassword = false;
			InitializeVisualStates();
			this.RegisterParentChangedCallback(this, OnParentChanged);
		}

		private void OnParentChanged(object instance, object key, DependencyObjectParentChangedEventArgs args)
		{
			UpdateFontPartial(this);
		}

		protected TextBox(bool isPassword)
		{
			_isPassword = isPassword;
		}

		private void InitializeProperties()
		{
			OnTextChanged(CreateInitialValueChangerEventArgs(TextProperty, null, Text));
			OnInputScopeChanged(CreateInitialValueChangerEventArgs(InputScopeProperty, null, InputScope));
			OnMaxLengthChanged(CreateInitialValueChangerEventArgs(MaxLengthProperty, null, MaxLength));
			OnAcceptsReturnChanged(CreateInitialValueChangerEventArgs(AcceptsReturnProperty, null, AcceptsReturn));
			OnIsEnabledChanged(false, IsEnabled);
			OnForegroundColorChanged(null, Foreground);
			UpdateFontPartial(this);
			OnHeaderChanged();
			OnIsTextPredictionEnabledChanged(CreateInitialValueChangerEventArgs(IsTextPredictionEnabledProperty, IsTextPredictionEnabledProperty.GetMetadata(GetType()).DefaultValue, IsTextPredictionEnabled));
			OnIsSpellCheckEnabledChanged(CreateInitialValueChangerEventArgs(IsSpellCheckEnabledProperty, IsSpellCheckEnabledProperty.GetMetadata(GetType()).DefaultValue, IsSpellCheckEnabled));
			OnTextAlignmentChanged(CreateInitialValueChangerEventArgs(TextAlignmentProperty, TextAlignmentProperty.GetMetadata(GetType()).DefaultValue, TextAlignment));
			OnTextWrappingChanged(CreateInitialValueChangerEventArgs(TextWrappingProperty, TextWrappingProperty.GetMetadata(GetType()).DefaultValue, TextWrapping));
			OnFocusStateChanged((FocusState)FocusStateProperty.GetMetadata(GetType()).DefaultValue, FocusState);

			var buttonRef = _deleteButton?.GetTarget();

			if (buttonRef != null)
			{
				buttonRef.Command = new DelegateCommand(DeleteText);
			}

			InitializePropertiesPartial();
		}

		protected override void OnApplyTemplate()
		{
			base.OnApplyTemplate();

#if !NET461
			// Ensures we don't keep a reference to a textBoxView that exists in a previous template
			_textBoxView = null;
#endif

			_placeHolder = GetTemplateChild(TextBoxConstants.PlaceHolderPartName) as IFrameworkElement;
			_contentElement = GetTemplateChild(TextBoxConstants.ContentElementPartName) as ContentControl;
			_header = GetTemplateChild(TextBoxConstants.HeaderContentPartName) as ContentPresenter;

			if (_contentElement is ScrollViewer scrollViewer)
			{
#if __IOS__
				// We disable scrolling because the inner TextBoxView provides its own scrolling
				scrollViewer.HorizontalScrollMode = ScrollMode.Disabled;
				scrollViewer.VerticalScrollMode = ScrollMode.Disabled;
				scrollViewer.HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled;
				scrollViewer.VerticalScrollBarVisibility = ScrollBarVisibility.Disabled;
#elif __WASM__
				// We disable horizontal scrolling because the inner SingleLineTextBoxView provides its own horizontal scrolling
				scrollViewer.HorizontalScrollMode = ScrollMode.Disabled;
#endif
			}

			if (GetTemplateChild(TextBoxConstants.DeleteButtonPartName) is Button button)
			{
				_deleteButton = new WeakReference<Button>(button);
			}

#if !NET461
			UpdateTextBoxView();
#endif
			InitializeProperties();
		}

		partial void InitializePropertiesPartial();

		private DependencyPropertyChangedEventArgs CreateInitialValueChangerEventArgs(DependencyProperty property, object oldValue, object newValue)
		{
			return new DependencyPropertyChangedEventArgs(property, oldValue, DependencyPropertyValuePrecedences.DefaultValue, newValue, DependencyPropertyValuePrecedences.DefaultValue);
		}

#region Text DependencyProperty

		public string Text
		{
			get { return (string)this.GetValue(TextProperty); }
			set { this.SetValue(TextProperty, value); }
		}

		public static readonly DependencyProperty TextProperty =
			DependencyProperty.Register(
				"Text",
				typeof(string),
				typeof(TextBox),
				new FrameworkPropertyMetadata(
					defaultValue: string.Empty,
					options: FrameworkPropertyMetadataOptions.None,
					propertyChangedCallback: (s, e) => ((TextBox)s)?.OnTextChanged(e),
					coerceValueCallback: null,
					defaultUpdateSourceTrigger: UpdateSourceTrigger.Explicit
				)
			);

		protected virtual void OnTextChanged(DependencyPropertyChangedEventArgs e)
		{
			TextChanged?.Invoke(this, new TextChangedEventArgs());

			if (_placeHolder != null)
			{
				_placeHolder.Visibility = Text.IsNullOrEmpty() ? Visibility.Visible : Visibility.Collapsed;
			}

			UpdateButtonStates();
		}

#endregion

		protected override void OnFontSizeChanged(double oldValue, double newValue)
		{
			base.OnFontSizeChanged(oldValue, newValue);
			UpdateFontPartial(this);
		}

		protected override void OnFontFamilyChanged(FontFamily oldValue, FontFamily newValue)
		{
			base.OnFontFamilyChanged(oldValue, newValue);
			UpdateFontPartial(this);
		}

		protected override void OnFontStyleChanged(FontStyle oldValue, FontStyle newValue)
		{
			base.OnFontStyleChanged(oldValue, newValue);
			UpdateFontPartial(this);
		}

		protected override void OnFontWeightChanged(FontWeight oldValue, FontWeight newValue)
		{
			base.OnFontWeightChanged(oldValue, newValue);
			UpdateFontPartial(this);
		}

		partial void UpdateFontPartial(object sender);

		protected override void OnForegroundColorChanged(Brush oldValue, Brush newValue)
		{
			OnForegroundColorChangedPartial(newValue);
		}

		partial void OnForegroundColorChangedPartial(Brush newValue);

#region PlaceholderText DependencyProperty

		public string PlaceholderText
		{
			get { return (string)this.GetValue(PlaceholderTextProperty); }
			set { this.SetValue(PlaceholderTextProperty, value); }
		}

		public static readonly DependencyProperty PlaceholderTextProperty =
			DependencyProperty.Register(
				"PlaceholderText",
				typeof(string),
				typeof(TextBox),
				new PropertyMetadata(defaultValue: string.Empty)
			);

#endregion

#region InputScope DependencyProperty

		public InputScope InputScope
		{
			get { return (InputScope)this.GetValue(InputScopeProperty); }
			set { this.SetValue(InputScopeProperty, value); }
		}

		public static readonly DependencyProperty InputScopeProperty =
			DependencyProperty.Register(
				"InputScope",
				typeof(InputScope),
				typeof(TextBox),
				new PropertyMetadata(
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
					propertyChangedCallback: (s, e) => ((TextBox)s)?.OnInputScopeChanged(e)
				)
			);

		protected void OnInputScopeChanged(DependencyPropertyChangedEventArgs e)
		{
			OnInputScopeChangedPartial(e);
		}
		partial void OnInputScopeChangedPartial(DependencyPropertyChangedEventArgs e);

#endregion

#region MaxLength DependencyProperty

		public int MaxLength
		{
			get { return (int)this.GetValue(MaxLengthProperty); }
			set { this.SetValue(MaxLengthProperty, value); }
		}

		public static readonly DependencyProperty MaxLengthProperty =
			DependencyProperty.Register(
				"MaxLength",
				typeof(int),
				typeof(TextBox),
				new PropertyMetadata(
					defaultValue: 0,
					propertyChangedCallback: (s, e) => ((TextBox)s)?.OnMaxLengthChanged(e)
				)
			);

		private void OnMaxLengthChanged(DependencyPropertyChangedEventArgs e)
		{
			OnMaxLengthChangedPartial(e);
		}

		partial void OnMaxLengthChangedPartial(DependencyPropertyChangedEventArgs e);

#endregion

#region AcceptsReturn DependencyProperty

		public bool AcceptsReturn
		{
			get { return (bool)this.GetValue(AcceptsReturnProperty); }
			set { this.SetValue(AcceptsReturnProperty, value); }
		}

		public static readonly DependencyProperty AcceptsReturnProperty =
			DependencyProperty.Register(
				"AcceptsReturn",
				typeof(bool),
				typeof(TextBox),
				new PropertyMetadata(
					defaultValue: false,
					propertyChangedCallback: (s, e) => ((TextBox)s)?.OnAcceptsReturnChanged(e)
				)
			);

		private void OnAcceptsReturnChanged(DependencyPropertyChangedEventArgs e)
		{
			OnAcceptsReturnChangedPartial(e);
			UpdateButtonStates();
		}

		partial void OnAcceptsReturnChangedPartial(DependencyPropertyChangedEventArgs e);

#endregion

#region TextWrapping DependencyProperty
		public TextWrapping TextWrapping
		{
			get { return (TextWrapping)this.GetValue(TextWrappingProperty); }
			set { this.SetValue(TextWrappingProperty, value); }
		}

		public static readonly DependencyProperty TextWrappingProperty =
			DependencyProperty.Register(
				"TextWrapping",
				typeof(TextWrapping),
				typeof(TextBox),
				new PropertyMetadata(
					defaultValue: TextWrapping.NoWrap,
					propertyChangedCallback: (s, e) => ((TextBox)s)?.OnTextWrappingChanged(e))
				);

		private void OnTextWrappingChanged(DependencyPropertyChangedEventArgs args)
		{
			OnTextWrappingChangedPartial(args);
			UpdateButtonStates();
		}

		partial void OnTextWrappingChangedPartial(DependencyPropertyChangedEventArgs e);

#endregion

#region IsReadOnly DependencyProperty

		public bool IsReadOnly
		{
			get { return (bool)GetValue(IsReadOnlyProperty); }
			set { SetValue(IsReadOnlyProperty, value); }
		}

		public static readonly DependencyProperty IsReadOnlyProperty =
			DependencyProperty.Register(
				"IsReadOnly",
				typeof(bool),
				typeof(TextBox),
				new PropertyMetadata(
					false,
					propertyChangedCallback: (s, e) => ((TextBox)s)?.OnIsReadonlyChanged(e)
				)
			);

		private void OnIsReadonlyChanged(DependencyPropertyChangedEventArgs e)
		{
			OnIsReadonlyChangedPartial(e);
			UpdateButtonStates();
		}

		partial void OnIsReadonlyChangedPartial(DependencyPropertyChangedEventArgs e);

#endregion

#region Header DependencyProperties

		public object Header
		{
			get { return (object)GetValue(HeaderProperty); }
			set { SetValue(HeaderProperty, value); }
		}
		public static readonly DependencyProperty HeaderProperty =
			DependencyProperty.Register("Header",
				typeof(object),
				typeof(TextBox),
				new PropertyMetadata(defaultValue: null,
					propertyChangedCallback: (s, e) => ((TextBox)s)?.OnHeaderChanged()
				)
			);

		public DataTemplate HeaderTemplate
		{
			get { return (DataTemplate)GetValue(HeaderTemplateProperty); }
			set { SetValue(HeaderTemplateProperty, value); }
		}

		public static readonly DependencyProperty HeaderTemplateProperty =
			DependencyProperty.Register("HeaderTemplate",
				typeof(DataTemplate),
				typeof(TextBox),
				new PropertyMetadata(defaultValue: null,
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
			get { return (bool)this.GetValue(IsSpellCheckEnabledProperty); }
			set { this.SetValue(IsSpellCheckEnabledProperty, value); }
		}

		public static readonly DependencyProperty IsSpellCheckEnabledProperty =
			DependencyProperty.Register(
				"IsSpellCheckEnabled",
				typeof(bool),
				typeof(TextBox),
				new PropertyMetadata(
					defaultValue: true,
					propertyChangedCallback: (s, e) => ((TextBox)s)?.OnIsSpellCheckEnabledChanged(e)
				)
			);

		protected virtual void OnIsSpellCheckEnabledChanged(DependencyPropertyChangedEventArgs e)
		{
			OnIsSpellCheckEnabledChangedPartial(e);
		}

		partial void OnIsSpellCheckEnabledChangedPartial(DependencyPropertyChangedEventArgs e);

#endregion

#region IsTextPredictionEnabled DependencyProperty

		public bool IsTextPredictionEnabled
		{
			get { return (bool)this.GetValue(IsTextPredictionEnabledProperty); }
			set { this.SetValue(IsTextPredictionEnabledProperty, value); }
		}

		public static readonly DependencyProperty IsTextPredictionEnabledProperty =
			DependencyProperty.Register(
				"IsTextPredictionEnabled",
				typeof(bool),
				typeof(TextBox),
				new PropertyMetadata(
					defaultValue: true,
					propertyChangedCallback: (s, e) => ((TextBox)s)?.OnIsTextPredictionEnabledChanged(e)
				)
			);

		protected virtual void OnIsTextPredictionEnabledChanged(DependencyPropertyChangedEventArgs e)
		{
			OnIsTextPredictionEnabledChangedPartial(e);
		}

		partial void OnIsTextPredictionEnabledChangedPartial(DependencyPropertyChangedEventArgs e);

#endregion

#region TextAlignment DependencyProperty

#if XAMARIN_ANDROID
		public new TextAlignment TextAlignment
#else
		public TextAlignment TextAlignment
#endif
		{
			get { return (TextAlignment)GetValue(TextAlignmentProperty); }
			set { SetValue(TextAlignmentProperty, value); }
		}

		public static readonly DependencyProperty TextAlignmentProperty =
			DependencyProperty.Register("TextAlignment", typeof(TextAlignment), typeof(TextBox), new PropertyMetadata(TextAlignment.Left, (s, e) => ((TextBox)s)?.OnTextAlignmentChanged(e)));


		protected virtual void OnTextAlignmentChanged(DependencyPropertyChangedEventArgs e)
		{
			OnTextAlignmentChangedPartial(e);
		}

		partial void OnTextAlignmentChangedPartial(DependencyPropertyChangedEventArgs e);

#endregion

		protected override void OnFocusStateChanged(FocusState oldValue, FocusState newValue)
		{
			base.OnFocusStateChanged(oldValue, newValue);
			OnFocusStateChangedPartial(newValue);

			if (newValue == FocusState.Unfocused)
			{
				// Manually update Source when losing focus because TextProperty's default UpdateSourceTrigger is Explicit
				var bindingExpression = GetBindingExpression(TextProperty);
				bindingExpression?.UpdateSource(Text);
			}

			UpdateCommonStates();
			UpdateButtonStates();
		}

		protected override void OnPointerPressed(PointerRoutedEventArgs args)
		{
			base.OnPointerPressed(args);

			args.Handled = true;
#if !__WASM__
			Focus(FocusState.Pointer);
#endif
		}

		private void UpdateCommonStates()
		{
			var commonState = "Normal";

			if (FocusState != FocusState.Unfocused)
			{
				commonState = "Focused";
			}

			if (!IsEnabled)
			{
				commonState = "Disabled";
			}

			VisualStateManager.GoToState(this, commonState, true);
		}

		partial void OnFocusStateChangedPartial(FocusState focusState);

		private void UpdateButtonStates()
		{
			if (Text.HasValue() 
			&& FocusState != FocusState.Unfocused 
			&& !IsReadOnly
			&& !AcceptsReturn
			&& TextWrapping == TextWrapping.NoWrap)
			{
				VisualStateManager.GoToState(this, ButtonVisibleStateName, true);
			}
			else
			{
				VisualStateManager.GoToState(this, ButtonCollapsedStateName, true);
			}
		}

		private void DeleteText()
		{
			Text = string.Empty;
			OnTextClearedPartial();
		}

		partial void OnTextClearedPartial();

		internal void OnSelectionChanged()
		{
			SelectionChanged?.Invoke(this, new RoutedEventArgs());
		}

		public void OnTemplateRecycled()
		{
			DeleteText();
		}

		protected override AutomationPeer OnCreateAutomationPeer()
		{
			return new TextBoxAutomationPeer(this);
		}

		public override string GetAccessibilityInnerText()
		{
			return Text;
		}
	}
}
