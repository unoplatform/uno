#if __WASM__ || __MACOS__
#pragma warning disable CS0067, CS0414
#endif

using Windows.Foundation;
using Windows.UI.Xaml.Controls;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Uno.Extensions;
using Uno;

namespace Windows.UI.Xaml.Controls;

#if __WASM__ || __SKIA__
[NotImplemented]
#endif
public partial class WebView : Control
{
	/// <summary>
	/// Initializes a new instance of the WebView class.
	/// </summary>
	public WebView()
	{
		DefaultStyleKey = typeof(WebView);
	}
}
