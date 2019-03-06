#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.UI.Xaml.Media.Animation
{
	#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __MACOS__
	[global::Uno.NotImplemented]
	#endif
	public  partial class ConditionallyIndependentlyAnimatableAttribute : global::System.Attribute
	{
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __MACOS__
		[global::Uno.NotImplemented]
		public ConditionallyIndependentlyAnimatableAttribute() : base()
		{
			global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.UI.Xaml.Media.Animation.ConditionallyIndependentlyAnimatableAttribute", "ConditionallyIndependentlyAnimatableAttribute.ConditionallyIndependentlyAnimatableAttribute()");
		}
		#endif
		// Forced skipping of method Windows.UI.Xaml.Media.Animation.ConditionallyIndependentlyAnimatableAttribute.ConditionallyIndependentlyAnimatableAttribute()
	}
}
