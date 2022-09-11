#nullable enable

using System;
using System.Windows.Forms;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;

namespace UnoSolutionTemplate.Wizard
{
	internal class DialogParentWindow : IWin32Window, IDisposable
	{
		private readonly IntPtr _handle;

		private bool _enableModeless;

		private readonly IServiceProvider _serviceProvider;

		public IntPtr Handle => _handle;

		public DialogParentWindow(IntPtr handle, bool enableModeless, IServiceProvider serviceProvider)
		{
			ThreadHelper.ThrowIfNotOnUIThread();

			_serviceProvider = serviceProvider;
			var service = _serviceProvider.GetService(typeof(IVsUIShell));

			if (service is IVsUIShell uiShell)
			{
				if (_handle == IntPtr.Zero)
				{
					uiShell.GetDialogOwnerHwnd(out _handle);
				}
				else
				{
					_handle = handle;
				}

				if (enableModeless)
				{
					uiShell.EnableModeless(0);
					_enableModeless = true;
				}
			}
		}

		public void Dispose()
		{
			ThreadHelper.ThrowIfNotOnUIThread();

			if (_enableModeless)
			{
				_enableModeless = false;
				try
				{
					var service = _serviceProvider.GetService(typeof(IVsUIShell));

					if(service is IVsUIShell uiShell)
					{
						uiShell.EnableModeless(1);
					}
				}
				catch
				{
				}
			}
		}
	}

}
