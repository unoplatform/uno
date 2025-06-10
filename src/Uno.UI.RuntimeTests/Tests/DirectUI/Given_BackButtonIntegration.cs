using DirectUI;
using Windows.UI.Core;

namespace Uno.UI.RuntimeTests.Tests.DirectUI;

[TestClass]
public class Given_BackButtonIntegration
{
#if HAS_UNO
	[TestMethod]
	[RunsOnUIThread]
	public void When_Back_Listener_Registered()
	{
		var listener = new TestListener();
		try
		{
			BackButtonIntegration.RegisterListener(listener);
			SystemNavigationManager.GetForCurrentView().RequestBack();
			Assert.IsTrue(listener.WasTriggered, "Listener should have been triggered on back request.");
		}
		finally
		{
			BackButtonIntegration.UnregisterListener(listener);
		}
	}

	[TestMethod]
	[RunsOnUIThread]
	public void When_Back_Listener_Unregistered()
	{
		var listener = new TestListener();
		try
		{
			BackButtonIntegration.RegisterListener(listener);
			BackButtonIntegration.UnregisterListener(listener);
			SystemNavigationManager.GetForCurrentView().RequestBack();
			Assert.IsFalse(listener.WasTriggered, "Listener should not have been triggered after unregistration.");
		}
		finally
		{
			BackButtonIntegration.UnregisterListener(listener);
		}
	}

	[TestMethod]
	[RunsOnUIThread]
	public void When_Normal_Back_Handling_Handled()
	{
		var listener = new TestListener();
		try
		{
			listener.Handle = true;
			BackButtonIntegration.RegisterListener(listener);
			var manager = SystemNavigationManager.GetForCurrentView();
			bool wasTriggeredLocally = false;
			manager.BackRequested += (s, e) => wasTriggeredLocally = true;
			manager.RequestBack();
			Assert.IsTrue(listener.WasTriggered, "Listener should have been triggered on back request.");
			Assert.IsFalse(wasTriggeredLocally, "Local handling should not be triggered after listener handles.");
		}
		finally
		{
			BackButtonIntegration.UnregisterListener(listener);
		}
	}

	[TestMethod]
	[RunsOnUIThread]
	public void When_Normal_Back_Handling_NotHandled()
	{
		var listener = new TestListener();
		try
		{
			listener.Handle = false;
			BackButtonIntegration.RegisterListener(listener);
			var manager = SystemNavigationManager.GetForCurrentView();
			bool wasTriggeredLocally = false;
			manager.BackRequested += (s, e) => wasTriggeredLocally = true;
			manager.RequestBack();
			Assert.IsTrue(listener.WasTriggered, "Listener should have been triggered on back request.");
			Assert.IsTrue(wasTriggeredLocally, "Local handling should be triggered when listener does not handle.");
		}
		finally
		{
			BackButtonIntegration.UnregisterListener(listener);
		}
	}

	private class TestListener : IBackButtonListener
	{
		public bool WasTriggered { get; set; }

		public bool Handle { get; set; } = true;

		public bool OnBackButtonPressed()
		{
			WasTriggered = true;
			return Handle;
		}
	}
#endif
}
