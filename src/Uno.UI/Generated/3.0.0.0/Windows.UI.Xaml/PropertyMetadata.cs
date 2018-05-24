#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.UI.Xaml
{
	#if false || false || false || false
	[global::Uno.NotImplemented]
	#endif
	public  partial class PropertyMetadata 
	{
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		[global::Uno.NotImplemented]
		public  global::Windows.UI.Xaml.CreateDefaultValueCallback CreateDefaultValueCallback
		{
			get
			{
				throw new global::System.NotImplementedException("The member CreateDefaultValueCallback PropertyMetadata.CreateDefaultValueCallback is not implemented in Uno.");
			}
		}
		#endif
		#if false || false || false || false
		[global::Uno.NotImplemented]
		public  object DefaultValue
		{
			get
			{
				throw new global::System.NotImplementedException("The member object PropertyMetadata.DefaultValue is not implemented in Uno.");
			}
		}
		#endif
		#if false || false || false || false
		[global::Uno.NotImplemented]
		public PropertyMetadata( object defaultValue) 
		{
			global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.UI.Xaml.PropertyMetadata", "PropertyMetadata.PropertyMetadata(object defaultValue)");
		}
		#endif
		// Forced skipping of method Windows.UI.Xaml.PropertyMetadata.PropertyMetadata(object)
		#if false || false || false || false
		[global::Uno.NotImplemented]
		public PropertyMetadata( object defaultValue,  global::Windows.UI.Xaml.PropertyChangedCallback propertyChangedCallback) 
		{
			global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.UI.Xaml.PropertyMetadata", "PropertyMetadata.PropertyMetadata(object defaultValue, PropertyChangedCallback propertyChangedCallback)");
		}
		#endif
		// Forced skipping of method Windows.UI.Xaml.PropertyMetadata.PropertyMetadata(object, Windows.UI.Xaml.PropertyChangedCallback)
		// Forced skipping of method Windows.UI.Xaml.PropertyMetadata.DefaultValue.get
		// Forced skipping of method Windows.UI.Xaml.PropertyMetadata.CreateDefaultValueCallback.get
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		[global::Uno.NotImplemented]
		public static global::Windows.UI.Xaml.PropertyMetadata Create( object defaultValue)
		{
			throw new global::System.NotImplementedException("The member PropertyMetadata PropertyMetadata.Create(object defaultValue) is not implemented in Uno.");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		[global::Uno.NotImplemented]
		public static global::Windows.UI.Xaml.PropertyMetadata Create( object defaultValue,  global::Windows.UI.Xaml.PropertyChangedCallback propertyChangedCallback)
		{
			throw new global::System.NotImplementedException("The member PropertyMetadata PropertyMetadata.Create(object defaultValue, PropertyChangedCallback propertyChangedCallback) is not implemented in Uno.");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		[global::Uno.NotImplemented]
		public static global::Windows.UI.Xaml.PropertyMetadata Create( global::Windows.UI.Xaml.CreateDefaultValueCallback createDefaultValueCallback)
		{
			throw new global::System.NotImplementedException("The member PropertyMetadata PropertyMetadata.Create(CreateDefaultValueCallback createDefaultValueCallback) is not implemented in Uno.");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		[global::Uno.NotImplemented]
		public static global::Windows.UI.Xaml.PropertyMetadata Create( global::Windows.UI.Xaml.CreateDefaultValueCallback createDefaultValueCallback,  global::Windows.UI.Xaml.PropertyChangedCallback propertyChangedCallback)
		{
			throw new global::System.NotImplementedException("The member PropertyMetadata PropertyMetadata.Create(CreateDefaultValueCallback createDefaultValueCallback, PropertyChangedCallback propertyChangedCallback) is not implemented in Uno.");
		}
		#endif
	}
}
