using System;
using System.Threading;
using System.Threading.Tasks;
using System.Reflection;
using System.Linq;
using System.Collections.Generic;
using System.Diagnostics;
using SampleControl.Entities;
using Uno.UI.Samples.Controls;
using Uno.UI.Samples.Entities;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Globalization;
using Windows.UI.Xaml.Data;
using Windows.ApplicationModel.Core;
using Windows.UI.Core;
using Windows.Storage;
using Uno.Extensions;
using Uno.Logging;
using Microsoft.Extensions.Logging;
using Windows.UI.Xaml;
using System.IO;
using Uno.Disposables;
using System.ComponentModel;
using Uno.UI.Common;

#if XAMARIN || NETSTANDARD2_0
using Windows.UI.Xaml.Controls;
#else
using Windows.Graphics.Imaging;
using Windows.Graphics.Display;
using Windows.UI.Xaml.Media;
using Windows.UI;
using Windows.UI.Xaml.Controls;
#endif

namespace SampleControl.Presentation
{
	public partial class SampleChooserViewModel : INotifyPropertyChanged
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

		private void RaisePropertyChanged([System.Runtime.CompilerServices.CallerMemberName] string propertyName = "")
		{
			PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
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


		public event PropertyChangedEventHandler PropertyChanged;
	}
}
