#nullable enable

using System.Collections.Generic;
using System.Threading;
using Microsoft.UI.Dispatching;

namespace Microsoft.UI.Xaml.Hosting;

partial class WindowsXamlManager
{
	/// <summary>
	/// XamlCore is a per-thread object that represents the the running Xaml Core on the thread.
	/// It tracks the WindowsXamlManager objects on the thread, and shuts down the XAML
	/// state on the thread when all WXM instances on the thread are closed.  It may outlive
	/// all the instances of WindowsXamlManager.
	/// </summary>
	private partial class XamlCore
	{
		public enum XamlCoreState
		{
			Normal,
			Closing,
			Closed
		}

		public XamlCoreState State { get; set; } = XamlCoreState.Normal;

		private DispatcherQueue _dispatcherQueue;

		// These are raw non-ref-counted pointers, the WindowsXamlManager objects add and remove themselves.
		public List<WindowsXamlManager> _managers = new();

		private static int _instancesInProcess;
	}

	private DispatcherQueue _dispatcherQueue;
	//private bool _isClosed;

	private XamlCore? _xamlCore;

	// Note this is thread-local.  Each thread has zero or one tls_xamlCore.
	private ThreadLocal<XamlCore> _tlsXamlCore = new();
}
