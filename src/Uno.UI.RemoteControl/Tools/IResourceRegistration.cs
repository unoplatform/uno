#nullable enable

using System;

namespace Uno.UI.RemoteControl.Tools;

/// <summary>Handle for a registered resource. Signal content changes via <see cref="NotifyUpdated"/>; dispose to remove.</summary>
internal interface IResourceRegistration : IDisposable
{
	/// <summary>Raises <see cref="IToolCatalog.ResourceUpdated"/> for this resource. A no-op once disposed.</summary>
	void NotifyUpdated();
}
