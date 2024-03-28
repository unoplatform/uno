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

		protected virtual string GetNavigationStateCore()
		{
			return _navigationState;
		}

		protected virtual void SetNavigationStateCore(string navigationState)
		{
			_navigationState = navigationState;
		}
	}
}
