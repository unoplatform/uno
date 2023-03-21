//#if __WASM__ || __MACOS__
//#pragma warning disable CS0067, CS0414
//#endif

//#if XAMARIN || __WASM__ || __SKIA__
//using Windows.Foundation;
//using Windows.UI.Xaml.Controls;
//using System;
//using System.Collections.Generic;
//using System.Diagnostics.CodeAnalysis;
//using System.Linq;
//using System.Net.Http;
//using System.Text;
//using System.Threading;
//using System.Threading.Tasks;
//using Uno.Extensions;
//using Uno;

//namespace Windows.UI.Xaml.Controls
//{
//#if __WASM__ || __SKIA__
//	[NotImplemented]
//#endif
//	internal partial class WebView : Control
//	{
//		private const string BlankUrl = "about:blank";
//		private static readonly Uri BlankUri = new Uri(BlankUrl);

//		private object _internalSource;
//		private string _invokeScriptResponse = string.Empty;

//#pragma warning disable CS0414 // not used in skia
//		private bool _isLoaded;
//#pragma warning restore CS0414

//		internal void GoBack()
//		{
//			GoBackPartial();
//		}

//		internal void GoForward()
//		{
//			GoForwardPartial();
//		}

//		internal void Navigate(Uri uri)
//		{
//			_owner.SetInternalSource(uri ?? BlankUri);
//		}

//		//
//		// Summary:
//		//     Loads the specified HTML content as a new document.
//		//
//		// Parameters:
//		//   text:
//		//     The HTML content to display in the WebView control.
//		internal void NavigateToString(string text)
//		{
//			_owner.SetInternalSource(text ?? "");
//		}

//		internal void NavigateWithHttpRequestMessage(HttpRequestMessage requestMessage)
//		{
//			if (requestMessage?.RequestUri == null)
//			{
//				throw new ArgumentException("Invalid request message. It does not have a RequestUri.");
//			}

//			SetInternalSource(requestMessage);
//		}

//		internal void Stop()
//		{
//			StopPartial();
//		}

//		private protected override void OnLoaded()
//		{
//			base.OnLoaded();

//			_isLoaded = true;
//		}

//		private void SetInternalSource(object source)
//		{
//			_internalSource = source;

//			_owner.UpdateFromInternalSource();
//		}



//		private static string ConcatenateJavascriptArguments(string[] arguments)
//		{
//			var argument = string.Empty;
//			if (arguments != null && arguments.Any())
//			{
//				argument = string.Join(",", arguments);
//			}

//			return argument;
//		}

//		internal void OnUnsupportedUriSchemeIdentified(WebViewUnsupportedUriSchemeIdentifiedEventArgs args)
//		{
//			UnsupportedUriSchemeIdentified?.Invoke(_owner, args);
//		}

//		internal bool GetIsHistoryEntryValid(string url) => !url.IsNullOrWhiteSpace() && !url.Equals(BlankUrl, StringComparison.OrdinalIgnoreCase);
//	}
//}
//#endif
