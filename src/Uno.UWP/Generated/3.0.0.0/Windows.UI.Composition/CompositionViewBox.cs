#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.UI.Composition
{
	#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __MACOS__
	[global::Uno.NotImplemented]
	#endif
	public  partial class CompositionViewBox : global::Windows.UI.Composition.CompositionObject
	{
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __MACOS__
		[global::Uno.NotImplemented]
		public  float VerticalAlignmentRatio
		{
			get
			{
				throw new global::System.NotImplementedException("The member float CompositionViewBox.VerticalAlignmentRatio is not implemented in Uno.");
			}
			set
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.UI.Composition.CompositionViewBox", "float CompositionViewBox.VerticalAlignmentRatio");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __MACOS__
		[global::Uno.NotImplemented]
		public  global::Windows.UI.Composition.CompositionStretch Stretch
		{
			get
			{
				throw new global::System.NotImplementedException("The member CompositionStretch CompositionViewBox.Stretch is not implemented in Uno.");
			}
			set
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.UI.Composition.CompositionViewBox", "CompositionStretch CompositionViewBox.Stretch");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __MACOS__
		[global::Uno.NotImplemented]
		public  global::System.Numerics.Vector2 Size
		{
			get
			{
				throw new global::System.NotImplementedException("The member Vector2 CompositionViewBox.Size is not implemented in Uno.");
			}
			set
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.UI.Composition.CompositionViewBox", "Vector2 CompositionViewBox.Size");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __MACOS__
		[global::Uno.NotImplemented]
		public  global::System.Numerics.Vector2 Offset
		{
			get
			{
				throw new global::System.NotImplementedException("The member Vector2 CompositionViewBox.Offset is not implemented in Uno.");
			}
			set
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.UI.Composition.CompositionViewBox", "Vector2 CompositionViewBox.Offset");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __MACOS__
		[global::Uno.NotImplemented]
		public  float HorizontalAlignmentRatio
		{
			get
			{
				throw new global::System.NotImplementedException("The member float CompositionViewBox.HorizontalAlignmentRatio is not implemented in Uno.");
			}
			set
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.UI.Composition.CompositionViewBox", "float CompositionViewBox.HorizontalAlignmentRatio");
			}
		}
		#endif
		// Forced skipping of method Windows.UI.Composition.CompositionViewBox.HorizontalAlignmentRatio.get
		// Forced skipping of method Windows.UI.Composition.CompositionViewBox.HorizontalAlignmentRatio.set
		// Forced skipping of method Windows.UI.Composition.CompositionViewBox.Offset.get
		// Forced skipping of method Windows.UI.Composition.CompositionViewBox.Offset.set
		// Forced skipping of method Windows.UI.Composition.CompositionViewBox.Size.get
		// Forced skipping of method Windows.UI.Composition.CompositionViewBox.Size.set
		// Forced skipping of method Windows.UI.Composition.CompositionViewBox.Stretch.get
		// Forced skipping of method Windows.UI.Composition.CompositionViewBox.Stretch.set
		// Forced skipping of method Windows.UI.Composition.CompositionViewBox.VerticalAlignmentRatio.get
		// Forced skipping of method Windows.UI.Composition.CompositionViewBox.VerticalAlignmentRatio.set
	}
}
