using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Media.Animation;
using Microsoft.UI.Xaml.Navigation;
using Uno.Disposables;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace Microsoft.UI.Xaml.Controls;

/// <summary>
/// Displays Page instances, supports navigation to new pages,
/// and maintains a navigation history to support forward
/// and backward navigation.
/// </summary>
public partial class Frame : ContentControl
{
	/// <summary>
	/// Serializes the Frame navigation history into a string.
	/// </summary>
	public string GetNavigationState() => GetNavigationStateImpl();

	/// <summary>
	/// Navigates to the most recent item in back navigation history, if a Frame manages its own navigation history.
	/// </summary>
	public void GoBack() => GoBackImpl();

	/// <summary>
	/// Navigates to the most recent item in back navigation history, if a Frame manages its own navigation history,
	/// and specifies the animated transition to use.
	/// </summary>
	/// <param name="transitionInfoOverride">Info about the animated transition to use.</param>
	public void GoBack(NavigationTransitionInfo transitionInfoOverride) => GoBackWithTransitionInfoImpl(transitionInfoOverride);

	/// <summary>
	/// Navigates to the most recent item in forward navigation history, if a Frame manages its own navigation history.
	/// </summary>
	public void GoForward() => GoForwardImpl();

	/// <summary>
	/// Causes the Frame to load content represented by the specified Page.
	/// </summary>
	/// <param name="sourcePageType">The page to navigate to, specified as a type reference to its partial class type.</param>
	/// <returns>False if a NavigationFailed event handler has set Handled to true; otherwise, true. See Remarks for more info.</returns>
	public bool Navigate(Type sourcePageType) => NavigateImpl(sourcePageType);

	/// <summary>
	/// Causes the Frame to load content represented by the specified Page, also passing a parameter to be interpreted by the target of the navigation.
	/// </summary>
	/// <param name="sourcePageType">The page to navigate to, specified as a type reference to its partial class type.</param>
	/// <param name="parameter">The navigation parameter to pass to the target page.</param>
	/// <returns>False if a NavigationFailed event handler has set Handled to true; otherwise, true. See Remarks for more info.</returns>
	public bool Navigate(Type sourcePageType, object parameter) => NavigateImpl(sourcePageType, parameter);

	/// <summary>
	/// Causes the Frame to load content represented by the specified Page-derived data type, also passing a parameter to be interpreted by the target of the navigation,
	/// and a value indicating the animated transition to use.
	/// </summary>
	/// <param name="sourcePageType">The page to navigate to, specified as a type reference to its partial class type.</param>
	/// <param name="parameter">The navigation parameter to pass to the target page; must have a basic type (string, char, numeric, or GUID) to support parameter serialization using GetNavigationState.</param>
	/// <param name="infoOverride">Info about the animated transition.</param>
	/// <returns></returns>
	public bool Navigate(Type sourcePageType, object parameter, NavigationTransitionInfo infoOverride) =>
		NavigateWithTransitionInfoImpl(sourcePageType, parameter, infoOverride);

	/// <summary>
	/// Causes the Frame to load content represented by the specified Page, also passing a parameter to be interpreted by the target of the navigation.
	/// </summary>
	/// <param name="sourcePageType">The page to navigate to, specified as a type reference to its partial class type.</param>
	/// <param name="parameter">The navigation parameter to pass to the target page.</param>
	/// <param name="navigationOptions">Options for the navigation, including whether it is recorded in the navigation stack and what transition animation is used.</param>
	/// <returns></returns>
	public bool NavigateToType(Type sourcePageType, object parameter, FrameNavigationOptions navigationOptions) =>
		NavigateToTypeImpl(sourcePageType, parameter, navigationOptions);

	/// <summary>
	/// Reads and restores the navigation history of a Frame from a provided serialization string.
	/// </summary>
	/// <param name="navigationState">The serialization string that supplies the restore point for navigation history.</param>
	public void SetNavigationState(string navigationState) => SetNavigationStateImpl(navigationState);

	/// <summary>
	/// Reads and restores the navigation history of a Frame from a provided serialization string.
	/// </summary>
	/// <param name="navigationState">The serialization string that supplies the restore point for navigation history.</param>
	/// <param name="suppressNavigate">True to restore navigation history without navigating to the current page; otherwise, false.</param>
	public void SetNavigationState(string navigationState, bool suppressNavigate) => SetNavigationStateWithNavigationControlImpl(navigationState, suppressNavigate);

	internal static Page CreatePageInstance(Type sourcePageType)
	{
		if (Uno.UI.DataBinding.BindingPropertyHelper.BindableMetadataProvider != null)
		{
			var bindableType = Uno.UI.DataBinding.BindingPropertyHelper.BindableMetadataProvider.GetBindableTypeByType(sourcePageType);

			if (bindableType != null)
			{
				return bindableType.CreateInstance()() as Page;
			}
		}

		return Activator.CreateInstance(sourcePageType) as Page;
	}

#if __CROSSRUNTIME__
	internal PageStackEntry GetCurrentPageStackEntry() => m_tpNavigationHistory.GetCurrentPageStackEntry();

	internal Page EnsurePageInitialized(PageStackEntry entry) => entry.Instance;
#endif
}
