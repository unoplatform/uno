#nullable enable

using System;
using System.Threading;
using System.Threading.Tasks;

namespace Uno.UI.Runtime.Linux;

// https://flatpak.github.io/xdg-desktop-portal/docs/window-identifiers.html

/// <summary>
/// Supplies the "parent_window" identifier of portal requests for the display server in use.
/// </summary>
internal interface IPortalParentWindowProvider
{
	/// <summary>
	/// Gets the identifier of the window hosting the current application view, or null when there is no such window.
	/// The identifier stays valid until the result is disposed, which happens once the portal request completes.
	/// </summary>
	Task<PortalParentWindow?> GetParentWindowAsync(CancellationToken token);
}

internal sealed class PortalParentWindow(string identifier, IDisposable? lifetime = null) : IDisposable
{
	/// <summary>
	/// The portal window identifier, such as "x11:1a00003" or "wayland:handle"; empty when the parent cannot be identified.
	/// </summary>
	public string Identifier { get; } = identifier;

	public void Dispose() => lifetime?.Dispose();
}
