#nullable enable
namespace Uno.UI.RemoteControl.Messaging.IdeChannel;

public static class DevelopmentEnvironmentComponentPriorities
{
	// The lowest the value, the higher the priority

	public const int Solution = 1;

	public const int UnoCheck = 100;

	public const int DevServer = 200;
}
