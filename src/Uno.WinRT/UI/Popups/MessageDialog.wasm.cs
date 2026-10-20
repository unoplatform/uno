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

using NativeMethods = __Windows.UI.Popups.MessageDialog.NativeMethods;

namespace Windows.UI.Popups;

public partial class MessageDialog
{
	private IAsyncOperation<IUICommand> ShowNativeAsync(CancellationToken ct)
	{
		VisualTreeHelperProxy.CloseAllFlyouts();

		NativeMethods.Alert(Content);

		return AsyncOperation.FromTask<IUICommand>(
			ct => Task.FromResult<IUICommand>(new UICommand("OK")) // TODO: Localize (PBI 28711)
		);
	}
}
