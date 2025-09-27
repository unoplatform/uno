#if (__SKIA__ || __WASM__) && HAS_UNO
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices.JavaScript;
using System.Text;
using System.Threading.Tasks;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Uno.UI.RemoteControl.Messaging.IdeChannel;
using Windows.UI;
using static Private.Infrastructure.TestServices;
using Uno.UI.NativeElementHosting;

namespace Uno.UI.RuntimeTests.Tests.Windows_UI_Xaml;

[TestClass]
public class Given_BrowserHtmlElement
{
#if __WASM__
	[TestMethod]
	public async Task Given_HasParent()
	{
		var owner = new ContentControl() { Width = 100, Height = 100 };
		var root = new Border()
		{
			Child = owner,
			Width = 100,
			Height = 100,
			Background = new SolidColorBrush(Colors.Red),
			Padding = new Microsoft.UI.Xaml.Thickness(10)
		};
		var SUT = BrowserHtmlElement.CreateHtmlElement("div");
		owner.Content = SUT;

		WindowHelper.WindowContent = root;

		await WindowHelper.WaitForLoaded(owner);

		Assert.AreEqual(owner.TemplatedRoot.GetHtmlId(), SUT.ExecuteJavascript($"return element.parentElement.id"));
	}
#endif

	[TestMethod]
	public async Task Given_SetAttribute()
	{
		if (!OperatingSystem.IsBrowser())
		{
			Assert.Inconclusive("This test is only supported on the browser.");
		}

		var owner = new ContentControl() { Width = 100, Height = 100 };
		var root = new Border()
		{
			Child = owner,
			Width = 100,
			Height = 100,
			Background = new SolidColorBrush(Colors.Red),
			Padding = new Microsoft.UI.Xaml.Thickness(10)
		};
		var SUT = BrowserHtmlElement.CreateHtmlElement("div");
		owner.Content = SUT;

		WindowHelper.WindowContent = root;

		SUT.SetHtmlAttribute("myProperty", "myValue");
		Assert.AreEqual("myValue", SUT.GetHtmlAttribute("myProperty"));

		await WindowHelper.WaitForLoaded(owner);

		Assert.AreEqual("myValue", SUT.ExecuteJavascript($"return element.getAttribute(\"myproperty\")"));

		SUT.ClearHtmlAttribute("myproperty");

		await Task.Delay(100);

		Assert.AreEqual("", SUT.ExecuteJavascript($"return element.getAttribute(\"myproperty\")"));
	}

	[TestMethod]
	public async Task Given_SetHtmlContent()
	{
		if (!OperatingSystem.IsBrowser())
		{
			Assert.Inconclusive("This test is only supported on the browser.");
		}

		var owner = new ContentControl() { Width = 100, Height = 100 };
		var root = new Border()
		{
			Child = owner,
			Width = 100,
			Height = 100,
			Background = new SolidColorBrush(Colors.Red),
			Padding = new Microsoft.UI.Xaml.Thickness(10)
		};
		var SUT = BrowserHtmlElement.CreateHtmlElement("div");
		owner.Content = SUT;

		WindowHelper.WindowContent = root;

		SUT.SetHtmlContent("My Inner Content");

		await WindowHelper.WaitForLoaded(owner);

		Assert.AreEqual("My Inner Content", SUT.ExecuteJavascript($"return element.innerHTML"));
	}

	[TestMethod]
	public async Task Given_SetStyle()
	{
		if (!OperatingSystem.IsBrowser())
		{
			Assert.Inconclusive("This test is only supported on the browser.");
		}

		var owner = new ContentControl() { Width = 100, Height = 100 };
		var root = new Border()
		{
			Child = owner,
			Width = 100,
			Height = 100,
			Background = new SolidColorBrush(Colors.Red),
			Padding = new Microsoft.UI.Xaml.Thickness(10)
		};
		var SUT = BrowserHtmlElement.CreateHtmlElement("div");
		owner.Content = SUT;

		WindowHelper.WindowContent = root;

		SUT.SetCssStyle("pointer-events", "all");

		await WindowHelper.WaitForLoaded(owner);

		Assert.AreEqual("all", SUT.ExecuteJavascript($"return element.style.pointerEvents"));

		SUT.ClearCssStyle("pointer-events");

		Assert.AreEqual("", SUT.ExecuteJavascript($"return element.style.pointerEvents"));
	}

	[TestMethod]
	public async Task Given_SetStyles()
	{
		if (!OperatingSystem.IsBrowser())
		{
			Assert.Inconclusive("This test is only supported on the browser.");
		}

		var owner = new ContentControl() { Width = 100, Height = 100 };
		var root = new Border()
		{
			Child = owner,
			Width = 100,
			Height = 100,
			Background = new SolidColorBrush(Colors.Red),
			Padding = new Microsoft.UI.Xaml.Thickness(10)
		};
		var SUT = BrowserHtmlElement.CreateHtmlElement("div");
		owner.Content = SUT;

		WindowHelper.WindowContent = root;

		SUT.SetCssStyle(
			("pointer-events", "all"),
			("border-style", "none")
		);

		await WindowHelper.WaitForLoaded(owner);

		Assert.AreEqual("all", SUT.ExecuteJavascript($"return element.style.pointerEvents"));
		Assert.AreEqual("none", SUT.ExecuteJavascript($"return element.style.borderStyle"));
	}

	[TestMethod]
	public async Task Given_HtmlEvent()
	{
		if (!OperatingSystem.IsBrowser())
		{
			Assert.Inconclusive("This test is only supported on the browser.");
		}

		var owner = new ContentControl() { Width = 100, Height = 100 };
		var root = new Border()
		{
			Child = owner,
			Width = 100,
			Height = 100,
			Background = new SolidColorBrush(Colors.Red),
			Padding = new Microsoft.UI.Xaml.Thickness(10)
		};

		var SUT = BrowserHtmlElement.CreateHtmlElement("div");
		owner.Content = SUT;

		WindowHelper.WindowContent = root;

		JSObject result = null;

		void MyHandler(object s, JSObject e)
		{
			result = e;
		}

		SUT.RegisterHtmlEventHandler("simpleEvent", MyHandler);

		await WindowHelper.WaitForLoaded(owner);

		SUT.ExecuteJavascript($"element.dispatchEvent(new CustomEvent(\"simpleEvent\", {{ detail: 42 }}));");

		await Task.Delay(100);

		Assert.IsNotNull(result);
		Assert.AreEqual(42, result.GetPropertyAsInt32("detail"));

		result = null;

		// Validate unregistration to the event
		SUT.UnregisterHtmlEventHandler("simpleEvent", MyHandler);

		SUT.ExecuteJavascript($"element.dispatchEvent(new CustomEvent(\"simpleEvent\", {{ detail: 42 }}));");

		await Task.Delay(100);

		Assert.IsNull(result);
	}
}

#endif
