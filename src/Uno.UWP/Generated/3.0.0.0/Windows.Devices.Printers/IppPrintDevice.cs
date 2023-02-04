#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.Devices.Printers
{
	#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
	[global::Uno.NotImplemented]
	#endif
	public  partial class IppPrintDevice 
	{
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  string PrinterName
		{
			get
			{
				throw new global::System.NotImplementedException("The member string IppPrintDevice.PrinterName is not implemented. For more information, visit https://aka.platform.uno/notimplemented?m=string%20IppPrintDevice.PrinterName");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::System.Uri PrinterUri
		{
			get
			{
				throw new global::System.NotImplementedException("The member Uri IppPrintDevice.PrinterUri is not implemented. For more information, visit https://aka.platform.uno/notimplemented?m=Uri%20IppPrintDevice.PrinterUri");
			}
		}
		#endif
		// Forced skipping of method Windows.Devices.Printers.IppPrintDevice.PrinterName.get
		// Forced skipping of method Windows.Devices.Printers.IppPrintDevice.PrinterUri.get
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Storage.Streams.IBuffer GetPrinterAttributesAsBuffer( global::System.Collections.Generic.IEnumerable<string> attributeNames)
		{
			throw new global::System.NotImplementedException("The member IBuffer IppPrintDevice.GetPrinterAttributesAsBuffer(IEnumerable<string> attributeNames) is not implemented. For more information, visit https://aka.platform.uno/notimplemented?m=IBuffer%20IppPrintDevice.GetPrinterAttributesAsBuffer%28IEnumerable%3Cstring%3E%20attributeNames%29");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::System.Collections.Generic.IDictionary<string, global::Windows.Devices.Printers.IppAttributeValue> GetPrinterAttributes( global::System.Collections.Generic.IEnumerable<string> attributeNames)
		{
			throw new global::System.NotImplementedException("The member IDictionary<string, IppAttributeValue> IppPrintDevice.GetPrinterAttributes(IEnumerable<string> attributeNames) is not implemented. For more information, visit https://aka.platform.uno/notimplemented?m=IDictionary%3Cstring%2C%20IppAttributeValue%3E%20IppPrintDevice.GetPrinterAttributes%28IEnumerable%3Cstring%3E%20attributeNames%29");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Devices.Printers.IppSetAttributesResult SetPrinterAttributesFromBuffer( global::Windows.Storage.Streams.IBuffer printerAttributesBuffer)
		{
			throw new global::System.NotImplementedException("The member IppSetAttributesResult IppPrintDevice.SetPrinterAttributesFromBuffer(IBuffer printerAttributesBuffer) is not implemented. For more information, visit https://aka.platform.uno/notimplemented?m=IppSetAttributesResult%20IppPrintDevice.SetPrinterAttributesFromBuffer%28IBuffer%20printerAttributesBuffer%29");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Devices.Printers.IppSetAttributesResult SetPrinterAttributes( global::System.Collections.Generic.IEnumerable<global::System.Collections.Generic.KeyValuePair<string, global::Windows.Devices.Printers.IppAttributeValue>> printerAttributes)
		{
			throw new global::System.NotImplementedException("The member IppSetAttributesResult IppPrintDevice.SetPrinterAttributes(IEnumerable<KeyValuePair<string, IppAttributeValue>> printerAttributes) is not implemented. For more information, visit https://aka.platform.uno/notimplemented?m=IppSetAttributesResult%20IppPrintDevice.SetPrinterAttributes%28IEnumerable%3CKeyValuePair%3Cstring%2C%20IppAttributeValue%3E%3E%20printerAttributes%29");
		}
		#endif
	}
}
