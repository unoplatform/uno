#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.UI.Xaml.Markup
{
	#if false || false || false || false
	[global::Uno.NotImplemented]
	#endif
	public  partial class XamlBindingHelper 
	{
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		[global::Uno.NotImplemented]
		public static global::Windows.UI.Xaml.DependencyProperty DataTemplateComponentProperty { get; } = 
		Windows.UI.Xaml.DependencyProperty.RegisterAttached(
			"DataTemplateComponent", typeof(global::Windows.UI.Xaml.Markup.IDataTemplateComponent), 
			typeof(global::Windows.UI.Xaml.Markup.XamlBindingHelper), 
			new FrameworkPropertyMetadata(default(global::Windows.UI.Xaml.Markup.IDataTemplateComponent)));
		#endif
		// Forced skipping of method Windows.UI.Xaml.Markup.XamlBindingHelper.DataTemplateComponentProperty.get
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		[global::Uno.NotImplemented]
		public static global::Windows.UI.Xaml.Markup.IDataTemplateComponent GetDataTemplateComponent( global::Windows.UI.Xaml.DependencyObject element)
		{
			return (global::Windows.UI.Xaml.Markup.IDataTemplateComponent)element.GetValue(DataTemplateComponentProperty);
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		[global::Uno.NotImplemented]
		public static void SetDataTemplateComponent( global::Windows.UI.Xaml.DependencyObject element,  global::Windows.UI.Xaml.Markup.IDataTemplateComponent value)
		{
			element.SetValue(DataTemplateComponentProperty, value);
		}
		#endif
		#if false || false || false || false
		[global::Uno.NotImplemented]
		public static void SuspendRendering( global::Windows.UI.Xaml.UIElement target)
		{
			global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.UI.Xaml.Markup.XamlBindingHelper", "void XamlBindingHelper.SuspendRendering(UIElement target)");
		}
		#endif
		#if false || false || false || false
		[global::Uno.NotImplemented]
		public static void ResumeRendering( global::Windows.UI.Xaml.UIElement target)
		{
			global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.UI.Xaml.Markup.XamlBindingHelper", "void XamlBindingHelper.ResumeRendering(UIElement target)");
		}
		#endif
		#if false || false || false || false
		[global::Uno.NotImplemented]
		public static object ConvertValue( global::System.Type type,  object value)
		{
			throw new global::System.NotImplementedException("The member object XamlBindingHelper.ConvertValue(Type type, object value) is not implemented in Uno.");
		}
		#endif
		#if false || false || false || false
		[global::Uno.NotImplemented]
		public static void SetPropertyFromString( object dependencyObject,  global::Windows.UI.Xaml.DependencyProperty propertyToSet,  string value)
		{
			global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.UI.Xaml.Markup.XamlBindingHelper", "void XamlBindingHelper.SetPropertyFromString(object dependencyObject, DependencyProperty propertyToSet, string value)");
		}
		#endif
		#if false || false || false || false
		[global::Uno.NotImplemented]
		public static void SetPropertyFromBoolean( object dependencyObject,  global::Windows.UI.Xaml.DependencyProperty propertyToSet,  bool value)
		{
			global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.UI.Xaml.Markup.XamlBindingHelper", "void XamlBindingHelper.SetPropertyFromBoolean(object dependencyObject, DependencyProperty propertyToSet, bool value)");
		}
		#endif
		#if false || false || false || false
		[global::Uno.NotImplemented]
		public static void SetPropertyFromChar16( object dependencyObject,  global::Windows.UI.Xaml.DependencyProperty propertyToSet,  char value)
		{
			global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.UI.Xaml.Markup.XamlBindingHelper", "void XamlBindingHelper.SetPropertyFromChar16(object dependencyObject, DependencyProperty propertyToSet, char value)");
		}
		#endif
		#if false || false || false || false
		[global::Uno.NotImplemented]
		public static void SetPropertyFromDateTime( object dependencyObject,  global::Windows.UI.Xaml.DependencyProperty propertyToSet,  global::System.DateTimeOffset value)
		{
			global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.UI.Xaml.Markup.XamlBindingHelper", "void XamlBindingHelper.SetPropertyFromDateTime(object dependencyObject, DependencyProperty propertyToSet, DateTimeOffset value)");
		}
		#endif
		#if false || false || false || false
		[global::Uno.NotImplemented]
		public static void SetPropertyFromDouble( object dependencyObject,  global::Windows.UI.Xaml.DependencyProperty propertyToSet,  double value)
		{
			global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.UI.Xaml.Markup.XamlBindingHelper", "void XamlBindingHelper.SetPropertyFromDouble(object dependencyObject, DependencyProperty propertyToSet, double value)");
		}
		#endif
		#if false || false || false || false
		[global::Uno.NotImplemented]
		public static void SetPropertyFromInt32( object dependencyObject,  global::Windows.UI.Xaml.DependencyProperty propertyToSet,  int value)
		{
			global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.UI.Xaml.Markup.XamlBindingHelper", "void XamlBindingHelper.SetPropertyFromInt32(object dependencyObject, DependencyProperty propertyToSet, int value)");
		}
		#endif
		#if false || false || false || false
		[global::Uno.NotImplemented]
		public static void SetPropertyFromUInt32( object dependencyObject,  global::Windows.UI.Xaml.DependencyProperty propertyToSet,  uint value)
		{
			global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.UI.Xaml.Markup.XamlBindingHelper", "void XamlBindingHelper.SetPropertyFromUInt32(object dependencyObject, DependencyProperty propertyToSet, uint value)");
		}
		#endif
		#if false || false || false || false
		[global::Uno.NotImplemented]
		public static void SetPropertyFromInt64( object dependencyObject,  global::Windows.UI.Xaml.DependencyProperty propertyToSet,  long value)
		{
			global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.UI.Xaml.Markup.XamlBindingHelper", "void XamlBindingHelper.SetPropertyFromInt64(object dependencyObject, DependencyProperty propertyToSet, long value)");
		}
		#endif
		#if false || false || false || false
		[global::Uno.NotImplemented]
		public static void SetPropertyFromUInt64( object dependencyObject,  global::Windows.UI.Xaml.DependencyProperty propertyToSet,  ulong value)
		{
			global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.UI.Xaml.Markup.XamlBindingHelper", "void XamlBindingHelper.SetPropertyFromUInt64(object dependencyObject, DependencyProperty propertyToSet, ulong value)");
		}
		#endif
		#if false || false || false || false
		[global::Uno.NotImplemented]
		public static void SetPropertyFromSingle( object dependencyObject,  global::Windows.UI.Xaml.DependencyProperty propertyToSet,  float value)
		{
			global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.UI.Xaml.Markup.XamlBindingHelper", "void XamlBindingHelper.SetPropertyFromSingle(object dependencyObject, DependencyProperty propertyToSet, float value)");
		}
		#endif
		#if false || false || false || false
		[global::Uno.NotImplemented]
		public static void SetPropertyFromPoint( object dependencyObject,  global::Windows.UI.Xaml.DependencyProperty propertyToSet,  global::Windows.Foundation.Point value)
		{
			global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.UI.Xaml.Markup.XamlBindingHelper", "void XamlBindingHelper.SetPropertyFromPoint(object dependencyObject, DependencyProperty propertyToSet, Point value)");
		}
		#endif
		#if false || false || false || false
		[global::Uno.NotImplemented]
		public static void SetPropertyFromRect( object dependencyObject,  global::Windows.UI.Xaml.DependencyProperty propertyToSet,  global::Windows.Foundation.Rect value)
		{
			global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.UI.Xaml.Markup.XamlBindingHelper", "void XamlBindingHelper.SetPropertyFromRect(object dependencyObject, DependencyProperty propertyToSet, Rect value)");
		}
		#endif
		#if false || false || false || false
		[global::Uno.NotImplemented]
		public static void SetPropertyFromSize( object dependencyObject,  global::Windows.UI.Xaml.DependencyProperty propertyToSet,  global::Windows.Foundation.Size value)
		{
			global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.UI.Xaml.Markup.XamlBindingHelper", "void XamlBindingHelper.SetPropertyFromSize(object dependencyObject, DependencyProperty propertyToSet, Size value)");
		}
		#endif
		#if false || false || false || false
		[global::Uno.NotImplemented]
		public static void SetPropertyFromTimeSpan( object dependencyObject,  global::Windows.UI.Xaml.DependencyProperty propertyToSet,  global::System.TimeSpan value)
		{
			global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.UI.Xaml.Markup.XamlBindingHelper", "void XamlBindingHelper.SetPropertyFromTimeSpan(object dependencyObject, DependencyProperty propertyToSet, TimeSpan value)");
		}
		#endif
		#if false || false || false || false
		[global::Uno.NotImplemented]
		public static void SetPropertyFromByte( object dependencyObject,  global::Windows.UI.Xaml.DependencyProperty propertyToSet,  byte value)
		{
			global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.UI.Xaml.Markup.XamlBindingHelper", "void XamlBindingHelper.SetPropertyFromByte(object dependencyObject, DependencyProperty propertyToSet, byte value)");
		}
		#endif
		#if false || false || false || false
		[global::Uno.NotImplemented]
		public static void SetPropertyFromUri( object dependencyObject,  global::Windows.UI.Xaml.DependencyProperty propertyToSet,  global::System.Uri value)
		{
			global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.UI.Xaml.Markup.XamlBindingHelper", "void XamlBindingHelper.SetPropertyFromUri(object dependencyObject, DependencyProperty propertyToSet, Uri value)");
		}
		#endif
		#if false || false || false || false
		[global::Uno.NotImplemented]
		public static void SetPropertyFromObject( object dependencyObject,  global::Windows.UI.Xaml.DependencyProperty propertyToSet,  object value)
		{
			global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.UI.Xaml.Markup.XamlBindingHelper", "void XamlBindingHelper.SetPropertyFromObject(object dependencyObject, DependencyProperty propertyToSet, object value)");
		}
		#endif
	}
}
