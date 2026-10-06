// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

// MUX Reference VisualStatesHelper.cpp, tag winui3/release/2.5.1

using System;
using System.Collections.Generic;
using System.Text;

namespace Microsoft.UI.Xaml.Controls.Primitives
{
	internal static class VisualStatesHelper
	{
		internal static IEnumerable<string> GetValidVisualStatesListViewBaseItem(ListViewBaseItemVisualStatesCriteria criteria)
		{
			int index = 0;
			int expectedVisualStatesSize = 7;

			var validVisualStates = new string[expectedVisualStatesSize];

			// Focus States
			if (FocusState.Unfocused != criteria.focusState && criteria.isEnabled)
			{
				if (FocusState.Pointer == criteria.focusState)
				{
					validVisualStates[index] = "PointerFocused";
				}
				else
				{
					validVisualStates[index] = "Focused";
				}
			}
			else
			{
				validVisualStates[index] = "Unfocused";
			}

			++index;

			// Multi-Select States
			if (criteria.isMultiSelect)
			{
				validVisualStates[index] = "MultiSelectEnabled";
			}
			else
			{
				validVisualStates[index] = "MultiSelectDisabled";
			}

			++index;

			// Indicator-Select States
			if (criteria.isIndicatorSelect)
			{
				validVisualStates[index] = "SelectionIndicatorEnabled";
			}
			else
			{
				validVisualStates[index] = "SelectionIndicatorDisabled";
			}

			++index;

			// Enabled and Selection States
			if (criteria.isEnabled)
			{
				validVisualStates[index++] = "Enabled";

				if (criteria.isDraggedOver)
				{
					if (criteria.isSelected)
					{
						validVisualStates[index] = "PointerOverSelected";
					}
					else
					{
						validVisualStates[index] = "PointerOver";
					}
				}
				else if (criteria.isSelected)
				{
					if (criteria.isPressed)
					{
						validVisualStates[index] = "PressedSelected";
					}
					else if (criteria.isPointerOver)
					{
						validVisualStates[index] = "PointerOverSelected";
					}
					else
					{
						if (criteria.isDragging && criteria.isItemDragPrimary && !criteria.isDragVisualCaptured)
						{
							// Retain press till drag visual is captured
							validVisualStates[index] = "PressedSelected";
						}
						else
						{
							validVisualStates[index] = "Selected";
						}
					}
				}
				else if (criteria.isPointerOver)
				{
					if (criteria.isPressed)
					{
						validVisualStates[index] = "Pressed";
					}
					else
					{
						validVisualStates[index] = "PointerOver";
					}
				}
				else if (criteria.isPressed)
				{
					validVisualStates[index] = "Pressed";
				}
				else
				{
					if (criteria.isDragging && criteria.isItemDragPrimary && !criteria.isDragVisualCaptured)
					{
						// Retain press till drag visual is captured
						validVisualStates[index] = "Pressed";
					}
					else
					{
						validVisualStates[index] = "Normal";
					}
				}
			}
			else
			{
				validVisualStates[index++] = "Disabled";

				if (criteria.isSelected)
				{
					validVisualStates[index] = "Selected";
				}
				else
				{
					validVisualStates[index] = "Normal";
				}
			}

			++index;

			validVisualStates[index] = "NoReorderHint";

			++index;

			// Drag & Reorder States
			if ((criteria.isDragging || criteria.isHolding) && criteria.isInsideListView)
			{
				// to go into the DragOver state, an item must be DraggedOver, enabled, not selected and not the primary dragged item
				// selected items should go into MultipleDraggingSecondary
				// primary dragged items should go to DraggedPlaceholder state for the duration of the drag
				if (criteria.isDraggedOver && criteria.isEnabled && !criteria.isItemDragPrimary && !criteria.isSelected)
				{
					validVisualStates[index] = "DragOver";
				}
				else if (criteria.dragItemsCount > 1)
				{
					if (criteria.isItemDragPrimary)
					{
						if (criteria.isDragVisualCaptured)
						{
							if (criteria.canReorder)
							{
								validVisualStates[index] = "ReorderedPlaceholder";
							}
							else
							{
								validVisualStates[index] = "DraggedPlaceholder";
							}
						}
						else
						{
							if (criteria.canReorder)
							{
								validVisualStates[index] = "MultipleReorderingPrimary";
							}
							else
							{
								validVisualStates[index] = "MultipleDraggingPrimary";
							}
						}
					}
					else
					{
						if (criteria.isSelected)
						{
							if (criteria.canReorder)
							{
								validVisualStates[index] = "ReorderingTarget";
							}
							else
							{
								validVisualStates[index] = "MultipleDraggingSecondary";
							}
						}
						else
						{
							if (criteria.canReorder)
							{
								validVisualStates[index] = "ReorderingTarget";
							}
							else
							{
								validVisualStates[index] = "DraggingTarget";
							}
						}
					}
				}
				else
				{
					// Single drag
					if (criteria.isItemDragPrimary)
					{
						if (criteria.isDragVisualCaptured)
						{
							if (criteria.canReorder)
							{
								validVisualStates[index] = "ReorderedPlaceholder";
							}
							else
							{
								validVisualStates[index] = "DraggedPlaceholder";
							}
						}
						else
						{
							if (criteria.canReorder)
							{
								validVisualStates[index] = "Reordering";
							}
							else
							{
								validVisualStates[index] = "Dragging";
							}
						}
					}
					else
					{
						if (criteria.canReorder)
						{
							validVisualStates[index] = "ReorderingTarget";
						}
						else
						{
							validVisualStates[index] = "DraggingTarget";
						}
					}
				}
			}
			else
			{
				// No drag
				validVisualStates[index] = "NotDragging";
			}

			return validVisualStates;
		}
	}
}
