using Windows.Foundation;

namespace Microsoft.UI.Content;

public partial class ContentIslandEnvironment
{
	//UNO TODO: Properly implement ContentIslandEnvironment

	public WindowId AppWindowId => new WindowId();

	public DisplayId DisplayId => new DisplayId();

	public event TypedEventHandler<ContentIslandEnvironment, ContentEnvironmentSettingChangedEventArgs> SettingChanged
	{
		add
		{
		}
		remove
		{
		}
	}

	public event TypedEventHandler<ContentIslandEnvironment, ContentEnvironmentStateChangedEventArgs> StateChanged
	{
		add
		{
		}
		remove
		{
		}
	}
}
