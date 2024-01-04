#pragma warning disable 67 // TODO: Focus-related members are currently unused https://github.com/unoplatform/uno/issues/8978

using System;
using Uno.UI.Xaml.Islands;
using Windows.Foundation;
using Uno.UI.Xaml.Controls;
using WinUICoreServices = global::Uno.UI.Xaml.Core.CoreServices;

namespace Microsoft.UI.Xaml.Hosting;

// TODO: Port more of the actual implementation from WinUI

/// <summary>
/// Enables a target (for example, a window, or a WPF control)
/// to host Uno Platform controls in an UI element.
/// </summary>
public partial class DesktopWindowXamlSource : IDisposable
{
	/// <summary>
	/// Occurs when the DesktopWindowXamlSource gets focus in the desktop application
	/// (for example, the user presses the Tab key while focus is on the element just
	/// before the DesktopWindowXamlSource).
	/// </summary>
	public event TypedEventHandler<DesktopWindowXamlSource, DesktopWindowXamlSourceGotFocusEventArgs> GotFocus;

	/// <summary>
	/// Occurs when the host desktop application receives a request take back focus from the DesktopWindowXamlSource object
	/// (for example, the user is on the last focusable element in the DesktopWindowXamlSource and presses Tab).
	/// </summary>
	public event TypedEventHandler<DesktopWindowXamlSource, DesktopWindowXamlSourceTakeFocusRequestedEventArgs> TakeFocusRequested;

	/// <summary>
	/// Gets a value that indicates whether the DesktopWindowXamlSource currently has focus in the desktop application.
	/// </summary>
	public bool HasFocus => false; //TODO: Always false currently, should adhere to its purpose https://github.com/unoplatform/uno/issues/8978[focus]

	/// <summary>
	/// Attempts to programmatically give focus to the DesktopWindowXamlSource in the desktop application.
	/// </summary>
	/// <param name="request">An object that specifies the reason and other optional info for the focus navigation.</param>
	/// <returns>An object that provides data for the focus navigation request.</returns>
	public XamlSourceFocusNavigationResult NavigateFocus(XamlSourceFocusNavigationRequest request) => new XamlSourceFocusNavigationResult(false); //TODO: Always false currently, should adhere to its purpose https://github.com/unoplatform/uno/issues/8978[focus]

	internal void AttachToWindow(Window window)
	{
		ContentManager.AttachToWindow(XamlIsland, window);
		_xamlIsland.OwnerWindow = window;
	}
}
