using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Uno.Extensions;
using Windows.Foundation;
using Windows.UI.Core;

namespace Windows.UI.Popups
{
	public partial class MessageDialog
	{
		private static readonly SemaphoreSlim _viewControllerAccess = new SemaphoreSlim(1, 1);

		public IAsyncOperation<IUICommand> ShowAsync()
		{
			var command = $"Uno.UI.WindowManager.current.alert(\"{Uno.Foundation.WebAssemblyRuntime.EscapeJs(Content)}\");";
			Uno.Foundation.WebAssemblyRuntime.InvokeJS(command);

			return AsyncOperation.FromTask<IUICommand>(
				async ct => new UICommand("OK") // TODO: Localize (PBI 28711)
			);
		}

		partial void ValidateCommands()
		{
		}
	}
}
