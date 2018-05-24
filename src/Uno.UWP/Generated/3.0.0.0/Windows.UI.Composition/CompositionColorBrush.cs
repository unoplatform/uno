#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.UI.Composition
{
	#if false || false || false || false
	[global::Uno.NotImplemented]
	#endif
	public  partial class CompositionColorBrush : global::Windows.UI.Composition.CompositionBrush
	{
		#if false || false || false || false
		[global::Uno.NotImplemented]
		public  global::Windows.UI.Color Color
		{
			get
			{
				throw new global::System.NotImplementedException("The member Color CompositionColorBrush.Color is not implemented in Uno.");
			}
			set
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.UI.Composition.CompositionColorBrush", "Color CompositionColorBrush.Color");
			}
		}
		#endif
		// Forced skipping of method Windows.UI.Composition.CompositionColorBrush.Color.get
		// Forced skipping of method Windows.UI.Composition.CompositionColorBrush.Color.set
	}
}
