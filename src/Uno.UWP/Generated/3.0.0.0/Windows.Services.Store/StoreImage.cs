#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.Services.Store
{
	#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __MACOS__
	[global::Uno.NotImplemented]
	#endif
	public  partial class StoreImage 
	{
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __MACOS__
		[global::Uno.NotImplemented]
		public  string Caption
		{
			get
			{
				throw new global::System.NotImplementedException("The member string StoreImage.Caption is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __MACOS__
		[global::Uno.NotImplemented]
		public  uint Height
		{
			get
			{
				throw new global::System.NotImplementedException("The member uint StoreImage.Height is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __MACOS__
		[global::Uno.NotImplemented]
		public  string ImagePurposeTag
		{
			get
			{
				throw new global::System.NotImplementedException("The member string StoreImage.ImagePurposeTag is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __MACOS__
		[global::Uno.NotImplemented]
		public  global::System.Uri Uri
		{
			get
			{
				throw new global::System.NotImplementedException("The member Uri StoreImage.Uri is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __MACOS__
		[global::Uno.NotImplemented]
		public  uint Width
		{
			get
			{
				throw new global::System.NotImplementedException("The member uint StoreImage.Width is not implemented in Uno.");
			}
		}
		#endif
		// Forced skipping of method Windows.Services.Store.StoreImage.Uri.get
		// Forced skipping of method Windows.Services.Store.StoreImage.ImagePurposeTag.get
		// Forced skipping of method Windows.Services.Store.StoreImage.Width.get
		// Forced skipping of method Windows.Services.Store.StoreImage.Height.get
		// Forced skipping of method Windows.Services.Store.StoreImage.Caption.get
	}
}
