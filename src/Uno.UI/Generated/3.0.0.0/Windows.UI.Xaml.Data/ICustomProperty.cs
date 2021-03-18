#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.UI.Xaml.Data
{
	#if false
	[global::Uno.NotImplemented]
	#endif
	public  partial interface ICustomProperty 
	{
		#if false
		bool CanRead
		{
			get;
		}
		#endif
		#if false
		bool CanWrite
		{
			get;
		}
		#endif
		#if false
		string Name
		{
			get;
		}
		#endif
		#if false
		global::System.Type Type
		{
			get;
		}
		#endif
		// Forced skipping of method Windows.UI.Xaml.Data.ICustomProperty.Type.get
		// Forced skipping of method Windows.UI.Xaml.Data.ICustomProperty.Name.get
		#if false
		object GetValue( object target);
		#endif
		#if false
		void SetValue( object target,  object value);
		#endif
		#if false
		object GetIndexedValue( object target,  object index);
		#endif
		#if false
		void SetIndexedValue( object target,  object value,  object index);
		#endif
		// Forced skipping of method Windows.UI.Xaml.Data.ICustomProperty.CanWrite.get
		// Forced skipping of method Windows.UI.Xaml.Data.ICustomProperty.CanRead.get
	}
}
