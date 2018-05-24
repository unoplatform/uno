#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.Devices.Bluetooth
{
	#if __ANDROID__ || __IOS__ || NET46 || __WASM__
	#if __ANDROID__ || __IOS__ || NET46 || __WASM__
	[global::Uno.NotImplemented]
	#endif
	public   enum BluetoothMinorClass 
	{
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		Uncategorized,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		ComputerDesktop,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		ComputerServer,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		ComputerLaptop,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		ComputerHandheld,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		ComputerPalmSize,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		ComputerWearable,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		ComputerTablet,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		PhoneCellular,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		PhoneCordless,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		PhoneSmartPhone,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		PhoneWired,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		PhoneIsdn,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		NetworkFullyAvailable,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		NetworkUsed01To17Percent,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		NetworkUsed17To33Percent,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		NetworkUsed33To50Percent,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		NetworkUsed50To67Percent,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		NetworkUsed67To83Percent,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		NetworkUsed83To99Percent,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		NetworkNoServiceAvailable,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		AudioVideoWearableHeadset,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		AudioVideoHandsFree,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		AudioVideoMicrophone,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		AudioVideoLoudspeaker,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		AudioVideoHeadphones,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		AudioVideoPortableAudio,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		AudioVideoCarAudio,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		AudioVideoSetTopBox,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		AudioVideoHifiAudioDevice,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		AudioVideoVcr,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		AudioVideoVideoCamera,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		AudioVideoCamcorder,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		AudioVideoVideoMonitor,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		AudioVideoVideoDisplayAndLoudspeaker,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		AudioVideoVideoConferencing,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		AudioVideoGamingOrToy,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		PeripheralJoystick,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		PeripheralGamepad,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		PeripheralRemoteControl,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		PeripheralSensing,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		PeripheralDigitizerTablet,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		PeripheralCardReader,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		PeripheralDigitalPen,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		PeripheralHandheldScanner,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		PeripheralHandheldGesture,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		WearableWristwatch,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		WearablePager,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		WearableJacket,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		WearableHelmet,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		WearableGlasses,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		ToyRobot,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		ToyVehicle,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		ToyDoll,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		ToyController,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		ToyGame,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		HealthBloodPressureMonitor,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		HealthThermometer,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		HealthWeighingScale,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		HealthGlucoseMeter,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		HealthPulseOximeter,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		HealthHeartRateMonitor,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		HealthHealthDataDisplay,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		HealthStepCounter,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		HealthBodyCompositionAnalyzer,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		HealthPeakFlowMonitor,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		HealthMedicationMonitor,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		HealthKneeProsthesis,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		HealthAnkleProsthesis,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		HealthGenericHealthManager,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		HealthPersonalMobilityDevice,
		#endif
	}
	#endif
}
