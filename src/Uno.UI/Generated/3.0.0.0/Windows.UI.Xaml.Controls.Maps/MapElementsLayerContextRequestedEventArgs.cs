#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.UI.Xaml.Controls.Maps
{
	#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __MACOS__
	[global::Uno.NotImplemented]
	#endif
	public  partial class MapElementsLayerContextRequestedEventArgs 
	{
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __MACOS__
		[global::Uno.NotImplemented]
		public  global::Windows.Devices.Geolocation.Geopoint Location
		{
			get
			{
				throw new global::System.NotImplementedException("The member Geopoint MapElementsLayerContextRequestedEventArgs.Location is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __MACOS__
		[global::Uno.NotImplemented]
		public  global::System.Collections.Generic.IReadOnlyList<global::Windows.UI.Xaml.Controls.Maps.MapElement> MapElements
		{
			get
			{
				throw new global::System.NotImplementedException("The member IReadOnlyList<MapElement> MapElementsLayerContextRequestedEventArgs.MapElements is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __MACOS__
		[global::Uno.NotImplemented]
		public  global::Windows.Foundation.Point Position
		{
			get
			{
				throw new global::System.NotImplementedException("The member Point MapElementsLayerContextRequestedEventArgs.Position is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __MACOS__
		[global::Uno.NotImplemented]
		public MapElementsLayerContextRequestedEventArgs() 
		{
			global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.UI.Xaml.Controls.Maps.MapElementsLayerContextRequestedEventArgs", "MapElementsLayerContextRequestedEventArgs.MapElementsLayerContextRequestedEventArgs()");
		}
		#endif
		// Forced skipping of method Windows.UI.Xaml.Controls.Maps.MapElementsLayerContextRequestedEventArgs.MapElementsLayerContextRequestedEventArgs()
		// Forced skipping of method Windows.UI.Xaml.Controls.Maps.MapElementsLayerContextRequestedEventArgs.Position.get
		// Forced skipping of method Windows.UI.Xaml.Controls.Maps.MapElementsLayerContextRequestedEventArgs.Location.get
		// Forced skipping of method Windows.UI.Xaml.Controls.Maps.MapElementsLayerContextRequestedEventArgs.MapElements.get
	}
}
