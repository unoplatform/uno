#nullable enable

namespace Windows.ApplicationModel.Background;

public partial interface IBackgroundTask
{
	void Run(IBackgroundTaskInstance taskInstance);
}
