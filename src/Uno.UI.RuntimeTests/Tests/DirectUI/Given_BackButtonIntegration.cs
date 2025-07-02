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
		bool wasTriggeredLocally = false;
		var manager = SystemNavigationManager.GetForCurrentView();
		void OnBackRequested(object sender, BackRequestedEventArgs e)
		{
			wasTriggeredLocally = true;
		}
		try
		{
			listener.Handle = true;
			BackButtonIntegration.RegisterListener(listener);
			manager.BackRequested += OnBackRequested;
			manager.RequestBack();
			Assert.IsTrue(listener.WasTriggered, "Listener should have been triggered on back request.");
			Assert.IsFalse(wasTriggeredLocally, "Local handling should not be triggered after listener handles.");
		}
		finally
		{
			manager.BackRequested -= OnBackRequested;
			BackButtonIntegration.UnregisterListener(listener);
		}
	}

	[TestMethod]
	[RunsOnUIThread]
	public void When_Normal_Back_Handling_NotHandled()
	{
		var listener = new TestListener();
		bool wasTriggeredLocally = false;
		var manager = SystemNavigationManager.GetForCurrentView();
		void OnBackRequested(object sender, BackRequestedEventArgs e)
		{
			wasTriggeredLocally = true;
		}
		try
		{
			listener.Handle = false;
			BackButtonIntegration.RegisterListener(listener);
			manager.BackRequested += OnBackRequested;
			manager.RequestBack();
			Assert.IsTrue(listener.WasTriggered, "Listener should have been triggered on back request.");
			Assert.IsTrue(wasTriggeredLocally, "Local handling should be triggered when listener does not handle.");
		}
		finally
		{
			manager.BackRequested -= OnBackRequested;
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
