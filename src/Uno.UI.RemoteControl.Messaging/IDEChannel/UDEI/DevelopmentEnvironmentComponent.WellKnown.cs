namespace Uno.UI.RemoteControl.Messaging.IdeChannel;

partial record DevelopmentEnvironmentComponent
{
	// Well-known components of the development environment, this is **NOT** an exhaustive list!

	public static DevelopmentEnvironmentComponent Solution { get; } = new("uno.solution", "Solution", "Load of the solution, resolution of nuget packages and validation of uno's SDK version.");
	public static DevelopmentEnvironmentComponent UnoCheck { get; } = new("uno.check", "Uno Check", "Validates all external dependencies has been installed on the computer.");
	public static DevelopmentEnvironmentComponent DevServer { get; } = new("uno.dev_server", "Dev Server", "The local server that allows the application to interact with the IDE and the file-system.");
}
