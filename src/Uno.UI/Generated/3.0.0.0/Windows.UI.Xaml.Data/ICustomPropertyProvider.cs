#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.UI.Xaml.Data
{
	#if false
	[global::Uno.NotImplemented]
	#endif
	public  partial interface ICustomPropertyProvider 
	{
		#if false
		global::System.Type Type
		{
			get;
		}
		#endif
		#if false
		global::Windows.UI.Xaml.Data.ICustomProperty GetCustomProperty( string name);
		#endif
		#if false
		global::Windows.UI.Xaml.Data.ICustomProperty GetIndexedProperty( string name,  global::System.Type type);
		#endif
		#if false
		string GetStringRepresentation();
		#endif
		// Forced skipping of method Windows.UI.Xaml.Data.ICustomPropertyProvider.Type.get
	}
}
