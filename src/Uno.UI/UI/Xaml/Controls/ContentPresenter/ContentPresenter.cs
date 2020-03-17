using System;
using System.Collections.Generic;
using System.Text;
using Uno.Extensions;
using Uno.Logging;
using Uno.UI.DataBinding;
using Windows.UI.Xaml.Media.Animation;
using System.Collections;
using System.Linq;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Markup;
using Windows.Foundation;
using Uno.UI;
using Windows.UI.Xaml.Media;
using Windows.UI.Text;
using Microsoft.Extensions.Logging;
#if XAMARIN_ANDROID
using View = Android.Views.View;
using ViewGroup = Android.Views.ViewGroup;
using Font = Android.Graphics.Typeface;
using Android.Graphics;
#elif XAMARIN_IOS_UNIFIED
using UIKit;
using View = UIKit.UIView;
using ViewGroup = UIKit.UIView;
using Color = UIKit.UIColor;
using Font = UIKit.UIFont;
#elif __MACOS__
using AppKit;
using View = AppKit.NSView;
using ViewGroup = AppKit.NSView;
using Color = AppKit.NSColor;
using Font = AppKit.NSFont;
#elif __WASM__ || NET461
using View = Windows.UI.Xaml.UIElement;
using ViewGroup = Windows.UI.Xaml.UIElement;
#endif

namespace Windows.UI.Xaml.Controls
{
	[ContentProperty(Name = "Content")]
	public partial class ContentPresenter : FrameworkElement, ICustomClippingElement
	{
		private bool _firstLoadResetDone;
		private View _contentTemplateRoot;

		/// <summary>
		/// Will be set to either the result of ContentTemplateSelector or to ContentTemplate, depending on which is used
		/// </summary>
		private DataTemplate _dataTemplateUsedLastUpdate;

		private void InitializeContentPresenter()
		{
		}

		protected override bool IsSimpleLayout => true;

		#region Content DependencyProperty

		public virtual object Content
		{
			get { return (object)GetValue(ContentProperty); }
			set { SetValue(ContentProperty, value); }
		}

		public static readonly DependencyProperty ContentProperty =
			DependencyProperty.Register(
				"Content",
				typeof(object),
				typeof(ContentPresenter),
				new FrameworkPropertyMetadata(
					defaultValue: null,
					options: FrameworkPropertyMetadataOptions.None,
					propertyChangedCallback: (s, e) => ((ContentPresenter)s)?.OnContentChanged(e.OldValue, e.NewValue)
				)
			);

		#endregion

		#region ContentTemplate DependencyProperty

		public DataTemplate ContentTemplate
		{
			get { return (DataTemplate)GetValue(ContentTemplateProperty); }
			set { SetValue(ContentTemplateProperty, value); }
		}

		// Using a DependencyProperty as the backing store for ContentTemplate.  This enables animation, styling, binding, etc...
		public static readonly DependencyProperty ContentTemplateProperty =
			DependencyProperty.Register(
				"ContentTemplate",
				typeof(DataTemplate),
				typeof(ContentPresenter),
				new PropertyMetadata(
					null,
					(s, e) => ((ContentPresenter)s)?.OnContentTemplateChanged(e.OldValue as DataTemplate, e.NewValue as DataTemplate)
				)
			);
		#endregion

		#region ContentTemplateSelector DependencyProperty

		public DataTemplateSelector ContentTemplateSelector
		{
			get { return (DataTemplateSelector)GetValue(ContentTemplateSelectorProperty); }
			set { SetValue(ContentTemplateSelectorProperty, value); }
		}

		public static readonly DependencyProperty ContentTemplateSelectorProperty =
			DependencyProperty.Register(
				"ContentTemplateSelector",
				typeof(DataTemplateSelector),
				typeof(ContentPresenter),
				new PropertyMetadata(
					null,
					(s, e) => ((ContentPresenter)s)?.OnContentTemplateSelectorChanged(e.OldValue as DataTemplateSelector, e.NewValue as DataTemplateSelector)
				)
			);
		#endregion

		#region Transitions Dependency Property

		public TransitionCollection ContentTransitions
		{
			get { return (TransitionCollection)this.GetValue(ContentTransitionsProperty); }
			set { this.SetValue(ContentTransitionsProperty, value); }
		}

		public static readonly DependencyProperty ContentTransitionsProperty =
			DependencyProperty.Register("ContentTransitions", typeof(TransitionCollection), typeof(ContentPresenter), new PropertyMetadata(null, OnContentTransitionsChanged));

		private static void OnContentTransitionsChanged(object dependencyObject, DependencyPropertyChangedEventArgs args)
		{
			var control = dependencyObject as ContentPresenter;

			if (control != null)
			{
				var oldValue = (TransitionCollection)args.OldValue;
				var newValue = (TransitionCollection)args.NewValue;

				control.UpdateContentTransitions(oldValue, newValue);
			}
		}

		#endregion

		#region Foreground Dependency Property

		public
#if __ANDROID_23__
		new
#endif
		Brush Foreground
		{
			get { return (Brush)this.GetValue(ForegroundProperty); }
			set { this.SetValue(ForegroundProperty, value); }
		}

		public static readonly DependencyProperty ForegroundProperty =
			DependencyProperty.Register(
				"Foreground",
				typeof(Brush),
				typeof(ContentPresenter),
				new FrameworkPropertyMetadata(
					SolidColorBrushHelper.Black,
					FrameworkPropertyMetadataOptions.Inherits,
					propertyChangedCallback: (s, e) => ((ContentPresenter)s)?.OnForegroundColorChanged(e.OldValue as Brush, e.NewValue as Brush)
				)
			);

		#endregion

		#region FontWeight

		public FontWeight FontWeight
		{
			get { return (FontWeight)this.GetValue(FontWeightProperty); }
			set { this.SetValue(FontWeightProperty, value); }
		}

		public static readonly DependencyProperty FontWeightProperty =
			DependencyProperty.Register(
				"FontWeight",
				typeof(FontWeight),
				typeof(ContentPresenter),
				new FrameworkPropertyMetadata(
					FontWeights.Normal,
					FrameworkPropertyMetadataOptions.Inherits,
					(s, e) => ((ContentPresenter)s)?.OnFontWeightChanged((FontWeight)e.OldValue, (FontWeight)e.NewValue)
				)
			);

		#endregion

		#region FontSize

		public double FontSize
		{
			get { return (double)this.GetValue(FontSizeProperty); }
			set { this.SetValue(FontSizeProperty, value); }
		}

		public static readonly DependencyProperty FontSizeProperty =
			DependencyProperty.Register(
				"FontSize",
				typeof(double),
				typeof(ContentPresenter),
				new FrameworkPropertyMetadata(
					11.0,
					FrameworkPropertyMetadataOptions.Inherits,
					(s, e) => ((ContentPresenter)s)?.OnFontSizeChanged((double)e.OldValue, (double)e.NewValue)
				)
			);

		#endregion

		#region FontFamily

		public FontFamily FontFamily
		{
			get { return (FontFamily)this.GetValue(FontFamilyProperty); }
			set { this.SetValue(FontFamilyProperty, value); }
		}

		public static readonly DependencyProperty FontFamilyProperty =
			DependencyProperty.Register(
				"FontFamily",
				typeof(FontFamily),
				typeof(ContentPresenter),
				new FrameworkPropertyMetadata(
					FontFamily.Default,
					FrameworkPropertyMetadataOptions.Inherits,
					(s, e) => ((ContentPresenter)s)?.OnFontFamilyChanged(e.OldValue as FontFamily, e.NewValue as FontFamily)
				)
			);
		#endregion

		#region FontStyle

		public FontStyle FontStyle
		{
			get { return (FontStyle)this.GetValue(FontStyleProperty); }
			set { this.SetValue(FontStyleProperty, value); }
		}

		public static readonly DependencyProperty FontStyleProperty =
			DependencyProperty.Register(
				"FontStyle",
				typeof(FontStyle),
				typeof(ContentPresenter),
				new FrameworkPropertyMetadata(
					FontStyle.Normal,
					FrameworkPropertyMetadataOptions.Inherits,
					(s, e) => ((ContentPresenter)s)?.OnFontStyleChanged((FontStyle)e.OldValue, (FontStyle)e.NewValue)
				)
			);
		#endregion

		#region TextWrapping Dependency Property

		public TextWrapping TextWrapping
		{
			get { return (TextWrapping)this.GetValue(TextWrappingProperty); }
			set { this.SetValue(TextWrappingProperty, value); }
		}

		public static readonly DependencyProperty TextWrappingProperty =
			DependencyProperty.Register(
				"TextWrapping",
				typeof(TextWrapping),
				typeof(ContentPresenter),
				new PropertyMetadata(
					defaultValue: TextWrapping.NoWrap,
					propertyChangedCallback: (s, e) => ((ContentPresenter)s).OnTextWrappingChanged()
				)
			);

		private void OnTextWrappingChanged()
		{
			OnTextWrappingChangedPartial();
		}

		partial void OnTextWrappingChangedPartial();

		#endregion

		#region MaxLines Dependency Property

		public int MaxLines
		{
			get { return (int)this.GetValue(MaxLinesProperty); }
			set { this.SetValue(MaxLinesProperty, value); }
		}

		public static readonly DependencyProperty MaxLinesProperty =
			DependencyProperty.Register(
				"MaxLines",
				typeof(int),
				typeof(ContentPresenter),
				new PropertyMetadata(
					defaultValue: 0,
					propertyChangedCallback: (s, e) => ((ContentPresenter)s).OnMaxLinesChanged()
				)
			);

		private void OnMaxLinesChanged()
		{
			OnMaxLinesChangedPartial();
		}

		partial void OnMaxLinesChangedPartial();

		#endregion

		#region TextTrimming Dependency Property

		public TextTrimming TextTrimming
		{
			get { return (TextTrimming)this.GetValue(TextTrimmingProperty); }
			set { this.SetValue(TextTrimmingProperty, value); }
		}

		public static readonly DependencyProperty TextTrimmingProperty =
			DependencyProperty.Register(
				"TextTrimming",
				typeof(TextTrimming),
				typeof(ContentPresenter),
				new PropertyMetadata(
					defaultValue: TextTrimming.None,
					propertyChangedCallback: (s, e) => ((ContentPresenter)s).OnTextTrimmingChanged()
				)
			);

		private void OnTextTrimmingChanged()
		{
			OnTextTrimmingChangedPartial();
		}

		partial void OnTextTrimmingChangedPartial();

		#endregion

		#region TextAlignment Dependency Property

		public
#if XAMARIN_ANDROID
			new
#endif
			TextAlignment TextAlignment
		{
			get { return (TextAlignment)this.GetValue(TextAlignmentProperty); }
			set { this.SetValue(TextAlignmentProperty, value); }
		}

		public static readonly DependencyProperty TextAlignmentProperty =
			DependencyProperty.Register(
				"TextAlignment",
				typeof(TextAlignment),
				typeof(ContentPresenter),
				new PropertyMetadata(
					defaultValue: TextAlignment.Left,
					propertyChangedCallback: (s, e) => ((ContentPresenter)s).OnTextAlignmentChanged()
				)
			);

		private void OnTextAlignmentChanged()
		{
			OnTextAlignmentChangedPartial();
		}

		partial void OnTextAlignmentChangedPartial();

		#endregion

		#region HorizontalContentAlignment DependencyProperty

		public HorizontalAlignment HorizontalContentAlignment
		{
			get { return (HorizontalAlignment)this.GetValue(HorizontalContentAlignmentProperty); }
			set { this.SetValue(HorizontalContentAlignmentProperty, value); }
		}

		public static readonly DependencyProperty HorizontalContentAlignmentProperty =
			DependencyProperty.Register(
				"HorizontalContentAlignment",
				typeof(HorizontalAlignment),
				typeof(ContentPresenter),
				new PropertyMetadata(
					HorizontalAlignment.Stretch,
					(s, e) => ((ContentPresenter)s)?.OnHorizontalContentAlignmentChanged((HorizontalAlignment)e.OldValue, (HorizontalAlignment)e.NewValue)
				)
			);

		protected virtual void OnHorizontalContentAlignmentChanged(HorizontalAlignment oldHorizontalContentAlignment, HorizontalAlignment newHorizontalContentAlignment)
		{
			OnHorizontalContentAlignmentChangedPartial(oldHorizontalContentAlignment, newHorizontalContentAlignment);
			SynchronizeHorizontalContentAlignment(newHorizontalContentAlignment);
		}

		partial void OnHorizontalContentAlignmentChangedPartial(HorizontalAlignment oldHorizontalContentAlignment, HorizontalAlignment newHorizontalContentAlignment);

		#endregion

		#region VerticalContentAlignment DependencyProperty

		public VerticalAlignment VerticalContentAlignment
		{
			get { return (VerticalAlignment)this.GetValue(VerticalContentAlignmentProperty); }
			set { this.SetValue(VerticalContentAlignmentProperty, value); }
		}

		public static readonly DependencyProperty VerticalContentAlignmentProperty =
			DependencyProperty.Register(
				"VerticalContentAlignment",
				typeof(VerticalAlignment),
				typeof(ContentPresenter),
				new PropertyMetadata(
					VerticalAlignment.Stretch,
					(s, e) => ((ContentPresenter)s)?.OnVerticalContentAlignmentChanged((VerticalAlignment)e.OldValue, (VerticalAlignment)e.NewValue)
				)
			);

		protected virtual void OnVerticalContentAlignmentChanged(VerticalAlignment oldVerticalContentAlignment, VerticalAlignment newVerticalContentAlignment)
		{
			OnVerticalContentAlignmentChangedPartial(oldVerticalContentAlignment, newVerticalContentAlignment);
			SynchronizeVerticalContentAlignment(newVerticalContentAlignment);
		}

		partial void OnVerticalContentAlignmentChangedPartial(VerticalAlignment oldVerticalContentAlignment, VerticalAlignment newVerticalContentAlignment);

		#endregion

		#region Padding DependencyProperty

		public Thickness Padding
		{
			get { return (Thickness)GetValue(PaddingProperty); }
			set { SetValue(PaddingProperty, value); }
		}

		public static DependencyProperty PaddingProperty =
			DependencyProperty.Register(
				"Padding",
				typeof(Thickness),
				typeof(ContentPresenter),
				new PropertyMetadata(
					(Thickness)Thickness.Empty,
					(s, e) => ((ContentPresenter)s)?.OnPaddingChanged((Thickness)e.OldValue, (Thickness)e.NewValue)
				)
			);

		private void OnPaddingChanged(Thickness oldValue, Thickness newValue)
		{
			OnPaddingChangedPartial(oldValue, newValue);
		}

		partial void OnPaddingChangedPartial(Thickness oldValue, Thickness newValue);

		#endregion

		#region BorderThickness DependencyProperty

		public Thickness BorderThickness
		{
			get { return (Thickness)GetValue(BorderThicknessProperty); }
			set { SetValue(BorderThicknessProperty, value); }
		}

		public static DependencyProperty BorderThicknessProperty =
			DependencyProperty.Register(
				"BorderThickness",
				typeof(Thickness),
				typeof(ContentPresenter),
				new PropertyMetadata(
					(Thickness)Thickness.Empty,
					(s, e) => ((ContentPresenter)s)?.OnBorderThicknessChanged((Thickness)e.OldValue, (Thickness)e.NewValue)
				)
			);

		private void OnBorderThicknessChanged(Thickness oldValue, Thickness newValue)
		{
			UpdateBorder();
		}

		#endregion

		#region BorderBrush DependencyProperty

		public Brush BorderBrush
		{
			get { return (Brush)GetValue(BorderBrushProperty); }
			set { SetValue(BorderBrushProperty, value); }
		}

		public static DependencyProperty BorderBrushProperty =
			DependencyProperty.Register(
				"BorderBrush",
				typeof(Brush),
				typeof(ContentPresenter),
				new PropertyMetadata(
					null,
					(s, e) => ((ContentPresenter)s)?.OnBorderBrushChanged((Brush)e.OldValue, (Brush)e.NewValue)
				)
			);

		private void OnBorderBrushChanged(Brush oldValue, Brush newValue)
		{
			UpdateBorder();
		}


		#endregion

		#region CornerRadius DependencyProperty

		public CornerRadius CornerRadius
		{
			get { return (CornerRadius)GetValue(CornerRadiusProperty); }
			set { SetValue(CornerRadiusProperty, value); }
		}

		public static DependencyProperty CornerRadiusProperty =
			DependencyProperty.Register(
				"CornerRadius",
				typeof(CornerRadius),
				typeof(ContentPresenter),
				new FrameworkPropertyMetadata(
					CornerRadius.None,
					(s, e) => ((ContentPresenter)s)?.OnCornerRadiusChanged((CornerRadius)e.OldValue, (CornerRadius)e.NewValue)
				)
			);

		private void OnCornerRadiusChanged(CornerRadius oldValue, CornerRadius newValue)
		{
			UpdateBorder();
		}

		#endregion

		protected virtual void OnForegroundColorChanged(Brush oldValue, Brush newValue)
		{
			OnForegroundColorChangedPartial(oldValue, newValue);
		}

		partial void OnForegroundColorChangedPartial(Brush oldValue, Brush newValue);

		protected virtual void OnFontWeightChanged(FontWeight oldValue, FontWeight newValue)
		{
			OnFontWeightChangedPartial(oldValue, newValue);
		}

		partial void OnFontWeightChangedPartial(FontWeight oldValue, FontWeight newValue);

		protected virtual void OnFontFamilyChanged(FontFamily oldValue, FontFamily newValue)
		{
			OnFontFamilyChangedPartial(oldValue, newValue);
		}

		partial void OnFontFamilyChangedPartial(FontFamily oldValue, FontFamily newValue);

		protected virtual void OnFontSizeChanged(double oldValue, double newValue)
		{
			OnFontSizeChangedPartial(oldValue, newValue);
		}

		partial void OnFontSizeChangedPartial(double oldValue, double newValue);

		protected virtual void OnFontStyleChanged(FontStyle oldValue, FontStyle newValue)
		{
			OnFontStyleChangedPartial(oldValue, newValue);
		}

		partial void OnFontStyleChangedPartial(FontStyle oldValue, FontStyle newValue);


		protected virtual void OnContentChanged(object oldValue, object newValue)
		{
			if (oldValue != null && newValue == null)
			{
				// The content is being reset, remove the existing content properly.
				ContentTemplateRoot = null;
			}
			else if (oldValue is View || newValue is View)
			{
				// Make sure not to reuse the previous Content as a ContentTemplateRoot (i.e., in case there's no data template)
				// If setting Content to a new View, recreate the template
				ContentTemplateRoot = null;
			}

			if (newValue != null)
			{
				TrySetDataContextFromContent(newValue);

				SetUpdateTemplate();
			}
		}

		private void TrySetDataContextFromContent(object value)
		{
			if (value != null)
			{
				if (!(value is View))
				{
					// If the content is not a view, we apply the content as the
					// DataContext of the materialized content.
					DataContext = value;
				}
				else
				{
					// Restore DataContext propagation if the content is a view
					this.ClearValue(DataContextProperty, DependencyPropertyValuePrecedences.Local);
				}
			}
		}

		protected internal override void OnTemplatedParentChanged(DependencyPropertyChangedEventArgs e)
		{
			base.OnTemplatedParentChanged(e);

			SetImplicitContent();
		}

		protected virtual void OnContentTemplateChanged(DataTemplate oldTemplate, DataTemplate newTemplate)
		{
			if (ContentTemplateRoot != null)
			{
				ContentTemplateRoot = null;
			}

			SetUpdateTemplate();
		}

		private void OnContentTemplateSelectorChanged(DataTemplateSelector dataTemplateSelector1, DataTemplateSelector dataTemplateSelector2)
		{
		}

		partial void UnregisterContentTemplateRoot();

		public virtual View ContentTemplateRoot
		{
			get
			{
				return _contentTemplateRoot;
			}

			protected set
			{
				var previousValue = _contentTemplateRoot;

				if (previousValue != null)
				{
					CleanupView(previousValue);

					UnregisterContentTemplateRoot();

					UpdateContentTransitions(this.ContentTransitions, null);
				}

				_contentTemplateRoot = value;

				SynchronizeContentTemplatedParent();
				SynchronizeVerticalContentAlignment();
				SynchronizeHorizontalContentAlignment();

				if (_contentTemplateRoot != null)
				{
					RegisterContentTemplateRoot();

					UpdateContentTransitions(null, this.ContentTransitions);
				}
			}
		}

		private void SynchronizeContentTemplatedParent()
		{
			if (_contentTemplateRoot is IFrameworkElement binder)
			{
				var templatedParent = _contentTemplateRoot is ImplicitTextBlock
					? this // ImplicitTextBlock is a special case that requires its TemplatedParent to be the ContentPresenter
					: (this.TemplatedParent as IFrameworkElement)?.TemplatedParent;

				binder.TemplatedParent = templatedParent;
			}
		}

		private void UpdateContentTransitions(TransitionCollection oldValue, TransitionCollection newValue)
		{
			var contentRoot = this.ContentTemplateRoot as IFrameworkElement;

			if (contentRoot == null)
			{
				return;
			}

			if (oldValue != null)
			{
				foreach (var item in oldValue)
				{
					item.DetachFromElement(contentRoot);
				}
			}

			if (newValue != null)
			{
				foreach (var item in newValue)
				{
					item.AttachToElement(contentRoot);
				}
			}
		}

		/// <summary>
		/// Cleanup the view from its binding references
		/// </summary>
		/// <param name="previousValue"></param>
		private void CleanupView(View previousValue)
		{
		}

		protected override void OnLoaded()
		{
			base.OnLoaded();

			ResetDataContextOnFirstLoad();

			SetUpdateTemplate();

			// When the control is loaded, set the TemplatedParent
			// as it may have been reset during the last unload.
			SynchronizeContentTemplatedParent();

			UpdateBorder();
		}

		private void ResetDataContextOnFirstLoad()
		{
			if (!_firstLoadResetDone)
			{
				_firstLoadResetDone = true;

				// On first load UWP clears the local value of a ContentPresenter.
				// The reason for this behavior is unknown.
				this.ClearValue(DataContextProperty, DependencyPropertyValuePrecedences.Local);

				TrySetDataContextFromContent(Content);
			}
		}

		protected override void OnVisibilityChanged(Visibility oldValue, Visibility newValue)
		{
			base.OnVisibilityChanged(oldValue, newValue);

			if (oldValue == Visibility.Collapsed && newValue == Visibility.Visible)
			{
				SetUpdateTemplate();
			}
		}

		public void UpdateContentTemplateRoot()
		{
			if (Visibility == Visibility.Collapsed)
			{
				return;
			}

			//If ContentTemplateRoot is null, it must be updated even if the templates haven't changed
			if (ContentTemplateRoot == null)
			{
				_dataTemplateUsedLastUpdate = null;
			}

			//ContentTemplate/ContentTemplateSelector will only be applied to a control with no Template, normally the innermost element
			var dataTemplate = this.ResolveContentTemplate();

			//Only apply template if it has changed
			if (!object.Equals(dataTemplate, _dataTemplateUsedLastUpdate))
			{
				_dataTemplateUsedLastUpdate = dataTemplate;
				ContentTemplateRoot = dataTemplate?.LoadContentCached() ?? Content as View;
			}

			if (Content != null
				&& !(Content is View)
				&& ContentTemplateRoot == null
			)
			{
				// Use basic default root for non-View Content if no template is supplied
				SetContentTemplateRootToPlaceholder();
			}

			if (ContentTemplateRoot == null && Content is View contentView && dataTemplate == null)
			{
				// No template and Content is a View, set it directly as root
				ContentTemplateRoot = contentView as View;
			}
		}

		private void SetContentTemplateRootToPlaceholder()
		{
			if (this.Log().IsEnabled(Microsoft.Extensions.Logging.LogLevel.Debug))
			{
				this.Log().DebugFormat("No ContentTemplate was specified for {0} and content is not a UIView, defaulting to TextBlock.", GetType().Name);
			}

			var textBlock = new ImplicitTextBlock(this);

			void setBinding(DependencyProperty property, string path)
				=> textBlock.SetBinding(
					property,
					new Binding
					{
						Path = new PropertyPath(path),
						Source = this,
						Mode = BindingMode.OneWay
					}
				);

			setBinding(TextBlock.TextProperty, nameof(Content));
			setBinding(TextBlock.HorizontalAlignmentProperty, nameof(HorizontalContentAlignment));
			setBinding(TextBlock.VerticalAlignmentProperty, nameof(VerticalContentAlignment));
			setBinding(TextBlock.TextWrappingProperty, nameof(TextWrapping));
			setBinding(TextBlock.MaxLinesProperty, nameof(MaxLines));
			setBinding(TextBlock.TextAlignmentProperty, nameof(TextAlignment));

			ContentTemplateRoot = textBlock;
		}

		private bool _isBoundImplicitelyToContent;

		private void SetImplicitContent()
		{
			if (!FeatureConfiguration.ContentPresenter.UseImplicitContentFromTemplatedParent)
			{
				return;
			}

			if (!(TemplatedParent is ContentControl))
			{
				ClearImplicitBindinds();
				return; // Not applicable: no TemplatedParent or it's not a ContentControl
			}

			// Check if the Content is set to something
			var v = this.GetValueUnderPrecedence(ContentProperty, DependencyPropertyValuePrecedences.DefaultValue);
			if (v.precedence != DependencyPropertyValuePrecedences.DefaultValue)
			{
				ClearImplicitBindinds();
				return; // Nope, there's a value somewhere
			}

			// Check if the Content property is bound to something
			var b = GetBindingExpression(ContentProperty);
			if (b != null)
			{
				ClearImplicitBindinds();
				return; // Yep, there's a binding: a value "will" come eventually
			}

			// Create an implicit binding of Content to Content property of the TemplatedParent (which is a ContentControl)
			var binding =
				new Binding(new PropertyPath("Content"), null)
				{
					RelativeSource = RelativeSource.TemplatedParent,
				};
			SetBinding(ContentProperty, binding);
			_isBoundImplicitelyToContent = true;

			void ClearImplicitBindinds()
			{
				if (_isBoundImplicitelyToContent)
				{
					SetBinding(ContentProperty, new Binding());
				}
			}
		}

		partial void RegisterContentTemplateRoot();

		private void SynchronizeHorizontalContentAlignment(HorizontalAlignment? alignment = null)
		{
			var childControl = ContentTemplateRoot as FrameworkElement;

			if (childControl != null)
			{
				childControl.SetValue(
					FrameworkElement.HorizontalAlignmentProperty,
					alignment ?? HorizontalContentAlignment,
					DependencyPropertyValuePrecedences.Inheritance
				);
			}
			else
			{
				var childControl2 = ContentTemplateRoot as IFrameworkElement;

				if (childControl2 != null)
				{
					// This case is for controls that implement IFrameworkElement, but do not inherit from FrameworkElement (like TextBlock).

					var horizontalAlignmentProperty = DependencyProperty.GetProperty(ContentTemplateRoot.GetType(), "HorizontalAlignment");

					if (horizontalAlignmentProperty != null)
					{
						childControl2.SetValue(
							horizontalAlignmentProperty,
							alignment ?? HorizontalContentAlignment,
							DependencyPropertyValuePrecedences.Inheritance
						);
					}
					else
					{
						throw new InvalidOperationException($"The property HorizontalAlignment should exist on type {ContentTemplateRoot.GetType()}");
					}
				}
			}

		}

		private void SynchronizeVerticalContentAlignment(VerticalAlignment? alignment = null)
		{
			var childControl = ContentTemplateRoot as FrameworkElement;

			if (childControl != null)
			{
				childControl.SetValue(
					FrameworkElement.VerticalAlignmentProperty,
					alignment ?? VerticalContentAlignment,
					DependencyPropertyValuePrecedences.Inheritance
				);
			}
			else
			{
				var childControl2 = ContentTemplateRoot as IFrameworkElement;

				if (childControl2 != null)
				{
					// This case is for controls that implement IFrameworkElement, but do not inherit from FrameworkElement (like TextBlock).

					var verticalAlignmentProperty = DependencyProperty.GetProperty(ContentTemplateRoot.GetType(), "VerticalAlignment");

					if (verticalAlignmentProperty != null)
					{
						childControl2.SetValue(
							verticalAlignmentProperty,
							alignment ?? VerticalContentAlignment,
							DependencyPropertyValuePrecedences.Inheritance
						);
					}
					else
					{
						throw new InvalidOperationException($"The property VerticalAlignment should exist on type {ContentTemplateRoot.GetType()}");
					}
				}
			}
		}

		protected override void OnBackgroundChanged(DependencyPropertyChangedEventArgs e)
		{
			// Don't call base, the UpdateBorder() method handles drawing the background.
			// base.OnBackgroundChanged(e);

			UpdateBorder();
		}

#if XAMARIN_ANDROID
		// Support for the C# collection initializer style.
		public void Add(View view)
		{
			Content = view;
		}

		public IEnumerator GetEnumerator()
		{
			if (Content != null)
			{
				return new[] { Content }.GetEnumerator();
			}
			else
			{
				return Enumerable.Empty<object>().GetEnumerator();
			}
		}
#endif

		protected override Size ArrangeOverride(Size finalSize)
		{
			var child = this.FindFirstChild();

			if (child != null)
			{
				var padding = Padding;
				var borderThickness = BorderThickness;

				var finalRect = new Foundation.Rect(
					padding.Left + borderThickness.Left,
					padding.Top + borderThickness.Top,
					finalSize.Width - padding.Left - padding.Right - borderThickness.Left - borderThickness.Right,
					finalSize.Height - padding.Top - padding.Bottom - borderThickness.Top - borderThickness.Bottom
				);

				base.ArrangeElement(child, finalRect);
			}

			return finalSize;
		}

		protected override Size MeasureOverride(Size size)
		{
			var padding = Padding;
			var borderThickness = BorderThickness;

			var measuredSize = base.MeasureOverride(
				new Size(
					size.Width - padding.Left - padding.Right - borderThickness.Left - borderThickness.Right,
					size.Height - padding.Top - padding.Bottom - borderThickness.Top - borderThickness.Bottom
				)
			);

			return new Size(
				measuredSize.Width + padding.Left + padding.Right + borderThickness.Left + borderThickness.Right,
				measuredSize.Height + padding.Top + padding.Bottom + borderThickness.Top + borderThickness.Bottom
			);
		}
	}
}
