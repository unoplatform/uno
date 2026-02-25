namespace Uno.WinUI.Runtime.Skia.X11.DBus
{
	// From https://flatpak.github.io/xdg-desktop-portal/docs/doc-org.freedesktop.portal.Request.html#signals
	enum Response : uint
	{
		Success = 0,
		UserCancelled = 1,
		Other = 2
	}
}