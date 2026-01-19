using System;
using System.Linq;

namespace Microsoft.UI.Xaml;

public partial class FrameworkElement
{
	private TriggerCollection _triggers;
	private bool _triggersInitialized;

	/// <summary>
	/// Gets the collection of triggers for animations that are defined for a FrameworkElement.
	/// </summary>
	/// <remarks>
	/// Triggers, EventTrigger, Actions and BeginStoryboard are not commonly used.
	/// These API mainly exist for compatibility in XAML originally used for Microsoft Silverlight.
	/// </remarks>
	public TriggerCollection Triggers
	{
		get
		{
			if (_triggers == null)
			{
				_triggers = new TriggerCollection();
				_triggers.SetOwner(this);
				InitializeTriggers();
			}
			return _triggers;
		}
	}

	private void InitializeTriggers()
	{
		if (_triggersInitialized)
		{
			return;
		}

		_triggersInitialized = true;

		// Only subscribe to Loaded if not yet loaded
		// Triggers added after loading fire via TriggerCollection.OnTriggerAdded
		if (!IsLoaded)
		{
			this.Loaded += OnLoadedForTriggers;
		}
	}

	private void OnLoadedForTriggers(object sender, RoutedEventArgs e)
	{
		// Unsubscribe to prevent memory leaks
		this.Loaded -= OnLoadedForTriggers;

		FireLoadedTriggers();
	}

	private void FireLoadedTriggers()
	{
		if (_triggers == null || _triggers.Count == 0)
		{
			return;
		}

		// EventTrigger only supports Loaded event, so we fire all EventTrigger actions when Loaded fires
		// Use .ToArray() to avoid collection modification during enumeration
		foreach (var trigger in _triggers.OfType<EventTrigger>().ToArray())
		{
			trigger.FireActions();
		}
	}

	private static readonly RoutedEvent _loadedEvent = new RoutedEvent(
		Uno.UI.Xaml.RoutedEventFlag.None,
		nameof(Loaded));

	/// <summary>
	/// Identifies the Loaded routed event.
	/// </summary>
	internal static RoutedEvent LoadedEvent => _loadedEvent;
}
