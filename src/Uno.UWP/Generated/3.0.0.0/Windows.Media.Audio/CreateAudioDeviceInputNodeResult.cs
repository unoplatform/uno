#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.Media.Audio
{
	#if __ANDROID__ || __IOS__ || NET46 || __WASM__ || __MACOS__
	[global::Uno.NotImplemented]
	#endif
	public  partial class CreateAudioDeviceInputNodeResult 
	{
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__ || __MACOS__
		[global::Uno.NotImplemented]
		public  global::Windows.Media.Audio.AudioDeviceInputNode DeviceInputNode
		{
			get
			{
				throw new global::System.NotImplementedException("The member AudioDeviceInputNode CreateAudioDeviceInputNodeResult.DeviceInputNode is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__ || __MACOS__
		[global::Uno.NotImplemented]
		public  global::Windows.Media.Audio.AudioDeviceNodeCreationStatus Status
		{
			get
			{
				throw new global::System.NotImplementedException("The member AudioDeviceNodeCreationStatus CreateAudioDeviceInputNodeResult.Status is not implemented in Uno.");
			}
		}
		#endif
		// Forced skipping of method Windows.Media.Audio.CreateAudioDeviceInputNodeResult.Status.get
		// Forced skipping of method Windows.Media.Audio.CreateAudioDeviceInputNodeResult.DeviceInputNode.get
	}
}
