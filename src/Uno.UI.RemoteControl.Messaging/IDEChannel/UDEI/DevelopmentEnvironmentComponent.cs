#nullable enable
namespace Uno.UI.RemoteControl.Messaging.IdeChannel;

/// <summary>
/// Represents a component within the development environment, including its identifier and description.
/// </summary>
/// <param name="Id">
/// The unique identifier of the development environment component
/// (e.g. uno.dev_server)
/// </param>
/// <param name="Name">
/// A user-friendly name of the development environment component
/// (e.g. "Dev-server.").
/// </param>
/// <param name="Description">
/// A user-friendly description of the development environment component
/// (e.g. "The local server that allows the application to interact with the IDE and the file-system.").
/// </param>
public partial record DevelopmentEnvironmentComponent(string Id, string Name, string Description);
