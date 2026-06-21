#nullable enable

using System;

namespace Uno.UI.RemoteControl.Tools;

/// <summary>Event payload for <see cref="IToolCatalog.ResourceUpdated"/>: the uri of the updated resource.</summary>
internal sealed class ResourceUpdatedEventArgs(string uri) : EventArgs
{
	public string Uri { get; } = uri;
}
