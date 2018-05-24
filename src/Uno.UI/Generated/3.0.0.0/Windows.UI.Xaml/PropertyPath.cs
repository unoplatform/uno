#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.UI.Xaml
{
	#if false || false || false || false
	[global::Uno.NotImplemented]
	#endif
	public  partial class PropertyPath : global::Windows.UI.Xaml.DependencyObject
	{
		#if false || false || false || false
		[global::Uno.NotImplemented]
		public  string Path
		{
			get
			{
				throw new global::System.NotImplementedException("The member string PropertyPath.Path is not implemented in Uno.");
			}
		}
		#endif
		#if false || false || false || false
		[global::Uno.NotImplemented]
		public PropertyPath( string path) : base()
		{
			global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.UI.Xaml.PropertyPath", "PropertyPath.PropertyPath(string path)");
		}
		#endif
		// Forced skipping of method Windows.UI.Xaml.PropertyPath.PropertyPath(string)
		// Forced skipping of method Windows.UI.Xaml.PropertyPath.Path.get
	}
}
