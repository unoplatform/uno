#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.Web.Http
{
	#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __MACOS__
	[global::Uno.NotImplemented]
	#endif
	public  partial class HttpClient : global::System.IDisposable,global::Windows.Foundation.IStringable
	{
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __MACOS__
		[global::Uno.NotImplemented]
		public  global::Windows.Web.Http.Headers.HttpRequestHeaderCollection DefaultRequestHeaders
		{
			get
			{
				throw new global::System.NotImplementedException("The member HttpRequestHeaderCollection HttpClient.DefaultRequestHeaders is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __MACOS__
		[global::Uno.NotImplemented]
		public HttpClient( global::Windows.Web.Http.Filters.IHttpFilter filter) 
		{
			global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Web.Http.HttpClient", "HttpClient.HttpClient(IHttpFilter filter)");
		}
		#endif
		// Forced skipping of method Windows.Web.Http.HttpClient.HttpClient(Windows.Web.Http.Filters.IHttpFilter)
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __MACOS__
		[global::Uno.NotImplemented]
		public HttpClient() 
		{
			global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Web.Http.HttpClient", "HttpClient.HttpClient()");
		}
		#endif
		// Forced skipping of method Windows.Web.Http.HttpClient.HttpClient()
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __MACOS__
		[global::Uno.NotImplemented]
		public  global::Windows.Foundation.IAsyncOperationWithProgress<global::Windows.Web.Http.HttpResponseMessage, global::Windows.Web.Http.HttpProgress> DeleteAsync( global::System.Uri uri)
		{
			throw new global::System.NotImplementedException("The member IAsyncOperationWithProgress<HttpResponseMessage, HttpProgress> HttpClient.DeleteAsync(Uri uri) is not implemented in Uno.");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __MACOS__
		[global::Uno.NotImplemented]
		public  global::Windows.Foundation.IAsyncOperationWithProgress<global::Windows.Web.Http.HttpResponseMessage, global::Windows.Web.Http.HttpProgress> GetAsync( global::System.Uri uri)
		{
			throw new global::System.NotImplementedException("The member IAsyncOperationWithProgress<HttpResponseMessage, HttpProgress> HttpClient.GetAsync(Uri uri) is not implemented in Uno.");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __MACOS__
		[global::Uno.NotImplemented]
		public  global::Windows.Foundation.IAsyncOperationWithProgress<global::Windows.Web.Http.HttpResponseMessage, global::Windows.Web.Http.HttpProgress> GetAsync( global::System.Uri uri,  global::Windows.Web.Http.HttpCompletionOption completionOption)
		{
			throw new global::System.NotImplementedException("The member IAsyncOperationWithProgress<HttpResponseMessage, HttpProgress> HttpClient.GetAsync(Uri uri, HttpCompletionOption completionOption) is not implemented in Uno.");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __MACOS__
		[global::Uno.NotImplemented]
		public  global::Windows.Foundation.IAsyncOperationWithProgress<global::Windows.Storage.Streams.IBuffer, global::Windows.Web.Http.HttpProgress> GetBufferAsync( global::System.Uri uri)
		{
			throw new global::System.NotImplementedException("The member IAsyncOperationWithProgress<IBuffer, HttpProgress> HttpClient.GetBufferAsync(Uri uri) is not implemented in Uno.");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __MACOS__
		[global::Uno.NotImplemented]
		public  global::Windows.Foundation.IAsyncOperationWithProgress<global::Windows.Storage.Streams.IInputStream, global::Windows.Web.Http.HttpProgress> GetInputStreamAsync( global::System.Uri uri)
		{
			throw new global::System.NotImplementedException("The member IAsyncOperationWithProgress<IInputStream, HttpProgress> HttpClient.GetInputStreamAsync(Uri uri) is not implemented in Uno.");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __MACOS__
		[global::Uno.NotImplemented]
		public  global::Windows.Foundation.IAsyncOperationWithProgress<string, global::Windows.Web.Http.HttpProgress> GetStringAsync( global::System.Uri uri)
		{
			throw new global::System.NotImplementedException("The member IAsyncOperationWithProgress<string, HttpProgress> HttpClient.GetStringAsync(Uri uri) is not implemented in Uno.");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __MACOS__
		[global::Uno.NotImplemented]
		public  global::Windows.Foundation.IAsyncOperationWithProgress<global::Windows.Web.Http.HttpResponseMessage, global::Windows.Web.Http.HttpProgress> PostAsync( global::System.Uri uri,  global::Windows.Web.Http.IHttpContent content)
		{
			throw new global::System.NotImplementedException("The member IAsyncOperationWithProgress<HttpResponseMessage, HttpProgress> HttpClient.PostAsync(Uri uri, IHttpContent content) is not implemented in Uno.");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __MACOS__
		[global::Uno.NotImplemented]
		public  global::Windows.Foundation.IAsyncOperationWithProgress<global::Windows.Web.Http.HttpResponseMessage, global::Windows.Web.Http.HttpProgress> PutAsync( global::System.Uri uri,  global::Windows.Web.Http.IHttpContent content)
		{
			throw new global::System.NotImplementedException("The member IAsyncOperationWithProgress<HttpResponseMessage, HttpProgress> HttpClient.PutAsync(Uri uri, IHttpContent content) is not implemented in Uno.");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __MACOS__
		[global::Uno.NotImplemented]
		public  global::Windows.Foundation.IAsyncOperationWithProgress<global::Windows.Web.Http.HttpResponseMessage, global::Windows.Web.Http.HttpProgress> SendRequestAsync( global::Windows.Web.Http.HttpRequestMessage request)
		{
			throw new global::System.NotImplementedException("The member IAsyncOperationWithProgress<HttpResponseMessage, HttpProgress> HttpClient.SendRequestAsync(HttpRequestMessage request) is not implemented in Uno.");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __MACOS__
		[global::Uno.NotImplemented]
		public  global::Windows.Foundation.IAsyncOperationWithProgress<global::Windows.Web.Http.HttpResponseMessage, global::Windows.Web.Http.HttpProgress> SendRequestAsync( global::Windows.Web.Http.HttpRequestMessage request,  global::Windows.Web.Http.HttpCompletionOption completionOption)
		{
			throw new global::System.NotImplementedException("The member IAsyncOperationWithProgress<HttpResponseMessage, HttpProgress> HttpClient.SendRequestAsync(HttpRequestMessage request, HttpCompletionOption completionOption) is not implemented in Uno.");
		}
		#endif
		// Forced skipping of method Windows.Web.Http.HttpClient.DefaultRequestHeaders.get
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __MACOS__
		[global::Uno.NotImplemented]
		public  void Dispose()
		{
			global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Web.Http.HttpClient", "void HttpClient.Dispose()");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __MACOS__
		[global::Uno.NotImplemented]
		public override string ToString()
		{
			throw new global::System.NotImplementedException("The member string HttpClient.ToString() is not implemented in Uno.");
		}
		#endif
		// Processing: System.IDisposable
	}
}
