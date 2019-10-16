#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.Devices.Geolocation
{
	#if false || false || NET461 || false || __MACOS__
	[global::Uno.NotImplemented]
	#endif
	public  partial class Geolocator 
	{
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __MACOS__
		[global::Uno.NotImplemented]
		public  uint ReportInterval
		{
			get
			{
				throw new global::System.NotImplementedException("The member uint Geolocator.ReportInterval is not implemented in Uno.");
			}
			set
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Devices.Geolocation.Geolocator", "uint Geolocator.ReportInterval");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __MACOS__
		[global::Uno.NotImplemented]
		public  double MovementThreshold
		{
			get
			{
				throw new global::System.NotImplementedException("The member double Geolocator.MovementThreshold is not implemented in Uno.");
			}
			set
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Devices.Geolocation.Geolocator", "double Geolocator.MovementThreshold");
			}
		}
		#endif
		#if false || false || NET461 || false || __MACOS__
		[global::Uno.NotImplemented]
		public  global::Windows.Devices.Geolocation.PositionAccuracy DesiredAccuracy
		{
			get
			{
				throw new global::System.NotImplementedException("The member PositionAccuracy Geolocator.DesiredAccuracy is not implemented in Uno.");
			}
			set
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Devices.Geolocation.Geolocator", "PositionAccuracy Geolocator.DesiredAccuracy");
			}
		}
		#endif
		#if false || false || NET461 || false || __MACOS__
		[global::Uno.NotImplemented]
		public  global::Windows.Devices.Geolocation.PositionStatus LocationStatus
		{
			get
			{
				throw new global::System.NotImplementedException("The member PositionStatus Geolocator.LocationStatus is not implemented in Uno.");
			}
		}
		#endif
		#if false || false || NET461 || false || __MACOS__
		[global::Uno.NotImplemented]
		public  uint? DesiredAccuracyInMeters
		{
			get
			{
				throw new global::System.NotImplementedException("The member uint? Geolocator.DesiredAccuracyInMeters is not implemented in Uno.");
			}
			set
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Devices.Geolocation.Geolocator", "uint? Geolocator.DesiredAccuracyInMeters");
			}
		}
		#endif
		#if false || false || NET461 || false || __MACOS__
		[global::Uno.NotImplemented]
		public static global::Windows.Devices.Geolocation.BasicGeoposition? DefaultGeoposition
		{
			get
			{
				throw new global::System.NotImplementedException("The member BasicGeoposition? Geolocator.DefaultGeoposition is not implemented in Uno.");
			}
			set
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Devices.Geolocation.Geolocator", "BasicGeoposition? Geolocator.DefaultGeoposition");
			}
		}
		#endif
		#if false || false || NET461 || false || __MACOS__
		[global::Uno.NotImplemented]
		public static bool IsDefaultGeopositionRecommended
		{
			get
			{
				throw new global::System.NotImplementedException("The member bool Geolocator.IsDefaultGeopositionRecommended is not implemented in Uno.");
			}
		}
		#endif
		#if false || false || NET461 || false || __MACOS__
		[global::Uno.NotImplemented]
		public Geolocator() 
		{
			global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Devices.Geolocation.Geolocator", "Geolocator.Geolocator()");
		}
		#endif
		// Forced skipping of method Windows.Devices.Geolocation.Geolocator.Geolocator()
		// Forced skipping of method Windows.Devices.Geolocation.Geolocator.DesiredAccuracy.get
		// Forced skipping of method Windows.Devices.Geolocation.Geolocator.DesiredAccuracy.set
		// Forced skipping of method Windows.Devices.Geolocation.Geolocator.MovementThreshold.get
		// Forced skipping of method Windows.Devices.Geolocation.Geolocator.MovementThreshold.set
		// Forced skipping of method Windows.Devices.Geolocation.Geolocator.ReportInterval.get
		// Forced skipping of method Windows.Devices.Geolocation.Geolocator.ReportInterval.set
		// Forced skipping of method Windows.Devices.Geolocation.Geolocator.LocationStatus.get
		#if false || false || NET461 || false || __MACOS__
		[global::Uno.NotImplemented]
		public  global::Windows.Foundation.IAsyncOperation<global::Windows.Devices.Geolocation.Geoposition> GetGeopositionAsync()
		{
			throw new global::System.NotImplementedException("The member IAsyncOperation<Geoposition> Geolocator.GetGeopositionAsync() is not implemented in Uno.");
		}
		#endif
		#if false || false || NET461 || false || __MACOS__
		[global::Uno.NotImplemented]
		public  global::Windows.Foundation.IAsyncOperation<global::Windows.Devices.Geolocation.Geoposition> GetGeopositionAsync( global::System.TimeSpan maximumAge,  global::System.TimeSpan timeout)
		{
			throw new global::System.NotImplementedException("The member IAsyncOperation<Geoposition> Geolocator.GetGeopositionAsync(TimeSpan maximumAge, TimeSpan timeout) is not implemented in Uno.");
		}
		#endif
		// Forced skipping of method Windows.Devices.Geolocation.Geolocator.PositionChanged.add
		// Forced skipping of method Windows.Devices.Geolocation.Geolocator.PositionChanged.remove
		// Forced skipping of method Windows.Devices.Geolocation.Geolocator.StatusChanged.add
		// Forced skipping of method Windows.Devices.Geolocation.Geolocator.StatusChanged.remove
		// Forced skipping of method Windows.Devices.Geolocation.Geolocator.DesiredAccuracyInMeters.get
		// Forced skipping of method Windows.Devices.Geolocation.Geolocator.DesiredAccuracyInMeters.set
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __MACOS__
		[global::Uno.NotImplemented]
		public  void AllowFallbackToConsentlessPositions()
		{
			global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Devices.Geolocation.Geolocator", "void Geolocator.AllowFallbackToConsentlessPositions()");
		}
		#endif
		// Forced skipping of method Windows.Devices.Geolocation.Geolocator.IsDefaultGeopositionRecommended.get
		// Forced skipping of method Windows.Devices.Geolocation.Geolocator.DefaultGeoposition.set
		// Forced skipping of method Windows.Devices.Geolocation.Geolocator.DefaultGeoposition.get
		#if false || false || NET461 || false || __MACOS__
		[global::Uno.NotImplemented]
		public static global::Windows.Foundation.IAsyncOperation<global::Windows.Devices.Geolocation.GeolocationAccessStatus> RequestAccessAsync()
		{
			throw new global::System.NotImplementedException("The member IAsyncOperation<GeolocationAccessStatus> Geolocator.RequestAccessAsync() is not implemented in Uno.");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __MACOS__
		[global::Uno.NotImplemented]
		public static global::Windows.Foundation.IAsyncOperation<global::System.Collections.Generic.IReadOnlyList<global::Windows.Devices.Geolocation.Geoposition>> GetGeopositionHistoryAsync( global::System.DateTimeOffset startTime)
		{
			throw new global::System.NotImplementedException("The member IAsyncOperation<IReadOnlyList<Geoposition>> Geolocator.GetGeopositionHistoryAsync(DateTimeOffset startTime) is not implemented in Uno.");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __MACOS__
		[global::Uno.NotImplemented]
		public static global::Windows.Foundation.IAsyncOperation<global::System.Collections.Generic.IReadOnlyList<global::Windows.Devices.Geolocation.Geoposition>> GetGeopositionHistoryAsync( global::System.DateTimeOffset startTime,  global::System.TimeSpan duration)
		{
			throw new global::System.NotImplementedException("The member IAsyncOperation<IReadOnlyList<Geoposition>> Geolocator.GetGeopositionHistoryAsync(DateTimeOffset startTime, TimeSpan duration) is not implemented in Uno.");
		}
		#endif
		#if false || false || NET461 || false || __MACOS__
		[global::Uno.NotImplemented]
		public  event global::Windows.Foundation.TypedEventHandler<global::Windows.Devices.Geolocation.Geolocator, global::Windows.Devices.Geolocation.PositionChangedEventArgs> PositionChanged
		{
			[global::Uno.NotImplemented]
			add
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Devices.Geolocation.Geolocator", "event TypedEventHandler<Geolocator, PositionChangedEventArgs> Geolocator.PositionChanged");
			}
			[global::Uno.NotImplemented]
			remove
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Devices.Geolocation.Geolocator", "event TypedEventHandler<Geolocator, PositionChangedEventArgs> Geolocator.PositionChanged");
			}
		}
		#endif
		#if false || false || NET461 || false || __MACOS__
		[global::Uno.NotImplemented]
		public  event global::Windows.Foundation.TypedEventHandler<global::Windows.Devices.Geolocation.Geolocator, global::Windows.Devices.Geolocation.StatusChangedEventArgs> StatusChanged
		{
			[global::Uno.NotImplemented]
			add
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Devices.Geolocation.Geolocator", "event TypedEventHandler<Geolocator, StatusChangedEventArgs> Geolocator.StatusChanged");
			}
			[global::Uno.NotImplemented]
			remove
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Devices.Geolocation.Geolocator", "event TypedEventHandler<Geolocator, StatusChangedEventArgs> Geolocator.StatusChanged");
			}
		}
		#endif
	}
}
