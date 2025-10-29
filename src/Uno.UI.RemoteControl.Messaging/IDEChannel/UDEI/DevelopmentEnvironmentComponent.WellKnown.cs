namespace Uno.UI.RemoteControl.Messaging.IdeChannel;

partial record DevelopmentEnvironmentComponent
{
	// Well-known components of the development environment, this is **NOT** an exhaustive list!

	public static DevelopmentEnvironmentComponent Solution { get; } = new(
		"uno.solution",
		DevelopmentEnvironmentComponentPriorities.Solution,
		"Solution",
		"Loads the solution and resolves NuGet packages.");

	public static DevelopmentEnvironmentComponent UnoCheck { get; } = new(
		"uno.check",
		DevelopmentEnvironmentComponentPriorities.UnoCheck,
		"Uno Check",
		"Validates that all external dependencies are installed.");

	public static DevelopmentEnvironmentComponent DevServer { get; } = new(
		"uno.dev_server",
		DevelopmentEnvironmentComponentPriorities.DevServer,
		"Dev Server",
		"The local development server, which allows the application to interact with the IDE and file system.");
}
