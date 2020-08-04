#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.ApplicationModel.DataTransfer
{
	#if false || false || NET461 || false || false || false || false
	[global::Uno.NotImplemented]
	#endif
	public  partial class DataPackage 
	{
		#if false || false || NET461 || false || false || false || false
		[global::Uno.NotImplemented("NET461")]
		public  global::Windows.ApplicationModel.DataTransfer.DataPackageOperation RequestedOperation
		{
			get
			{
				throw new global::System.NotImplementedException("The member DataPackageOperation DataPackage.RequestedOperation is not implemented in Uno.");
			}
			set
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.ApplicationModel.DataTransfer.DataPackage", "DataPackageOperation DataPackage.RequestedOperation");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.ApplicationModel.DataTransfer.DataPackagePropertySet Properties
		{
			get
			{
				throw new global::System.NotImplementedException("The member DataPackagePropertySet DataPackage.Properties is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::System.Collections.Generic.IDictionary<string, global::Windows.Storage.Streams.RandomAccessStreamReference> ResourceMap
		{
			get
			{
				throw new global::System.NotImplementedException("The member IDictionary<string, RandomAccessStreamReference> DataPackage.ResourceMap is not implemented in Uno.");
			}
		}
		#endif
		#if false || false || NET461 || false || false || false || false
		[global::Uno.NotImplemented("NET461")]
		public DataPackage() 
		{
			global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.ApplicationModel.DataTransfer.DataPackage", "DataPackage.DataPackage()");
		}
		#endif
		// Forced skipping of method Windows.ApplicationModel.DataTransfer.DataPackage.DataPackage()
		#if false || false || NET461 || false || false || false || false
		[global::Uno.NotImplemented("NET461")]
		public  global::Windows.ApplicationModel.DataTransfer.DataPackageView GetView()
		{
			throw new global::System.NotImplementedException("The member DataPackageView DataPackage.GetView() is not implemented in Uno.");
		}
		#endif
		// Forced skipping of method Windows.ApplicationModel.DataTransfer.DataPackage.Properties.get
		// Forced skipping of method Windows.ApplicationModel.DataTransfer.DataPackage.RequestedOperation.get
		// Forced skipping of method Windows.ApplicationModel.DataTransfer.DataPackage.RequestedOperation.set
		// Forced skipping of method Windows.ApplicationModel.DataTransfer.DataPackage.OperationCompleted.add
		// Forced skipping of method Windows.ApplicationModel.DataTransfer.DataPackage.OperationCompleted.remove
		// Forced skipping of method Windows.ApplicationModel.DataTransfer.DataPackage.Destroyed.add
		// Forced skipping of method Windows.ApplicationModel.DataTransfer.DataPackage.Destroyed.remove
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  void SetData( string formatId,  object value)
		{
			global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.ApplicationModel.DataTransfer.DataPackage", "void DataPackage.SetData(string formatId, object value)");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  void SetDataProvider( string formatId,  global::Windows.ApplicationModel.DataTransfer.DataProviderHandler delayRenderer)
		{
			global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.ApplicationModel.DataTransfer.DataPackage", "void DataPackage.SetDataProvider(string formatId, DataProviderHandler delayRenderer)");
		}
		#endif
		#if false || false || NET461 || false || false || false || false
		[global::Uno.NotImplemented("NET461")]
		public  void SetText( string value)
		{
			global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.ApplicationModel.DataTransfer.DataPackage", "void DataPackage.SetText(string value)");
		}
		#endif
		#if false || false || NET461 || false || false || false || false
		[global::Uno.NotImplemented("NET461")]
		public  void SetUri( global::System.Uri value)
		{
			global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.ApplicationModel.DataTransfer.DataPackage", "void DataPackage.SetUri(Uri value)");
		}
		#endif
		#if false || false || NET461 || false || false || false || false
		[global::Uno.NotImplemented("NET461")]
		public  void SetHtmlFormat( string value)
		{
			global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.ApplicationModel.DataTransfer.DataPackage", "void DataPackage.SetHtmlFormat(string value)");
		}
		#endif
		// Forced skipping of method Windows.ApplicationModel.DataTransfer.DataPackage.ResourceMap.get
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  void SetRtf( string value)
		{
			global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.ApplicationModel.DataTransfer.DataPackage", "void DataPackage.SetRtf(string value)");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  void SetBitmap( global::Windows.Storage.Streams.RandomAccessStreamReference value)
		{
			global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.ApplicationModel.DataTransfer.DataPackage", "void DataPackage.SetBitmap(RandomAccessStreamReference value)");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  void SetStorageItems( global::System.Collections.Generic.IEnumerable<global::Windows.Storage.IStorageItem> value)
		{
			global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.ApplicationModel.DataTransfer.DataPackage", "void DataPackage.SetStorageItems(IEnumerable<IStorageItem> value)");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  void SetStorageItems( global::System.Collections.Generic.IEnumerable<global::Windows.Storage.IStorageItem> value,  bool readOnly)
		{
			global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.ApplicationModel.DataTransfer.DataPackage", "void DataPackage.SetStorageItems(IEnumerable<IStorageItem> value, bool readOnly)");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  void SetApplicationLink( global::System.Uri value)
		{
			global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.ApplicationModel.DataTransfer.DataPackage", "void DataPackage.SetApplicationLink(Uri value)");
		}
		#endif
		#if false || false || NET461 || false || false || false || false
		[global::Uno.NotImplemented("NET461")]
		public  void SetWebLink( global::System.Uri value)
		{
			global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.ApplicationModel.DataTransfer.DataPackage", "void DataPackage.SetWebLink(Uri value)");
		}
		#endif
		// Forced skipping of method Windows.ApplicationModel.DataTransfer.DataPackage.ShareCompleted.add
		// Forced skipping of method Windows.ApplicationModel.DataTransfer.DataPackage.ShareCompleted.remove
		// Forced skipping of method Windows.ApplicationModel.DataTransfer.DataPackage.ShareCanceled.add
		// Forced skipping of method Windows.ApplicationModel.DataTransfer.DataPackage.ShareCanceled.remove
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  event global::Windows.Foundation.TypedEventHandler<global::Windows.ApplicationModel.DataTransfer.DataPackage, object> Destroyed
		{
			[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
			add
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.ApplicationModel.DataTransfer.DataPackage", "event TypedEventHandler<DataPackage, object> DataPackage.Destroyed");
			}
			[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
			remove
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.ApplicationModel.DataTransfer.DataPackage", "event TypedEventHandler<DataPackage, object> DataPackage.Destroyed");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  event global::Windows.Foundation.TypedEventHandler<global::Windows.ApplicationModel.DataTransfer.DataPackage, global::Windows.ApplicationModel.DataTransfer.OperationCompletedEventArgs> OperationCompleted
		{
			[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
			add
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.ApplicationModel.DataTransfer.DataPackage", "event TypedEventHandler<DataPackage, OperationCompletedEventArgs> DataPackage.OperationCompleted");
			}
			[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
			remove
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.ApplicationModel.DataTransfer.DataPackage", "event TypedEventHandler<DataPackage, OperationCompletedEventArgs> DataPackage.OperationCompleted");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  event global::Windows.Foundation.TypedEventHandler<global::Windows.ApplicationModel.DataTransfer.DataPackage, global::Windows.ApplicationModel.DataTransfer.ShareCompletedEventArgs> ShareCompleted
		{
			[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
			add
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.ApplicationModel.DataTransfer.DataPackage", "event TypedEventHandler<DataPackage, ShareCompletedEventArgs> DataPackage.ShareCompleted");
			}
			[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
			remove
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.ApplicationModel.DataTransfer.DataPackage", "event TypedEventHandler<DataPackage, ShareCompletedEventArgs> DataPackage.ShareCompleted");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  event global::Windows.Foundation.TypedEventHandler<global::Windows.ApplicationModel.DataTransfer.DataPackage, object> ShareCanceled
		{
			[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
			add
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.ApplicationModel.DataTransfer.DataPackage", "event TypedEventHandler<DataPackage, object> DataPackage.ShareCanceled");
			}
			[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
			remove
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.ApplicationModel.DataTransfer.DataPackage", "event TypedEventHandler<DataPackage, object> DataPackage.ShareCanceled");
			}
		}
		#endif
	}
}
