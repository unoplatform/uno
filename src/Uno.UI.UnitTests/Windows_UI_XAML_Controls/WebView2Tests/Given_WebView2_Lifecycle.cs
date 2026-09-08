#nullable enable

using System;
using System.Runtime.CompilerServices;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.Web.WebView2.Core;
using Uno.UI.Xaml.Controls;
using Windows.UI;

namespace Uno.UI.Tests.Windows_UI_Xaml_Controls;

[TestClass]
public class Given_WebView2_Lifecycle
{
	[TestMethod]
	public void When_Constructed_The_Default_Style_Uses_The_WinUI_Resource_Dictionary()
	{
		var webView = new WebView2();
		try
		{
			Assert.AreEqual(
				new Uri("ms-appx:///Microsoft.UI.Xaml/Themes/themeresources.xaml"),
				webView.DefaultStyleResourceUri);
		}
		finally
		{
			webView.Close();
		}
	}

	[TestMethod]
	public void When_Settings_Outlive_A_Closed_Core_They_Do_Not_Retain_It()
	{
		var core = CreateClosedCore(out var settings);
		GC.Collect();
		GC.WaitForPendingFinalizers();
		GC.Collect();

		Assert.IsFalse(core.TryGetTarget(out _));
		GC.KeepAlive(settings);
	}

	[MethodImpl(MethodImplOptions.NoInlining)]
	private static WeakReference<CoreWebView2> CreateClosedCore(out CoreWebView2Settings settings)
	{
		var owner = new WebView2();
		var core = new CoreWebView2((IWebView)owner);
		settings = core.Settings;
		core.Close();
		owner.Close();
		return new WeakReference<CoreWebView2>(core);
	}

	[TestMethod]
	public void When_Source_Is_Null_It_Does_Not_Initialize_The_Core()
	{
		var webView = new WebView2();
		var initialized = 0;
		webView.CoreWebView2Initialized += (_, _) => initialized++;
		try
		{
			Assert.IsNull(webView.Source);
			webView.Source = null;
			Assert.IsNull(webView.Source);
			webView.ClearValue(WebView2.SourceProperty);
			Assert.IsNull(webView.Source);
			Assert.IsNull(webView.CoreWebView2);
			Assert.AreEqual(0, initialized);
		}
		finally
		{
			webView.Close();
		}
	}

	[TestMethod]
	public void When_Not_Initialized_CoreWebView2_Is_Null()
	{
		var webView = new WebView2();
		try
		{
			Assert.IsNull(webView.CoreWebView2);
		}
		finally
		{
			webView.Close();
		}
	}

	[TestMethod]
	[DataRow(true)]
	[DataRow(false)]
	public void When_History_Property_Is_Set_By_Application_It_Is_Rejected(bool back)
	{
		var webView = new WebView2();
		try
		{
			var property = back ? WebView2.CanGoBackProperty : WebView2.CanGoForwardProperty;
			Assert.ThrowsExactly<ArgumentException>(() => webView.SetValue(property, true));
		}
		finally
		{
			webView.Close();
		}
	}

	[TestMethod]
	public void When_Closed_History_Navigation_Is_A_NoOp()
	{
		var webView = new WebView2();
		webView.Close();
		webView.Close();

		webView.GoBack();
		webView.GoForward();
		Assert.IsNull(webView.CoreWebView2);
		Assert.IsFalse(webView.CanGoBack);
		Assert.IsFalse(webView.CanGoForward);
	}

	[TestMethod]
	public void When_Constructed_Default_Background_Matches_Controller()
	{
		var webView = new WebView2();
		try
		{
			Assert.AreEqual(Colors.White, webView.DefaultBackgroundColor);
		}
		finally
		{
			webView.Close();
		}
	}

	[TestMethod]
	public void When_Not_Initialized_Core_Operations_Are_Rejected()
	{
		var webView = new WebView2();
		try
		{
			Assert.ThrowsExactly<InvalidOperationException>(() => webView.ExecuteScriptAsync("1"));
			Assert.ThrowsExactly<InvalidOperationException>(webView.Reload);
			Assert.ThrowsExactly<InvalidOperationException>(() => webView.NavigateToString("<html></html>"));
		}
		finally
		{
			webView.Close();
		}
	}

	[TestMethod]
	public void When_Relative_Source_Is_Rejected()
	{
		var webView = new WebView2();
		try
		{
			Assert.ThrowsExactly<ArgumentException>(() => webView.Source = new Uri("relative.html", UriKind.Relative));
		}
		finally
		{
			webView.Close();
		}
	}

	[TestMethod]
	[DataRow(null)]
	[DataRow("")]
	public void When_UserAgent_Is_Empty_The_WinRT_Override_Is_Preserved(string? reset)
	{
		var settings = new CoreWebView2Settings();
		settings.UserAgent = "Uno-WebView2-Override";
		settings.UserAgent = reset;

		Assert.AreEqual("Uno-WebView2-Override", settings.UserAgent);
	}
}
