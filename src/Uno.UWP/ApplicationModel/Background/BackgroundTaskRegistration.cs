namespace Windows.ApplicationModel.Background;


	public partial class BackgroundTaskRegistration : IBackgroundTaskRegistration, IBackgroundTaskRegistration2, IBackgroundTaskRegistration3
	{
		public string Name { get; internal set; }
		public IBackgroundTrigger Trigger { get; internal set; }

		internal BackgroundTaskRegistration(string name, IBackgroundTrigger trigger)
		{
			Name = name;
			Trigger = trigger;
		}

	}


