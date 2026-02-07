using System;

namespace Microsoft.UI.Xaml;

/// <summary>
/// Represents a trigger that applies a set of actions (animation storyboards) in response to an event.
/// Currently, only the FrameworkElement.Loaded event is supported.
/// </summary>
public partial class EventTrigger : TriggerBase
{
	private RoutedEvent _routedEvent;

	/// <summary>
	/// Initializes a new instance of the EventTrigger class.
	/// </summary>
	public EventTrigger()
	{
		Actions = new TriggerActionCollection();
	}

	/// <summary>
	/// Gets or sets the name of the event that initiates the trigger.
	/// Currently, only the FrameworkElement.Loaded event is supported.
	/// </summary>
	/// <exception cref="ArgumentException">Thrown when a value other than the Loaded event is set.</exception>
	public RoutedEvent RoutedEvent
	{
		get => _routedEvent;
		set
		{
			// EventTrigger only supports the Loaded event, matching WinUI behavior
			// See: https://github.com/microsoft/microsoft-ui-xaml/blob/b1db15715bfead9fe8ad2e7f78b0172589225e69/src/dxaml/xcp/dxaml/lib/EventTrigger_Partial.cpp#L5
			if (value != null && value.Name != "Loaded")
			{
				throw new ArgumentException("EventTrigger only supports the Loaded event.", nameof(value));
			}
			_routedEvent = value;
		}
	}

	/// <summary>
	/// Gets the collection of BeginStoryboard objects that this EventTrigger maintains.
	/// </summary>
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
