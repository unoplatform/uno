#pragma warning disable 105 // Disabled until the tree is migrate to WinUI

using System;
using System.Linq;
using System.Collections.Generic;
using SampleControl.Entities;
using Windows.UI.Xaml.Data;
using Uno.Extensions;
using Windows.UI.Xaml;
using Uno.UI.Common;
using Microsoft/* UWP don't rename */.UI.Xaml.Controls;
using Uno.UI.Samples.Controls;

#if XAMARIN || UNO_REFERENCE_API
using Windows.UI.Xaml.Controls;
#else
using Windows.Graphics.Imaging;
using Microsoft.Graphics.Display;
using Windows.UI.Xaml.Media;
using Windows.UI;
using Windows.UI.Xaml.Controls;
#endif

#if HAS_UNO
using Uno.UI.Xaml.Core;
using WinUICoreServices = Uno.UI.Xaml.Core.CoreServices;
#endif

namespace SampleControl.Presentation
{
	public partial class SampleChooserViewModel : System.ComponentModel.INotifyPropertyChanged
	{
		private bool _categoriesSelected = true;
		private bool _favoritesSelected = false;
		private bool _recentsSelected = false;
		private bool _searchSelected = false;
		private bool _isSplitVisible = true;
		private bool _categoryVisibility = true;
		private bool _sampleVisibility = false;
		private bool _recentsVisibility = false;
		private bool _favoritesVisibility = false;
		private bool _searchVisibility = false;
		private bool _contentVisibility = false;
		private bool _isFavoritedSample = false;
		private bool _isAnyContentVisible = false;
		private bool _contentAttachedToWindow;
		private bool _useFluentStyles;
		private bool _useDarkTheme;
		private bool _manualTestsOnly;
		private object _contentPhone = null;
		private string _searchTerm = "";

		private List<SampleChooserContent> _sampleContents;
		private List<SampleChooserContent> _favoriteSamples = new List<SampleChooserContent>();
		private SampleChooserCategory _selectedCategory;
		private IEnumerable<SampleChooserContent> _recentSamples;
		private SampleChooserContent _currentSelectedSample;
		private SampleChooserContent _previousSample;
		private SampleChooserContent _nextSample;
		private SampleChooserContent _selectedLibrarySample;
		private SampleChooserContent _selectedRecentSample;
		private SampleChooserContent _selectedFavoriteSample;
		private SampleChooserContent _selectedSearchSample;
		private List<SampleChooserContent> _filteredSamples;
		private XamlControlsResources _fluentResources;
		private bool _isRecordAllTests = false;

		private void RaisePropertyChanged([System.Runtime.CompilerServices.CallerMemberName] string propertyName = "")
		{
			PropertyChanged?.Invoke(this, new System.ComponentModel.PropertyChangedEventArgs(propertyName));
		}

		//TABS

		public bool CategoriesSelected
		{
			get => _categoriesSelected;
			set
			{
				_categoriesSelected = value;
				RaisePropertyChanged();
			}
		}

		public bool FavoritesSelected
		{
			get => _favoritesSelected;
			set
			{
				_favoritesSelected = value;
				RaisePropertyChanged();
			}
		}

		public bool RecentsSelected
		{
			get => _recentsSelected;
			set
			{
				_recentsSelected = value;
				RaisePropertyChanged();
			}
		}

		public bool SearchSelected
		{
			get => _searchSelected;
			set
			{
				_searchSelected = value;
				RaisePropertyChanged();
			}
		}

		public bool IsSplitVisible
		{
			get => _isSplitVisible;
			set
			{
				_isSplitVisible = value;
				RaisePropertyChanged();
			}
		}


		//TABS

		public bool CategoryVisibility
		{
			get => _categoryVisibility;
			set
			{
				_categoryVisibility = value;
				RaisePropertyChanged();
			}
		}

		public bool ManualTestsOnly
		{
			get => _manualTestsOnly;
			set
			{
				_manualTestsOnly = value;
				RaisePropertyChanged();
				RefreshSamples();
			}
		}

		public bool SampleVisibility
		{
			get => _sampleVisibility;
			set
			{
				_sampleVisibility = value;
				RaisePropertyChanged();
			}
		}

		public bool RecentsVisibility
		{
			get => _recentsVisibility;
			set
			{
				_recentsVisibility = value;
				RaisePropertyChanged();
			}
		}

		public bool FavoritesVisibility
		{
			get => _favoritesVisibility;
			set
			{
				_favoritesVisibility = value;
				RaisePropertyChanged();
			}
		}

		public bool SearchVisibility
		{
			get => _searchVisibility;
			set
			{
				_searchVisibility = value;
				RaisePropertyChanged();
			}
		}

		public bool ContentVisibility
		{
			get => _contentVisibility;
			set
			{
				_contentVisibility = value;
				RaisePropertyChanged();
				OnContentVisibilityChanged();
			}
		}


		//CONTENTS

		public List<SampleChooserCategory> Categories
		{
			get => _categories;
			set
			{
				_categories = value;
				RaisePropertyChanged();
			}
		}

		public List<SampleChooserContent> SampleContents
		{
			get => _sampleContents;
			set
			{
				_sampleContents = value;
				RaisePropertyChanged();
			}
		}

		public List<SampleChooserContent> FavoriteSamples
		{
			get => _favoriteSamples;
			set
			{
				_favoriteSamples = value;
				RaisePropertyChanged();
			}
		}

		public SampleChooserCategory SelectedCategory
		{
			get => _selectedCategory;
			set
			{
				_selectedCategory = value;
				RaisePropertyChanged();
				OnSelectedCategoryChanged();
			}
		}

		public IEnumerable<SampleChooserContent> RecentSamples
		{
			get => _recentSamples;
			set
			{
				_recentSamples = value;
				RaisePropertyChanged();
			}
		}

		public SampleChooserContent CurrentSelectedSample
		{
			get => _currentSelectedSample;
			set
			{
				_currentSelectedSample = value;
				RaisePropertyChanged();
				(ReloadCurrentTestCommand as DelegateCommand).CanExecuteEnabled = true;

				var currentTextIndex = SelectedCategory?.SamplesContent.IndexOf(value);
				// Set Previous
				PreviousSample = currentTextIndex == null || currentTextIndex < 1
					? null
					: SelectedCategory.SamplesContent.Skip((int)currentTextIndex - 1).FirstOrDefault();

				// Set Next
				NextSample = currentTextIndex == null || currentTextIndex < 0 || currentTextIndex == SelectedCategory.SamplesContent.Count - 1
					? null
					: SelectedCategory.SamplesContent.Skip((int)currentTextIndex + 1).FirstOrDefault();
			}
		}

		public SampleChooserContent PreviousSample
		{
			get => _previousSample;
			set
			{
				_previousSample = value;
				RaisePropertyChanged();
				(LoadPreviousTestCommand as DelegateCommand).CanExecuteEnabled = value != null;
			}
		}

		public SampleChooserContent NextSample
		{
			get => _nextSample;
			set
			{
				_nextSample = value;
				RaisePropertyChanged();
				(LoadNextTestCommand as DelegateCommand).CanExecuteEnabled = value != null;
			}
		}

		public SampleChooserContent SelectedLibrarySample
		{
			get => _selectedLibrarySample;
			set
			{
				_selectedLibrarySample = value;
				RaisePropertyChanged();
			}
		}

		public SampleChooserContent SelectedRecentSample
		{
			get => _selectedRecentSample;
			set
			{
				_selectedRecentSample = value;
				RaisePropertyChanged();
			}
		}

		public SampleChooserContent SelectedFavoriteSample
		{
			get => _selectedFavoriteSample;
			set
			{
				_selectedFavoriteSample = value;
				RaisePropertyChanged();
			}
		}

		public SampleChooserContent SelectedSearchSample
		{
			get => _selectedSearchSample;
			set
			{
				_selectedSearchSample = value;
				RaisePropertyChanged();
			}
		}

		public List<SampleChooserContent> FilteredSamples
		{
			get => _filteredSamples;
			set
			{
				_filteredSamples = value;
				RaisePropertyChanged();
			}
		}

		public object ContentPhone
		{
			get => _contentPhone;
			set
			{
				_contentPhone = value;
				RaisePropertyChanged();
			}
		}

		// OTHER
		public string SearchTerm
		{
			get => _searchTerm;
			set
			{
				_searchTerm = value;
				RaisePropertyChanged();
			}
		}

		public bool IsFavoritedSample
		{
			get => _isFavoritedSample;
			set
			{
				_isFavoritedSample = value;
				RaisePropertyChanged();
			}
		}

		public bool IsAnyContentVisible
		{
			get => _isAnyContentVisible;
			set
			{
				_isAnyContentVisible = value;
				RaisePropertyChanged();
			}
		}

		/// <summary>
		/// Toggling this property will detach and reattach the sample control without destroying or recreating it. Useful for verifying correct behaviour.
		/// </summary>
		public bool ContentAttachedToWindow
		{
			get => _contentAttachedToWindow;
			set
			{
				_contentAttachedToWindow = value;
				RaisePropertyChanged();
			}
		}

		public bool UseFluentStyles
		{
			get => _useFluentStyles;
			set
			{
				_useFluentStyles = value;
				if (_useFluentStyles)
				{
					_fluentResources = _fluentResources ?? new XamlControlsResources()
					{
#if !WINAPPSDK
						ControlsResourcesVersion = ControlsResourcesVersion.Version2
#endif
					};
					Application.Current.Resources.MergedDictionaries.Add(_fluentResources);
				}
				else
				{
					Application.Current.Resources.MergedDictionaries.Remove(_fluentResources);
				}
#if HAS_UNO
				// Force the in app styles to reload
				var updateReason = ResourceUpdateReason.ThemeResource;
				Application.Current.Resources?.UpdateThemeBindings(updateReason);
				Uno.UI.ResourceResolver.UpdateSystemThemeBindings(updateReason);
				foreach (var root in WinUICoreServices.Instance.ContentRootCoordinator.ContentRoots)
				{
					Application.PropagateResourcesChanged(root.XamlRoot?.Content, updateReason);
				}
#endif
				RaisePropertyChanged();
			}
		}

		public bool UseRtl
		{
			get => Owner.FlowDirection == FlowDirection.RightToLeft;
			set
			{
				var newValue = value ? FlowDirection.RightToLeft : FlowDirection.LeftToRight;
				if (newValue != Owner.FlowDirection)
				{
					Owner.FlowDirection = newValue;
					RaisePropertyChanged();
				}
			}
		}

		public bool UseDarkTheme
		{
			get => _useDarkTheme;
			set
			{
				_useDarkTheme = value;
				if (Owner.XamlRoot.Content is FrameworkElement root)
				{
					root.RequestedTheme = _useDarkTheme ? ElementTheme.Dark : ElementTheme.Light;
				}
				RaisePropertyChanged();
			}
		}

		public event System.ComponentModel.PropertyChangedEventHandler PropertyChanged;

		public bool IsRecordAllTests
		{
			get => _isRecordAllTests;
			set
			{
				if (value != _isRecordAllTests)
				{
					_isRecordAllTests = value;
					RaisePropertyChanged();
				}
			}
		}
	}
}
