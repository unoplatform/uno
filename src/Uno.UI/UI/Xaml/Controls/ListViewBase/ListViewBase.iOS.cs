using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Windows.Input;
using CoreGraphics;
using Foundation;
using UIKit;
using Windows.UI.Xaml.Data;
using Uno.Extensions;
using System.Collections.Specialized;
using Uno.Disposables;

using Windows.UI.Xaml.Controls.Primitives;
using Uno.Foundation.Logging;

namespace Windows.UI.Xaml.Controls
{
	public partial class ListViewBase
	{
		private readonly SerialDisposable _callbackSubscriptions = new SerialDisposable();

		/// <summary>
		/// This Uno-only flag sets whether the native list animates collection changes (insertions, deletions, etc). The default is true.
		/// </summary>
		[Uno.UnoOnly]
		public bool UseCollectionAnimations { get; set; } = true;

		private bool _animateScrollIntoView = Uno.UI.FeatureConfiguration.ListViewBase.AnimateScrollIntoView;
		public bool AnimateScrollIntoView
		{
			get { return _animateScrollIntoView; }
			set
			{
				_animateScrollIntoView = value;
				if (NativePanel != null)
				{
					NativePanel.AnimateScrollIntoView = value;
				}
			}
		}

		private void InitializeNativePanel(NativeListViewBase panel)
		{
			var source = new ListViewBaseSource(panel);
			panel.Source = source;
			panel.NativeLayout.Source = new WeakReference<ListViewBaseSource>(panel.Source);

			BindToPanel(panel, nameof(ItemsSource));

			panel.AnimateScrollIntoView = AnimateScrollIntoView;

			var disposables = new CompositeDisposable();

			Action headerFooterCallback = () => panel?.UpdateHeaderAndFooter();
			RegisterCallback(HeaderProperty, headerFooterCallback).DisposeWith(disposables);
			RegisterCallback(HeaderTemplateProperty, headerFooterCallback).DisposeWith(disposables);
			RegisterCallback(FooterProperty, headerFooterCallback).DisposeWith(disposables);
			RegisterCallback(FooterTemplateProperty, headerFooterCallback).DisposeWith(disposables);

			_callbackSubscriptions.Disposable = disposables;
		}

		partial void CleanUpNativePanel(NativeListViewBase panel)
		{
			_callbackSubscriptions.Disposable = null;
			panel.Source?.Dispose();
			panel.Source = null;
		}

		/// <summary>
		/// Bind a property on the native collection panel to its equivalent on ListViewBase
		/// </summary>
		private void BindToPanel(NativeListViewBase panel, string propertyName, BindingMode bindingMode = BindingMode.OneWay)
		{
			panel.Binding(propertyName, propertyName, this, bindingMode);
		}

		private IDisposable RegisterCallback(DependencyProperty property, Action callback)
		{
			return this.RegisterDisposablePropertyChangedCallback(property, (_, __) => callback());
		}

		public void ScrollIntoView(object item)
		{
			if (NativePanel != null)
			{
				NativePanel.ScrollIntoView(item);
			}
			else if (this.Log().IsEnabled(LogLevel.Warning))
			{
				this.Log().LogWarning($"{nameof(ScrollIntoView)} not supported when using non-virtualizing panels.");
			}
		}
		public void ScrollIntoView(object item, ScrollIntoViewAlignment alignment)
		{
			if (NativePanel != null)
			{
				NativePanel?.ScrollIntoView(item, alignment);
			}
			else if (this.Log().IsEnabled(LogLevel.Warning))
			{
				this.Log().LogWarning($"{nameof(ScrollIntoView)} not supported when using non-virtualizing panels.");
			}
		}

		internal override DependencyObject ContainerFromIndexInner(int index)
		{
			if (NativePanel != null)
			{
				var indexPath = GetIndexPathFromIndex(index)?.ToNSIndexPath();
				if (indexPath != null)
				{
					return NativePanel.ContainerFromIndex(indexPath);
				}
			}

			return base.ContainerFromIndexInner(index);
		}

		private ContentControl ContainerFromGroupIndex(int groupIndex)
		{
			return NativePanel?.ContainerFromGroupIndex(NSIndexPath.FromRowSection(0, groupIndex));
		}

		internal override int IndexFromContainerInner(DependencyObject container)
		{
			if (NativePanel != null)
			{
				var nativeContainer = (container as SelectorItem)?.FindFirstParent<ListViewBaseInternalContainer>();
				if (nativeContainer == null)
				{
					return -1;
				}
				var indexPath = NativePanel.IndexPathForCell(nativeContainer);
				if (indexPath != null)
				{
					return GetIndexFromIndexPath(indexPath.ToIndexPath());
				}
			}
			return base.IndexFromContainerInner(container);
		}

		protected internal override IEnumerable<DependencyObject> GetItemsPanelChildren()
		{
			if (NativePanel != null)
			{
				return NativePanel
						.Subviews
						.OfType<ListViewBaseInternalContainer>()
						.Select(cell => cell.Content)
						.Trim();
			}
			else
			{
				return base.GetItemsPanelChildren();
			}
		}

		private void AddItems(int firstItem, int count, int section)
		{
			NativePanel?.InsertItems(GetIndexPathsFromStartAndCount(firstItem, count, section));

			ManagedVirtualizingPanel?.GetLayouter().AddItems(firstItem, count, section);
		}

		private void RemoveItems(int firstItem, int count, int section)
		{
			NativePanel?.DeleteItems(GetIndexPathsFromStartAndCount(firstItem, count, section));

			ManagedVirtualizingPanel?.GetLayouter().RemoveItems(firstItem, count, section);
		}

		partial void NativeReplaceItems(int firstItem, int count, int section)
		{
			NativePanel?.ReloadItems(GetIndexPathsFromStartAndCount(firstItem, count, section));
		}

		/// <summary>
		/// Add a group using the native in-place modifier.
		/// </summary>
		/// <param name="groupIndexInView">The index of the group from the native collection view's perspective, ie ignoring empty groups 
		/// if HidesIfEmpty=true.</param>
		private void AddGroup(int groupIndexInView)
		{
			NativePanel?.InsertSections(NSIndexSet.FromIndex(groupIndexInView));

			if (ManagedVirtualizingPanel != null)
			{
				Refresh();
			}
		}

		private void RemoveGroup(int groupIndexInView)
		{
			NativePanel?.DeleteSections(NSIndexSet.FromIndex(groupIndexInView));

			if (ManagedVirtualizingPanel != null)
			{
				Refresh();
			}
		}

		private void ReplaceGroup(int groupIndexInView)
		{
			NativePanel?.ReloadSections(NSIndexSet.FromIndex(groupIndexInView));

			if (ManagedVirtualizingPanel != null)
			{
				Refresh();
			}
		}

		private NSIndexPath[] GetIndexPathsFromStartAndCount(int startIndex, int count, int section)
		{
			return Enumerable.Range(startIndex, count)
				.Select(index => NSIndexPath.FromItemSection(index, section))
				.ToArray();
		}

		private protected override void Refresh()
		{
			base.Refresh();

			NativePanel?.Refresh();

			if (ManagedVirtualizingPanel != null)
			{
				ManagedVirtualizingPanel.GetLayouter().Refresh();

				InvalidateMeasure();
			}
		}

		public override void MovedToWindow()
		{
			base.MovedToWindow();

			// Uno#13172: The header container can sometimes lose its data-context between/after frame navigation,
			// especially so when the header has been scrolled out of viewport (far enough to be recycled) once.
			if (Window is { })
			{
				if (InternalItemsPanelRoot is NativeListViewBase panel)
				{
					panel.UpdateHeaderAndFooter();
				}
			}
		}
	}
}
