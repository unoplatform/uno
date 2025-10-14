namespace Microsoft.UI.Xaml
{
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
			set => _routedEvent = value;
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
}
