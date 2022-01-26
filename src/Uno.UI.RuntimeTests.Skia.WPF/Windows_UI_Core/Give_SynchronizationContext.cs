#if HAS_UNO
using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Windows.UI.Core;
using static Uno.UI.RuntimeTests.Skia.WPF.Helpers.NativeMethods;

namespace Uno.UI.RuntimeTests.Tests.Windows_UI_Core
{
	[TestClass]
	public class Give_SynchronizationContext
	{
		/// <summary>
		/// This method test the GitHub Issue #7829.
		/// This issue is only happen on WPFHost because
		/// when message pump intercept input message (Mouse, Keyboard, ecc...)
		/// call Dispatcher.Invoke to propagate the event.
		/// Dispatcher.Invoke, before executing the callback,
		/// replace the current SynchronizationContext with your own.
		/// see: https://github.com/dotnet/wpf/blob/bda4e10fd0e0d56b8baedc4674b7a5a9d336712c/src/Microsoft.DotNet.Wpf/src/WindowsBase/System/Windows/Threading/Dispatcher.cs#L593-L621
		/// see: https://github.com/dotnet/wpf/blob/bda4e10fd0e0d56b8baedc4674b7a5a9d336712c/src/Microsoft.DotNet.Wpf/src/WindowsBase/System/Windows/Threading/Dispatcher.cs#L736-L766
		/// </summary>
		[STATestMethod]		
		public void Give_CoreSynchronizationContext_When_NativeDispatcher_Invoke()
		{
			var nativeApp = new System.Windows.Application();
			var nativewWPFWindow = new System.Windows.Window();
			var nativeDispatcher = nativewWPFWindow.Dispatcher;
			var host = new Uno.UI.Skia.Platform.WpfHost(nativeDispatcher, () => new Windows.UI.Xaml.Application());
			nativewWPFWindow.Content = host;

			try
			{
				Assert.IsNotNull(nativeDispatcher);

				Assert.IsInstanceOfType(nativeDispatcher, typeof(System.Windows.Threading.Dispatcher));

				System.Threading.AutoResetEvent @event = new System.Threading.AutoResetEvent(false);

				host.KeyDown += (s, e) =>
				{
					try
					{
						Assert.IsInstanceOfType(System.Threading.SynchronizationContext.Current, typeof(CoreDispatcherSynchronizationContext));
					}
					finally
					{
						@event.Set();
					}
				};

				var currentCtx = System.Threading.SynchronizationContext.Current;
				Assert.IsInstanceOfType(currentCtx
					, typeof(CoreDispatcherSynchronizationContext));

				nativewWPFWindow.ShowActivated = true;
				nativewWPFWindow.Show();
				nativewWPFWindow.Activate();
				nativewWPFWindow.Focus();
				System.Windows.Input.Keyboard.Focus(host);

				var hWnd = new System.Windows.Interop.WindowInteropHelper(nativewWPFWindow).EnsureHandle();

				Assert.AreNotEqual(IntPtr.Zero, hWnd);

				// Simulate native key donw 
				SendKey(hWnd, 'A');

				Assert.IsInstanceOfType(System.Threading.SynchronizationContext.Current
					, typeof(CoreDispatcherSynchronizationContext));

				var signal = @event.WaitOne(3000);

				Assert.AreEqual(true, signal);

				Assert.IsInstanceOfType(System.Threading.SynchronizationContext.Current
					, typeof(CoreDispatcherSynchronizationContext));

			}
			finally
			{
				nativewWPFWindow?.Close();
			}
		}
	}
}
#endif
