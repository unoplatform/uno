#nullable enable

using System;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Uno.UI.NativeElementHosting;
using Uno.UI.Samples.Controls;

namespace UITests.Shared.Windows_UI_Xaml_Controls.BrowserHtmlElementTests;

[Sample("Native Element Hosting", Name = "BrowserHtmlElement native scroll negotiation", IsManualTest = true, IgnoreInSnapshotTests = true)]
public sealed partial class BrowserHtmlElement_NativeScrollNegotiation : Page
{
#if __SKIA__
	private BrowserHtmlElement? _nativeElement;
#endif

	public BrowserHtmlElement_NativeScrollNegotiation()
	{
		this.InitializeComponent();

#if __SKIA__
		if (OperatingSystem.IsBrowser())
		{
			try
			{
				_nativeElement = BrowserHtmlElement.CreateHtmlElement("div");
				_nativeElement.InputPolicy = EnableNegotiatedInput.IsChecked is true
					? BrowserHtmlElementInputPolicy.Negotiated
					: BrowserHtmlElementInputPolicy.NativeOnly;
				NativeHost.Content = _nativeElement;
				_nativeElement.SetCssStyle(
					("box-sizing", "border-box"),
					("border", "2px solid #0078d4"),
					("padding", "12px"),
					("background", "white"),
					("color", "black"));
				_nativeElement.SetHtmlContent("""
					<label>Native text input <input aria-label="Native text input" value="Tap to focus and edit" /></label>
					<button type="button">Native button</button>
					<p>Drag over this paragraph or the button: this native content cannot scroll, so the drag goes to the Uno Platform ScrollViewer.</p>
					<div id="native-scroll-region" style="height: 120px; overflow-y: auto; border: 1px dashed #666;">
						<div style="height: 480px; padding: 8px;">Drag here to scroll the native region. At its boundary, continue dragging to test scroll chaining to the Uno Platform parent.</div>
					</div>
					""");
				NativeHostStatus.Text = "Native host: mounted.";
			}
			catch (Exception e)
			{
				NativeHostStatus.Text = $"Native host setup failed: {e.GetType().Name}: {e.Message}";
			}
		}
#endif
	}

	private void OnInnerScrollToggled(object sender, RoutedEventArgs e)
	{
#if __SKIA__
		_nativeElement?.ExecuteJavascript($"element.querySelector('#native-scroll-region').style.overflowY = '{(EnableInnerScroll.IsChecked is true ? "auto" : "hidden")}';");
#endif
	}

	private void OnNegotiatedInputToggled(object sender, RoutedEventArgs e)
	{
#if __SKIA__
		if (_nativeElement is { } nativeElement)
		{
			nativeElement.InputPolicy = EnableNegotiatedInput.IsChecked is true
				? BrowserHtmlElementInputPolicy.Negotiated
				: BrowserHtmlElementInputPolicy.NativeOnly;
		}
#endif
	}
}
