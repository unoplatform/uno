#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.Devices.PointOfService
{
	#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
	[global::Uno.NotImplemented]
	#endif
	public  partial class LineDisplayWindow : global::System.IDisposable
	{
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::System.TimeSpan InterCharacterWaitInterval
		{
			get
			{
				throw new global::System.NotImplementedException("The member TimeSpan LineDisplayWindow.InterCharacterWaitInterval is not implemented in Uno.");
			}
			set
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Devices.PointOfService.LineDisplayWindow", "TimeSpan LineDisplayWindow.InterCharacterWaitInterval");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Foundation.Size SizeInCharacters
		{
			get
			{
				throw new global::System.NotImplementedException("The member Size LineDisplayWindow.SizeInCharacters is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Devices.PointOfService.LineDisplayCursor Cursor
		{
			get
			{
				throw new global::System.NotImplementedException("The member LineDisplayCursor LineDisplayWindow.Cursor is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Devices.PointOfService.LineDisplayMarquee Marquee
		{
			get
			{
				throw new global::System.NotImplementedException("The member LineDisplayMarquee LineDisplayWindow.Marquee is not implemented in Uno.");
			}
		}
		#endif
		// Forced skipping of method Windows.Devices.PointOfService.LineDisplayWindow.SizeInCharacters.get
		// Forced skipping of method Windows.Devices.PointOfService.LineDisplayWindow.InterCharacterWaitInterval.get
		// Forced skipping of method Windows.Devices.PointOfService.LineDisplayWindow.InterCharacterWaitInterval.set
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Foundation.IAsyncOperation<bool> TryRefreshAsync()
		{
			throw new global::System.NotImplementedException("The member IAsyncOperation<bool> LineDisplayWindow.TryRefreshAsync() is not implemented in Uno.");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Foundation.IAsyncOperation<bool> TryDisplayTextAsync( string text,  global::Windows.Devices.PointOfService.LineDisplayTextAttribute displayAttribute)
		{
			throw new global::System.NotImplementedException("The member IAsyncOperation<bool> LineDisplayWindow.TryDisplayTextAsync(string text, LineDisplayTextAttribute displayAttribute) is not implemented in Uno.");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Foundation.IAsyncOperation<bool> TryDisplayTextAsync( string text,  global::Windows.Devices.PointOfService.LineDisplayTextAttribute displayAttribute,  global::Windows.Foundation.Point startPosition)
		{
			throw new global::System.NotImplementedException("The member IAsyncOperation<bool> LineDisplayWindow.TryDisplayTextAsync(string text, LineDisplayTextAttribute displayAttribute, Point startPosition) is not implemented in Uno.");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Foundation.IAsyncOperation<bool> TryDisplayTextAsync( string text)
		{
			throw new global::System.NotImplementedException("The member IAsyncOperation<bool> LineDisplayWindow.TryDisplayTextAsync(string text) is not implemented in Uno.");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Foundation.IAsyncOperation<bool> TryScrollTextAsync( global::Windows.Devices.PointOfService.LineDisplayScrollDirection direction,  uint numberOfColumnsOrRows)
		{
			throw new global::System.NotImplementedException("The member IAsyncOperation<bool> LineDisplayWindow.TryScrollTextAsync(LineDisplayScrollDirection direction, uint numberOfColumnsOrRows) is not implemented in Uno.");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Foundation.IAsyncOperation<bool> TryClearTextAsync()
		{
			throw new global::System.NotImplementedException("The member IAsyncOperation<bool> LineDisplayWindow.TryClearTextAsync() is not implemented in Uno.");
		}
		#endif
		// Forced skipping of method Windows.Devices.PointOfService.LineDisplayWindow.Cursor.get
		// Forced skipping of method Windows.Devices.PointOfService.LineDisplayWindow.Marquee.get
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Foundation.IAsyncOperation<uint> ReadCharacterAtCursorAsync()
		{
			throw new global::System.NotImplementedException("The member IAsyncOperation<uint> LineDisplayWindow.ReadCharacterAtCursorAsync() is not implemented in Uno.");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Foundation.IAsyncOperation<bool> TryDisplayStoredBitmapAtCursorAsync( global::Windows.Devices.PointOfService.LineDisplayStoredBitmap bitmap)
		{
			throw new global::System.NotImplementedException("The member IAsyncOperation<bool> LineDisplayWindow.TryDisplayStoredBitmapAtCursorAsync(LineDisplayStoredBitmap bitmap) is not implemented in Uno.");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Foundation.IAsyncOperation<bool> TryDisplayStorageFileBitmapAtCursorAsync( global::Windows.Storage.StorageFile bitmap)
		{
			throw new global::System.NotImplementedException("The member IAsyncOperation<bool> LineDisplayWindow.TryDisplayStorageFileBitmapAtCursorAsync(StorageFile bitmap) is not implemented in Uno.");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Foundation.IAsyncOperation<bool> TryDisplayStorageFileBitmapAtCursorAsync( global::Windows.Storage.StorageFile bitmap,  global::Windows.Devices.PointOfService.LineDisplayHorizontalAlignment horizontalAlignment,  global::Windows.Devices.PointOfService.LineDisplayVerticalAlignment verticalAlignment)
		{
			throw new global::System.NotImplementedException("The member IAsyncOperation<bool> LineDisplayWindow.TryDisplayStorageFileBitmapAtCursorAsync(StorageFile bitmap, LineDisplayHorizontalAlignment horizontalAlignment, LineDisplayVerticalAlignment verticalAlignment) is not implemented in Uno.");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Foundation.IAsyncOperation<bool> TryDisplayStorageFileBitmapAtCursorAsync( global::Windows.Storage.StorageFile bitmap,  global::Windows.Devices.PointOfService.LineDisplayHorizontalAlignment horizontalAlignment,  global::Windows.Devices.PointOfService.LineDisplayVerticalAlignment verticalAlignment,  int widthInPixels)
		{
			throw new global::System.NotImplementedException("The member IAsyncOperation<bool> LineDisplayWindow.TryDisplayStorageFileBitmapAtCursorAsync(StorageFile bitmap, LineDisplayHorizontalAlignment horizontalAlignment, LineDisplayVerticalAlignment verticalAlignment, int widthInPixels) is not implemented in Uno.");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Foundation.IAsyncOperation<bool> TryDisplayStorageFileBitmapAtPointAsync( global::Windows.Storage.StorageFile bitmap,  global::Windows.Foundation.Point offsetInPixels)
		{
			throw new global::System.NotImplementedException("The member IAsyncOperation<bool> LineDisplayWindow.TryDisplayStorageFileBitmapAtPointAsync(StorageFile bitmap, Point offsetInPixels) is not implemented in Uno.");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Foundation.IAsyncOperation<bool> TryDisplayStorageFileBitmapAtPointAsync( global::Windows.Storage.StorageFile bitmap,  global::Windows.Foundation.Point offsetInPixels,  int widthInPixels)
		{
			throw new global::System.NotImplementedException("The member IAsyncOperation<bool> LineDisplayWindow.TryDisplayStorageFileBitmapAtPointAsync(StorageFile bitmap, Point offsetInPixels, int widthInPixels) is not implemented in Uno.");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  void Dispose()
		{
			global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Devices.PointOfService.LineDisplayWindow", "void LineDisplayWindow.Dispose()");
		}
		#endif
		// Processing: System.IDisposable
	}
}
