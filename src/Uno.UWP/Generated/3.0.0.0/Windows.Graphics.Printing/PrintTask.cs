#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.Graphics.Printing
{
	#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
	[global::Uno.NotImplemented]
	#endif
	public  partial class PrintTask 
	{
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Graphics.Printing.PrintTaskOptions Options
		{
			get
			{
				throw new global::System.NotImplementedException("The member PrintTaskOptions PrintTask.Options is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.ApplicationModel.DataTransfer.DataPackagePropertySet Properties
		{
			get
			{
				throw new global::System.NotImplementedException("The member DataPackagePropertySet PrintTask.Properties is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Graphics.Printing.IPrintDocumentSource Source
		{
			get
			{
				throw new global::System.NotImplementedException("The member IPrintDocumentSource PrintTask.Source is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  bool IsPreviewEnabled
		{
			get
			{
				throw new global::System.NotImplementedException("The member bool PrintTask.IsPreviewEnabled is not implemented in Uno.");
			}
			set
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Graphics.Printing.PrintTask", "bool PrintTask.IsPreviewEnabled");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  bool IsPrinterTargetEnabled
		{
			get
			{
				throw new global::System.NotImplementedException("The member bool PrintTask.IsPrinterTargetEnabled is not implemented in Uno.");
			}
			set
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Graphics.Printing.PrintTask", "bool PrintTask.IsPrinterTargetEnabled");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  bool Is3DManufacturingTargetEnabled
		{
			get
			{
				throw new global::System.NotImplementedException("The member bool PrintTask.Is3DManufacturingTargetEnabled is not implemented in Uno.");
			}
			set
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Graphics.Printing.PrintTask", "bool PrintTask.Is3DManufacturingTargetEnabled");
			}
		}
		#endif
		// Forced skipping of method Windows.Graphics.Printing.PrintTask.Properties.get
		// Forced skipping of method Windows.Graphics.Printing.PrintTask.Source.get
		// Forced skipping of method Windows.Graphics.Printing.PrintTask.Options.get
		// Forced skipping of method Windows.Graphics.Printing.PrintTask.Previewing.add
		// Forced skipping of method Windows.Graphics.Printing.PrintTask.Previewing.remove
		// Forced skipping of method Windows.Graphics.Printing.PrintTask.Submitting.add
		// Forced skipping of method Windows.Graphics.Printing.PrintTask.Submitting.remove
		// Forced skipping of method Windows.Graphics.Printing.PrintTask.Progressing.add
		// Forced skipping of method Windows.Graphics.Printing.PrintTask.Progressing.remove
		// Forced skipping of method Windows.Graphics.Printing.PrintTask.Completed.add
		// Forced skipping of method Windows.Graphics.Printing.PrintTask.Completed.remove
		// Forced skipping of method Windows.Graphics.Printing.PrintTask.IsPrinterTargetEnabled.set
		// Forced skipping of method Windows.Graphics.Printing.PrintTask.IsPrinterTargetEnabled.get
		// Forced skipping of method Windows.Graphics.Printing.PrintTask.Is3DManufacturingTargetEnabled.set
		// Forced skipping of method Windows.Graphics.Printing.PrintTask.Is3DManufacturingTargetEnabled.get
		// Forced skipping of method Windows.Graphics.Printing.PrintTask.IsPreviewEnabled.set
		// Forced skipping of method Windows.Graphics.Printing.PrintTask.IsPreviewEnabled.get
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  event global::Windows.Foundation.TypedEventHandler<global::Windows.Graphics.Printing.PrintTask, global::Windows.Graphics.Printing.PrintTaskCompletedEventArgs> Completed
		{
			[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
			add
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Graphics.Printing.PrintTask", "event TypedEventHandler<PrintTask, PrintTaskCompletedEventArgs> PrintTask.Completed");
			}
			[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
			remove
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Graphics.Printing.PrintTask", "event TypedEventHandler<PrintTask, PrintTaskCompletedEventArgs> PrintTask.Completed");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  event global::Windows.Foundation.TypedEventHandler<global::Windows.Graphics.Printing.PrintTask, object> Previewing
		{
			[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
			add
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Graphics.Printing.PrintTask", "event TypedEventHandler<PrintTask, object> PrintTask.Previewing");
			}
			[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
			remove
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Graphics.Printing.PrintTask", "event TypedEventHandler<PrintTask, object> PrintTask.Previewing");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  event global::Windows.Foundation.TypedEventHandler<global::Windows.Graphics.Printing.PrintTask, global::Windows.Graphics.Printing.PrintTaskProgressingEventArgs> Progressing
		{
			[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
			add
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Graphics.Printing.PrintTask", "event TypedEventHandler<PrintTask, PrintTaskProgressingEventArgs> PrintTask.Progressing");
			}
			[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
			remove
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Graphics.Printing.PrintTask", "event TypedEventHandler<PrintTask, PrintTaskProgressingEventArgs> PrintTask.Progressing");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  event global::Windows.Foundation.TypedEventHandler<global::Windows.Graphics.Printing.PrintTask, object> Submitting
		{
			[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
			add
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Graphics.Printing.PrintTask", "event TypedEventHandler<PrintTask, object> PrintTask.Submitting");
			}
			[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
			remove
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Graphics.Printing.PrintTask", "event TypedEventHandler<PrintTask, object> PrintTask.Submitting");
			}
		}
		#endif
	}
}
