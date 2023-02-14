#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Microsoft.UI.Xaml.Controls
{
	#if false || false || false || false || false || false || false
	[global::Uno.NotImplemented]
	#endif
	public  partial class ItemsPresenter : global::Microsoft.UI.Xaml.FrameworkElement,global::Microsoft.UI.Xaml.Controls.Primitives.IScrollSnapPointsInfo
	{
		// Skipping already declared property Padding
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Microsoft.UI.Xaml.Media.Animation.TransitionCollection HeaderTransitions
		{
			get
			{
				return (global::Microsoft.UI.Xaml.Media.Animation.TransitionCollection)this.GetValue(HeaderTransitionsProperty);
			}
			set
			{
				this.SetValue(HeaderTransitionsProperty, value);
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Microsoft.UI.Xaml.DataTemplate HeaderTemplate
		{
			get
			{
				return (global::Microsoft.UI.Xaml.DataTemplate)this.GetValue(HeaderTemplateProperty);
			}
			set
			{
				this.SetValue(HeaderTemplateProperty, value);
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  object Header
		{
			get
			{
				return (object)this.GetValue(HeaderProperty);
			}
			set
			{
				this.SetValue(HeaderProperty, value);
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Microsoft.UI.Xaml.Media.Animation.TransitionCollection FooterTransitions
		{
			get
			{
				return (global::Microsoft.UI.Xaml.Media.Animation.TransitionCollection)this.GetValue(FooterTransitionsProperty);
			}
			set
			{
				this.SetValue(FooterTransitionsProperty, value);
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Microsoft.UI.Xaml.DataTemplate FooterTemplate
		{
			get
			{
				return (global::Microsoft.UI.Xaml.DataTemplate)this.GetValue(FooterTemplateProperty);
			}
			set
			{
				this.SetValue(FooterTemplateProperty, value);
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  object Footer
		{
			get
			{
				return (object)this.GetValue(FooterProperty);
			}
			set
			{
				this.SetValue(FooterProperty, value);
			}
		}
		#endif
		// Skipping already declared property AreHorizontalSnapPointsRegular
		// Skipping already declared property AreVerticalSnapPointsRegular
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public static global::Microsoft.UI.Xaml.DependencyProperty FooterProperty { get; } = 
		Microsoft.UI.Xaml.DependencyProperty.Register(
			nameof(Footer), typeof(object), 
			typeof(global::Microsoft.UI.Xaml.Controls.ItemsPresenter), 
			new FrameworkPropertyMetadata(default(object)));
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public static global::Microsoft.UI.Xaml.DependencyProperty FooterTemplateProperty { get; } = 
		Microsoft.UI.Xaml.DependencyProperty.Register(
			nameof(FooterTemplate), typeof(global::Microsoft.UI.Xaml.DataTemplate), 
			typeof(global::Microsoft.UI.Xaml.Controls.ItemsPresenter), 
			new FrameworkPropertyMetadata(default(global::Microsoft.UI.Xaml.DataTemplate)));
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public static global::Microsoft.UI.Xaml.DependencyProperty FooterTransitionsProperty { get; } = 
		Microsoft.UI.Xaml.DependencyProperty.Register(
			nameof(FooterTransitions), typeof(global::Microsoft.UI.Xaml.Media.Animation.TransitionCollection), 
			typeof(global::Microsoft.UI.Xaml.Controls.ItemsPresenter), 
			new FrameworkPropertyMetadata(default(global::Microsoft.UI.Xaml.Media.Animation.TransitionCollection)));
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public static global::Microsoft.UI.Xaml.DependencyProperty HeaderProperty { get; } = 
		Microsoft.UI.Xaml.DependencyProperty.Register(
			nameof(Header), typeof(object), 
			typeof(global::Microsoft.UI.Xaml.Controls.ItemsPresenter), 
			new FrameworkPropertyMetadata(default(object)));
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public static global::Microsoft.UI.Xaml.DependencyProperty HeaderTemplateProperty { get; } = 
		Microsoft.UI.Xaml.DependencyProperty.Register(
			nameof(HeaderTemplate), typeof(global::Microsoft.UI.Xaml.DataTemplate), 
			typeof(global::Microsoft.UI.Xaml.Controls.ItemsPresenter), 
			new FrameworkPropertyMetadata(default(global::Microsoft.UI.Xaml.DataTemplate)));
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public static global::Microsoft.UI.Xaml.DependencyProperty HeaderTransitionsProperty { get; } = 
		Microsoft.UI.Xaml.DependencyProperty.Register(
			nameof(HeaderTransitions), typeof(global::Microsoft.UI.Xaml.Media.Animation.TransitionCollection), 
			typeof(global::Microsoft.UI.Xaml.Controls.ItemsPresenter), 
			new FrameworkPropertyMetadata(default(global::Microsoft.UI.Xaml.Media.Animation.TransitionCollection)));
		#endif
		// Skipping already declared property PaddingProperty
		// Skipping already declared method Microsoft.UI.Xaml.Controls.ItemsPresenter.ItemsPresenter()
		// Forced skipping of method Microsoft.UI.Xaml.Controls.ItemsPresenter.ItemsPresenter()
		// Forced skipping of method Microsoft.UI.Xaml.Controls.ItemsPresenter.Header.get
		// Forced skipping of method Microsoft.UI.Xaml.Controls.ItemsPresenter.Header.set
		// Forced skipping of method Microsoft.UI.Xaml.Controls.ItemsPresenter.HeaderTemplate.get
		// Forced skipping of method Microsoft.UI.Xaml.Controls.ItemsPresenter.HeaderTemplate.set
		// Forced skipping of method Microsoft.UI.Xaml.Controls.ItemsPresenter.HeaderTransitions.get
		// Forced skipping of method Microsoft.UI.Xaml.Controls.ItemsPresenter.HeaderTransitions.set
		// Forced skipping of method Microsoft.UI.Xaml.Controls.ItemsPresenter.Footer.get
		// Forced skipping of method Microsoft.UI.Xaml.Controls.ItemsPresenter.Footer.set
		// Forced skipping of method Microsoft.UI.Xaml.Controls.ItemsPresenter.FooterTemplate.get
		// Forced skipping of method Microsoft.UI.Xaml.Controls.ItemsPresenter.FooterTemplate.set
		// Forced skipping of method Microsoft.UI.Xaml.Controls.ItemsPresenter.FooterTransitions.get
		// Forced skipping of method Microsoft.UI.Xaml.Controls.ItemsPresenter.FooterTransitions.set
		// Forced skipping of method Microsoft.UI.Xaml.Controls.ItemsPresenter.Padding.get
		// Forced skipping of method Microsoft.UI.Xaml.Controls.ItemsPresenter.Padding.set
		// Forced skipping of method Microsoft.UI.Xaml.Controls.ItemsPresenter.AreHorizontalSnapPointsRegular.get
		// Forced skipping of method Microsoft.UI.Xaml.Controls.ItemsPresenter.AreVerticalSnapPointsRegular.get
		// Forced skipping of method Microsoft.UI.Xaml.Controls.ItemsPresenter.HorizontalSnapPointsChanged.add
		// Forced skipping of method Microsoft.UI.Xaml.Controls.ItemsPresenter.HorizontalSnapPointsChanged.remove
		// Forced skipping of method Microsoft.UI.Xaml.Controls.ItemsPresenter.VerticalSnapPointsChanged.add
		// Forced skipping of method Microsoft.UI.Xaml.Controls.ItemsPresenter.VerticalSnapPointsChanged.remove
		// Skipping already declared method Microsoft.UI.Xaml.Controls.ItemsPresenter.GetIrregularSnapPoints(Microsoft.UI.Xaml.Controls.Orientation, Microsoft.UI.Xaml.Controls.Primitives.SnapPointsAlignment)
		// Skipping already declared method Microsoft.UI.Xaml.Controls.ItemsPresenter.GetRegularSnapPoints(Microsoft.UI.Xaml.Controls.Orientation, Microsoft.UI.Xaml.Controls.Primitives.SnapPointsAlignment, out float)
		// Forced skipping of method Microsoft.UI.Xaml.Controls.ItemsPresenter.HeaderProperty.get
		// Forced skipping of method Microsoft.UI.Xaml.Controls.ItemsPresenter.HeaderTemplateProperty.get
		// Forced skipping of method Microsoft.UI.Xaml.Controls.ItemsPresenter.HeaderTransitionsProperty.get
		// Forced skipping of method Microsoft.UI.Xaml.Controls.ItemsPresenter.FooterProperty.get
		// Forced skipping of method Microsoft.UI.Xaml.Controls.ItemsPresenter.FooterTemplateProperty.get
		// Forced skipping of method Microsoft.UI.Xaml.Controls.ItemsPresenter.FooterTransitionsProperty.get
		// Forced skipping of method Microsoft.UI.Xaml.Controls.ItemsPresenter.PaddingProperty.get
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  event global::System.EventHandler<object> HorizontalSnapPointsChanged
		{
			[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
			add
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Microsoft.UI.Xaml.Controls.ItemsPresenter", "event EventHandler<object> ItemsPresenter.HorizontalSnapPointsChanged");
			}
			[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
			remove
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Microsoft.UI.Xaml.Controls.ItemsPresenter", "event EventHandler<object> ItemsPresenter.HorizontalSnapPointsChanged");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  event global::System.EventHandler<object> VerticalSnapPointsChanged
		{
			[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
			add
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Microsoft.UI.Xaml.Controls.ItemsPresenter", "event EventHandler<object> ItemsPresenter.VerticalSnapPointsChanged");
			}
			[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
			remove
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Microsoft.UI.Xaml.Controls.ItemsPresenter", "event EventHandler<object> ItemsPresenter.VerticalSnapPointsChanged");
			}
		}
		#endif
		// Processing: Microsoft.UI.Xaml.Controls.Primitives.IScrollSnapPointsInfo
	}
}
