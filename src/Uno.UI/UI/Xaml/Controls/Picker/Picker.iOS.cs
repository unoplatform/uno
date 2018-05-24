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
using System.Windows.Input;
using CoreGraphics;
using Uno.Extensions.Specialized;

namespace Windows.UI.Xaml.Controls
{
	public partial class Picker : UIPickerView, ISelector, IFrameworkElement, DependencyObject
	{
		public event SelectionChangedEventHandler SelectionChanged;

		public Picker()
		{
			IFrameworkElementHelper.Initialize(this);
			this.InitializeBinder();

			AutoresizingMask = UIViewAutoresizing.None;

			OnDisplayMemberPathChangedPartial(string.Empty, this.DisplayMemberPath);

			this.Model = new PickerModel(this);
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

			if (this.Frame.Size != size)
			{
				this.SetDimensions(size.Width, size.Height);
				
				// forces PickerModel.GetComponentWidth to get called, 
				// which then receives the new dimensions (which might differ from the default value of 320)
				this.SetNeedsLayout(); 
			}

			return size;
		}



		public object[] Items { get; private set; } = new object[] { null }; // ensure there's always a null present to allow deselection
		
		partial void OnItemsSourceChangedPartialNative(object oldItemsSource, object newItemsSource)
		{
			var items = (newItemsSource as IEnumerable)?.ToObjectArray() ?? new object[0];
			Items = new[] { (object)null }.Concat(items).ToArray(); // ensure there's always a null present to allow deselection

			ReloadAllComponents();
			ClearSelection();
		}

		private void ClearSelection()
		{
			this.SelectedItem = null;
		}

		partial void OnSelectedItemChangedPartial(object oldSelectedItem, object newSelectedItem)
		{
			var row = Items.Safe().IndexOf(newSelectedItem);
			if (row == -1) // item not found
			{
				ClearSelection();
				return;
			}			

			Select(row, component: 0, animated: true);

			SelectionChanged?.Invoke(
				this,
				// TODO: Add multi-selection support
				new SelectionChangedEventArgs(
					new[] { oldSelectedItem },
					new[] { newSelectedItem }
				)
			);
		}
		
		partial void OnDisplayMemberPathChangedPartial(string oldDisplayMemberPath, string newDisplayMemberPath)
		{
			this.UpdateItemTemplateSelectorForDisplayMemberPath(newDisplayMemberPath);
		}
	}
}
