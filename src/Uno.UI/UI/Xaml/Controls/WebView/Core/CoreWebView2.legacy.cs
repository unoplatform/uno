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

//		private object _internalSource;
//		private string _invokeScriptResponse = string.Empty;

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
//		//     The HTML content to display in t he WebView control.
//		internal void NavigateToString(string text)
//		{
//			_owner.SetInternalSource(text ?? "");
//		}



//		private void SetInternalSource(object source)
//		{
//			_internalSource = source;

//			_owner.UpdateFromInternalSource();
//		}





//		internal void OnUnsupportedUriSchemeIdentified(WebViewUnsupportedUriSchemeIdentifiedEventArgs args)
//		{
//			UnsupportedUriSchemeIdentified?.Invoke(_owner, args);
//		}

//	}
//}
//#endif
