using System;
using System.Runtime.InteropServices;
using System.Threading;
using Windows.UI.Core;
using Private.Infrastructure;

#if HAS_UNO_WINUI || WINAPPSDK
using Microsoft.UI.Dispatching;
#endif

namespace MUXControlsTestApp.Utilities
{
	public class IdleSynchronizer
	{
#if HAS_UNO_WINUI || WINAPPSDK
		public static DispatcherQueue DispatcherQueue
		{
			get;
			private set;
		}
#endif

		//private static IdleSynchronizer instance = null;

		//private static IdleSynchronizer Instance
		//{
		//	get
		//	{
		//		if (instance == null)
		//		{
		//			throw new Exception("IdleSynchronizer.Init() must be called on the UI thread before waiting.");
		//		}

		//		return instance;
		//	}
		//}

		//private AppTestAutomationHelpers.IdleSynchronizer _idleSynchronizerImpl;

		//private IdleSynchronizer(DispatcherQueue dispatcherQueue)
		//{
		//	_idleSynchronizerImpl = new AppTestAutomationHelpers.IdleSynchronizer(dispatcherQueue);
		//}

#if HAS_UNO_WINUI || WINAPPSDK
		public static void Init()
		{
			DispatcherQueue dispatcherQueue = DispatcherQueue.GetForCurrentThread();

			if (dispatcherQueue == null)
			{
				throw new Exception("IdleSynchronizer.Init() must be called on the UI thread.");
			}

			DispatcherQueue = dispatcherQueue;
			//instance = new IdleSynchronizer(dispatcherQueue);
		}
#endif

		public static void Wait()
		{
#if __WASM__
			if (!Uno.UI.Dispatching.NativeDispatcher.IsThreadingSupported)
			{
				return;
			}
#endif
#if !WINAPPSDK
			if (!CoreDispatcher.Main.HasThreadAccess)
#endif
			{
				TestServices.WindowHelper.WaitForIdle().Wait(TestUtilities.DefaultWaitMs);
			}
		}
	}
}
