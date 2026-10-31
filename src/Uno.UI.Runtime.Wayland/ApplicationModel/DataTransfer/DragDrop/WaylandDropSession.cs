#nullable enable

using System;
using System.Threading.Tasks;
using Windows.ApplicationModel.DataTransfer;

namespace Uno.UI.Runtime.Wayland;

// UI thread. Only a successful drop may keep the offer alive for outstanding data reads.
internal class WaylandDropSession(Func<Task<bool>> whenReadsCompleted, Action<uint> finishAndDestroy, Action destroy)
{
	private bool _completed;
	internal bool Dropped { get; set; }
	internal uint DropAction { get; set; }

	internal void Leave() => Complete(DataPackageOperation.None);

	internal void Complete(DataPackageOperation result)
	{
		if (_completed)
		{
			return;
		}
		_completed = true;
		// finish allows the source to delete moved data, so only report the action actually performed.
		var succeeded = Dropped && DropAction != 0 && (WaylandDataFormats.ToDndActions(result) & DropAction) != 0;
		if (!succeeded)
		{
			destroy();
			return;
		}
		_ = FinishAsync();
	}

	private async Task FinishAsync()
	{
		bool received;
		try
		{
			received = await whenReadsCompleted().ConfigureAwait(false);
		}
		catch
		{
			destroy();
			return;
		}
		if (received)
		{
			finishAndDestroy(DropAction);
		}
		else
		{
			destroy();
		}
	}
}
