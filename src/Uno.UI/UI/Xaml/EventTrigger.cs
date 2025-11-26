using System;

namespace Microsoft.UI.Xaml;

public partial class EventTrigger : TriggerBase
{
	private RoutedEvent _routedEvent;

	public EventTrigger()
	{
		Actions = new TriggerActionCollection();
	}

	public RoutedEvent RoutedEvent
	{
		get => _routedEvent;
		set
		{
			// EventTrigger only supports the Loaded event, matching WinUI behavior
			// See: https://github.com/microsoft/microsoft-ui-xaml/blob/b1db15715bfead9fe8ad2e7f78b0172589225e69/src/dxaml/xcp/dxaml/lib/EventTrigger_Partial.cpp#L5
			if (value != null && value != FrameworkElement.LoadedEvent)
			{
				throw new NotSupportedException("EventTrigger only supports the Loaded event.");
			}
			_routedEvent = value;
		}
	}

	public TriggerActionCollection Actions { get; }

	internal void FireActions()
	{
		foreach (var action in Actions)
		{
			if (action is Media.Animation.BeginStoryboard beginStoryboard)
			{
				beginStoryboard.Storyboard?.Begin();
			}
		}
	}
}
