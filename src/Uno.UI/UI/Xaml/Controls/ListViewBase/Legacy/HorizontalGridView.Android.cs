using Android.Graphics.Drawables;
using Uno.Extensions;
using Uno.Foundation.Logging;
using Uno.UI.DataBinding;
using Windows.UI.Xaml.Data;
using System;
using System.Collections;
using System.ComponentModel;
using Uno.Disposables;
using System.Runtime.CompilerServices;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml;

namespace Uno.UI.Controls.Legacy
{
	public partial class HorizontalGridView : Uno.UI.Controls.HorizontalGridView, IListView, DependencyObject
	{
		public HorizontalGridView()
		{
			InitializeBinder();
			IFrameworkElementHelper.Initialize(this);
		}

		public new object ItemsSource
		{
			get { return base.ItemsSource; }
			set { base.ItemsSource = value as IEnumerable; }
		}

		public new DataTemplate ItemTemplate
		{
			get { return base.ItemTemplate; }
			set { base.ItemTemplate = value; }
		}

		public DataTemplateSelector ItemTemplateSelector
		{
			get { return base.BindableAdapter.ItemTemplateSelector; }
			set { base.BindableAdapter.ItemTemplateSelector = value; }
		}

		public ItemsPanelTemplate ItemsPanel { get; set; }

		public Android.Graphics.Color DividerColor
		{
			get { return base.BindableAdapter.DividerColor; }
			set { base.BindableAdapter.DividerColor = value; }
		}

		public bool IsItemClickEnabled { get; set; }
		object IListView.SelectedItem { get; set; }
	}
}
