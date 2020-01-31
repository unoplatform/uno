using System;
using System.Collections.Generic;
using System.Text;
using CoreGraphics;
using Foundation;
using Microsoft.Extensions.Logging;
using Uno.Extensions;
using Windows.Foundation;
using Windows.UI.Xaml.Data;
using Uno.Logging;
using System.Globalization;
using Uno.UI;
using Windows.UI.Xaml.Controls.Primitives;
using Uno.Diagnostics.Eventing;
#if __IOS__
using UIKit;
#else
using AppKit;
#endif

namespace Windows.UI.Xaml.Controls
{
	public partial class ListViewBaseSource
	{

		private readonly static IEventProvider _trace = Tracing.Get(TraceProvider.Id);

		public static class TraceProvider
		{
			public readonly static Guid Id = Guid.Parse("{EE64E62E-67BD-496A-A8B1-4A142642B3A3}");

			public const int ListViewBaseSource_GetCellStart = 1;
			public const int ListViewBaseSource_GetCellStop = 2;
		}

		/// <summary>
		/// Key used to represent a null DataTemplate in _templateCache and _templateCells dictionaries (because null is a not a valid key) 
		/// </summary>
		private readonly static DataTemplate _nullDataTemplateKey = new DataTemplate(() => null);

		/// <summary>
		/// We include one extra section to ensure Header and Footer can display even when we have no sections (ie an empty grouped source).
		/// </summary>
		internal const int SupplementarySections = 1;

		private DataTemplateSelector _currentSelector;
		private Dictionary<DataTemplate, CGSize> _templateCache = new Dictionary<DataTemplate, CGSize>(DataTemplate.FrameworkTemplateEqualityComparer.Default);
		private Dictionary<DataTemplate, NSString> _templateCells = new Dictionary<DataTemplate, NSString>(DataTemplate.FrameworkTemplateEqualityComparer.Default);
		/// <summary>
		/// The furthest item in the source which has already been materialized. Items up to this point can safely be retrieved.
		/// </summary>
		private NSIndexPath _lastMaterializedItem = NSIndexPath.FromItemSection(0, 0);

		private WeakReference<NativeListViewBase> _owner;
		public NativeListViewBase Owner
		{
			get { return _owner?.GetTarget(); }
			set { _owner = new WeakReference<NativeListViewBase>(value); }
		}


		public ListViewBaseSource(NativeListViewBase owner)
		{
			Owner = owner;
		}

		#region Overrides
#if __IOS__
		public override nint NumberOfSections(UICollectionView collectionView)
#else
		public override nint GetNumberOfSections(NSCollectionView collectionView)
#endif
		{
			var itemsSections = Owner.XamlParent.IsGrouping ?
				Owner.XamlParent.NumberOfDisplayGroups :
				1;
			return itemsSections + SupplementarySections;
		}

#if __IOS__
		public override nint GetItemsCount(UICollectionView collectionView, nint section)
#else
		public override nint GetNumberofItems(NSCollectionView collectionView, nint section)
#endif
		{
			int count;
			if (Owner.XamlParent.IsGrouping)
			{
				count = GetGroupedItemsCount(section);
			}
			else
			{
				count = GetUngroupedItemsCount(section);
			}

			if (this.Log().IsEnabled(Microsoft.Extensions.Logging.LogLevel.Debug))
			{
				this.Log().Debug($"Count requested for section {section}, returning {count}");
			}
			return count;
		}

		private int GetUngroupedItemsCount(nint section)
		{
			int count;
			if (section == 0)
			{
				count = Owner.XamlParent.NumberOfItems;
			}
			else
			{
				// Extra section added to accommodate header+footer, contains no items.
				count = 0;
			}

			return count;
		}

		private int GetGroupedItemsCount(nint section)
		{
			if ((int)section >= Owner.XamlParent.NumberOfDisplayGroups)
			{
				// Header+footer section which is empty
				return 0;
			}
			return Owner.XamlParent.GetDisplayGroupCount((int)section);
		}
#if __IOS__
		public override UICollectionViewCell GetCell(UICollectionView collectionView, NSIndexPath indexPath)
#else
		public override NSCollectionViewItem GetItem(NSCollectionView collectionView, NSIndexPath indexPath)
#endif
		{
			using (
			   _trace.WriteEventActivity(
				   TraceProvider.ListViewBaseSource_GetCellStart,
				   TraceProvider.ListViewBaseSource_GetCellStop
			   )
			)
			{
				// Marks this item to be materialized, so its actual measured size 
				// is used during the calculation of the layout.
				// This is required for paged lists, so that the layout calculation
				// does not eagerly get all the items of the ItemsSource.
				UpdateLastMaterializedItem(indexPath);

				var index = Owner?.XamlParent?.GetIndexFromIndexPath(IndexPath.FromNSIndexPath(indexPath)) ?? -1;

				var identifier = GetReusableCellIdentifier(indexPath);

				var listView = (NativeListViewBase)collectionView;
#if __IOS__
				var cell = (ListViewBaseInternalContainer)collectionView.DequeueReusableCell(identifier, indexPath);
#else
				var cell = (ListViewBaseInternalContainer)collectionView.MakeItem(GetReusableCellIdentifier(indexPath), indexPath);
#endif

				using (cell.InterceptSetNeedsLayout())
				{
					var selectorItem = cell.Content as SelectorItem;

					if (selectorItem == null ||
						// If it's not a generated container then it must be an item that returned true for IsItemItsOwnContainerOverride (eg an
						// explicitly-defined ListViewItem), and shouldn't be recycled for a different item.
						!selectorItem.IsGeneratedContainer)
					{
						cell.Owner = Owner;
						selectorItem = Owner?.XamlParent?.GetContainerForIndex(index) as SelectorItem;
						cell.Content = selectorItem;
						if (this.Log().IsEnabled(Microsoft.Extensions.Logging.LogLevel.Debug))
						{
							this.Log().Debug($"Creating new view at indexPath={indexPath}.");
						}

						FrameworkElement.InitializePhaseBinding(selectorItem);

						// Ensure the item has a parent, since it's added to the native collection view
						// which does not automatically sets the parent DependencyObject.
						selectorItem.SetParent(Owner?.XamlParent);
					}
					else if (this.Log().IsEnabled(Microsoft.Extensions.Logging.LogLevel.Debug))
					{
						this.Log().Debug($"Reusing view at indexPath={indexPath}, previously bound to {selectorItem.DataContext}.");
					}

					Owner?.XamlParent?.PrepareContainerForIndex(selectorItem, index);

					// Normally this happens when the SelectorItem.Content is set, but there's an edge case where after a refresh, a
					// container can be dequeued which happens to have had exactly the same DataContext as the new item.
					cell.ClearMeasuredSize();
				}

				Owner?.XamlParent?.TryLoadMoreItems(index);

				return cell;
			}
		}

		/// <summary>
		/// Update record of the furthest item materialized.
		/// </summary>
		/// <param name="newItem">Item currently being materialized.</param>
		/// <returns>True if the value has changed and the layout would change.</returns>
		internal bool UpdateLastMaterializedItem(NSIndexPath newItem)
		{
			if (newItem.Compare(_lastMaterializedItem) > 0)
			{
				_lastMaterializedItem = newItem;
				return Owner.ItemTemplateSelector != null; ;
			}
			else
			{
				return false;
			}
		}

#if __IOS__ //TODO
#if __IOS__
		public override UICollectionReusableView GetViewForSupplementaryElement(
			UICollectionView collectionView,
			NSString elementKind,
			NSIndexPath indexPath)
#else
		public override NSView GetView(NSCollectionView collectionView, NSString elementKind, NSIndexPath indexPath)
#endif
		{
			var listView = (NativeListViewBase)collectionView;

			if (elementKind == NativeListViewBase.ListViewHeaderElementKind)
			{
				return GetBindableSupplementaryView(
					collectionView: listView,
					elementKind: NativeListViewBase.ListViewHeaderElementKindNS,
					indexPath: indexPath,
					reuseIdentifier: NativeListViewBase.ListViewHeaderReuseIdentifierNS,
					context: listView.Header,
					template: listView.HeaderTemplate,
					style: null
				);
			}

			else if (elementKind == NativeListViewBase.ListViewFooterElementKind)
			{
				return GetBindableSupplementaryView(
					collectionView: listView,
					elementKind: NativeListViewBase.ListViewFooterElementKindNS,
					indexPath: indexPath,
					reuseIdentifier: NativeListViewBase.ListViewFooterReuseIdentifierNS,
					context: listView.Footer,
					template: listView.FooterTemplate,
					style: null
				);
			}

			else if (elementKind == NativeListViewBase.ListViewSectionHeaderElementKind)
			{
				// Ensure correct template can be retrieved
				UpdateLastMaterializedItem(indexPath);

				return GetBindableSupplementaryView(
					collectionView: listView,
					elementKind: NativeListViewBase.ListViewSectionHeaderElementKindNS,
					indexPath: indexPath,
					reuseIdentifier: NativeListViewBase.ListViewSectionHeaderReuseIdentifierNS,
					//ICollectionViewGroup.Group is used as context for sectionHeader
					context: listView.XamlParent.GetGroupAtDisplaySection((int)indexPath.Section).Group,
					template: GetTemplateForGroupHeader((int)indexPath.Section),
					style: listView.GroupStyle?.HeaderContainerStyle
				);
			}

			else
			{
				throw new NotSupportedException("Unsupported element kind: {0}".InvariantCultureFormat(elementKind));
			}
		}

		private _ReusableView GetBindableSupplementaryView(
			NativeListViewBase collectionView,
			NSString elementKind,
			NSIndexPath indexPath,
			NSString reuseIdentifier,
			object context,
			DataTemplate template,
			Style style)
		{
			var supplementaryView = (ListViewBaseInternalContainer)collectionView.DequeueReusableSupplementaryView(
				elementKind,
				reuseIdentifier,
				indexPath);

			using (supplementaryView.InterceptSetNeedsLayout())
			{
				if (supplementaryView.Content == null)
				{
					supplementaryView.Owner = Owner;
					var content = CreateContainerForElementKind(elementKind);
					content.HorizontalContentAlignment = HorizontalAlignment.Stretch;
					content.VerticalContentAlignment = VerticalAlignment.Stretch;
					supplementaryView.Content = content
						.Binding("Content", "");
				}
				supplementaryView.Content.ContentTemplate = template;
				supplementaryView.Content.DataContext = context;
				if (style != null)
				{
					supplementaryView.Content.Style = style;
				}
			}

			return supplementaryView;
		}
#endif
		#endregion

		/// <summary>
		/// Visual element which stops layout requests from propagating up. Used for measuring templates.
		/// </summary>
		private BlockLayout BlockLayout { get; } = new BlockLayout();

		internal CGSize GetHeaderSize(Size availableSize)
		{
			return Owner.HeaderTemplate != null ? GetTemplateSize(Owner.HeaderTemplate, NativeListViewBase.ListViewHeaderElementKindNS, availableSize) : CGSize.Empty;
		}

		internal CGSize GetFooterSize(Size availableSize)
		{
			return Owner.FooterTemplate != null ? GetTemplateSize(Owner.FooterTemplate, NativeListViewBase.ListViewFooterElementKindNS, availableSize) : CGSize.Empty;
		}

		internal CGSize GetSectionHeaderSize(int section, Size availableSize)
		{
			var template = GetTemplateForGroupHeader(section);
			return template.SelectOrDefault(ht => GetTemplateSize(ht, NativeListViewBase.ListViewSectionHeaderElementKindNS, availableSize), CGSize.Empty);
		}


		internal CGSize GetItemSize(NativeListViewBase collectionView, NSIndexPath indexPath, Size availableSize)
		{
			DataTemplate itemTemplate = GetTemplateForItem(indexPath);

			if (_currentSelector != Owner.ItemTemplateSelector)
			{
				// If the templateSelector has changed, clear the cache
				_currentSelector = Owner.ItemTemplateSelector;
				_templateCache.Clear();
				_templateCells.Clear();
			}

			var size = GetTemplateSize(itemTemplate, NativeListViewBase.ListViewItemElementKindNS, availableSize);

			if (size == CGSize.Empty)
			{
				// The size of the template is usually empty for items that have not been displayed yet when using ItemTemplateSelector.
				// The reason why we can't measure the template is because we do not resolve it, 
				// as it would require enumerating through all items of a possibly virtualized ItemsSource.
				// To ensure a first display (after which they will be properly measured), we need them to have a non-empty size. 
				size = new CGSize(44, 44); // 44 is the default MinHeight/MinWidth of ListViewItem/GridViewItem on UWP.
			}

			return size;
		}

		private DataTemplate GetTemplateForItem(NSIndexPath indexPath)
		{
			if (IsMaterialized(indexPath))
			{
				return Owner?.XamlParent?.ResolveItemTemplate(Owner.XamlParent.GetDisplayItemFromIndexPath(indexPath.ToIndexPath()));
			}
			else
			{
				// Ignore ItemTemplateSelector since we do not know what the item is
				return Owner?.XamlParent?.ItemTemplate;
			}
		}

		private DataTemplate GetTemplateForGroupHeader(int section)
		{
			var groupStyle = Owner.GroupStyle;
			if (IsMaterialized(section))
			{
				return DataTemplateHelper.ResolveTemplate(
					groupStyle?.HeaderTemplate,
					groupStyle?.HeaderTemplateSelector,
					Owner.XamlParent.GetGroupAtDisplaySection(section).Group,
					Owner);
			}
			else
			{
				return groupStyle?.HeaderTemplate;
			}
		}

		/// <summary>
		/// Gets the actual item template size, using a non-databound materialized
		/// view of the template.
		/// </summary>
		/// <param name="dataTemplate">A data template</param>
		/// <returns>The actual size of the template</returns>
		private CGSize GetTemplateSize(DataTemplate dataTemplate, NSString elementKind, Size availableSize)
		{
			CGSize size;

			// Cache the sizes to avoid creating new templates every time.
			if (!_templateCache.TryGetValue(dataTemplate ?? _nullDataTemplateKey, out size))
			{
				var container = CreateContainerForElementKind(elementKind);

				// Force a null DataContext so the parent's value does not flow
				// through when temporarily adding the container to Owner.XamlParent
				container.SetValue(FrameworkElement.DataContextProperty, null);

				Style style = null;
				if (elementKind == NativeListViewBase.ListViewItemElementKind)
				{
					style = Owner.ItemContainerStyle;
				}
				else if (elementKind == NativeListViewBase.ListViewSectionHeaderElementKind)
				{
					style = Owner.GroupStyle?.HeaderContainerStyle;
				}
				if (style != null)
				{
					container.Style = style;
				}

				container.ContentTemplate = dataTemplate;
				try
				{
					// Attach templated container to visual tree while measuring. This works around the bug that default Style is not 
					// applied until view is loaded.
					Owner.XamlParent.AddSubview(BlockLayout);
					BlockLayout.AddSubview(container);
					// Measure with PositiveInfinity rather than MaxValue, since some views handle this better.
					size = Owner.NativeLayout.Layouter.MeasureChild(container, availableSize);

					if ((size.Height > nfloat.MaxValue / 2 || size.Width > nfloat.MaxValue / 2) &&
						this.Log().IsEnabled(LogLevel.Warning)
					)
					{
						this.Log().LogWarning($"Infinite item size reported, this can crash native collection view.");
					}
				}
				finally
				{
					Owner.XamlParent.RemoveChild(BlockLayout);
					BlockLayout.RemoveChild(container);

					// Reset the DataContext for reuse.
					container.ClearValue(FrameworkElement.DataContextProperty);
				}

				_templateCache[dataTemplate ?? _nullDataTemplateKey] = size;
			}

			return size;
		}

		/// <summary>
		/// Get item container corresponding to an element kind (header, footer, list item, etc)
		/// </summary>
		private ContentControl CreateContainerForElementKind(NSString elementKind)
		{
			if (elementKind == NativeListViewBase.ListViewSectionHeaderElementKindNS)
			{
				return Owner?.XamlParent?.GetGroupHeaderContainer(null);
			}
			else if (elementKind == NativeListViewBase.ListViewItemElementKindNS)
			{
				return Owner?.XamlParent?.GetContainerForIndex(-1) as ContentControl;
			}
			else
			{
				//Used for header and footer
				return ContentControl.CreateItemContainer();
			}
		}

		internal void ReloadData()
		{
			_lastMaterializedItem = NSIndexPath.FromItemSection(0, 0);
		}

		/// <summary>
		/// Determine the identifier to use for the dequeuing.
		/// This avoid for already materialized items with a specific template
		/// to switch to a different template, and have to re-create the content 
		/// template.
		/// </summary>
		private NSString GetReusableCellIdentifier(NSIndexPath indexPath)
		{
			NSString identifier;
			var template = GetTemplateForItem(indexPath);

			if (!_templateCells.TryGetValue(template ?? _nullDataTemplateKey, out identifier))
			{
				identifier = new NSString(_templateCache.Count.ToString(CultureInfo.InvariantCulture));
				_templateCells[template ?? _nullDataTemplateKey] = identifier;

#if __IOS__
				Owner.RegisterClassForCell(typeof(ListViewBaseInternalContainer), identifier);
#else
				Owner.RegisterClassForItem(typeof(ListViewBaseInternalContainer), identifier);
#endif
			}

			return identifier;
		}

		/// <summary>
		/// Is item in the range of already-materialized items?
		/// </summary>
		private bool IsMaterialized(NSIndexPath itemPath) => itemPath.Compare(_lastMaterializedItem) <= 0;

		// Consider group header to be materialized if first item in group is materialized
		private bool IsMaterialized(int section) => section <= _lastMaterializedItem.Section;
	}
}
