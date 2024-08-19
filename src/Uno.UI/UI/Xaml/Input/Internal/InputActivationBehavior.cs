using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Uno.UI.Xaml.Input;

/// <summary>
/// InputActivationBehavior is used in some element focus move operations to indicate whether the
/// element focus change should also request input activation. Requesting input activation means
/// requesting this island be focused for keyboard input even if a different island or window
/// previously had focus/activation. In an HWND-hosted world, this means moving HWND focus to this
/// island. Taking focus/activation in a Window scenario will effectively "window.Activate()" the
/// window, bring it in front of other Windows on the same thread.
///
/// There are some clear scenarios where input activation should *not* happen, such as when moving
/// element focus because the focus element is being hidden (set to Visibility::Collapsed). For
/// example, if a background Window had focus on a Button which the app decided to hide because it
/// currently didn't apply (such as hiding a "Save" button because the content just saved), we
/// don't want to bring that Window to the front just because a button was hidden. Similarly,
/// removing an element from the tree should not cause activation.
///
/// In general, there are probably very few cases which should request activation. To be consistent
/// with WPF and Win32, direct uielement.Focus() calls should take activation, since this is the
/// mechanism we have for apps to move full focus+activation to an element. A good example of this
/// is to imagine a single top-level HWND containing two XAML Islands, where one island contains
/// a Search box the app wants to focus+activate when the user presses Ctrl+E. Ideally the app can
/// simply call Focus() on that Search box rather than also needing to find a separate island API
/// to Activate() that island; WPF and Win32 don't require an extra call.
///
/// But to minimize risk we curently only request NoActivate in cases where we're confident that
/// activation should not be requested. Use of NoActivate will likely increase over time. It may
/// also be necessary/desirable in the future to add a new public uielement.Focus() or FocusManager
/// API to allow control libraries or apps to indicate any cases they have where they don't want
/// to request activation.
/// </summary>
internal enum InputActivationBehavior
{
	NoActivate,        // just move element focus
	RequestActivation  // request full input activation, moving system focus to this island
}
