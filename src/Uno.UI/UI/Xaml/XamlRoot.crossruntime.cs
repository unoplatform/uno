using System;
using Uno.Foundation.Logging;
using Uno.UI.Dispatching;

namespace Windows.UI.Xaml;

public sealed partial class XamlRoot
{
	private bool _renderQueued;

	internal event Action InvalidateRender = () => { };

	internal void InvalidateMeasure() => VisualTree.RootElement.InvalidateMeasure();

	internal void InvalidateArrange() => VisualTree.RootElement.InvalidateArrange();

	internal void RaiseInvalidateRender()
	{
		InvalidateRender();
	}

	internal void QueueInvalidateRender()
	{
		if (!_renderQueued)
		{
			_renderQueued = true;

			DispatchQueueRender();
		}
	}

	private void DispatchQueueRender()
	{
		NativeDispatcher.Main.Enqueue(() =>
		{
			if (_renderQueued)
			{
				_renderQueued = false;
				InvalidateRender();
			}
		});
	}
}
