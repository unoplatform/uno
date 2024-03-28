using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using Uno.Extensions;

namespace Windows.UI.Xaml.Data
{
	public partial class CollectionViewSource : DependencyObject
	{
		public CollectionViewSource()
		{
			InitializeBinder();
		}

		#region Dependency Properties
		public bool IsSourceGrouped
		{
			get { return (bool)this.GetValue(IsSourceGroupedProperty); }
			set { this.SetValue(IsSourceGroupedProperty, value); }
		}

		// Using a DependencyProperty as the backing store for IsSourceGrouped.  This enables animation, styling, binding, etc...
		public static DependencyProperty IsSourceGroupedProperty { get; } =
			DependencyProperty.Register(nameof(IsSourceGrouped), typeof(bool), typeof(CollectionViewSource), new FrameworkPropertyMetadata(defaultValue: false, propertyChangedCallback: (o, e) => ((CollectionViewSource)o).UpdateView()));

		public object Source
		{
			get { return (object)this.GetValue(SourceProperty); }
			set { this.SetValue(SourceProperty, value); }
		}

		// Using a DependencyProperty as the backing store for Source.  This enables animation, styling, binding, etc...
		public static DependencyProperty SourceProperty { get; } =
			DependencyProperty.Register(nameof(Source), typeof(object), typeof(CollectionViewSource), new FrameworkPropertyMetadata(defaultValue: null, propertyChangedCallback: (o, e) => ((CollectionViewSource)o).UpdateView()));

		private void UpdateView()
		{
			if (Source is IEnumerable enumerable)
			{
				View = new CollectionView(enumerable, IsSourceGrouped, ItemsPath);
			}
			else
			{
				View = null;
			}
		}

		#endregion

		public static DependencyProperty ViewProperty { get; } =
			Windows.UI.Xaml.DependencyProperty.Register(
				name: nameof(View),
				propertyType: typeof(ICollectionView),
				ownerType: typeof(CollectionViewSource),
				typeMetadata: new FrameworkPropertyMetadata(null)
			);

		public ICollectionView View
		{
			get => (ICollectionView)GetValue(ViewProperty);
			private set => SetValue(ViewProperty, value);
		}

		public global::Windows.UI.Xaml.PropertyPath ItemsPath
		{
			get => (PropertyPath)this.GetValue(ItemsPathProperty);
			set => this.SetValue(ItemsPathProperty, value);
		}

		public static DependencyProperty ItemsPathProperty { get; } =
			DependencyProperty.Register(
				name: nameof(ItemsPath),
				propertyType: typeof(PropertyPath),
				ownerType: typeof(CollectionViewSource),
				typeMetadata: new FrameworkPropertyMetadata(defaultValue: null, propertyChangedCallback: (o, e) => ((CollectionViewSource)o).UpdateView())
			);
	}
}
