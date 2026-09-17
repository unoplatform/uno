#nullable enable

using System;

namespace Windows.ApplicationModel.Background
{
	public partial interface IBackgroundTaskRegistration
	{
		string Name { get; }

		Guid TaskId { get; }

		void Unregister(bool cancelTask);

		event BackgroundTaskCompletedEventHandler Completed;

		event BackgroundTaskProgressEventHandler Progress;
	}
}
