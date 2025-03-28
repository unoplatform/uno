using System;
using System.Collections.Generic;
using System.Linq;
using System.Drawing;
using System.Text;
using Windows.UI.Xaml;
using Uno.UI;
using Uno.UI.Extensions;
using Uno.Extensions;
using UIKit;
using System.Collections;
using System.Collections.Specialized;
using System.Windows.Input;
using CoreGraphics;
using Uno.Extensions.Specialized;
using ObjCRuntime;

namespace Windows.UI.Xaml.Controls
{
	/// <summary>
	/// This class is mainly used for native looking ComboBox templates on iPhone.
	/// This class will automatically prepend a null item to its Items when there's no selection.
	/// Once a selection is made, that null item is removed. That means that this control cannot deselect (just like a ComboBox).
	/// </summary>
	public partial class Picker : UIPickerView, ISelector, IFrameworkElement, DependencyObject
	{
		public event SelectionChangedEventHandler SelectionChanged;

		public Picker()
		{
			IFrameworkElementHelper.Initialize(this);
			this.InitializeBinder();

			AutoresizingMask = UIViewAutoresizing.None;
			SetupSelectionIndicator();

			OnDisplayMemberPathChangedPartial(string.Empty, this.DisplayMemberPath);

			this.Model = new PickerModel(this);
		}

		private void SetupSelectionIndicator()
		{
			// The "selection indicator" refers the the thin lines above and below the selected item in the spinner.

			// Setting this flag should be enough but it isn't.
			ShowSelectionIndicator = true;

			// Selecting the first item strangely fixes the selection not showing otherwise.
			// See this for more info: https://stackoverflow.com/questions/39564660/uipickerview-selection-indicator-not-visible-in-ios10
			Select(row: 0, component: 0, animated: false);
		}

		public override CGSize SizeThatFits(CGSize size)
		{
			size = IFrameworkElementHelper.SizeThatFits(this, size);

			if (nfloat.IsPositiveInfinity(size.Height) || nfloat.IsPositiveInfinity(size.Width))
			{
				// Changes from IOS 9 doc.
				// See following
				// https://jira.appcelerator.org/browse/TIMOB-19203
				// https://developer.apple.com/library/prerelease/ios/releasenotes/General/RN-iOSSDK-9.0/
				size = base.SizeThatFits(size);
			}


			return size;
		}

		public object[] Items { get; private set; } = new object[] { null }; // Ensure there's always a null present to allow the empty selection at the beginning.

		partial void OnItemsSourceChangedPartialNative(object oldItemsSource, object newItemsSource)
		{
			if (oldItemsSource is INotifyCollectionChanged oldObservableCollection)
			{
				oldObservableCollection.CollectionChanged -= OnItemSourceChanged;
			}

			if (newItemsSource is INotifyCollectionChanged newObservableCollection)
			{
				newObservableCollection.CollectionChanged += OnItemSourceChanged;
			}

			OnItemSourceChanged(newItemsSource, null);
		}

		private void OnItemSourceChanged(object collection, NotifyCollectionChangedEventArgs _)
		{
			if (SelectedItem == null)
			{
				Items = new[] { (object)null }
				  .Concat((collection as IEnumerable)?.ToObjectArray() ?? Array.Empty<object>())
				  .ToObjectArray();
			}
			else
			{
				Items = (collection as IEnumerable)?.ToObjectArray() ?? Array.Empty<object>();
			}

			ReloadAllComponents();

			if (!Items.Contains(SelectedItem))
			{
				SelectedItem = Items.FirstOrDefault();
			}
		}

		partial void OnSelectedItemChangedPartial(object oldSelectedItem, object newSelectedItem)
		{
			var row = Items?.IndexOf(newSelectedItem) ?? -1;
			if (row == -1) // item not found
			{
				var firstItem = Items?.FirstOrDefault();
				if (firstItem != newSelectedItem) // We compare to 'newSelectedItem' so we allow them to be both 'null'
				{
					SelectedItem = firstItem;
					return;
				}

				if (Items.Length > 0) // If we have no Items, we don't need to call UIPickerView.Select(). Skipping the call avoids a native crash under certain narrow circumstances. (https://github.com/unoplatform/private/issues/115)
				{
					Select(row, component: 0, animated: true);
				}
			}
			else if (newSelectedItem != null && Items[0] == null)
			{
				// On ItemSelection remove initial null item at Items[0]
				Items = Items
					.Skip(1)
					.ToObjectArray();

				// Now that Items changed, we must reload the UIPickerView's components.
				ReloadAllComponents();

				// Because we removed the first item, we decrement the row by 1.
				--row;

				// We select the row without the animation, because the previous state has a different items source in wich the items have different indexes.
				Select(row, component: 0, animated: false);
			}

			SelectionChanged?.Invoke(
				this,
				// TODO: Add multi-selection support
				new SelectionChangedEventArgs(
					this,
					new[] { oldSelectedItem },
					new[] { newSelectedItem }
				)
			);
		}
	}
}
