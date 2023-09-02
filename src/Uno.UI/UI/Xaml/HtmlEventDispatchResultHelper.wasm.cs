using System;
using System.Linq;
using Uno.UI.Xaml.Input;

namespace Windows.UI.Xaml;

internal struct HtmlEventDispatchResultHelper
{
	private HtmlEventDispatchResult _value = HtmlEventDispatchResult.Ok;
	private bool _shouldStop = false;

	public HtmlEventDispatchResultHelper()
	{
	}

	public HtmlEventDispatchResult Value => _value;

	public bool ShouldStop => _shouldStop;

	public void Add(object args, object handlerResult)
	{
		switch (handlerResult)
		{
			case bool isHandledInManaged when isHandledInManaged && args is IHtmlHandleableRoutedEventArgs customHandledResult:
				Add(customHandledResult, true);
				break;

			case bool isHandledInManaged when isHandledInManaged:
				Add(HtmlEventDispatchResult.StopPropagation | HtmlEventDispatchResult.PreventDefault);
				break;

			case HtmlEventDispatchResult dispatchResult:
				Add(dispatchResult);
				break;
		}
	}

	public void Add(HtmlEventDispatchResult result)
	{
		_shouldStop |= result.HasFlag(HtmlEventDispatchResult.StopPropagation);
		_value |= result & ~HtmlEventDispatchResult.NotDispatched; // Note: We always remove the NotDispatched flag as it's forbidden for app usage.
	}

	public void Add(IHtmlHandleableRoutedEventArgs args, bool handled)
	{
		// Note: This allow users to return Handled=true without sending back StopPropagation to the native event.
		// This is most probably invalid, but as this is internal only there is no need to enforce that for now.

		if (handled)
		{
			_shouldStop = true;
			_value |= args.HandledResult & ~HtmlEventDispatchResult.NotDispatched; // Note: We always remove the NotDispatched flag as it's forbidden for app usage.
		}
	}
}
