using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Uno.Extensions;
using Uno.Helpers;
using Windows.Foundation;
using Windows.UI.Core;

#if NET7_0_OR_GREATER
using NativeMethods = __Windows.UI.Popups.MessageDialog.NativeMethods;
#endif

namespace Windows.UI.Popups;

public partial class MessageDialog
{
	private static readonly SemaphoreSlim _viewControllerAccess = new SemaphoreSlim(1, 1);

	private IAsyncOperation<IUICommand> ShowNativeAsync(CancellationToken ct)
	{
		VisualTreeHelperProxy.CloseAllFlyouts();

#if NET7_0_OR_GREATER
		NativeMethods.Alert(Content);
#else
		var command = $"Uno.UI.WindowManager.current.alert(\"{Uno.Foundation.WebAssemblyRuntime.EscapeJs(Content)}\");";
		Uno.Foundation.WebAssemblyRuntime.InvokeJS(command);
#endif

		return AsyncOperation.FromTask<IUICommand>(
			ct => Task.FromResult<IUICommand>(new UICommand("OK")) // TODO: Localize (PBI 28711)
		);
	}
}
