#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.Devices.Sensors
{
	#if false || false || false || false
	[global::Uno.NotImplemented]
	#endif
	public  partial class SimpleOrientationSensor 
	{
		#if false || false || false || false
		[global::Uno.NotImplemented]
		public  global::Windows.Graphics.Display.DisplayOrientations ReadingTransform
		{
			get
			{
				throw new global::System.NotImplementedException("The member DisplayOrientations SimpleOrientationSensor.ReadingTransform is not implemented in Uno.");
			}
			set
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Devices.Sensors.SimpleOrientationSensor", "DisplayOrientations SimpleOrientationSensor.ReadingTransform");
			}
		}
		#endif
		#if false || false || false || false
		[global::Uno.NotImplemented]
		public  string DeviceId
		{
			get
			{
				throw new global::System.NotImplementedException("The member string SimpleOrientationSensor.DeviceId is not implemented in Uno.");
			}
		}
		#endif
		#if false || false || false || false
		[global::Uno.NotImplemented]
		public  global::Windows.Devices.Sensors.SimpleOrientation GetCurrentOrientation()
		{
			throw new global::System.NotImplementedException("The member SimpleOrientation SimpleOrientationSensor.GetCurrentOrientation() is not implemented in Uno.");
		}
		#endif
		// Forced skipping of method Windows.Devices.Sensors.SimpleOrientationSensor.OrientationChanged.add
		// Forced skipping of method Windows.Devices.Sensors.SimpleOrientationSensor.OrientationChanged.remove
		// Forced skipping of method Windows.Devices.Sensors.SimpleOrientationSensor.DeviceId.get
		// Forced skipping of method Windows.Devices.Sensors.SimpleOrientationSensor.ReadingTransform.set
		// Forced skipping of method Windows.Devices.Sensors.SimpleOrientationSensor.ReadingTransform.get
		#if false || false || false || false
		[global::Uno.NotImplemented]
		public static global::Windows.Devices.Sensors.SimpleOrientationSensor GetDefault()
		{
			throw new global::System.NotImplementedException("The member SimpleOrientationSensor SimpleOrientationSensor.GetDefault() is not implemented in Uno.");
		}
		#endif
		#if false || false || false || false
		[global::Uno.NotImplemented]
		public  event global::Windows.Foundation.TypedEventHandler<global::Windows.Devices.Sensors.SimpleOrientationSensor, global::Windows.Devices.Sensors.SimpleOrientationSensorOrientationChangedEventArgs> OrientationChanged
		{
			[global::Uno.NotImplemented]
			add
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Devices.Sensors.SimpleOrientationSensor", "event TypedEventHandler<SimpleOrientationSensor, SimpleOrientationSensorOrientationChangedEventArgs> SimpleOrientationSensor.OrientationChanged");
			}
			[global::Uno.NotImplemented]
			remove
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Devices.Sensors.SimpleOrientationSensor", "event TypedEventHandler<SimpleOrientationSensor, SimpleOrientationSensorOrientationChangedEventArgs> SimpleOrientationSensor.OrientationChanged");
			}
		}
		#endif
	}
}
