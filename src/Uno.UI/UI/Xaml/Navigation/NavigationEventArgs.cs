using System;
using System.Collections.Generic;
using System.Text;
using Windows.UI.Xaml.Media.Animation;

namespace Windows.UI.Xaml.Navigation
{
	public sealed partial class NavigationEventArgs
	{
		internal NavigationEventArgs(
			object content,
			NavigationMode navigationMode,
			NavigationTransitionInfo navigationTransitionInfo,
			object parameter,
			Type sourcePageType,
			Uri uri
		)
		{
			Content = content;
			NavigationMode = navigationMode;
			NavigationTransitionInfo = navigationTransitionInfo;
			Parameter = parameter;
			SourcePageType = sourcePageType;
			Uri = uri;
		}

		public object Content { get; }

		public NavigationMode NavigationMode { get; }

		public NavigationTransitionInfo NavigationTransitionInfo { get; }

		public object Parameter { get; }

		public Type SourcePageType { get; }

		public Uri Uri { get; set; }
	}
}
