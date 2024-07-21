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
using Uno.UI;
using Uno.UI.Helpers;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace Microsoft.UI.Xaml.Controls;

/// <summary>
/// Displays Page instances, supports navigation to new pages,
/// and maintains a navigation history to support forward
/// and backward navigation.
/// </summary>
public partial class Frame : ContentControl
{
	private bool _useWinUIBehavior;

	/// <summary>
	/// Initializes a new instance of the Frame class.
	/// </summary>
	public Frame()
	{
		_useWinUIBehavior = FeatureConfiguration.Frame.UseWinUIBehavior;
		if (_useWinUIBehavior)
		{
			CtorWinUI();
		}
		else
		{
			CtorLegacy();
		}
	}

	/// <summary>
	/// Serializes the Frame navigation history into a string.
	/// </summary>
	public string GetNavigationState() =>
		_useWinUIBehavior ? GetNavigationStateImpl() : GetNavigationStateLegacy();

	/// <summary>
	/// Navigates to the most recent item in back navigation history, if a Frame manages its own navigation history.
	/// </summary>
	public void GoBack()
	{
		if (_useWinUIBehavior)
		{
			GoBackImpl();
		}
		else
		{
			GetNavigationStateLegacy();
		}
	}

	/// <summary>
	/// Navigates to the most recent item in back navigation history, if a Frame manages its own navigation history,
	/// and specifies the animated transition to use.
	/// </summary>
	/// <param name="transitionInfoOverride">Info about the animated transition to use.</param>
	public void GoBack(NavigationTransitionInfo transitionInfoOverride)
	{
		if (_useWinUIBehavior)
		{
			GoBackWithTransitionInfoImpl(transitionInfoOverride);
		}
		else
		{
			GoBackWithTransitionInfoLegacy(transitionInfoOverride);
		}
	}

	/// <summary>
	/// Navigates to the most recent item in forward navigation history, if a Frame manages its own navigation history.
	/// </summary>
	public void GoForward()
	{
		if (_useWinUIBehavior)
		{
			GoForwardImpl();
		}
		else
		{
			GoForwardLegacy();
		}
	}

	/// <summary>
	/// Causes the Frame to load content represented by the specified Page.
	/// </summary>
	/// <param name="sourcePageType">The page to navigate to, specified as a type reference to its partial class type.</param>
	/// <returns>False if a NavigationFailed event handler has set Handled to true; otherwise, true. See Remarks for more info.</returns>
	public bool Navigate(Type sourcePageType) => _useWinUIBehavior ? NavigateImpl(sourcePageType) : NavigateLegacy(sourcePageType);

	/// <summary>
	/// Causes the Frame to load content represented by the specified Page, also passing a parameter to be interpreted by the target of the navigation.
	/// </summary>
	/// <param name="sourcePageType">The page to navigate to, specified as a type reference to its partial class type.</param>
	/// <param name="parameter">The navigation parameter to pass to the target page.</param>
	/// <returns>False if a NavigationFailed event handler has set Handled to true; otherwise, true. See Remarks for more info.</returns>
	public bool Navigate(Type sourcePageType, object parameter) => _useWinUIBehavior ? NavigateImpl(sourcePageType, parameter) : NavigateLegacy(sourcePageType, parameter);

	/// <summary>
	/// Causes the Frame to load content represented by the specified Page-derived data type, also passing a parameter to be interpreted by the target of the navigation,
	/// and a value indicating the animated transition to use.
	/// </summary>
	/// <param name="sourcePageType">The page to navigate to, specified as a type reference to its partial class type.</param>
	/// <param name="parameter">The navigation parameter to pass to the target page; must have a basic type (string, char, numeric, or GUID) to support parameter serialization using GetNavigationState.</param>
	/// <param name="infoOverride">Info about the animated transition.</param>
	/// <returns></returns>
	public bool Navigate(Type sourcePageType, object parameter, NavigationTransitionInfo infoOverride) =>
		_useWinUIBehavior ? NavigateWithTransitionInfoImpl(sourcePageType, parameter, infoOverride) : NavigateWithTransitionInfoLegacy(sourcePageType, parameter, infoOverride);

	/// <summary>
	/// Causes the Frame to load content represented by the specified Page, also passing a parameter to be interpreted by the target of the navigation.
	/// </summary>
	/// <param name="sourcePageType">The page to navigate to, specified as a type reference to its partial class type.</param>
	/// <param name="parameter">The navigation parameter to pass to the target page.</param>
	/// <param name="navigationOptions">Options for the navigation, including whether it is recorded in the navigation stack and what transition animation is used.</param>
	/// <returns></returns>
	public bool NavigateToType(Type sourcePageType, object parameter, FrameNavigationOptions navigationOptions) =>
		_useWinUIBehavior ? NavigateToTypeImpl(sourcePageType, parameter, navigationOptions) : NavigateToTypeLegacy(sourcePageType, parameter, navigationOptions);

	/// <summary>
	/// Reads and restores the navigation history of a Frame from a provided serialization string.
	/// </summary>
	/// <param name="navigationState">The serialization string that supplies the restore point for navigation history.</param>
	public void SetNavigationState(string navigationState)
	{
		if (_useWinUIBehavior)
		{
			SetNavigationStateImpl(navigationState);
		}
		else
		{
			SetNavigationStateLegacy(navigationState);
		}
	}

	/// <summary>
	/// Reads and restores the navigation history of a Frame from a provided serialization string.
	/// </summary>
	/// <param name="navigationState">The serialization string that supplies the restore point for navigation history.</param>
	/// <param name="suppressNavigate">True to restore navigation history without navigating to the current page; otherwise, false.</param>
	public void SetNavigationState(string navigationState, bool suppressNavigate)
	{
		if (_useWinUIBehavior)
		{
			SetNavigationStateWithNavigationControlImpl(navigationState, suppressNavigate);
		}
		else
		{
			SetNavigationStateWithNavigationControlLegacy(navigationState, suppressNavigate);
		}
	}

	internal static object CreatePageInstance(Type sourcePageType)
	{
		var replacementType = sourcePageType.GetReplacementType(); // Get latest replacement type to handle Hot Reload.
		if (Uno.UI.DataBinding.BindingPropertyHelper.BindableMetadataProvider != null)
		{
			var bindableType = Uno.UI.DataBinding.BindingPropertyHelper.BindableMetadataProvider.GetBindableTypeByType(replacementType);

			if (bindableType != null)
			{
				return bindableType.CreateInstance()();
			}
		}

		return Activator.CreateInstance(replacementType);
	}

	internal PageStackEntry GetCurrentPageStackEntry() => _useWinUIBehavior ? m_tpNavigationHistory.GetCurrentPageStackEntry() : CurrentEntry;

	internal Page EnsurePageInitialized(PageStackEntry entry) => _useWinUIBehavior ? entry.Instance : EnsurePageInitializedLegacy(entry);
}
