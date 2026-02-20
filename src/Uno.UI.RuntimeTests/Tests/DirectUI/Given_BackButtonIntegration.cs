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

	[TestMethod]
	[RunsOnUIThread]
	public void When_First_Listener_Registered_HasAnyBackHandlers_True()
	{
		var manager = SystemNavigationManager.GetForCurrentView();
		var listener = new TestListener();
		try
		{
			Assert.IsFalse(manager.HasAnyBackHandlers, "Should start with no handlers.");
			BackButtonIntegration.RegisterListener(listener);
			Assert.IsTrue(manager.HasAnyBackHandlers, "Should have handlers after registering a listener.");
			Assert.IsTrue(manager.HasInternalBackListeners, "Should have internal listeners.");
		}
		finally
		{
			BackButtonIntegration.UnregisterListener(listener);
		}
	}

	[TestMethod]
	[RunsOnUIThread]
	public void When_Last_Listener_Unregistered_HasAnyBackHandlers_False()
	{
		var manager = SystemNavigationManager.GetForCurrentView();
		var listener1 = new TestListener();
		var listener2 = new TestListener();
		try
		{
			BackButtonIntegration.RegisterListener(listener1);
			BackButtonIntegration.RegisterListener(listener2);
			Assert.IsTrue(manager.HasAnyBackHandlers);

			BackButtonIntegration.UnregisterListener(listener1);
			Assert.IsTrue(manager.HasAnyBackHandlers, "Should still have handlers with one listener remaining.");

			BackButtonIntegration.UnregisterListener(listener2);
			Assert.IsFalse(manager.HasAnyBackHandlers, "Should have no handlers after all listeners removed.");
			Assert.IsFalse(manager.HasInternalBackListeners);
		}
		finally
		{
			BackButtonIntegration.UnregisterListener(listener1);
			BackButtonIntegration.UnregisterListener(listener2);
		}
	}

	[TestMethod]
	[RunsOnUIThread]
	public void When_Public_And_Internal_Handlers_Combined()
	{
		var manager = SystemNavigationManager.GetForCurrentView();
		var listener = new TestListener();
		void OnBackRequested(object sender, BackRequestedEventArgs e) { }

		try
		{
			Assert.IsFalse(manager.HasAnyBackHandlers);

			// Add internal listener only
			BackButtonIntegration.RegisterListener(listener);
			Assert.IsTrue(manager.HasAnyBackHandlers);
			Assert.IsFalse(manager.HasBackRequestedSubscribers);

			// Add public subscriber
			manager.BackRequested += OnBackRequested;
			Assert.IsTrue(manager.HasAnyBackHandlers);
			Assert.IsTrue(manager.HasBackRequestedSubscribers);

			// Remove internal listener - still has public subscriber
			BackButtonIntegration.UnregisterListener(listener);
			Assert.IsTrue(manager.HasAnyBackHandlers);

			// Remove public subscriber
			manager.BackRequested -= OnBackRequested;
			Assert.IsFalse(manager.HasAnyBackHandlers);
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
