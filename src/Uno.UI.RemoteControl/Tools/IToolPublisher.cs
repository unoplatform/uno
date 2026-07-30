#nullable enable

using System;

namespace Uno.UI.RemoteControl.Tools;

/// <summary>The registration face of the registry, used by publishers to declare capabilities.</summary>
internal interface IToolPublisher
{
	/// <summary>
	/// Declares a tool. Dispose the result to remove it. On a duplicate <see cref="ToolDescriptor.Name"/>
	/// the registration is rejected (a warning is logged), the existing tool is left untouched, and a
	/// no-op <see cref="IDisposable"/> is returned whose <see cref="IDisposable.Dispose"/> affects nothing.
	/// </summary>
	IDisposable RegisterTool(ToolDescriptor descriptor, ToolHandler handler, bool runOnUIThread = true);

	/// <summary>
	/// Declares a resource. Use the result to read it and to signal updates. Duplicate
	/// <see cref="ResourceDescriptor.Uri"/> values follow the same no-op rule as <see cref="RegisterTool"/>.
	/// </summary>
	IResourceRegistration RegisterResource(ResourceDescriptor descriptor, ResourceReader reader);
}
