using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Input;
using Uno.Extensions;
using Uno.UI.Views.Controls;
using Windows.UI.Xaml.Data;
using Uno.UI.Converters;
using Uno.Client;
using System.Threading.Tasks;
using Uno.Diagnostics.Eventing;
using Uno.UI.Controls;
using Windows.UI.Core;
using System.Globalization;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Uno.Extensions.Specialized;
using Uno.UI.Extensions;
using ObjCRuntime;

using Foundation;
using UIKit;
using CoreGraphics;

namespace Uno.UI.Controls.Legacy
{
	[Bindable]
	public abstract partial class ListViewBaseSource : UICollectionViewSource
	{
		/// <summary>
		/// Key used to represent a null DataTemplate in _templateCache and _templateCells dictionaries (because null is a not a valid key) 
		/// </summary>
		private readonly static DataTemplate _nullDataTemplateKey = new DataTemplate(() => null);

		private readonly static IEventProvider _trace = Tracing.Get(TraceProvider.Id);

		public static class TraceProvider
		{
			public readonly static Guid Id = Guid.Parse("{EE64E62E-67BD-496A-A8B1-4A142642B3A3}");

			public const int ListViewBaseSource_GetCellStart = 1;
			public const int ListViewBaseSource_GetCellStop = 2;
		}


		#region Members
		private DataTemplateSelector _currentSelector;
		private Dictionary<DataTemplate, CGSize> _templateCache = new Dictionary<DataTemplate, CGSize>(DataTemplate.FrameworkTemplateEqualityComparer.Default);
		private Dictionary<DataTemplate, NSString> _templateCells = new Dictionary<DataTemplate, NSString>(DataTemplate.FrameworkTemplateEqualityComparer.Default);
		private Dictionary<InternalContainer, List<Action>> _onRecycled = new Dictionary<InternalContainer, List<Action>>();
		private IEnumerable _items;
		#endregion

		public ListViewBaseSource()
		{
		}

		public TimeSpan MinTimeBetweenPressStates { get; } = TimeSpan.FromMilliseconds(100);

		#region Properties

		public ListViewSelectionMode SelectionMode
		{
			get;
			set;
		}

		public IEnumerable Items
		{
			get
			{
				return GroupedSource?.SelectManyUntyped(g => g)
					?? _items;

			}
			set
			{
				_items = value ?? Enumerable.Empty<object>();
				_materializedItems.Clear();
			}
		}

		/// <summary>
		/// Grouped source, if any, including groups even if they may not be visible to the UICollectionView (because they are empty and HidesIfEmpty is true).
		/// </summary>
		internal IEnumerable<IEnumerable> UnfilteredGroupedSource => (_items as ICollectionView)?.GetCollectionGroups();

		internal IEnumerable<IEnumerable> GroupedSource
		{
			get
			{
				var groupedSource = UnfilteredGroupedSource;

				if (Owner.GroupStyle?.HidesIfEmpty ?? false)
				{
					// removes empty groups
					groupedSource = groupedSource?
									.Where(group => group.Any())
									.ToList();
				}
				return groupedSource;
			}
		}

		internal bool AreEmptyGroupsHidden => UnfilteredGroupedSource != null && (Owner.GroupStyle?.HidesIfEmpty ?? false);

		private WeakReference<ListViewBase> _owner;

		public ListViewBase Owner
		{
			get { return _owner?.GetTarget(); }
			set { _owner = new WeakReference<ListViewBase>(value); }
		}

		public event SelectionChangedEventHandler SelectionChanged;
		public event ItemClickEventHandler ItemClick;

		public ICommand ItemClickCommand
		{
			get;
			set;
		}

		#endregion

		public ListViewBaseSource(ListViewBase owner)
		{
			Owner = owner;
		}

		#region Overrides
		public override nint NumberOfSections(UICollectionView collectionView)
		{
			return GroupedSource?.Count() == 0 ? 1 : GroupedSource?.Count() ?? 1;
		}

		public override nint GetItemsCount(UICollectionView collectionView, nint section)
		{
			return GroupedSource?.ElementAtOrDefault((int)section)?.Count() ?? Items.Count();
		}

		public override bool ShouldSelectItem(UICollectionView collectionView, NSIndexPath indexPath)
		{
			var listViewBase = (ListViewBase)collectionView;

			if (collectionView.AllowsSelection)
			{
				switch (SelectionMode)
				{
					case ListViewSelectionMode.Single:
						HandleSingleSelection(listViewBase, indexPath);
						break;

					case ListViewSelectionMode.Extended:
					case ListViewSelectionMode.Multiple:
						HandleMultipleSelection(listViewBase, indexPath);
						break;
				}
			}

			if (listViewBase.IsItemClickEnabled)
			{
				var item = GetItem(listViewBase, indexPath);
				ItemClickCommand.ExecuteIfPossible(item);
				ItemClick?.Invoke(Owner, new ItemClickEventArgs() { ClickedItem = item });
			}

			return false;
		}

		public override void CellDisplayingEnded(UICollectionView collectionView, UICollectionViewCell cell, NSIndexPath indexPath)
		{
			var key = cell as InternalContainer;

			if (_onRecycled.TryGetValue(key, out var actions))
			{
				actions.ForEach(a => a());
				_onRecycled.Remove(key);
			}
		}

		/// <summary>
		/// Queues an action to be executed when the provided viewHolder is being recycled.
		/// </summary>
		internal void RegisterForRecycled(InternalContainer container, Action action)
		{
			if (!_onRecycled.TryGetValue(container, out var actions))
			{
				_onRecycled[container] = actions = new List<Action>();
			}

			actions.Add(action);
		}

		public override UICollectionViewCell GetCell(UICollectionView collectionView, NSIndexPath indexPath)
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
				if (!_materializedItems.Contains(indexPath))
				{
					if (Owner?.ItemTemplateSelector != null)
					{
						//Request relayout so that item is measured with its actual template
						Owner.Layout.InvalidateLayout();
					}
					_materializedItems.Add(indexPath);
				}

				var item = GetItem(collectionView, indexPath);

				var identifier = GetReusableCellIdentifier(indexPath);

				var listView = (ListViewBase)collectionView;
				var cell = (InternalContainer)collectionView.DequeueReusableCell(identifier, indexPath);

				var selectorItem = cell.Content as SelectorItem;

				if (selectorItem == null)
				{
					selectorItem = CreateSelectorItem();
					selectorItem.Style = listView.ItemContainerStyle;
					selectorItem.Binding("Content", "");
					cell.Content = selectorItem;

					FrameworkElement.InitializePhaseBinding(selectorItem);
				}

				selectorItem.ContentTemplateSelector = listView.ItemTemplateSelector;
				selectorItem.ContentTemplate = listView.ItemTemplate;

				cell.Content.DataContext = item;
				switch (listView.SelectionMode)
				{
					case ListViewSelectionMode.Single:
						selectorItem.IsSelected = object.Equals(listView.SelectedItem, item);
						break;

					case ListViewSelectionMode.Multiple:
					case ListViewSelectionMode.Extended:
						selectorItem.IsSelected = (listView.SelectedItems?.Contains(item)).GetValueOrDefault(false);
						break;

					default:
						selectorItem.IsSelected = false;
						break;
				}

				return cell;
			}
		}

		public override void WillDisplayCell(UICollectionView collectionView, UICollectionViewCell cell, NSIndexPath indexPath)
		{
			var container = cell as InternalContainer;
			FrameworkElement.RegisterPhaseBinding(container.Content, a => RegisterForRecycled(container, a));
		}

		public override bool ShouldHighlightItem(UICollectionView collectionView, NSIndexPath indexPath)
		{
			var listView = (ListViewBase)collectionView;
			return listView.IsItemClickEnabled ||
				   listView.SelectionMode != ListViewSelectionMode.None;
		}

		public override void ItemUnhighlighted(UICollectionView collectionView, NSIndexPath indexPath)
		{
			Press(collectionView, indexPath, false);
		}

		public override void ItemHighlighted(UICollectionView collectionView, NSIndexPath indexPath)
		{
			Press(collectionView, indexPath, true);
		}

		private void Press(UICollectionView collectionView, NSIndexPath indexPath, bool value)
		{
			var cell = collectionView.CellForItem(indexPath);
			var selectorItem = ((InternalContainer)cell).Content as SelectorItem;

			if (selectorItem != null)
			{
				if (value)
				{
					selectorItem.LegacySetPressed(true);
				}
				else
				{
					_ = CoreDispatcher.Main
						.RunAsync(
							CoreDispatcherPriority.Normal,
							async () =>
							{
								await Task.Delay(MinTimeBetweenPressStates);
								selectorItem.LegacySetPressed(false);
							}
						);
				}
			}
		}

		public override UICollectionReusableView GetViewForSupplementaryElement(
			UICollectionView collectionView,
			NSString elementKind,
			NSIndexPath indexPath)
		{
			var listView = (ListViewBase)collectionView;

			if (elementKind == ListViewBase.ListViewHeaderElementKind)
			{
				var headerContext = listView.IsDependencyPropertySet(ListViewBase.HeaderProperty)
				  ? listView.GetValue(ListViewBase.HeaderProperty)
				  : listView.DataContext;

				return GetBindableSupplementaryView(
					collectionView: collectionView,
					elementKind: ListViewBase.ListViewHeaderElementKindNS,
					indexPath: indexPath,
					reuseIdentifier: ListViewBase.ListViewHeaderReuseIdentifierNS,
					context: headerContext,
					template: listView.HeaderTemplate,
					style: null
				);
			}

			else if (elementKind == ListViewBase.ListViewFooterElementKind)
			{
				var footerContext = listView.IsDependencyPropertySet(ListViewBase.FooterProperty)
				  ? listView.GetValue(ListViewBase.FooterProperty)
				  : listView.DataContext;

				return GetBindableSupplementaryView(
					collectionView: collectionView,
					elementKind: ListViewBase.ListViewFooterElementKindNS,
					indexPath: indexPath,
					reuseIdentifier: ListViewBase.ListViewFooterReuseIdentifierNS,
					context: footerContext,
					template: listView.FooterTemplate,
					style: null
				);
			}

			else if (elementKind == ListViewBase.ListViewSectionHeaderElementKind)
			{
				return GetBindableSupplementaryView(
					collectionView: collectionView,
					elementKind: ListViewBase.ListViewSectionHeaderElementKindNS,
					indexPath: indexPath,
					reuseIdentifier: ListViewBase.ListViewSectionHeaderReuseIdentifierNS,
					//IGrouping is used as context for sectionHeader
					context: listView.Source.GroupedSource?.ElementAtOrDefault(indexPath.Section),
					template: listView.GroupStyle?.HeaderTemplate,
					style: listView.GroupStyle?.HeaderContainerStyle
				);
			}

			else
			{
				throw new NotSupportedException("Unsupported element kind: {0}".InvariantCultureFormat(elementKind));
			}
		}

		private UICollectionReusableView GetBindableSupplementaryView(
			UICollectionView collectionView,
			NSString elementKind,
			NSIndexPath indexPath,
			NSString reuseIdentifier,
			object context,
			DataTemplate template,
			Style style)
		{
			var supplementaryView = (InternalContainer)collectionView.DequeueReusableSupplementaryView(
				elementKind,
				reuseIdentifier,
				indexPath);

			if (supplementaryView.Content == null)
			{
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

			return supplementaryView;
		}

		#endregion

		/// <summary>
		/// Get item container corresponding to an element kind (header, footer, list item, etc)
		/// </summary>
		private ContentControl CreateContainerForElementKind(NSString elementKind)
		{
			if (elementKind == ListViewBase.ListViewSectionHeaderElementKindNS)
			{
				return CreateSectionHeaderItem();
			}
			else if (elementKind == ListViewBase.ListViewItemElementKindNS)
			{
				return CreateSelectorItem();
			}
			else
			{
				//Used for header and footer
				return ContentControl.CreateItemContainer();
			}
		}

		protected abstract ListViewBaseHeaderItem CreateSectionHeaderItem();

		protected abstract SelectorItem CreateSelectorItem();

		protected virtual void OnSelectionChanged(SelectionChangedEventArgs e)
		{
			SelectionChanged?.Invoke(this, e);
		}

		public virtual object GetItem(UICollectionView collectionView, NSIndexPath indexPath)
		{
			var section = GroupedSource?.ElementAtOrDefault(indexPath.Section);
			if (section != null)
			{
				return section.Cast<object>().ElementAtOrDefault(indexPath.Row);
			}
			else
			{
				return Items.ElementAtOrDefault(indexPath.Row);
			}
		}

		private void HandleMultipleSelection(ListViewBase listViewBase, NSIndexPath indexPath)
		{
			var item = GetItem(listViewBase, indexPath);

			var originalList = listViewBase.SelectedItems;

			var isSelected = !originalList.Contains(item);

			if (isSelected)
			{
				listViewBase.SelectedItems = originalList.Concat(item).ToArray();
			}
			else
			{
				listViewBase.SelectedItems = originalList.Except(item).ToArray();
			}
		}

		private void HandleSingleSelection(ListViewBase listViewBase, NSIndexPath indexPath)
		{
			var itemToSelect = GetItem(listViewBase, indexPath);
			var currentlySelectedItem = listViewBase.SelectedItem;

			if (listViewBase.UnselectOnClick && object.Equals(itemToSelect, currentlySelectedItem))
			{
				listViewBase.SelectedItem = null;
			}
			else
			{
				listViewBase.SelectedItem = itemToSelect;
			}
		}

		internal CGSize GetHeaderSize()
		{
			return Owner.HeaderTemplate != null ? GetTemplateSize(Owner.HeaderTemplate, ListViewBase.ListViewHeaderElementKindNS) : CGSize.Empty;
		}

		internal CGSize GetFooterSize()
		{
			return Owner.FooterTemplate != null ? GetTemplateSize(Owner.FooterTemplate, ListViewBase.ListViewFooterElementKindNS) : CGSize.Empty;
		}

		internal CGSize GetSectionHeaderSize()
		{
			return (Owner.GroupStyle?.HeaderTemplate).SelectOrDefault(ht => GetTemplateSize(ht, ListViewBase.ListViewSectionHeaderElementKindNS), CGSize.Empty);
		}

		HashSet<NSIndexPath> _materializedItems = new HashSet<NSIndexPath>();

		public virtual CGSize GetItemSize(UICollectionView collectionView, NSIndexPath indexPath)
		{
			DataTemplate itemTemplate = GetTemplateForItem(indexPath);

			if (_currentSelector != Owner.ItemTemplateSelector)
			{
				// If the templateSelector has changed, clear the cache
				_currentSelector = Owner.ItemTemplateSelector;
				_templateCache.Clear();
				_templateCells.Clear();
			}

			var size = GetTemplateSize(itemTemplate, ListViewBase.ListViewItemElementKindNS);

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
			if (_materializedItems.Contains(indexPath))
			{
				return Owner?.ResolveItemTemplate(GetItem(Owner, indexPath));
			}
			else
			{
				// Ignore ItemTemplateSelector since we do not know what the item is
				return Owner?.ItemTemplate;
			}
		}

		/// <summary>
		/// Gets the actual item template size, using a non-databound materialized
		/// view of the template.
		/// </summary>
		/// <param name="dataTemplate">A data template</param>
		/// <returns>The actual size of the template</returns>
		private CGSize GetTemplateSize(DataTemplate dataTemplate, NSString elementKind)
		{
			CGSize size;

			// Cache the sizes to avoid creating new templates every time.
			if (!_templateCache.TryGetValue(dataTemplate ?? _nullDataTemplateKey, out size))
			{
				var container = CreateContainerForElementKind(elementKind);

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
				size = container.SizeThatFits(new CGSize(double.MaxValue, double.MaxValue));

				_templateCache[dataTemplate ?? _nullDataTemplateKey] = size;
			}

			return size;
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

				Owner.RegisterClassForCell(typeof(InternalContainer), identifier);
			}

			return identifier;
		}

		/// <summary>
		/// A hiddeen root item that allows the reuse of a ContentControl features.
		/// </summary>
		[global::Foundation.Register("LegacyInternalContainer")]
		internal class InternalContainer : UICollectionViewCell
		{
			/// <summary>
			/// Native constructor, do not use explicitly.
			/// </summary>
			/// <remarks>
			/// Used by the Xamarin Runtime to materialize native 
			/// objects that may have been collected in the managed world.
			/// </remarks>
			public InternalContainer(NativeHandle handle) : base(handle) { }

			private CGSize _lastUsedSize;

			protected override void Dispose(bool disposing)
			{
				if (!disposing)
				{
					GC.ReRegisterForFinalize(this);

					_ = Windows.UI.Core.CoreDispatcher.Main.RunIdleAsync(_ => Dispose());
				}
				else
				{
					// We need to explicitly remove the content before being disposed
					// otherwise, the children will try to reference ContentView which 
					// will have been disposed.

					foreach (var v in ContentView.Subviews)
					{
						v.RemoveFromSuperview();
					}

					base.Dispose(disposing);
				}
			}

			public ContentControl Content
			{
				get
				{
					return ContentView.Subviews.FirstOrDefault() as ContentControl;
				}
				set
				{
					if (ContentView.Subviews.Any())
					{
						ContentView.Subviews[0].RemoveFromSuperview();
					}

					value.Frame = ContentView.Bounds;
					value.AutoresizingMask = UIViewAutoresizing.FlexibleWidth | UIViewAutoresizing.FlexibleHeight;

					ContentView.AddSubview(value);
				}
			}

			public override void ApplyLayoutAttributes(UICollectionViewLayoutAttributes layoutAttributes)
			{
				// We don't call base because it adds unnecessary interop. From the doc: "The default implementation of this method does nothing." https://developer.apple.com/library/ios/documentation/UIKit/Reference/UICollectionReusableView_class/#//apple_ref/occ/instm/UICollectionReusableView/applyLayoutAttributes:
				//base.ApplyLayoutAttributes(layoutAttributes);

				var size = layoutAttributes.Frame.Size;
				// If frame size has changed, call SizeThatFits on item content. This allows views that expect a measure prior to an arrange (eg StackPanel, StarStackPanel)
				// to be laid out correctly.
				if (size != _lastUsedSize)
				{
					Content?.SizeThatFits(size);
					_lastUsedSize = size;
				}
			}
		}
	}
}

