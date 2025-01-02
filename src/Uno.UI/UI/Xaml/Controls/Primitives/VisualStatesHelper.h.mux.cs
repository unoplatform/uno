using System;
using System.Collections.Generic;
using System.Text;

namespace Windows.UI.Xaml.Controls.Primitives
{
	internal struct ListViewBaseItemVisualStatesCriteria
	{
		public bool isEnabled;
		public bool isSelected;
		public bool isPressed;
		public bool isPointerOver;
		public bool isMultiSelect;
		public bool isDragging;
		public bool isItemDragPrimary;
		public bool isInsideListView;
		public bool isDragVisualCaptured;
		public bool isHolding;
		public bool canDrag;
		public bool canReorder;
		public bool isDraggedOver;

		public int dragItemsCount;

		public FocusState focusState;

		/// <inheritdoc />
		public override string ToString()
			=> $"isEnabled: {isEnabled} | isSelected: {isSelected} | isPressed: {isPressed} | isPointerOver: {isPointerOver} | isMultiSelect: {isMultiSelect} | isDragging: {isDragging} | isItemDragPrimary: {isItemDragPrimary} | isInsideListView: {isInsideListView} | isDragVisualCaptured: {isDragVisualCaptured} | isHolding: {isHolding} | canDrag: {canDrag} | canReorder: {canReorder} | isDraggedOver: {isDraggedOver} | dragItemsCount: {dragItemsCount} | focusState: {focusState}"
		;
	}
}
