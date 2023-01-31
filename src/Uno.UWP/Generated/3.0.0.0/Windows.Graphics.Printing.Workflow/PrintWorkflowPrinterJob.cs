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
				throw new global::System.NotImplementedException("The member int PrintWorkflowPrinterJob.JobId is not implemented. For more information, visit https://aka.platform.uno/notimplemented?m=int%20PrintWorkflowPrinterJob.JobId");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Devices.Printers.IppPrintDevice Printer
		{
			get
			{
				throw new global::System.NotImplementedException("The member IppPrintDevice PrintWorkflowPrinterJob.Printer is not implemented. For more information, visit https://aka.platform.uno/notimplemented?m=IppPrintDevice%20PrintWorkflowPrinterJob.Printer");
			}
		}
		#endif
		// Forced skipping of method Windows.Graphics.Printing.Workflow.PrintWorkflowPrinterJob.JobId.get
		// Forced skipping of method Windows.Graphics.Printing.Workflow.PrintWorkflowPrinterJob.Printer.get
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Graphics.Printing.Workflow.PrintWorkflowPrinterJobStatus GetJobStatus()
		{
			throw new global::System.NotImplementedException("The member PrintWorkflowPrinterJobStatus PrintWorkflowPrinterJob.GetJobStatus() is not implemented. For more information, visit https://aka.platform.uno/notimplemented?m=PrintWorkflowPrinterJobStatus%20PrintWorkflowPrinterJob.GetJobStatus%28%29");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Graphics.Printing.PrintTicket.WorkflowPrintTicket GetJobPrintTicket()
		{
			throw new global::System.NotImplementedException("The member WorkflowPrintTicket PrintWorkflowPrinterJob.GetJobPrintTicket() is not implemented. For more information, visit https://aka.platform.uno/notimplemented?m=WorkflowPrintTicket%20PrintWorkflowPrinterJob.GetJobPrintTicket%28%29");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Storage.Streams.IBuffer GetJobAttributesAsBuffer( global::System.Collections.Generic.IEnumerable<string> attributeNames)
		{
			throw new global::System.NotImplementedException("The member IBuffer PrintWorkflowPrinterJob.GetJobAttributesAsBuffer(IEnumerable<string> attributeNames) is not implemented. For more information, visit https://aka.platform.uno/notimplemented?m=IBuffer%20PrintWorkflowPrinterJob.GetJobAttributesAsBuffer%28IEnumerable%3Cstring%3E%20attributeNames%29");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::System.Collections.Generic.IDictionary<string, global::Windows.Devices.Printers.IppAttributeValue> GetJobAttributes( global::System.Collections.Generic.IEnumerable<string> attributeNames)
		{
			throw new global::System.NotImplementedException("The member IDictionary<string, IppAttributeValue> PrintWorkflowPrinterJob.GetJobAttributes(IEnumerable<string> attributeNames) is not implemented. For more information, visit https://aka.platform.uno/notimplemented?m=IDictionary%3Cstring%2C%20IppAttributeValue%3E%20PrintWorkflowPrinterJob.GetJobAttributes%28IEnumerable%3Cstring%3E%20attributeNames%29");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Devices.Printers.IppSetAttributesResult SetJobAttributesFromBuffer( global::Windows.Storage.Streams.IBuffer jobAttributesBuffer)
		{
			throw new global::System.NotImplementedException("The member IppSetAttributesResult PrintWorkflowPrinterJob.SetJobAttributesFromBuffer(IBuffer jobAttributesBuffer) is not implemented. For more information, visit https://aka.platform.uno/notimplemented?m=IppSetAttributesResult%20PrintWorkflowPrinterJob.SetJobAttributesFromBuffer%28IBuffer%20jobAttributesBuffer%29");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Devices.Printers.IppSetAttributesResult SetJobAttributes( global::System.Collections.Generic.IEnumerable<global::System.Collections.Generic.KeyValuePair<string, global::Windows.Devices.Printers.IppAttributeValue>> jobAttributes)
		{
			throw new global::System.NotImplementedException("The member IppSetAttributesResult PrintWorkflowPrinterJob.SetJobAttributes(IEnumerable<KeyValuePair<string, IppAttributeValue>> jobAttributes) is not implemented. For more information, visit https://aka.platform.uno/notimplemented?m=IppSetAttributesResult%20PrintWorkflowPrinterJob.SetJobAttributes%28IEnumerable%3CKeyValuePair%3Cstring%2C%20IppAttributeValue%3E%3E%20jobAttributes%29");
		}
		#endif
	}
}
