using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Threading.Tasks;
using Uno.UI.Hosting;

namespace Uno.UI.Tests.Hosting
{
	[TestClass]
	public class Given_UnoPlatformHost
	{
		[TestMethod]
		public void When_Exception_In_Initialize_Should_Throw_Actual_Exception()
		{
			// Arrange
			var host = new TestHost_ThrowsInInitialize();

			// Act & Assert
			var exception = Assert.ThrowsException<InvalidOperationException>(() => host.Run());
			Assert.AreEqual("Test exception in Initialize", exception.Message);
		}

		[TestMethod]
		public void When_Exception_In_InitializeAsync_Should_Throw_Actual_Exception()
		{
			// Arrange
			var host = new TestHost_ThrowsInInitializeAsync();

			// Act & Assert
			var exception = Assert.ThrowsException<InvalidOperationException>(() => host.Run());
			Assert.AreEqual("Test exception in InitializeAsync", exception.Message);
		}

		[TestMethod]
		public void When_Run_With_Async_RunLoop_Should_Throw_InvalidOperationException()
		{
			// Arrange
			var host = new TestHost_AsyncRunLoop();

			// Act & Assert
			var exception = Assert.ThrowsException<InvalidOperationException>(() => host.Run());
			Assert.IsTrue(exception.Message.Contains("requires calling 'await host.RunAsync()' instead of 'host.Run()'"));
		}

		[TestMethod]
		public void When_Run_With_Synchronous_Completion_Should_Not_Throw()
		{
			// Arrange
			var host = new TestHost_SynchronousCompletion();

			// Act & Assert - should not throw
			host.Run();
			Assert.IsTrue(host.InitializeCalled);
		}

		[TestMethod]
		public async Task When_RunAsync_With_Exception_Should_Throw_Actual_Exception()
		{
			// Arrange
			var host = new TestHost_ThrowsInInitialize();

			// Act & Assert
			var exception = await Assert.ThrowsExceptionAsync<InvalidOperationException>(() => host.RunAsync());
			Assert.AreEqual("Test exception in Initialize", exception.Message);
		}

		// Test host implementations
		private class TestHost_ThrowsInInitialize : UnoPlatformHost
		{
			protected override void Initialize()
			{
				throw new InvalidOperationException("Test exception in Initialize");
			}

			protected override Task RunLoop()
			{
				return Task.CompletedTask;
			}
		}

		private class TestHost_ThrowsInInitializeAsync : UnoPlatformHost
		{
			protected override void Initialize()
			{
				// No-op
			}

			protected override Task InitializeAsync()
			{
				throw new InvalidOperationException("Test exception in InitializeAsync");
			}

			protected override Task RunLoop()
			{
				return Task.CompletedTask;
			}
		}

		private class TestHost_AsyncRunLoop : UnoPlatformHost
		{
			protected override void Initialize()
			{
				// No-op
			}

			protected override async Task RunLoop()
			{
				await Task.Delay(100); // Simulate async operation
			}
		}

		private class TestHost_SynchronousCompletion : UnoPlatformHost
		{
			public bool InitializeCalled { get; private set; }

			protected override void Initialize()
			{
				InitializeCalled = true;
			}

			protected override Task RunLoop()
			{
				return Task.CompletedTask;
			}
		}
	}
}
