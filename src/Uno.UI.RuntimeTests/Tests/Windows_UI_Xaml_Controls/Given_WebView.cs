using System;
using Windows.UI.Xaml.Controls;

namespace Uno.UI.RuntimeTests.Tests.Windows_UI_Xaml_Controls
{
	[TestClass]
	public class Given_WebView
	{
#if __ANDROID__ || __IOS__ || __MACOS__
		[TestMethod]
		public void When_Navigate()
		{
			var webView = new WebView();
			var uri = new Uri("https://bing.com");
			webView.Navigate(uri);
			Assert.IsNotNull(webView.Source);
			Assert.AreEqual("https://bing.com/", webView.Source.OriginalString);
			Assert.AreEqual("https://bing.com", uri.OriginalString);
		}

		[TestMethod]
		public void When_NavigateWithHttpRequestMessage()
		{
			var webView = new WebView();
			var uri = new Uri("https://bing.com");
			webView.NavigateWithHttpRequestMessage(new System.Net.Http.HttpRequestMessage(System.Net.Http.HttpMethod.Get, uri));
			Assert.IsNotNull(webView.Source);
			Assert.AreEqual("https://bing.com/", webView.Source.OriginalString);
			Assert.AreEqual("https://bing.com", uri.OriginalString);
		}

		[TestMethod]
		public void When_NavigateToString()
		{
			var webView = new WebView();
			var uri = new Uri("https://bing.com/");
			webView.Source = uri;

			Assert.AreEqual("https://bing.com/", webView.Source.OriginalString);
			Assert.AreEqual("https://bing.com", uri.OriginalString);
			Assert.AreSame(uri, webView.Source);

			webView.NavigateToString("<html></html>");
			Assert.IsNull(webView.Source);
		}
#endif
	}
}
