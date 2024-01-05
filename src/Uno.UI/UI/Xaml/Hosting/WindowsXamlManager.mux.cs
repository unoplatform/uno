#nullable enable

using System;
using DirectUI;
using Microsoft.UI.Dispatching;
using FrameworkApplication = Microsoft.UI.Xaml.Application;

namespace Microsoft.UI.Xaml.Hosting;

partial class WindowsXamlManager
{
	/// <summary>
	/// Initializes the WinUI XAML framework in a non-Windows App SDK (WASDK) desktop application 
	/// (for example, a WPF or Windows Forms application) on the current thread.
	/// </summary>
	/// <returns>An object that contains a reference to the WinUI XAML framework.</returns>
	public static WindowsXamlManager InitializeForCurrentThread() => new();

	private WindowsXamlManager()
	{
		//IFC_RETURN(WeakReferenceSourceNoThreadId::Initialize());

		_dispatcherQueue = DispatcherQueue.GetForCurrentThread();
		if (_dispatcherQueue is null)
		{
			throw new InvalidOperationException(
					"A DispatcherQueue must exist on the thread before performing this operation. " +
					"Please create a DispatcherQueueController before creating a WindowsXamlManager or " +
					"DesktopWindowXamlSource object.");
		}

		if (!_tlsXamlCore.IsValueCreated)
		{
			// CheckWindowingModelPolicy();

			_tlsXamlCore.Value = new XamlCore(_dispatcherQueue);
		}

		// Take a strong ref on tls_xamlCore here just to keep it alive if the thread shuts down suddenly.
		_xamlCore = _tlsXamlCore.Value;

		if (_tlsXamlCore.Value is not null && _tlsXamlCore.Value.State == XamlCore.XamlCoreState.Closing)
		{
			// The XamlCore is in the middle of an asynchronous shutdown.
			// Cancel the Close, we're going to re-use this tls_xamlCore.
			_tlsXamlCore.Value.State = XamlCore.XamlCoreState.Normal;
		}

		_tlsXamlCore.Value!.AddManager(this);

		// This will make sure that it doesn't get cleared off thread in WeakReferenceSourceNoThreadId::OnFinalReleaseOffThread()
		// as it has thread-local variables and needs to be disposed off by the same thread
		// AddToReferenceTrackingList();
	}

	partial class XamlCore
	{
		internal XamlCore(DispatcherQueue dispatcherQueue)
		{
			_dispatcherQueue = dispatcherQueue;

			//
			// Deal with Application.Current. There are a few cases here:
			//
			//   1. The app has already created an instance of Application (or a derived type) before initializing Xaml. In this
			//      case Application.Current is already set, and Xaml should hook up to it for things like OnLaunched.
			//
			//   2. The app hasn't created an instance of Application (or a derived type) before initializing Xaml, but is
			//      keeping one alive from a previous time when Xaml was initialized. This is the scenario where an app has a
			//      reference to an Application object that persists after deinitializing Xaml and is now reinitializing Xaml.
			//      In this case we want to pick up their existing instance and reuse it as Application.Current.
			//
			//   3. The app hasn't created an instance of Application before initializing Xaml. In this case we make an instance
			//      of FrameworkApplication and use that as Application.Current.
			//
			FrameworkApplication? frameworkApplication = null;
			// Take the lock here to ensure FrameworkApplication::ReleaseCurrent isn't happening on another thread while
			//  we're starting up the Application (see XamlCore::Close)
			// CApplicationLock lock;
			++_instancesInProcess;

			frameworkApplication = FrameworkApplication.Current;
			if (frameworkApplication is not null)
			{
				// Case 1. We can no-op here - the FrameworkApplication (or its derived class) has already registered itself via
				// FrameworkApplication::Initialize when it was created.
			}
			//else if (FrameworkApplication.InitializeFromPreviousApplication())
			//{
			//	// Case 2. FrameworkApplication successfully picked up the existing instance and reused it.
			//	frameworkApplication = FrameworkApplication.Current;
			//}
			else
			{
				// Case 3. We have to create the framework application ourselves.
				frameworkApplication = new();
			}

			frameworkApplication.StartOnCurrentThread(null /*pCallback*/);

			var pCore = DXamlCore.Current;
			pCore.EnsureCoreApplicationInitialized();

			//// In XamlBridge mode, we can't use WinRT-only input.  Explicitly turn it off.
			//// TODO: OS bug 14726409
			//XamlOneCoreTransforms.EnsureInitialized(XamlOneCoreTransforms::InitMode::ForceDisable);


			//::EnableMouseInPointer(TRUE);

			//// NotifyEndOfReferenceTrackingOnThread destroys all RCWs of the current thread including NON Xaml ones
			//// This will break text input as WPF maintains RCW for TSF among other things.
			//pCore.DisableNotifyEndOfReferenceTrackingOnThread();

			//auto shutdownStartingCallback = WRLHelper::MakeAgileCallback<
			//	wf::ITypedEventHandler<msy::DispatcherQueue*, msy::DispatcherQueueShutdownStartingEventArgs*>>
			//	([](msy::IDispatcherQueue *, msy::IDispatcherQueueShutdownStartingEventArgs *)

			//{
			//	tls_xamlCore->SyncCloseAllManagers();
			//	return S_OK;
			//});

			//IFCFAILFAST(m_dispatcherQueue3->add_FrameworkShutdownStarting(
			//	shutdownStartingCallback.Get(),
			//	&m_frameworkShutdownStartingToken));
		}

		internal void AddManager(WindowsXamlManager manager)
		{
			_managers.Add(manager);
		}

		internal void RemoveManager(WindowsXamlManager manager)
		{
			_managers.Remove(manager);
		}
	}
}
