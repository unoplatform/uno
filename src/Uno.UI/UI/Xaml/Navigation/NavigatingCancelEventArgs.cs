using System;
using System.Collections.Generic;
using System.Text;
using Windows.UI.Xaml.Media.Animation;

namespace Windows.UI.Xaml.Navigation
{
	public sealed partial class NavigatingCancelEventArgs
	{
		public NavigatingCancelEventArgs(NavigationMode navigationMode, NavigationTransitionInfo navigationTransitionInfo, object parameter, Type sourcePageType)
		{
			NavigationMode = navigationMode;
			NavigationTransitionInfo = navigationTransitionInfo;
			Parameter = parameter;
			SourcePageType = sourcePageType;
		}

		public bool Cancel { get; set; }

		public NavigationMode NavigationMode { get; }

		public NavigationTransitionInfo NavigationTransitionInfo { get; }

		public object Parameter { get; }

		public Type SourcePageType { get; }
	}
}
