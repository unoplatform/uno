// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using Windows.Foundation;
using Windows.UI.Xaml.Media;
using Uno.Extensions;
using DateTime = Windows.Foundation.WindowsFoundationDateTime;

namespace Windows.UI.Xaml.Controls.Primitives
{
	public sealed partial class CalendarPanel : Panel
	{
		private void Initialize()
		{
			CalendarLayoutStrategy spCalendarLayoutStrategy;

			// Initalize the base class first.
			// base.Initialize();
			base_Initialize();

			spCalendarLayoutStrategy = new CalendarLayoutStrategy();
			SetLayoutStrategyBase(spCalendarLayoutStrategy);
		}

		/// <inheritdoc />
		protected override Size MeasureOverride(Size availableSize)
		{
			if (m_type != CalendarPanelType.Invalid)
			{
				// for secondary panel, we don't care about the biggest item size, because this type means
				// we have an explict dimensions set, in this case we'll always honor the dimensions.
				if (m_type != CalendarPanelType.Secondary && !m_isBiggestItemSizeDetermined)
				{
					CalendarViewGeneratorHost spOwner;

					spOwner = Owner;

					if (spOwner is { })
					{
						Size biggestItemSize = default;

						DetermineTheBiggestItemSize(spOwner, availableSize, out biggestItemSize);

						if (biggestItemSize.Width != m_biggestItemSize.Width || biggestItemSize.Height != m_biggestItemSize.Height)
						{
							m_biggestItemSize = biggestItemSize;

							// for primary panel, we should notify the CalendarView, so CalendarView can update
							// the size for other template parts.
							if (m_type == CalendarPanelType.Primary)
							{
								SetItemMinimumSize(biggestItemSize);
								spOwner.OnPrimaryPanelDesiredSizeChanged();
							}
						}
					}

					m_isBiggestItemSizeDetermined = true;
				}
			}
			// else CalendarPanel.SetPanelType has not been called yet.

			var pDesired = base_MeasureOverride(availableSize);

			return pDesired;
		}

		protected override Size ArrangeOverride(Size finalSize)
		{
			Size returnValue = default;
			bool needsRemeasure = false;

			if (m_type != CalendarPanelType.Invalid)
			{
				ILayoutStrategy spCalendarLayoutStrategy;
				Size viewportSize = new Size(0.0, 0.0);

				// When we begin to arrange, we are good to know the scrollviewer viewport size.
				// We need to check if the viewport can perfectly fit Rows x Cols items,
				// if not we need to change the items size and remeasure the panel.
				viewportSize = GetViewportSize();

				global::System.Diagnostics.Debug.Assert(viewportSize.Height != 0.0 && viewportSize.Width != 0.0);

				// and the size of biggest item has been determined if the Panel is not Secondary Panel.
				global::System.Diagnostics.Debug.Assert(m_type == CalendarPanelType.Secondary ||
					(m_isBiggestItemSizeDetermined && m_biggestItemSize.Width != 0.0 && m_biggestItemSize.Height != 0.0));

				if (m_type == CalendarPanelType.Secondary_SelfAdaptive)
				{
					int effectiveCols = (int)(viewportSize.Width / m_biggestItemSize.Width);
					int effectiveRows = (int)(viewportSize.Height / m_biggestItemSize.Height);

					effectiveCols = Math.Max(1, Math.Min(effectiveCols, m_suggestedCols));
					effectiveRows = Math.Max(1, Math.Min(effectiveRows, m_suggestedRows));

					SetPanelDimension(effectiveCols, effectiveRows);
				}

				spCalendarLayoutStrategy = LayoutStrategy;

				// tell layout strategy that the new viewport size so it can compute the
				// arrange bound for items correctly.

				((CalendarLayoutStrategy)spCalendarLayoutStrategy).SetViewportSize(viewportSize, out needsRemeasure);
			}
			// else CalendarPanel.SetPanelType has not been called yet.

			// when we need remeasure, we could skip the current arrange.
			if (needsRemeasure)
			{
				InvalidateMeasure();
			}
			else
			{
				returnValue = base_ArrangeOverride(finalSize);
			}

			return returnValue;
		}


		internal override void OnPropertyChanged2(DependencyPropertyChangedEventArgs args)
		{
			base.OnPropertyChanged2(args);

			switch (args.Property)
			{
				case DependencyProperty orientationProperty when orientationProperty == CalendarPanel.OrientationProperty:
					{
						Orientation orientation = (Orientation)(args.NewValue);
						ILayoutStrategy spCalendarLayoutStrategy;

						spCalendarLayoutStrategy = LayoutStrategy;

						CacheFirstVisibleElementBeforeOrientationChange();

						// CalendarPanel orientation is the stacking direction. Which is the opposite of the
						// virtualization direction.
						if (orientation == Orientation.Horizontal)
						{
							((CalendarLayoutStrategy)spCalendarLayoutStrategy).SetVirtualizationDirection(Orientation.Vertical);
						}
						else
						{
							((CalendarLayoutStrategy)spCalendarLayoutStrategy).SetVirtualizationDirection(Orientation.Horizontal);
						}

						// let the base know
						ProcessOrientationChange();
					}
					break;

				case DependencyProperty rowsProperty when rowsProperty == CalendarPanel.RowsProperty:
					{
						int rows = 0;
						ILayoutStrategy spCalendarLayoutStrategy;

						rows = (int)args.NewValue;
						spCalendarLayoutStrategy = LayoutStrategy;
						global::System.Diagnostics.Debug.Assert(rows > 0); // guaranteed to be positive number
						((CalendarLayoutStrategy)spCalendarLayoutStrategy).SetRows(rows);
						OnRowsOrColsChanged(Orientation.Vertical);
					}
					break;

				case DependencyProperty colsProperty when colsProperty == CalendarPanel.ColsProperty:
					{
						int cols = 0;
						ILayoutStrategy spCalendarLayoutStrategy;

						cols = (int)args.NewValue;
						spCalendarLayoutStrategy = LayoutStrategy;
						global::System.Diagnostics.Debug.Assert(cols > 0); // guaranteed to be positive number
						((CalendarLayoutStrategy)spCalendarLayoutStrategy).SetCols(cols);
						OnRowsOrColsChanged(Orientation.Horizontal);
					}
					break;

				case DependencyProperty cacheLengthProperty when cacheLengthProperty == CalendarPanel.CacheLengthProperty:
					{
						double newCacheLength = (double)args.NewValue;
						CacheLengthBase = newCacheLength;
					}
					break;

				case DependencyProperty startIndexProperty when startIndexProperty == CalendarPanel.StartIndexProperty:
					{
						ILayoutStrategy spCalendarLayoutStrategy;
						int startIndex = 0;

						startIndex = (int)args.NewValue;

						global::System.Diagnostics.Debug.Assert(startIndex >= 0);

						spCalendarLayoutStrategy = LayoutStrategy;
						var table = ((CalendarLayoutStrategy)spCalendarLayoutStrategy).GetIndexCorrectionTable();
						table.SetCorrectionEntryForElementStartAt(startIndex);

						InvalidateMeasure();
					}
					break;

				default:
					break;
			}
		}

#if false
		// Logical Orientation override
		private Orientation LogicalOrientation => Orientation;

		// Physical Orientation override
		private Orientation PhysicalOrientation
		{
			get
			{
				Orientation orientation = Orientation.Horizontal;

				var pValue = orientation;

				if (orientation == Orientation.Vertical)
				{
					pValue = Orientation.Horizontal;
				}
				else
				{
					pValue = Orientation.Vertical;
				}

				return pValue;
			}
		}
#endif

		// Virtual helper method to the ItemsPerPage that can be overridden by derived classes.
		//protected /* override */ void ItemsPerPageImpl(
		//	Rect window,
		//	out double pItemsPerPage)
		//{
		//	throw new NotImplementedException();
		//}

		#region Special elements overrides

		//protected /* override */ void NeedsSpecialItem(out bool pResult) /*override*/
		//{
		//	ILayoutStrategy spCalendarLayoutStrategy;
		//	pResult = false;

		//	spCalendarLayoutStrategy = LayoutStrategy;
		//	pResult = ((CalendarLayoutStrategy)spCalendarLayoutStrategy).NeedsSpecialItem();
		//}

		//protected /* override */ void SpecialItemIndex(out int pResult) /*override*/
		//{
		//	ILayoutStrategy spCalendarLayoutStrategy;
		//	pResult = -1;

		//	spCalendarLayoutStrategy = LayoutStrategy;
		//	pResult = ((CalendarLayoutStrategy)spCalendarLayoutStrategy).GetSpecialItemIndex();
		//}

		#endregion

#if false
		private void DesiredViewportSize(out Size pSize)
		{
			ILayoutStrategy spCalendarLayoutStrategy;
			pSize = default;

			pSize.Width = 0;
			pSize.Height = 0;

			spCalendarLayoutStrategy = LayoutStrategy;
			pSize = ((CalendarLayoutStrategy)spCalendarLayoutStrategy).GetDesiredViewportSize();
		}
#endif

		private void SetItemMinimumSize(Size size)
		{
			bool needsRemeasure = false;
			ILayoutStrategy spCalendarLayoutStrategy;

			spCalendarLayoutStrategy = LayoutStrategy;
			((CalendarLayoutStrategy)spCalendarLayoutStrategy).SetItemMinimumSize(size, out needsRemeasure);

			if (needsRemeasure)
			{
				InvalidateMeasure();
			}
		}

		internal void SetSnapPointFilterFunction(
			Func<int, bool> func)
		{
			ILayoutStrategy spCalendarLayoutStrategy;

			spCalendarLayoutStrategy = LayoutStrategy;
			((CalendarLayoutStrategy)spCalendarLayoutStrategy).SetSnapPointFilterFunction(func);
		}

		// when Rows or Cols changed, we'll invalidate measure on panel itself.
		// however this might not change the panel's desired size, especially when we
		// change the dimension on the scrolling direction. To make sure the viewport size being adjusted
		// correctly, we need to invalidate measure on the parent SCP.
		private void OnRowsOrColsChanged(Orientation orientation)
		{
			if (m_type == CalendarPanelType.Primary)
			{
				// Primary Panel, we should InvalidateMeasure our parent to grant the new size.
				ILayoutStrategy spCalendarLayoutStrategy;
				Orientation virtualizationDirection = Orientation.Horizontal;

				spCalendarLayoutStrategy = LayoutStrategy;
				virtualizationDirection = spCalendarLayoutStrategy.VirtualizationDirection;

				if (virtualizationDirection == orientation)
				{
					DependencyObject spParent;
					ScrollContentPresenter spParentAsSCP;

					spParent = VisualTreeHelper.GetParent(this);
					spParentAsSCP = spParent as ScrollContentPresenter;
					if (spParentAsSCP is { })
					{
						((ScrollContentPresenter)spParentAsSCP).InvalidateMeasure();
					}
				}
			}
			else
			{
				// Secondary and Secondary_SelfAdaptive, we won't affect the whole size, just need a new Arrange pass
				InvalidateArrange();
			}
		}

		internal CalendarViewGeneratorHost Owner
		{
			set
			{
				m_wrGeneartorHostOwner = new WeakReference<CalendarViewGeneratorHost>(value);
			}
			get
			{
				CalendarViewGeneratorHost spOwner;

				m_wrGeneartorHostOwner.TryGetTarget(out spOwner);

				return spOwner;
			}
		}

		internal void SetNeedsToDetermineBiggestItemSize()
		{
			m_isBiggestItemSizeDetermined = false;
			InvalidateMeasure();
		}

		private void DetermineTheBiggestItemSize(
			CalendarViewGeneratorHost pOwner,
			Size availableSize,
			out Size pSize)
		{
			DependencyObject spChildAsIDO;
			CalendarViewBaseItem spItemAsI;
			CalendarViewBaseItem spCalendarViewBaseItem;
			string mainText;
			pSize = default;
			pSize.Height = 0.0;
			pSize.Width = 0.0;

			// we'll try to change all the possible strings on the first item and see the biggest desired size.
			spChildAsIDO = ContainerFromIndex(ContainerManager.StartOfContainerVisualSection());
			if (spChildAsIDO is null)
			{
				Size ignored = default;
				// no children yet, call base.MeasureOverride to generate at least one anchor item
				ignored = base_MeasureOverride(availableSize);

				spChildAsIDO = ContainerFromIndex(ContainerManager.StartOfContainerVisualSection());
				global::System.Diagnostics.Debug.Assert(spChildAsIDO is { });
			}

			// there is at least one item in Panel, and the item has entered visual tree
			// we are good to measure it to the desired size.
			spItemAsI = spChildAsIDO as CalendarViewBaseItem;

			if (spItemAsI is { })
			{
				spCalendarViewBaseItem = (CalendarViewBaseItem)spItemAsI;
				// save the maintext
				mainText = spCalendarViewBaseItem.GetMainText();

				List<string> pStrings = null;
				pStrings = pOwner.GetPossibleItemStrings();

				global::System.Diagnostics.Debug.Assert(!pStrings.Empty());

				// try all the possible string and find the biggest desired size.
				foreach (var str in pStrings)
				{
					Size desiredSize = default;

					spCalendarViewBaseItem.UpdateMainText(str);
					spCalendarViewBaseItem.InvalidateMeasure();
					spCalendarViewBaseItem.Measure(availableSize);
					desiredSize = spCalendarViewBaseItem.DesiredSize;
					pSize.Width = Math.Max(pSize.Width, desiredSize.Width);
					pSize.Height = Math.Max(pSize.Height, desiredSize.Height);
				}

				// restore the maintext
				spCalendarViewBaseItem.UpdateMainText(mainText);
			}
		}

		internal CalendarPanelType PanelType
		{
			get => m_type;
			set
			{
				var type = value;

				global::System.Diagnostics.Debug.Assert(type != CalendarPanelType.Invalid);

				if (m_type != type)
				{
					// we don't allow the type to be changed dynamically, only expect if
					// we change the type from Secondary_SelfAdaptive to Secondary.
					// the scenario is: by default YearPanel and DecadePanel are Secondary_SelfAdaptive, but once
					// developer calls SetYearDecadeDisplayDimensions, we'll change the type to Secondary (and never change back).
					global::System.Diagnostics.Debug.Assert(m_type == CalendarPanelType.Invalid ||
						(m_type == CalendarPanelType.Secondary_SelfAdaptive &&
							type == CalendarPanelType.Secondary));

					m_type = type;

					if (m_type == CalendarPanelType.Primary || m_type == CalendarPanelType.Secondary)
					{
						// for Primary and Secondary Panels, we don't need to adjust the dimension based on actual viewport size,
						// so the suggested dimensions are the actual dimensions

						if (m_suggestedCols != -1 && m_suggestedRows != -1)
						{
							SetPanelDimension(m_suggestedCols, m_suggestedRows);
						}
					}

					// for Secondary_SelfAdaptive panel, we'll determine the exact dimension in Arrange pass whe we know the exact viewport size.
				}
			}
		}

		internal void SetSuggestedDimension(int cols, int rows)
		{
			if (m_type == CalendarPanelType.Primary || m_type == CalendarPanelType.Secondary)
			{
				// for Primary or Secondary Panels, the suggested dimensions are the exact dimensions
				SetPanelDimension(cols, rows);
			}
			else //if (m_type == CalendarPanelType.Invalid || m_type == CalendarPanelType.Secondary_SelfAdaptive)
			{
				// we'll determine the rows and cols later, when
				// 1. PanelType is set
				// 2. for Secondary_SelfAdaptive, in the Arrange pass when we know the exact viewport size.
				m_suggestedRows = rows;
				m_suggestedCols = cols;
			}
		}

		private void SetPanelDimension(int col, int row)
		{
			int actualRows = 0;
			int actualCols = 0;

			actualRows = Rows;
			actualCols = Cols;

			if (row != actualRows || col != actualCols)
			{
				Rows = row;
				Cols = col;

				CalendarViewGeneratorHost spOwner;

				spOwner = Owner;

				// dimension changed, we should check if we need to update the snap point filter function.
				if (spOwner is { })
				{
					bool canPanelShowFullScope = false;

					CalendarView.CanPanelShowFullScope(spOwner, out canPanelShowFullScope);

					// If the current dimension setting allows us to show a full scope,
					// we'll have irregular snap point on each scope. Otherwise we'll
					// have regular snap point (on each row).

					if (!canPanelShowFullScope)
					{
						// We have not enough space, remove the customize function to default regular snap point behaivor.
						SetSnapPointFilterFunction(null);
					}
					else
					{
						// We have enough space, so we'll use irregular snap point
						// and we use the customize function to filter the snap point
						// so we only put a snap point on the first item of each scope.
						var pHost = spOwner;
						SetSnapPointFilterFunction(itemIndex =>
						{
							return pHost.GetIsFirstItemInScope(itemIndex);
						});
					}
				}
			}
		}
	}
}
