#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.UI.Composition
{
	#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
	[global::Uno.NotImplemented]
	#endif
	public  partial class Vector3NaturalMotionAnimation : global::Windows.UI.Composition.NaturalMotionAnimation
	{
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::System.Numerics.Vector3 InitialVelocity
		{
			get
			{
				throw new global::System.NotImplementedException("The member Vector3 Vector3NaturalMotionAnimation.InitialVelocity is not implemented in Uno.");
			}
			set
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.UI.Composition.Vector3NaturalMotionAnimation", "Vector3 Vector3NaturalMotionAnimation.InitialVelocity");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::System.Numerics.Vector3? InitialValue
		{
			get
			{
				throw new global::System.NotImplementedException("The member Vector3? Vector3NaturalMotionAnimation.InitialValue is not implemented in Uno.");
			}
			set
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.UI.Composition.Vector3NaturalMotionAnimation", "Vector3? Vector3NaturalMotionAnimation.InitialValue");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::System.Numerics.Vector3? FinalValue
		{
			get
			{
				throw new global::System.NotImplementedException("The member Vector3? Vector3NaturalMotionAnimation.FinalValue is not implemented in Uno.");
			}
			set
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.UI.Composition.Vector3NaturalMotionAnimation", "Vector3? Vector3NaturalMotionAnimation.FinalValue");
			}
		}
		#endif
		// Forced skipping of method Windows.UI.Composition.Vector3NaturalMotionAnimation.FinalValue.get
		// Forced skipping of method Windows.UI.Composition.Vector3NaturalMotionAnimation.FinalValue.set
		// Forced skipping of method Windows.UI.Composition.Vector3NaturalMotionAnimation.InitialValue.get
		// Forced skipping of method Windows.UI.Composition.Vector3NaturalMotionAnimation.InitialValue.set
		// Forced skipping of method Windows.UI.Composition.Vector3NaturalMotionAnimation.InitialVelocity.get
		// Forced skipping of method Windows.UI.Composition.Vector3NaturalMotionAnimation.InitialVelocity.set
	}
}
