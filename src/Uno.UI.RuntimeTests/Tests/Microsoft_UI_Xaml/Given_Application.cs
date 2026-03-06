#nullable enable

using System;
using System.Threading.Tasks;
using Microsoft.UI.Xaml;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Uno.UI.RuntimeTests.Tests.Microsoft_UI_Xaml;

[TestClass]
public class Given_Application
{
	[TestMethod]
	[RunsOnUIThread]
	public void When_DispatcherShutdownMode_Default_Then_OnLastWindowClose()
	{
		// Arrange
		var application = Application.Current;

		// Assert - Default should be OnLastWindowClose
		Assert.AreEqual(DispatcherShutdownMode.OnLastWindowClose, application.DispatcherShutdownMode);
	}

	[TestMethod]
	[RunsOnUIThread]
	public void When_DispatcherShutdownMode_Set_Then_Value_Changes()
	{
		// Arrange
		var application = Application.Current;
		var originalMode = application.DispatcherShutdownMode;

		try
		{
			// Act
			application.DispatcherShutdownMode = DispatcherShutdownMode.OnExplicitShutdown;

			// Assert
			Assert.AreEqual(DispatcherShutdownMode.OnExplicitShutdown, application.DispatcherShutdownMode);

			// Act
			application.DispatcherShutdownMode = DispatcherShutdownMode.OnLastWindowClose;

			// Assert
			Assert.AreEqual(DispatcherShutdownMode.OnLastWindowClose, application.DispatcherShutdownMode);
		}
		finally
		{
			// Cleanup - Restore original mode
			application.DispatcherShutdownMode = originalMode;
		}
	}
}
