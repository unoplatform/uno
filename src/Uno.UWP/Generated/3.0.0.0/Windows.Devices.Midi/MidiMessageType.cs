#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.Devices.Midi
{
	#if __ANDROID__ || __IOS__ || NET46 || __WASM__
	#if __ANDROID__ || __IOS__ || NET46 || __WASM__
	[global::Uno.NotImplemented]
	#endif
	public   enum MidiMessageType 
	{
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		None,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		NoteOff,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		NoteOn,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		PolyphonicKeyPressure,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		ControlChange,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		ProgramChange,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		ChannelPressure,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		PitchBendChange,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		SystemExclusive,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		MidiTimeCode,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		SongPositionPointer,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		SongSelect,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		TuneRequest,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		EndSystemExclusive,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		TimingClock,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		Start,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		Continue,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		Stop,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		ActiveSensing,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		SystemReset,
		#endif
	}
	#endif
}
