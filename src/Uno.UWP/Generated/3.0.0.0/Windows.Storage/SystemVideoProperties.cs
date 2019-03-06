#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.Storage
{
	#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __MACOS__
	[global::Uno.NotImplemented]
	#endif
	public  partial class SystemVideoProperties 
	{
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __MACOS__
		[global::Uno.NotImplemented]
		public  string Director
		{
			get
			{
				throw new global::System.NotImplementedException("The member string SystemVideoProperties.Director is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __MACOS__
		[global::Uno.NotImplemented]
		public  string FrameHeight
		{
			get
			{
				throw new global::System.NotImplementedException("The member string SystemVideoProperties.FrameHeight is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __MACOS__
		[global::Uno.NotImplemented]
		public  string FrameWidth
		{
			get
			{
				throw new global::System.NotImplementedException("The member string SystemVideoProperties.FrameWidth is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __MACOS__
		[global::Uno.NotImplemented]
		public  string Orientation
		{
			get
			{
				throw new global::System.NotImplementedException("The member string SystemVideoProperties.Orientation is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __MACOS__
		[global::Uno.NotImplemented]
		public  string TotalBitrate
		{
			get
			{
				throw new global::System.NotImplementedException("The member string SystemVideoProperties.TotalBitrate is not implemented in Uno.");
			}
		}
		#endif
		// Forced skipping of method Windows.Storage.SystemVideoProperties.Director.get
		// Forced skipping of method Windows.Storage.SystemVideoProperties.FrameHeight.get
		// Forced skipping of method Windows.Storage.SystemVideoProperties.FrameWidth.get
		// Forced skipping of method Windows.Storage.SystemVideoProperties.Orientation.get
		// Forced skipping of method Windows.Storage.SystemVideoProperties.TotalBitrate.get
	}
}
