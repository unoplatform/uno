using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using CoreGraphics;
using Uno.Extensions;
using Uno.UI.Controls;
using UIKit;
using Windows.UI.Xaml.Controls.Primitives;
using ObjCRuntime;

namespace Windows.UI.Xaml.Controls
{
	public partial class PickerModel : UIPickerViewModel
	{
		private readonly Picker _picker;

		public PickerModel(Picker picker)
		{
			_picker = picker ?? throw new ArgumentNullException(nameof(picker));
		}

		public override string GetTitle(UIPickerView picker, nint row, nint component) => string.Empty;
		public override nint GetComponentCount(UIPickerView picker) => 1; // designed to support only 1 column
		public override nint GetRowsInComponent(UIPickerView picker, nint component) => _picker.Items.Length;
		public override nfloat GetRowHeight(UIPickerView picker, nint component) => 44;
		public override nfloat GetComponentWidth(UIPickerView picker, nint component) => picker.Bounds.Width;
		public override UIView GetView(UIPickerView picker, nint row, nint component, UIView view)
		{
			var item = FindItem(row) ?? _picker.Placeholder;
			var selectorItem = (view as ContentPresenter)?.Content as SelectorItem;

			//Note: at present selectorItem is always null because recycling is apparently broken in UIPickerView in iOS 7-9; see eg http://stackoverflow.com/questions/20635949/reusing-view-in-uipickerview-with-ios-7
			if (selectorItem == null)
			{
				selectorItem = new PickerItem()
				{
					Style = _picker.ItemContainerStyle,
					ContentTemplateSelector = _picker.ItemTemplateSelector,
					ContentTemplate = _picker.ItemTemplate,
					Frame = new CGRect(
						0,
						0,
						GetComponentWidth(picker, component),
						GetRowHeight(picker, component)
					),
					AutoresizingMask = UIViewAutoresizing.All,
				}
				.Binding("Content", "");
			}

			// For some unknown reasons, the DataContext must be applied after the Binding is set,
			// otherwise the ContentProperty's callback isn't called and things don't work properly (bug #26052).
			selectorItem.DataContext = item;

			// Wrap the PickerItem inside another container. This prevents a Xamarin run-time crash which occurs when accessing Superview that is being removed when clearing bindings from ContentControl.
			var internalContainer = new ContentPresenter { Content = selectorItem };
			return internalContainer;
		}

		public override void Selected(UIPickerView picker, nint row, nint component)
		{
			_picker.SelectedItem = FindItem(row);
		}

		private object FindItem(nint row)
		{
			return _picker.Items.ElementAtOrDefault((int)row);
		}
	}
}
