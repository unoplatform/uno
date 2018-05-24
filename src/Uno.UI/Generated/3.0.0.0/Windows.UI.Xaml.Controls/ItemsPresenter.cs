#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.UI.Xaml.Controls
{
	#if false || false || false || false
	[global::Uno.NotImplemented]
	#endif
	public  partial class ItemsPresenter : global::Windows.UI.Xaml.FrameworkElement,global::Windows.UI.Xaml.Controls.Primitives.IScrollSnapPointsInfo
	{
		#if false || false || false || false
		[global::Uno.NotImplemented]
		public  global::Windows.UI.Xaml.Thickness Padding
		{
			get
			{
				return (global::Windows.UI.Xaml.Thickness)this.GetValue(PaddingProperty);
			}
			set
			{
				this.SetValue(PaddingProperty, value);
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		[global::Uno.NotImplemented]
		public  global::Windows.UI.Xaml.Media.Animation.TransitionCollection HeaderTransitions
		{
			get
			{
				return (global::Windows.UI.Xaml.Media.Animation.TransitionCollection)this.GetValue(HeaderTransitionsProperty);
			}
			set
			{
				this.SetValue(HeaderTransitionsProperty, value);
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		[global::Uno.NotImplemented]
		public  global::Windows.UI.Xaml.DataTemplate HeaderTemplate
		{
			get
			{
				return (global::Windows.UI.Xaml.DataTemplate)this.GetValue(HeaderTemplateProperty);
			}
			set
			{
				this.SetValue(HeaderTemplateProperty, value);
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		[global::Uno.NotImplemented]
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
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		[global::Uno.NotImplemented]
		public  global::Windows.UI.Xaml.Media.Animation.TransitionCollection FooterTransitions
		{
			get
			{
				return (global::Windows.UI.Xaml.Media.Animation.TransitionCollection)this.GetValue(FooterTransitionsProperty);
			}
			set
			{
				this.SetValue(FooterTransitionsProperty, value);
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		[global::Uno.NotImplemented]
		public  global::Windows.UI.Xaml.DataTemplate FooterTemplate
		{
			get
			{
				return (global::Windows.UI.Xaml.DataTemplate)this.GetValue(FooterTemplateProperty);
			}
			set
			{
				this.SetValue(FooterTemplateProperty, value);
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		[global::Uno.NotImplemented]
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
		#if false || false || false || false
		[global::Uno.NotImplemented]
		public  bool AreHorizontalSnapPointsRegular
		{
			get
			{
				throw new global::System.NotImplementedException("The member bool ItemsPresenter.AreHorizontalSnapPointsRegular is not implemented in Uno.");
			}
		}
		#endif
		#if false || false || false || false
		[global::Uno.NotImplemented]
		public  bool AreVerticalSnapPointsRegular
		{
			get
			{
				throw new global::System.NotImplementedException("The member bool ItemsPresenter.AreVerticalSnapPointsRegular is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		[global::Uno.NotImplemented]
		public static global::Windows.UI.Xaml.DependencyProperty HeaderProperty { get; } = 
		Windows.UI.Xaml.DependencyProperty.Register(
			"Header", typeof(object), 
			typeof(global::Windows.UI.Xaml.Controls.ItemsPresenter), 
			new FrameworkPropertyMetadata(default(object)));
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		[global::Uno.NotImplemented]
		public static global::Windows.UI.Xaml.DependencyProperty HeaderTemplateProperty { get; } = 
		Windows.UI.Xaml.DependencyProperty.Register(
			"HeaderTemplate", typeof(global::Windows.UI.Xaml.DataTemplate), 
			typeof(global::Windows.UI.Xaml.Controls.ItemsPresenter), 
			new FrameworkPropertyMetadata(default(global::Windows.UI.Xaml.DataTemplate)));
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		[global::Uno.NotImplemented]
		public static global::Windows.UI.Xaml.DependencyProperty HeaderTransitionsProperty { get; } = 
		Windows.UI.Xaml.DependencyProperty.Register(
			"HeaderTransitions", typeof(global::Windows.UI.Xaml.Media.Animation.TransitionCollection), 
			typeof(global::Windows.UI.Xaml.Controls.ItemsPresenter), 
			new FrameworkPropertyMetadata(default(global::Windows.UI.Xaml.Media.Animation.TransitionCollection)));
		#endif
		#if false || false || false || false
		[global::Uno.NotImplemented]
		public static global::Windows.UI.Xaml.DependencyProperty PaddingProperty { get; } = 
		Windows.UI.Xaml.DependencyProperty.Register(
			"Padding", typeof(global::Windows.UI.Xaml.Thickness), 
			typeof(global::Windows.UI.Xaml.Controls.ItemsPresenter), 
			new FrameworkPropertyMetadata(default(global::Windows.UI.Xaml.Thickness)));
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		[global::Uno.NotImplemented]
		public static global::Windows.UI.Xaml.DependencyProperty FooterProperty { get; } = 
		Windows.UI.Xaml.DependencyProperty.Register(
			"Footer", typeof(object), 
			typeof(global::Windows.UI.Xaml.Controls.ItemsPresenter), 
			new FrameworkPropertyMetadata(default(object)));
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		[global::Uno.NotImplemented]
		public static global::Windows.UI.Xaml.DependencyProperty FooterTemplateProperty { get; } = 
		Windows.UI.Xaml.DependencyProperty.Register(
			"FooterTemplate", typeof(global::Windows.UI.Xaml.DataTemplate), 
			typeof(global::Windows.UI.Xaml.Controls.ItemsPresenter), 
			new FrameworkPropertyMetadata(default(global::Windows.UI.Xaml.DataTemplate)));
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		[global::Uno.NotImplemented]
		public static global::Windows.UI.Xaml.DependencyProperty FooterTransitionsProperty { get; } = 
		Windows.UI.Xaml.DependencyProperty.Register(
			"FooterTransitions", typeof(global::Windows.UI.Xaml.Media.Animation.TransitionCollection), 
			typeof(global::Windows.UI.Xaml.Controls.ItemsPresenter), 
			new FrameworkPropertyMetadata(default(global::Windows.UI.Xaml.Media.Animation.TransitionCollection)));
		#endif
		#if false || false || false || false
		[global::Uno.NotImplemented]
		public ItemsPresenter() : base()
		{
			global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.UI.Xaml.Controls.ItemsPresenter", "ItemsPresenter.ItemsPresenter()");
		}
		#endif
		// Forced skipping of method Windows.UI.Xaml.Controls.ItemsPresenter.ItemsPresenter()
		// Forced skipping of method Windows.UI.Xaml.Controls.ItemsPresenter.Header.get
		// Forced skipping of method Windows.UI.Xaml.Controls.ItemsPresenter.Header.set
		// Forced skipping of method Windows.UI.Xaml.Controls.ItemsPresenter.HeaderTemplate.get
		// Forced skipping of method Windows.UI.Xaml.Controls.ItemsPresenter.HeaderTemplate.set
		// Forced skipping of method Windows.UI.Xaml.Controls.ItemsPresenter.HeaderTransitions.get
		// Forced skipping of method Windows.UI.Xaml.Controls.ItemsPresenter.HeaderTransitions.set
		// Forced skipping of method Windows.UI.Xaml.Controls.ItemsPresenter.Padding.get
		// Forced skipping of method Windows.UI.Xaml.Controls.ItemsPresenter.Padding.set
		// Forced skipping of method Windows.UI.Xaml.Controls.ItemsPresenter.AreHorizontalSnapPointsRegular.get
		// Forced skipping of method Windows.UI.Xaml.Controls.ItemsPresenter.AreVerticalSnapPointsRegular.get
		// Forced skipping of method Windows.UI.Xaml.Controls.ItemsPresenter.HorizontalSnapPointsChanged.add
		// Forced skipping of method Windows.UI.Xaml.Controls.ItemsPresenter.HorizontalSnapPointsChanged.remove
		// Forced skipping of method Windows.UI.Xaml.Controls.ItemsPresenter.VerticalSnapPointsChanged.add
		// Forced skipping of method Windows.UI.Xaml.Controls.ItemsPresenter.VerticalSnapPointsChanged.remove
		#if false || false || false || false
		[global::Uno.NotImplemented]
		public  global::System.Collections.Generic.IReadOnlyList<float> GetIrregularSnapPoints( global::Windows.UI.Xaml.Controls.Orientation orientation,  global::Windows.UI.Xaml.Controls.Primitives.SnapPointsAlignment alignment)
		{
			throw new global::System.NotImplementedException("The member IReadOnlyList<float> ItemsPresenter.GetIrregularSnapPoints(Orientation orientation, SnapPointsAlignment alignment) is not implemented in Uno.");
		}
		#endif
		#if false || false || false || false
		[global::Uno.NotImplemented]
		public  float GetRegularSnapPoints( global::Windows.UI.Xaml.Controls.Orientation orientation,  global::Windows.UI.Xaml.Controls.Primitives.SnapPointsAlignment alignment, out float offset)
		{
			throw new global::System.NotImplementedException("The member float ItemsPresenter.GetRegularSnapPoints(Orientation orientation, SnapPointsAlignment alignment, out float offset) is not implemented in Uno.");
		}
		#endif
		// Forced skipping of method Windows.UI.Xaml.Controls.ItemsPresenter.Footer.get
		// Forced skipping of method Windows.UI.Xaml.Controls.ItemsPresenter.Footer.set
		// Forced skipping of method Windows.UI.Xaml.Controls.ItemsPresenter.FooterTemplate.get
		// Forced skipping of method Windows.UI.Xaml.Controls.ItemsPresenter.FooterTemplate.set
		// Forced skipping of method Windows.UI.Xaml.Controls.ItemsPresenter.FooterTransitions.get
		// Forced skipping of method Windows.UI.Xaml.Controls.ItemsPresenter.FooterTransitions.set
		// Forced skipping of method Windows.UI.Xaml.Controls.ItemsPresenter.FooterProperty.get
		// Forced skipping of method Windows.UI.Xaml.Controls.ItemsPresenter.FooterTemplateProperty.get
		// Forced skipping of method Windows.UI.Xaml.Controls.ItemsPresenter.FooterTransitionsProperty.get
		// Forced skipping of method Windows.UI.Xaml.Controls.ItemsPresenter.HeaderProperty.get
		// Forced skipping of method Windows.UI.Xaml.Controls.ItemsPresenter.HeaderTemplateProperty.get
		// Forced skipping of method Windows.UI.Xaml.Controls.ItemsPresenter.HeaderTransitionsProperty.get
		// Forced skipping of method Windows.UI.Xaml.Controls.ItemsPresenter.PaddingProperty.get
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		[global::Uno.NotImplemented]
		public  event global::System.EventHandler<object> HorizontalSnapPointsChanged
		{
			[global::Uno.NotImplemented]
			add
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.UI.Xaml.Controls.ItemsPresenter", "event EventHandler<object> ItemsPresenter.HorizontalSnapPointsChanged");
			}
			[global::Uno.NotImplemented]
			remove
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.UI.Xaml.Controls.ItemsPresenter", "event EventHandler<object> ItemsPresenter.HorizontalSnapPointsChanged");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		[global::Uno.NotImplemented]
		public  event global::System.EventHandler<object> VerticalSnapPointsChanged
		{
			[global::Uno.NotImplemented]
			add
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.UI.Xaml.Controls.ItemsPresenter", "event EventHandler<object> ItemsPresenter.VerticalSnapPointsChanged");
			}
			[global::Uno.NotImplemented]
			remove
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.UI.Xaml.Controls.ItemsPresenter", "event EventHandler<object> ItemsPresenter.VerticalSnapPointsChanged");
			}
		}
		#endif
		// Processing: Windows.UI.Xaml.Controls.Primitives.IScrollSnapPointsInfo
	}
}
