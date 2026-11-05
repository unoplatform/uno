#nullable enable

using System.Collections.Generic;
using Uno.UI.Composition.Drawing;

namespace Uno.UI.Runtime.MacOS;

internal partial class MacOSWindowHost
{
	// Native callbacks arrive with the popup's window, which isn't a host of its own.
	private static readonly Dictionary<nint, MacOSWindowedPopupSite> _popupSitesByHandle = new();

	private readonly List<MacOSWindowedPopupSite> _popupSites = new();

	internal nint NativeHandle => _nativeWindow.Handle;

	internal GraphicsContextKind GraphicsContextKind => _context.Kind;

	internal void AddPopupSite(MacOSWindowedPopupSite site)
	{
		_popupSites.Add(site);
		_popupSitesByHandle[site.Handle] = site;
	}

	internal void RemovePopupSite(MacOSWindowedPopupSite site)
	{
		_popupSites.Remove(site);
		_popupSitesByHandle.Remove(site.Handle);
	}

	private void NotifyPopupSitesOfBounds()
	{
		foreach (var site in _popupSites.ToArray())
		{
			site.OnOwnerBoundsChanged();
		}
	}

	private void ClosePopupSites()
	{
		foreach (var site in _popupSites.ToArray())
		{
			site.Dispose();
		}
	}
}
