// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.


using Windows.UI.Xaml.Controls;

namespace Microsoft/* UWP don't rename */.UI.Xaml.Controls
{

	public partial class LayoutPanel : Panel
	{
		// public

		//IInspectable LayoutState() { return m_layoutState.get(); }
		//void LayoutState(IInspectable value) { m_layoutState.set(value); }
		public object LayoutState { get; set; }

		//void OnPropertyChanged(DependencyPropertyChangedEventArgs args);

		//Size MeasureOverride(Size availableSize);
		//Size ArrangeOverride(Size finalSize);

		// private
		LayoutContext m_layoutContext;

		//Layout.MeasureInvalidated_revoker m_measureInvalidated
		//{
		//}

		//Layout.ArrangeInvalidated_revoker m_arrangeInvalidated
		//{
		//}

		//void OnLayoutChanged(Layout oldValue, Layout newValue);

		//void InvalidateMeasureForLayout(Layout sender, IInspectable args);
		//void InvalidateArrangeForLayout(Layout sender, IInspectable args);
	}
}

