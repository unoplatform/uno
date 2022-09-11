#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.Graphics.Printing.Workflow
{
	#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
	[global::Uno.NotImplemented]
	#endif
	public  partial class PrintWorkflowPrinterJob 
	{
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  int JobId
		{
			get
			{
				throw new global::System.NotImplementedException("The member int PrintWorkflowPrinterJob.JobId is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Devices.Printers.IppPrintDevice Printer
		{
			get
			{
				throw new global::System.NotImplementedException("The member IppPrintDevice PrintWorkflowPrinterJob.Printer is not implemented in Uno.");
			}
		}
		#endif
		// Forced skipping of method Windows.Graphics.Printing.Workflow.PrintWorkflowPrinterJob.JobId.get
		// Forced skipping of method Windows.Graphics.Printing.Workflow.PrintWorkflowPrinterJob.Printer.get
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Graphics.Printing.Workflow.PrintWorkflowPrinterJobStatus GetJobStatus()
		{
			throw new global::System.NotImplementedException("The member PrintWorkflowPrinterJobStatus PrintWorkflowPrinterJob.GetJobStatus() is not implemented in Uno.");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Graphics.Printing.PrintTicket.WorkflowPrintTicket GetJobPrintTicket()
		{
			throw new global::System.NotImplementedException("The member WorkflowPrintTicket PrintWorkflowPrinterJob.GetJobPrintTicket() is not implemented in Uno.");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Storage.Streams.IBuffer GetJobAttributesAsBuffer( global::System.Collections.Generic.IEnumerable<string> attributeNames)
		{
			throw new global::System.NotImplementedException("The member IBuffer PrintWorkflowPrinterJob.GetJobAttributesAsBuffer(IEnumerable<string> attributeNames) is not implemented in Uno.");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::System.Collections.Generic.IDictionary<string, global::Windows.Devices.Printers.IppAttributeValue> GetJobAttributes( global::System.Collections.Generic.IEnumerable<string> attributeNames)
		{
			throw new global::System.NotImplementedException("The member IDictionary<string, IppAttributeValue> PrintWorkflowPrinterJob.GetJobAttributes(IEnumerable<string> attributeNames) is not implemented in Uno.");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Devices.Printers.IppSetAttributesResult SetJobAttributesFromBuffer( global::Windows.Storage.Streams.IBuffer jobAttributesBuffer)
		{
			throw new global::System.NotImplementedException("The member IppSetAttributesResult PrintWorkflowPrinterJob.SetJobAttributesFromBuffer(IBuffer jobAttributesBuffer) is not implemented in Uno.");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Devices.Printers.IppSetAttributesResult SetJobAttributes( global::System.Collections.Generic.IEnumerable<global::System.Collections.Generic.KeyValuePair<string, global::Windows.Devices.Printers.IppAttributeValue>> jobAttributes)
		{
			throw new global::System.NotImplementedException("The member IppSetAttributesResult PrintWorkflowPrinterJob.SetJobAttributes(IEnumerable<KeyValuePair<string, IppAttributeValue>> jobAttributes) is not implemented in Uno.");
		}
		#endif
	}
}
