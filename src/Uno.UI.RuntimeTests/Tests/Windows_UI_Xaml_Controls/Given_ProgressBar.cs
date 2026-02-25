using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.UI.Xaml.Automation;
using Microsoft.UI.Xaml.Automation.Peers;
using Microsoft.UI.Xaml.Controls;
using Private.Infrastructure;
#if __SKIA__
using Uno.UI.Xaml;
#endif

namespace Uno.UI.RuntimeTests.Tests.Windows_UI_Xaml_Controls;

[TestClass]
public class Given_ProgressBar
{
#if __SKIA__
	[TestMethod]
	[RunsOnUIThread]
	public async Task When_ProgressBar_Automation_Listener_Attached()
	{
		try
		{
			var grid = new Grid() { Width = 100, Height = 100 };
			var progressBar = new ProgressBar();
			grid.Children.Add(progressBar);
			TestServices.WindowHelper.WindowContent = grid;
			await TestServices.WindowHelper.WaitForLoaded(grid);

			var stubListener = new StubListener();
			AutomationPeer.TestAutomationPeerListener = stubListener;
			progressBar.Maximum = 10;
			Assert.IsTrue(stubListener.Notified);
		}
		finally
		{
			AutomationPeer.TestAutomationPeerListener = null;
		}
	}

	private class StubListener : IAutomationPeerListener
	{
		public bool Notified { get; private set; }

		public bool ListenerExistsHelper(AutomationEvents eventId) => true;
		public void NotifyPropertyChangedEvent(AutomationPeer peer, AutomationProperty automationProperty, object oldValue, object newValue) =>
			Notified = true;
		public void NotifyAutomationEvent(AutomationPeer peer, AutomationEvents eventId) => Notified = true;
		public void NotifyNotificationEvent(AutomationPeer peer, AutomationNotificationKind notificationKind, AutomationNotificationProcessing notificationProcessing, string displayString, string activityId) => Notified = true;
	}
#endif
}
