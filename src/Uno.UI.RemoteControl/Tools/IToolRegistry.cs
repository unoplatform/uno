#nullable enable

namespace Uno.UI.RemoteControl.Tools;

/// <summary>The combined contract implemented by the registry singleton; used by <see cref="ToolRegistry.SetForTesting"/>.</summary>
internal interface IToolRegistry : IToolPublisher, IToolCatalog
{
}
