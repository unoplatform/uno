#nullable disable

using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Windows.Foundation;
using IAsyncOperation = System.Threading.Tasks.Task;

namespace Windows.UI.Popups
{
	public sealed partial class PopupMenu
	{
		[Uno.NotImplemented]
		public PopupMenu()
		{
			throw new NotImplementedException();
		}

		[Uno.NotImplemented]
		public Task<IUICommand> ShowForSelectionAsync()
		{
			throw new NotImplementedException();
		}

		[Uno.NotImplemented]
		public Task<IUICommand> ShowForSelectionAsync(Rect selection)
		{
			throw new NotImplementedException();
		}

		[Uno.NotImplemented]
		public Task<IUICommand> ShowForSelectionAsync(
		  Rect selection,
		  Placement preferredPlacement
		)
		{
			throw new NotImplementedException();
		}
	}
}
