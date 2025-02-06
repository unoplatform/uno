// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using Windows.UI.Xaml.Controls;
using MUXControlsTestApp.Samples.Model;
using System;
using System.Collections.ObjectModel;
using Uno.UI.Samples.Controls;

namespace MUXControlsTestApp
{
	[Sample("SelectorBar")]
	public sealed partial class SelectorBarSamplePage : TestPage
	{
		private ObservableCollection<Recipe> _colRemotePhotos = null;
		private ObservableCollection<Recipe> _colSharedPhotos = null;
		private ObservableCollection<Recipe> _colFavoritePhotos = null;

		public SelectorBarSamplePage()
		{
			this.InitializeComponent();

			_colRemotePhotos = new ObservableCollection<Recipe>();
			_colSharedPhotos = new ObservableCollection<Recipe>();
			_colFavoritePhotos = new ObservableCollection<Recipe>();

			int imageCount = 20;

			for (int itemIndex = 0; itemIndex < imageCount; itemIndex++)
			{
				_colRemotePhotos.Add(new Recipe()
				{
					ImageUri = new Uri(string.Format("ms-appx:///Images/vette{0}.jpg", itemIndex % 126 + 1))
				});
				_colSharedPhotos.Add(new Recipe()
				{
					ImageUri = new Uri(string.Format("ms-appx:///Images/vette{0}.jpg", itemIndex % 126 + imageCount + 1))
				});
				_colFavoritePhotos.Add(new Recipe()
				{
					ImageUri = new Uri(string.Format("ms-appx:///Images/vette{0}.jpg", itemIndex % 126 + 2 * imageCount + 1))
				});
			}
		}

		~SelectorBarSamplePage()
		{
		}

		private void SelectorBar_SelectionChanged(SelectorBar sender, SelectorBarSelectionChangedEventArgs args)
		{
			if (sender.SelectedItem == selectorBarItemRemote)
			{
				photos.ItemsSource = _colRemotePhotos;
			}
			else if (sender.SelectedItem == selectorBarItemShared)
			{
				photos.ItemsSource = _colSharedPhotos;
			}
			else
			{
				photos.ItemsSource = _colFavoritePhotos;
			}

			if (tblSelectedSelectorBarItem != null)
			{
				tblSelectedSelectorBarItem.Text = sender.SelectedItem == null ? "null" : sender.SelectedItem.Name;
			}
		}
	}
}
