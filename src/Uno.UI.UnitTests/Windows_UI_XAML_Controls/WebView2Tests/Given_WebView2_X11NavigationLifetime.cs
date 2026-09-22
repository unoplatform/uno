#nullable enable

using System;
using System.Collections.Generic;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Uno.UI.WebView.Skia.X11;

namespace Uno.UI.Tests.Windows_UI_Xaml_Controls;

[TestClass]
public class Given_WebView2_X11NavigationLifetime
{
	[TestMethod]
	public void When_Closed_Before_Dispatch_No_Navigation_Or_Stop_Is_Queued()
	{
		var closed = false;
		var navigations = 0;
		var gtk = new Queue<Action>();
		var callback = X11WebViewNavigationStarting.CreateCallback(
			() => closed,
			() => { navigations++; return true; },
			gtk.Enqueue,
			() => Assert.Fail("A closed native view cannot be stopped."));

		closed = true;
		callback();

		Assert.AreEqual(0, navigations);
		Assert.AreEqual(0, gtk.Count);
	}

	[TestMethod]
	public void When_Closed_Between_Dispatch_And_Gtk_Stop_The_Native_View_Is_Not_Touched()
	{
		var closed = false;
		var stops = 0;
		var gtk = new Queue<Action>();
		var callback = X11WebViewNavigationStarting.CreateCallback(
			() => closed,
			() => true,
			gtk.Enqueue,
			() => { Assert.IsFalse(closed, "StopLoading reached a disposed native view."); stops++; });

		callback();
		Assert.AreEqual(1, gtk.Count);
		closed = true;
		gtk.Dequeue()();

		Assert.AreEqual(0, stops);
	}

	[TestMethod]
	public void When_Navigation_Handler_Closes_The_Provider_No_Stop_Is_Queued()
	{
		var closed = false;
		var gtk = new Queue<Action>();
		var callback = X11WebViewNavigationStarting.CreateCallback(
			() => closed,
			() => { closed = true; return true; },
			gtk.Enqueue,
			() => Assert.Fail("The provider was closed by the navigation handler."));

		callback();

		Assert.AreEqual(0, gtk.Count);
	}

	[TestMethod]
	[DataRow(false)]
	[DataRow(true)]
	public void When_The_Provider_Is_Open_Only_Cancelled_Navigations_Stop(bool cancel)
	{
		var stops = 0;
		var gtk = new Queue<Action>();
		var callback = X11WebViewNavigationStarting.CreateCallback(
			() => false,
			() => cancel,
			gtk.Enqueue,
			() => stops++);

		callback();
		Assert.AreEqual(cancel ? 1 : 0, gtk.Count);
		if (cancel)
		{
			gtk.Dequeue()();
		}
		Assert.AreEqual(cancel ? 1 : 0, stops);
	}
}
