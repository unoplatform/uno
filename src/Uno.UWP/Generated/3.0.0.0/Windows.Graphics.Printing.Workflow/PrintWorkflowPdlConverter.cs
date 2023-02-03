#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.Graphics.Printing.Workflow
{
	#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
	[global::Uno.NotImplemented]
	#endif
	public  partial class PrintWorkflowPdlConverter 
	{
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Foundation.IAsyncAction ConvertPdlAsync( global::Windows.Graphics.Printing.PrintTicket.WorkflowPrintTicket printTicket,  global::Windows.Storage.Streams.IInputStream inputStream,  global::Windows.Storage.Streams.IOutputStream outputStream)
		{
			throw new global::System.NotImplementedException("The member IAsyncAction PrintWorkflowPdlConverter.ConvertPdlAsync(WorkflowPrintTicket printTicket, IInputStream inputStream, IOutputStream outputStream) is not implemented. For more information, visit https://aka.platform.uno/notimplemented?m=IAsyncAction%20PrintWorkflowPdlConverter.ConvertPdlAsync%28WorkflowPrintTicket%20printTicket%2C%20IInputStream%20inputStream%2C%20IOutputStream%20outputStream%29");
		}
		#endif
	}
}
