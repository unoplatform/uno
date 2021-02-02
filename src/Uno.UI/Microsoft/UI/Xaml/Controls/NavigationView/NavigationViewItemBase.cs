// MUX reference NavigationViewItemBase.cpp, commit f3da94f 

using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media;

namespace Microsoft.UI.Xaml.Controls
{
	public partial class NavigationViewItemBase : ContentControl
	{
		internal NavigationViewRepeaterPosition Position
		{
			get => m_position;
			set
			{
				if (m_position != value)
				{
					m_position = value;
					OnNavigationViewItemBasePositionChanged();
				}
			}
		}

		internal NavigationView GetNavigationView()
		{
			return m_navigationView;
		}

		// TODO: MZ Uno specific: existing Depth property inherited from base class
		internal
#if __NETSTD_REFERENCE__ || __SKIA__ || __WASM__
			new
#endif
			int Depth
		{
			get => m_depth;
			set
			{
				if (m_depth != value)
				{
					m_depth = value;
					OnNavigationViewItemBaseDepthChanged();
				}
			}
		}

		protected SplitView GetSplitView()
		{
			SplitView splitView = null;
			var navigationView = GetNavigationView();
			if (navigationView != null)
			{
				splitView = navigationView.GetSplitView();
			}
			return splitView;
		}

		internal void SetNavigationViewParent(NavigationView navigationView)
		{
			m_navigationView = navigationView;
		}

		private void OnPropertyChanged(DependencyPropertyChangedEventArgs args)
		{
			if (args.Property == IsSelectedProperty)
			{
				OnNavigationViewItemBaseIsSelectedChanged();
			}
		}

#if IS_UNO
		// TODO: Uno specific: Remove when #4689 is fixed

		protected bool _fullyInitialized = false;

		internal void Reinitialize()
		{
			if (!_fullyInitialized)
			{
				OnApplyTemplate();
			}
		}
#endif
	}
}
