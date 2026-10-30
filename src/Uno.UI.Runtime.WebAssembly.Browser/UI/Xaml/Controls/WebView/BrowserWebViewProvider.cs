using Microsoft.UI.Xaml.Controls;
using Microsoft.Web.WebView2.Core;
using Uno.UI;
using Uno.UI.NativeElementHosting;
using Uno.UI.Runtime;
using Uno.UI.Xaml.Controls;
using Uno.Web.WebView2.Core;

namespace Microsoft.UI.Xaml.Controls;

internal class BrowserWebViewProvider : INativeWebViewProvider
{
	private readonly CoreWebView2 _owner;

	public BrowserWebViewProvider(CoreWebView2 owner)
	{
		_owner = owner;
	}

	public INativeWebView CreateNativeWebView(ContentPresenter contentPresenter)
	{
		var content = contentPresenter.Content as BrowserHtmlElement;
		if (content is null)
		{
			content = BrowserHtmlElement.CreateHtmlElement("iframe");
			// The policy applies to the documents the frame loads after it is set, so it must precede the first navigation.
			SetPermissionsPolicy(content);
			// As in WinUI, the browser survives unloads (a reparent re-enters the same WebView2) and goes away with the control.
			content.RemoveFromDomWhenCollected();
			contentPresenter.Content = content;
		}

		var nativeWebView = new NativeWebView(_owner, content.ElementId);
		return nativeWebView;
	}

	private static void SetPermissionsPolicy(BrowserHtmlElement frame)
	{
		var (allow, allowFullscreen) = WebViewFramePermissions.GetFrameAttributes(FeatureConfiguration.WebView2.PermissionsPolicy);

		if (allow is not null)
		{
			frame.SetHtmlAttribute("allow", allow);
		}

		if (allowFullscreen)
		{
			frame.SetHtmlAttribute("allowfullscreen", "");
		}
	}
}
