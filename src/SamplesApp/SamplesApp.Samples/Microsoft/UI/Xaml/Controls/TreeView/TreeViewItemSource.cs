// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System;
using System.Collections.ObjectModel;
using System.ComponentModel;

namespace UITests.Shared.Microsoft_UI_Xaml_Controls.TreeViewTests
{
	public partial class TreeViewItemSource : INotifyPropertyChanged
	{
		public event PropertyChangedEventHandler PropertyChanged;
		public String Content { get; set; }
		public ObservableCollection<TreeViewItemSource> Children { get; set; }

		private bool m_isExpanded;
		public bool IsExpanded
		{
			get { return m_isExpanded; }
			set
			{
				if (m_isExpanded != value)
				{
					m_isExpanded = value;
					NotifyPropertyChanged("IsExpanded");
				}
			}
		}

		private bool m_isSelected;
		public bool IsSelected
		{
			get { return m_isSelected; }

			set
			{
				if (m_isSelected != value)
				{
					m_isSelected = value;
					NotifyPropertyChanged("IsSelected");
				}
			}

		}

		private bool m_hasUnrealizedChildren;
		public bool HasUnrealizedChildren
		{
			get { return m_hasUnrealizedChildren; }

			set
			{
				if (m_hasUnrealizedChildren != value)
				{
					m_hasUnrealizedChildren = value;
					NotifyPropertyChanged("HasUnrealizedChildren");
				}
			}
		}



		private void NotifyPropertyChanged(String propertyName)
		{
			if (PropertyChanged != null)
			{
				PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
			}
		}

		public TreeViewItemSource()
		{
			Children = new ObservableCollection<TreeViewItemSource>();
		}

	}
}
