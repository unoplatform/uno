using System;
using System.Collections.Generic;
using System.Text;

namespace Windows.UI.Xaml.Media.Animation
{
	public partial class NavigationTransitionInfo : DependencyObject
	{
		private string _navigationState;

		protected NavigationTransitionInfo()
		{
			InitializeBinder();
		}

		protected virtual string GetNavigationStateCore() => _navigationState;

		internal string GetNavigationStateCoreInternal() => GetNavigationStateCore();

		protected virtual void SetNavigationStateCore(string navigationState) => _navigationState = navigationState;

		internal void SetNavigationStateCoreInternal(string navigationState) => SetNavigationStateCore(navigationState);
	}
}
