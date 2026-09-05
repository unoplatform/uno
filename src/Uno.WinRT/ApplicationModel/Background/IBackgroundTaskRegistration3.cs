#nullable enable

namespace Windows.ApplicationModel.Background;

public partial interface IBackgroundTaskRegistration3 : IBackgroundTaskRegistration
{
	BackgroundTaskRegistrationGroup TaskGroup { get; }
}
