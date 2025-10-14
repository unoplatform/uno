using System;
using System.Linq;

namespace Microsoft.UI.Xaml
{
	public partial class FrameworkElement
	{
		private TriggerCollection _triggers;
		private bool _triggersInitialized;

		public TriggerCollection Triggers
		{
			get
			{
				if (_triggers == null)
				{
					_triggers = new TriggerCollection();
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

			// Hook into the Loaded event to process triggers
			this.Loaded += OnLoadedForTriggers;
		}

		private void OnLoadedForTriggers(object sender, RoutedEventArgs e)
		{
			if (_triggers == null || _triggers.Count == 0)
			{
				return;
			}

			// EventTrigger only supports Loaded event, so we fire all EventTrigger actions when Loaded fires
			foreach (var trigger in _triggers.OfType<EventTrigger>())
			{
				trigger.FireActions();
			}
		}

		private static RoutedEvent _loadedEvent;
		internal static RoutedEvent LoadedEvent
		{
			get
			{
				if (_loadedEvent == null)
				{
					_loadedEvent = new RoutedEvent(
						Uno.UI.Xaml.RoutedEventFlag.None,
						nameof(Loaded));
				}
				return _loadedEvent;
			}
		}
	}
}
