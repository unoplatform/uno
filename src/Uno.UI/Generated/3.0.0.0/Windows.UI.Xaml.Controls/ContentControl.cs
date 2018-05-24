#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.UI.Xaml.Controls
{
	#if false || false || false || false
	[global::Uno.NotImplemented]
	#endif
	public  partial class ContentControl : global::Windows.UI.Xaml.Controls.Control
	{
		#if false || false || false || false
		[global::Uno.NotImplemented]
		public  global::Windows.UI.Xaml.Media.Animation.TransitionCollection ContentTransitions
		{
			get
			{
				return (global::Windows.UI.Xaml.Media.Animation.TransitionCollection)this.GetValue(ContentTransitionsProperty);
			}
			set
			{
				this.SetValue(ContentTransitionsProperty, value);
			}
		}
		#endif
		#if false || false || false || false
		[global::Uno.NotImplemented]
		public  global::Windows.UI.Xaml.Controls.DataTemplateSelector ContentTemplateSelector
		{
			get
			{
				return (global::Windows.UI.Xaml.Controls.DataTemplateSelector)this.GetValue(ContentTemplateSelectorProperty);
			}
			set
			{
				this.SetValue(ContentTemplateSelectorProperty, value);
			}
		}
		#endif
		#if false || false || false || false
		[global::Uno.NotImplemented]
		public  global::Windows.UI.Xaml.DataTemplate ContentTemplate
		{
			get
			{
				return (global::Windows.UI.Xaml.DataTemplate)this.GetValue(ContentTemplateProperty);
			}
			set
			{
				this.SetValue(ContentTemplateProperty, value);
			}
		}
		#endif
		#if false || false || false || false
		[global::Uno.NotImplemented]
		public  object Content
		{
			get
			{
				return (object)this.GetValue(ContentProperty);
			}
			set
			{
				this.SetValue(ContentProperty, value);
			}
		}
		#endif
		#if false || false || false || false
		[global::Uno.NotImplemented]
		public  global::Windows.UI.Xaml.UIElement ContentTemplateRoot
		{
			get
			{
				throw new global::System.NotImplementedException("The member UIElement ContentControl.ContentTemplateRoot is not implemented in Uno.");
			}
		}
		#endif
		#if false || false || false || false
		[global::Uno.NotImplemented]
		public static global::Windows.UI.Xaml.DependencyProperty ContentProperty { get; } = 
		Windows.UI.Xaml.DependencyProperty.Register(
			"Content", typeof(object), 
			typeof(global::Windows.UI.Xaml.Controls.ContentControl), 
			new FrameworkPropertyMetadata(default(object)));
		#endif
		#if false || false || false || false
		[global::Uno.NotImplemented]
		public static global::Windows.UI.Xaml.DependencyProperty ContentTemplateProperty { get; } = 
		Windows.UI.Xaml.DependencyProperty.Register(
			"ContentTemplate", typeof(global::Windows.UI.Xaml.DataTemplate), 
			typeof(global::Windows.UI.Xaml.Controls.ContentControl), 
			new FrameworkPropertyMetadata(default(global::Windows.UI.Xaml.DataTemplate)));
		#endif
		#if false || false || false || false
		[global::Uno.NotImplemented]
		public static global::Windows.UI.Xaml.DependencyProperty ContentTemplateSelectorProperty { get; } = 
		Windows.UI.Xaml.DependencyProperty.Register(
			"ContentTemplateSelector", typeof(global::Windows.UI.Xaml.Controls.DataTemplateSelector), 
			typeof(global::Windows.UI.Xaml.Controls.ContentControl), 
			new FrameworkPropertyMetadata(default(global::Windows.UI.Xaml.Controls.DataTemplateSelector)));
		#endif
		#if false || false || false || false
		[global::Uno.NotImplemented]
		public static global::Windows.UI.Xaml.DependencyProperty ContentTransitionsProperty { get; } = 
		Windows.UI.Xaml.DependencyProperty.Register(
			"ContentTransitions", typeof(global::Windows.UI.Xaml.Media.Animation.TransitionCollection), 
			typeof(global::Windows.UI.Xaml.Controls.ContentControl), 
			new FrameworkPropertyMetadata(default(global::Windows.UI.Xaml.Media.Animation.TransitionCollection)));
		#endif
		#if false || false || false || false
		[global::Uno.NotImplemented]
		public ContentControl() : base()
		{
			global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.UI.Xaml.Controls.ContentControl", "ContentControl.ContentControl()");
		}
		#endif
		// Forced skipping of method Windows.UI.Xaml.Controls.ContentControl.ContentControl()
		// Forced skipping of method Windows.UI.Xaml.Controls.ContentControl.Content.get
		// Forced skipping of method Windows.UI.Xaml.Controls.ContentControl.Content.set
		// Forced skipping of method Windows.UI.Xaml.Controls.ContentControl.ContentTemplate.get
		// Forced skipping of method Windows.UI.Xaml.Controls.ContentControl.ContentTemplate.set
		// Forced skipping of method Windows.UI.Xaml.Controls.ContentControl.ContentTemplateSelector.get
		// Forced skipping of method Windows.UI.Xaml.Controls.ContentControl.ContentTemplateSelector.set
		// Forced skipping of method Windows.UI.Xaml.Controls.ContentControl.ContentTransitions.get
		// Forced skipping of method Windows.UI.Xaml.Controls.ContentControl.ContentTransitions.set
		#if false || false || false || false
		[global::Uno.NotImplemented]
		protected virtual void OnContentChanged( object oldContent,  object newContent)
		{
			global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.UI.Xaml.Controls.ContentControl", "void ContentControl.OnContentChanged(object oldContent, object newContent)");
		}
		#endif
		#if false || false || false || false
		[global::Uno.NotImplemented]
		protected virtual void OnContentTemplateChanged( global::Windows.UI.Xaml.DataTemplate oldContentTemplate,  global::Windows.UI.Xaml.DataTemplate newContentTemplate)
		{
			global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.UI.Xaml.Controls.ContentControl", "void ContentControl.OnContentTemplateChanged(DataTemplate oldContentTemplate, DataTemplate newContentTemplate)");
		}
		#endif
		#if false || false || false || false
		[global::Uno.NotImplemented]
		protected virtual void OnContentTemplateSelectorChanged( global::Windows.UI.Xaml.Controls.DataTemplateSelector oldContentTemplateSelector,  global::Windows.UI.Xaml.Controls.DataTemplateSelector newContentTemplateSelector)
		{
			global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.UI.Xaml.Controls.ContentControl", "void ContentControl.OnContentTemplateSelectorChanged(DataTemplateSelector oldContentTemplateSelector, DataTemplateSelector newContentTemplateSelector)");
		}
		#endif
		// Forced skipping of method Windows.UI.Xaml.Controls.ContentControl.ContentTemplateRoot.get
		// Forced skipping of method Windows.UI.Xaml.Controls.ContentControl.ContentProperty.get
		// Forced skipping of method Windows.UI.Xaml.Controls.ContentControl.ContentTemplateProperty.get
		// Forced skipping of method Windows.UI.Xaml.Controls.ContentControl.ContentTemplateSelectorProperty.get
		// Forced skipping of method Windows.UI.Xaml.Controls.ContentControl.ContentTransitionsProperty.get
	}
}
