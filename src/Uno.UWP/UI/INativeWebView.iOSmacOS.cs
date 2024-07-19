using Foundation;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Web.WebView2.Core;

namespace Uno.UI.Xaml.Controls;

/// <summary>
/// Wrapper for a version-dependent native iOS WebView
/// </summary>
internal partial interface INativeWebView
{
	void SetOwner(CoreWebView2 xamlWebView);
}
