// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

// MUX Reference ItemsPresenter_ISnapPointsInfo.cpp, tag winui3/release/2.5.1, commit ba3a8d59e6d

#nullable enable

using System;
using System.Collections.Generic;
using DirectUI;
using Microsoft.UI.Xaml.Controls.Primitives;

namespace Microsoft.UI.Xaml.Controls;

public partial class ItemsPresenter
{
	//-------------------------------------------------------------------------
	//
	//  Function:   ItemsPresenter::NotifySnapPointsInfoPanelChanged
	//
	//  Synopsis:
	//      Called when the inner panel has been updated
	//
	//-------------------------------------------------------------------------
	private void NotifySnapPointsInfoPanelChanged()
	{
		var spPanel = Panel;

		if (!ReferenceEquals(m_tpScrollSnapPointsInfo, spPanel))
		{
			// Refresh the m_tpScrollSnapPointsInfo member based on the new content.
			// The snap point event handlers are unhooked.
			UnhookScrollSnapPointsInfoEvents(true /*isForHorizontalSnapPoints*/);
			UnhookScrollSnapPointsInfoEvents(false /*isForHorizontalSnapPoints*/);
			m_tpScrollSnapPointsInfo = null;

			if (spPanel is IScrollSnapPointsInfo spScrollSnapPointsInfo)
			{
				m_tpScrollSnapPointsInfo = spScrollSnapPointsInfo;

				// Uno: SetItemsPanel can run after the ScrollViewer already subscribed.
				if (m_horizontalSnapPointsChanged is not null)
				{
					HookScrollSnapPointsInfoEvents(true /*isForHorizontalSnapPoints*/);
				}

				if (m_verticalSnapPointsChanged is not null)
				{
					HookScrollSnapPointsInfoEvents(false /*isForHorizontalSnapPoints*/);
				}
			}
		}
	}

	//-------------------------------------------------------------------------
	//
	//  Function:   ItemsPresenter::HookScrollSnapPointsInfoEvents
	//
	//  Synopsis:
	//    Hooks up one snap points change event
	//
	//-------------------------------------------------------------------------
	private void HookScrollSnapPointsInfoEvents(bool isForHorizontalSnapPoints)
	{
		if (m_tpScrollSnapPointsInfo is { } spScrollSnapPointsInfo)
		{
			if (m_spHorizontalSnapPointsChangedEventHandler is null && isForHorizontalSnapPoints)
			{
				// Uno: the DMMotionTypes dispatch of OnSnapPointsChanged is folded into the handler.
				EventHandler<object> spHorizontalSnapPointsChangedEventHandler = (sender, args) => OnHorizontalSnapPointsChanged();
				spScrollSnapPointsInfo.HorizontalSnapPointsChanged += spHorizontalSnapPointsChangedEventHandler;
				m_spHorizontalSnapPointsChangedEventHandler = spHorizontalSnapPointsChangedEventHandler;
			}

			if (m_spVerticalSnapPointsChangedEventHandler is null && !isForHorizontalSnapPoints)
			{
				EventHandler<object> spVerticalSnapPointsChangedEventHandler = (sender, args) => OnVerticalSnapPointsChanged();
				spScrollSnapPointsInfo.VerticalSnapPointsChanged += spVerticalSnapPointsChangedEventHandler;
				m_spVerticalSnapPointsChangedEventHandler = spVerticalSnapPointsChangedEventHandler;
			}
		}
	}

	//-------------------------------------------------------------------------
	//
	//  Function:   ItemsPresenter::UnhookScrollSnapPointsInfoEvents
	//
	//  Synopsis:
	//    Unhooks one snap points change event
	//
	//-------------------------------------------------------------------------
	private void UnhookScrollSnapPointsInfoEvents(bool isForHorizontalSnapPoints)
	{
		if (m_tpScrollSnapPointsInfo is { } spScrollSnapPointsInfo)
		{
			if (m_spHorizontalSnapPointsChangedEventHandler is not null && isForHorizontalSnapPoints)
			{
				spScrollSnapPointsInfo.HorizontalSnapPointsChanged -= m_spHorizontalSnapPointsChangedEventHandler;
				m_spHorizontalSnapPointsChangedEventHandler = null;
			}

			if (m_spVerticalSnapPointsChangedEventHandler is not null && !isForHorizontalSnapPoints)
			{
				spScrollSnapPointsInfo.VerticalSnapPointsChanged -= m_spVerticalSnapPointsChangedEventHandler;
				m_spVerticalSnapPointsChangedEventHandler = null;
			}
		}
	}

	// IScrollSnapPointsInfo interface implementation:

	public bool AreHorizontalSnapPointsRegular
	{
		get
		{
			var value = false;

			var isHorizontal = IsHorizontal();

			if (isHorizontal)
			{
				value = AreScrollSnapPointsRegular(isHorizontal);
			}

			// When the orientation is vertical, there are no horizontal snap points.
			// We simply return FALSE then.

			return value;
		}
	}

	public bool AreVerticalSnapPointsRegular
	{
		get
		{
			var value = false;

			var isHorizontal = IsHorizontal();

			if (!isHorizontal)
			{
				value = AreScrollSnapPointsRegular(isHorizontal);
			}

			// When the orientation is horizontal, there are no vertical snap points.
			// We simply return FALSE then.

			return value;
		}
	}

	//-------------------------------------------------------------------------
	//
	//  Function:   ItemsPresenter::GetRegularSnapPoints
	//
	//  Synopsis:
	//    Returns an original offset and interval for equidistant snap points for
	//    the provided orientation. Returns 0 when no snap points are present.
	//
	//-------------------------------------------------------------------------
	public float GetRegularSnapPoints(
		// The direction of the requested snap points.
		Orientation orientation,
		// The alignment used by the caller when applying the requested snap points.
		SnapPointsAlignment alignment,
		// The offset of the first snap point.
		out float offset)
	{
		// The interval between the regular snap points.
		float interval = 0.0f;
		double leadingSnapPointKey = 0.0;
		double trailingSnapPointKey = 0.0;
		double leadingMarginSnapPointKey = 0.0;
		double trailingMarginSnapPointKey = 0.0;

		offset = 0.0f;

		var physicalOrientation = PhysicalOrientation;

		if (orientation == physicalOrientation)
		{
			var areScrollSnapPointsRegular = AreScrollSnapPointsRegular(physicalOrientation == Orientation.Horizontal);

			if (!areScrollSnapPointsRegular)
			{
				// Accessing the regular snap points while AreScrollSnapPointsRegular is False is not supported.
				throw new InvalidOperationException("The snap points are not regular.");
			}

			ResetSnapPointKeys();

			GetCommonSnapPointKeys(out leadingSnapPointKey, out trailingSnapPointKey, out leadingMarginSnapPointKey, out trailingMarginSnapPointKey);

			if (m_tpScrollSnapPointsInfo is { } spScrollSnapPointsInfo)
			{
				interval = spScrollSnapPointsInfo.GetRegularSnapPoints(orientation, alignment, out offset);
			}

			switch (alignment)
			{
				case SnapPointsAlignment.Near:
				case SnapPointsAlignment.Center:
					offset += (float)leadingSnapPointKey;
					break;
				case SnapPointsAlignment.Far:
					offset += (float)trailingSnapPointKey;
					break;
			}

			m_regularSnapPointKey = interval;
			m_leadingSnapPointKey = leadingSnapPointKey;
			m_trailingSnapPointKey = trailingSnapPointKey;
			m_leadingMarginSnapPointKey = leadingMarginSnapPointKey;
			m_trailingMarginSnapPointKey = trailingMarginSnapPointKey;
			m_bAreSnapPointsKeysHorizontal = (orientation == Orientation.Horizontal);

			// Next snap point change needs to raise a notification
			if (m_bAreSnapPointsKeysHorizontal)
			{
				m_bNotifiedHorizontalSnapPointsChanges = false;
			}
			else
			{
				m_bNotifiedVerticalSnapPointsChanges = false;
			}
		}

		return interval;
	}

	//-------------------------------------------------------------------------
	//
	//  Function:   VirtualizingStackPanel::GetIrregularSnapPointsInternal
	//
	//  Synopsis:
	//    Returns a read-only collection of numbers representing the snap points for
	//    the provided orientation. Returns an empty collection when no snap points are present.
	//
	//-------------------------------------------------------------------------
	public IReadOnlyList<float>? GetIrregularSnapPoints(
		// The direction of the requested snap points.
		Orientation orientation,
		// The alignment used by the caller when applying the requested snap points.
		SnapPointsAlignment alignment)
	{
		IReadOnlyList<float>? spPanelsSnapPoints = null;
		double leadingSnapPointKey = 0.0;
		double trailingSnapPointKey = 0.0;
		double leadingMarginSnapPointKey = 0.0;
		double trailingMarginSnapPointKey = 0.0;
		double headerSize = 0;
		double panelSize = 0;
		double footerSize = 0;
		float offset = 0.0f;
		float interval = 0.0f;
		uint cSnapPoints = 0;
		uint cSnapPointKeys = 0;
		float[]? pSnapPoints = null;
		float[]? pSnapPointKeys = null;
		IReadOnlyList<float>? pValue = null;

		var physicalOrientation = PhysicalOrientation;

		if (orientation == physicalOrientation)
		{
			var areScrollSnapPointsRegular = AreScrollSnapPointsRegular(orientation == Orientation.Horizontal);

			if (areScrollSnapPointsRegular)
			{
				// Accessing the regular snap points while AreScrollSnapPointsRegular is False is not supported.
				throw new InvalidOperationException("The snap points are regular.");
			}

			ResetSnapPointKeys();

			GetCommonSnapPointKeys(out leadingSnapPointKey, out trailingSnapPointKey, out leadingMarginSnapPointKey, out trailingMarginSnapPointKey);

			if (m_tpScrollSnapPointsInfo is { } spScrollSnapPointsInfo)
			{
				if (orientation == Orientation.Horizontal)
				{
					areScrollSnapPointsRegular = spScrollSnapPointsInfo.AreHorizontalSnapPointsRegular;
				}
				else
				{
					areScrollSnapPointsRegular = spScrollSnapPointsInfo.AreVerticalSnapPointsRegular;
				}

				if (!areScrollSnapPointsRegular)
				{
					spPanelsSnapPoints = spScrollSnapPointsInfo.GetIrregularSnapPoints(orientation, alignment);
				}
				else
				{
					interval = spScrollSnapPointsInfo.GetRegularSnapPoints(orientation, alignment, out offset);
				}
			}

			headerSize = GetSentinelItemSize(HeaderSentinelIndex, orientation == Orientation.Horizontal);
			footerSize = GetSentinelItemSize(FooterSentinelIndex, orientation == Orientation.Horizontal);
			panelSize = GetSentinelItemSize(PanelSentinelIndex, orientation == Orientation.Horizontal);

			if (spPanelsSnapPoints is not null)
			{
				uint nCount = 0;
				uint nHeaderIncrement = headerSize > 0 ? 1u : 0u;
				uint nFooterIncrement = footerSize > 0 ? 1u : 0u;

				nCount = (uint)spPanelsSnapPoints.Count;

				uint nTotalCount = nCount + nHeaderIncrement + nFooterIncrement;

				pSnapPoints = new float[nTotalCount];
				pSnapPointKeys = new float[nTotalCount];

				if (nHeaderIncrement > 0)
				{
					pSnapPointKeys[0] = (float)leadingSnapPointKey;

					switch (alignment)
					{
						case SnapPointsAlignment.Near:
							{
								pSnapPoints[0] = (float)leadingSnapPointKey;
								break;
							}
						case SnapPointsAlignment.Center:
							{
								pSnapPoints[0] = (float)(leadingSnapPointKey + headerSize / 2);
								break;
							}
						case SnapPointsAlignment.Far:
							{
								pSnapPoints[0] = (float)(leadingSnapPointKey + headerSize);
								break;
							}
					}
				}

				for (cSnapPoints = 0; cSnapPoints < nCount; cSnapPoints++, cSnapPointKeys++)
				{
					float snapPoint = spPanelsSnapPoints[(int)cSnapPoints];

					pSnapPoints[cSnapPoints + nHeaderIncrement] = (float)(leadingSnapPointKey + headerSize + snapPoint);
					pSnapPointKeys[cSnapPointKeys + nHeaderIncrement] = snapPoint;
				}

				if (nFooterIncrement > 0)
				{
					float footerSnapPointKey = (float)(leadingSnapPointKey + headerSize + panelSize);

					pSnapPointKeys[nCount + nHeaderIncrement] = footerSnapPointKey;

					switch (alignment)
					{
						case SnapPointsAlignment.Near:
							{
								pSnapPoints[nCount + nHeaderIncrement] = footerSnapPointKey;
								break;
							}
						case SnapPointsAlignment.Center:
							{
								pSnapPoints[nCount + nHeaderIncrement] = footerSnapPointKey + (float)(footerSize / 2);
								break;
							}
						case SnapPointsAlignment.Far:
							{
								pSnapPoints[nCount + nHeaderIncrement] = footerSnapPointKey + (float)footerSize;
								break;
							}
					}
				}

				cSnapPoints += nHeaderIncrement + nFooterIncrement;
				cSnapPointKeys += nHeaderIncrement + nFooterIncrement;
			}
			else if (interval > 0)
			{
				// let's prepare 5 screens of snap points.
				// Since footers are not supported for logical panels, we will
				// not add a footer snap point if our panel is logical.

				float viewport = 0.0f;
				float count = 0.0f;
				float start = 0.0f;
				uint nCount = 0;

				float snapPoint = 0;

				if (m_tpScrollSnapPointsInfo is ModernCollectionBasePanel)
				{
					// Get the viewport and offset of the ScrollViewer
					ScrollViewer? spScrollViewer = null;
					m_wrScrollViewer?.TryGetTarget(out spScrollViewer);

					if (spScrollViewer is not null)
					{
						float zoomFactor;
						float scrollOffset;

						zoomFactor = spScrollViewer.ZoomFactor;

						if (orientation == Orientation.Horizontal)
						{
							double temp;
							temp = spScrollViewer.ViewportWidth;
							viewport = (float)temp / zoomFactor;
							temp = spScrollViewer.HorizontalOffset;
							scrollOffset = (float)temp / zoomFactor;
						}
						else
						{
							double temp;
							temp = spScrollViewer.ViewportHeight;
							viewport = (float)temp / zoomFactor;
							temp = spScrollViewer.VerticalOffset;
							scrollOffset = (float)temp / zoomFactor;
						}

						count = viewport / interval * 5;

						float startOfPanel = (float)(leadingSnapPointKey + headerSize);
						float startOfSnapPointWindowOnPanel = Math.Max(scrollOffset - (2 * viewport) - startOfPanel, 0.0f);

						// If alignment is near or center, we just need to start from the
						// first snap point and count intervals until we reach our desired pixel start
						if (alignment == SnapPointsAlignment.Near ||
							alignment == SnapPointsAlignment.Center)
						{
							float intervalsToWindow = Math.Max((startOfSnapPointWindowOnPanel - offset) / interval, 0.0f);
							snapPoint = offset + ((float)Math.Floor(intervalsToWindow) * interval);
							if (alignment == SnapPointsAlignment.Center)
							{
								snapPoint -= interval / 2;
							}
						}
						else
						{
							// We need to walk backwards from the end snap point
							float endOfPanel = (float)(leadingSnapPointKey + headerSize + panelSize);
							float intervalsToWindow = Math.Max((endOfPanel - offset - startOfSnapPointWindowOnPanel) / interval, 0.0f);
							snapPoint = (float)panelSize - offset - ((float)Math.Ceiling(intervalsToWindow) * interval);
						}
					}
				}
				else
				{
					// Look at the viewport and offset of the panel if it is logical
					var viewportSize = DesiredSize;

					if (orientation == Orientation.Horizontal)
					{
						viewport = (float)viewportSize.Width;
					}
					else
					{
						viewport = (float)viewportSize.Height;
					}

					count = viewport / interval * 5;
					start = viewport / interval * 2;

					double panelOffset = 0;
					double panelMaxOffset = 0;
					panelOffset = GetInnerPanelOffset(orientation == Orientation.Horizontal);
					panelMaxOffset = GetInnerPanelsScrollableDimension(orientation == Orientation.Horizontal);
					panelOffset = Math.Max(panelOffset + Math.Min(panelMaxOffset - (panelOffset + (count - start)), 0) - start, 0);
					snapPoint = (float)DoubleUtil.Floor(panelOffset) * interval;
				}

				nCount = (uint)DoubleUtil.Floor(count);

				if (nCount > 0)
				{
					pSnapPoints = new float[nCount];
					pSnapPointKeys = new float[nCount];
				}

				for (cSnapPoints = 0; cSnapPoints < nCount; ++cSnapPoints, ++cSnapPointKeys)
				{
					switch (alignment)
					{
						case SnapPointsAlignment.Near:
							{
								if (headerSize > 0 && cSnapPoints == 0)
								{
									pSnapPoints![cSnapPoints] = (float)leadingSnapPointKey;
									pSnapPointKeys![cSnapPointKeys] = (float)leadingSnapPointKey;
								}
								else
								{
									pSnapPoints![cSnapPoints] = (float)(leadingSnapPointKey + headerSize + snapPoint);
									pSnapPointKeys![cSnapPointKeys] = (float)(leadingSnapPointKey + headerSize + snapPoint);
									snapPoint += interval;
								}
								break;
							}
						case SnapPointsAlignment.Center:
							{
								if (headerSize > 0 && cSnapPoints == 0)
								{
									pSnapPoints![cSnapPoints] = (float)(leadingSnapPointKey + headerSize / 2);
									pSnapPointKeys![cSnapPointKeys] = (float)leadingSnapPointKey;
								}
								else
								{
									pSnapPoints![cSnapPoints] = (float)(leadingSnapPointKey + headerSize + snapPoint + interval / 2);
									pSnapPointKeys![cSnapPointKeys] = (float)(leadingSnapPointKey + headerSize + snapPoint);
									snapPoint += interval;
								}
								break;
							}
						case SnapPointsAlignment.Far:
							{
								if (headerSize > 0 && cSnapPoints == 0)
								{
									pSnapPoints![cSnapPoints] = (float)(leadingSnapPointKey + headerSize);
									pSnapPointKeys![cSnapPointKeys] = (float)leadingSnapPointKey;
								}
								else
								{
									pSnapPoints![cSnapPoints] = (float)(leadingSnapPointKey + headerSize + snapPoint + interval);
									pSnapPointKeys![cSnapPointKeys] = (float)(leadingSnapPointKey + headerSize + snapPoint);
									snapPoint += interval;
								}
								break;
							}
					}
				}
			}

			if (cSnapPoints > 0)
			{
				pValue = pSnapPoints;
			}

			if (cSnapPointKeys > 0)
			{
				m_pIrregularSnapPointKeys = pSnapPointKeys;
				m_cIrregularSnapPointKeys = (int)cSnapPointKeys;
				// TODO figure out how to re-pack offset
				// m_irregularSnapPointKeysOffset = irregularSnapPointKeysOffset;
				m_bAreSnapPointsKeysHorizontal = orientation == Orientation.Horizontal;
				pSnapPointKeys = null;
			}

			m_leadingSnapPointKey = leadingSnapPointKey;
			m_trailingSnapPointKey = trailingSnapPointKey;
			m_leadingMarginSnapPointKey = leadingMarginSnapPointKey;
			m_trailingMarginSnapPointKey = trailingMarginSnapPointKey;
			m_bAreSnapPointsKeysHorizontal = (orientation == Orientation.Horizontal);

			// Next snap point change needs to raise a notification
			if (m_bAreSnapPointsKeysHorizontal)
			{
				m_bNotifiedHorizontalSnapPointsChanges = false;
			}
			else
			{
				m_bNotifiedVerticalSnapPointsChanges = false;
			}
		}

		return pValue;
	}

	//-------------------------------------------------------------------------
	//
	//  Function:   ItemsPresenter::add_HorizontalSnapPointsChanged
	//
	//  Synopsis:
	//    Adds an event handler for the HorizontalSnapPointsChanged event.
	//
	//-------------------------------------------------------------------------
	//-------------------------------------------------------------------------
	//
	//  Function:   OrientedVirtualizingPanel::remove_HorizontalSnapPointsChanged
	//
	//  Synopsis:
	//    Removes an event handler for the HorizontalSnapPointsChanged event.
	//
	//-------------------------------------------------------------------------
	public event EventHandler<object> HorizontalSnapPointsChanged
	{
		add
		{
			if (m_horizontalSnapPointsChanged is null)
			{
				SetSnapPointsChangeNotificationsRequirement(
					true /*isForHorizontalSnapPoints*/,
					true /*notifyChanges*/);

				HookScrollSnapPointsInfoEvents(true);
			}

			m_horizontalSnapPointsChanged += value;
		}
		remove
		{
			m_horizontalSnapPointsChanged -= value;

			if (m_horizontalSnapPointsChanged is null)
			{
				SetSnapPointsChangeNotificationsRequirement(
					true /*isForHorizontalSnapPoints*/,
					false /*notifyChanges*/);

				UnhookScrollSnapPointsInfoEvents(true);
			}
		}
	}

	//-------------------------------------------------------------------------
	//
	//  Function:   OrientedVirtualizingPanel::add_VerticalSnapPointsChanged
	//
	//  Synopsis:
	//    Adds an event handler for the VerticalSnapPointsChanged event.
	//
	//-------------------------------------------------------------------------
	//-------------------------------------------------------------------------
	//
	//  Function:   OrientedVirtualizingPanel::remove_VerticalSnapPointsChanged
	//
	//  Synopsis:
	//    Removes an event handler for the VerticalSnapPointsChanged event.
	//
	//-------------------------------------------------------------------------
	public event EventHandler<object> VerticalSnapPointsChanged
	{
		add
		{
			if (m_verticalSnapPointsChanged is null)
			{
				SetSnapPointsChangeNotificationsRequirement(
					false /*isForHorizontalSnapPoints*/,
					true /*notifyChanges*/);
				HookScrollSnapPointsInfoEvents(false);
			}

			m_verticalSnapPointsChanged += value;
		}
		remove
		{
			m_verticalSnapPointsChanged -= value;

			if (m_verticalSnapPointsChanged is null)
			{
				SetSnapPointsChangeNotificationsRequirement(
					false /*isForHorizontalSnapPoints*/,
					false /*notifyChanges*/);

				UnhookScrollSnapPointsInfoEvents(false);
			}
		}
	}

	// End of IScrollSnapPointsInfo interface implementation

	//-------------------------------------------------------------------------
	//
	//  Function:   ItemsPresenter::AreScrollSnapPointsRegular
	//
	//  Synopsis:
	//    Returns combined information about children snap points and self
	//    True only if children snap points are regular and LeadingPadding==HeaderSize==ContainerSize=TrailingPadding
	//
	//-------------------------------------------------------------------------
	private bool AreScrollSnapPointsRegular(bool isForHorizontalSnapPoints)
	{
		var areSnapPointsRegular = false;

		if (m_tpScrollSnapPointsInfo is { } spScrollSnapPointsInfo)
		{
			if (isForHorizontalSnapPoints)
			{
				areSnapPointsRegular = spScrollSnapPointsInfo.AreHorizontalSnapPointsRegular;
			}
			else
			{
				areSnapPointsRegular = spScrollSnapPointsInfo.AreVerticalSnapPointsRegular;
			}
		}

		var areScrollSnapPointsRegular = areSnapPointsRegular;

		if (areSnapPointsRegular)
		{
			double leadingPaddingSize = 0;
			double trailingPaddingSize = 0;
			double headerSize = 0;
			double footerSize = 0;

			trailingPaddingSize = GetSentinelItemSize(TrailingPaddingSentinelIndex, isForHorizontalSnapPoints);
			leadingPaddingSize = GetSentinelItemSize(LeadingPaddingSentinelIndex, isForHorizontalSnapPoints);
			headerSize = GetSentinelItemSize(HeaderSentinelIndex, isForHorizontalSnapPoints);
			footerSize = GetSentinelItemSize(FooterSentinelIndex, isForHorizontalSnapPoints);

			// is we don't affect layout then we regular
			if (trailingPaddingSize == 0 && leadingPaddingSize == 0 && headerSize == 0 && footerSize == 0)
			{
				return areScrollSnapPointsRegular;
			}

			var containerSize = GetSizeOfContainer();

			areScrollSnapPointsRegular = DoubleUtil.Floor(trailingPaddingSize) == DoubleUtil.Floor(leadingPaddingSize);
			areScrollSnapPointsRegular &= DoubleUtil.Floor(leadingPaddingSize) == DoubleUtil.Floor(footerSize);
			areScrollSnapPointsRegular &= DoubleUtil.Floor(footerSize) == DoubleUtil.Floor(headerSize);
			areScrollSnapPointsRegular &= DoubleUtil.Floor(headerSize) == DoubleUtil.Floor(isForHorizontalSnapPoints ? containerSize.Width : containerSize.Height);
		}

		return areScrollSnapPointsRegular;
	}

	//-------------------------------------------------------------------------
	//
	//  Function:   ItemsPresenter::SetSnapPointsChangeNotificationsRequirement
	//
	//  Synopsis:
	//    Determines whether the OrientedVirtualizingPanel must call NotifySnapPointsChanged
	//    when snap points change or not.
	//
	//-------------------------------------------------------------------------
	private void SetSnapPointsChangeNotificationsRequirement(bool isForHorizontalSnapPoints, bool notifyChanges)
	{
		var isHorizontal = IsHorizontal();

		var areScrollSnapPointsRegular = AreScrollSnapPointsRegular(isHorizontal);

		if (isForHorizontalSnapPoints)
		{
			m_bNotifyHorizontalSnapPointsChanges = notifyChanges;
			if (isHorizontal && notifyChanges)
			{
				if (areScrollSnapPointsRegular)
				{
					RefreshRegularSnapPointKeys();
				}
				else
				{
					RefreshIrregularSnapPointKeys();
				}
				m_bNotifiedHorizontalSnapPointsChanges = false;
			}
		}
		else
		{
			m_bNotifyVerticalSnapPointsChanges = notifyChanges;
			if (isHorizontal && notifyChanges)
			{
				if (areScrollSnapPointsRegular)
				{
					RefreshRegularSnapPointKeys();
				}
				else
				{
					RefreshIrregularSnapPointKeys();
				}
				m_bNotifiedVerticalSnapPointsChanges = false;
			}
		}
	}

	//-------------------------------------------------------------------------
	//
	//  Function:   ItemsPresenter::RefreshRegularSnapPointKeys
	//
	//  Synopsis:
	//    Refreshes the m_regularSnapPointKey field based on a single child.
	//    Refreshes also the m_leadingSnapPointKey/m_trailingSnapPointKey fields based
	//    on the current margins.
	//
	//-------------------------------------------------------------------------
	private void RefreshRegularSnapPointKeys()
	{
		var isHorizontal = IsHorizontal();

		global::System.Diagnostics.Debug.Assert(AreScrollSnapPointsRegular(isHorizontal));

		ResetSnapPointKeys();

		m_regularSnapPointKey = 0.0f;

		GetRegularSnapPointKeys(isHorizontal, out var snapPointKey, out var leadingSnapPointKey, out var trailingSnapPointKey, out var leadingMarginSnapPointKey, out var trailingMarginSnapPointKey);

		m_bAreSnapPointsKeysHorizontal = isHorizontal;
		m_regularSnapPointKey = snapPointKey;
		m_leadingSnapPointKey = leadingSnapPointKey;
		m_trailingSnapPointKey = trailingSnapPointKey;
		m_leadingMarginSnapPointKey = leadingMarginSnapPointKey;
		m_trailingMarginSnapPointKey = trailingMarginSnapPointKey;
	}

	//-------------------------------------------------------------------------
	//
	//  Function:   ItemsPresenter::RefreshIrregularSnapPointKeys
	//
	//  Synopsis:
	//    Refreshes the m_pIrregularSnapPointKeys/m_cIrregularSnapPointKeys
	//    fields based on all children.
	//
	//-------------------------------------------------------------------------
	private void RefreshIrregularSnapPointKeys()
	{
		var isHorizontal = IsHorizontal();

		global::System.Diagnostics.Debug.Assert(!AreScrollSnapPointsRegular(isHorizontal));

		ResetSnapPointKeys();

		GetIrregularSnapPointKeys(
			isHorizontal,
			out var pSnapPointKeys,
			out var cSnapPointKeys,
			out var irregularSnapPointKeysOffset,
			out var leadingSnapPointKey,
			out var trailingSnapPointKey,
			out var leadingMarginSnapPointKey,
			out var trailingMarginSnapPointKey);

		m_pIrregularSnapPointKeys = pSnapPointKeys;
		m_cIrregularSnapPointKeys = cSnapPointKeys;
		m_irregularSnapPointKeysOffset = irregularSnapPointKeysOffset;
		m_leadingSnapPointKey = leadingSnapPointKey;
		m_trailingSnapPointKey = trailingSnapPointKey;
		m_leadingMarginSnapPointKey = leadingMarginSnapPointKey;
		m_trailingMarginSnapPointKey = trailingMarginSnapPointKey;
		m_bAreSnapPointsKeysHorizontal = isHorizontal;
	}

	//-------------------------------------------------------------------------
	//
	//  Function:   ItemsPresenter::ResetSnapPointKeys
	//
	//  Synopsis:
	//    Resets regular snap point keys.
	//
	//-------------------------------------------------------------------------
	private void ResetSnapPointKeys()
	{
		m_regularSnapPointKey = -1.0f;
		m_leadingSnapPointKey = 0;
		m_trailingSnapPointKey = 0;
		m_leadingMarginSnapPointKey = 0;
		m_trailingMarginSnapPointKey = 0;

		m_pIrregularSnapPointKeys = null;
		m_cIrregularSnapPointKeys = -1;
		m_irregularSnapPointKeysOffset = 0.0f;
	}

	//-------------------------------------------------------------------------
	//
	//  Function:   ItemsPresenter::GetRegularSnapPointKeys
	//
	//  Synopsis:
	//    Determines the keys for regular snap points.
	//
	//-------------------------------------------------------------------------
	private void GetRegularSnapPointKeys(
		bool isForHorizontalSnapPoints,
		out float pSnapPointKey,
		out double pLeadingSnapPointKey,
		out double pTrailingSnapPointKey,
		out double pLeadingMarginSnapPointKey,
		out double pTrailingMarginSnapPointKey)
	{
		var containerSize = GetSizeOfContainer();
		pSnapPointKey = (float)(isForHorizontalSnapPoints ? containerSize.Width : containerSize.Height);

		GetCommonSnapPointKeys(out pLeadingSnapPointKey, out pTrailingSnapPointKey, out pLeadingMarginSnapPointKey, out pTrailingMarginSnapPointKey);
	}

	//-------------------------------------------------------------------------
	//
	//  Function:   ItemsPresenter::GetIrregularSnapPointKeys
	//
	//  Synopsis:
	//    Determines the keys for irregular snap points.
	//
	//-------------------------------------------------------------------------
	private void GetIrregularSnapPointKeys(
		bool isForHorizontalSnapPoints,
		out float[]? ppSnapPointKeys,
		out int pcSnapPointKeys,
		out float pSnapPointKeysOffset,
		out double pLeadingSnapPointKey,
		out double pTrailingSnapPointKey,
		out double pLeadingMarginSnapPointKey,
		out double pTrailingMarginSnapPointKey)
	{
		ppSnapPointKeys = null;
		pcSnapPointKeys = 0;
		pSnapPointKeysOffset = 0.0f;
		pLeadingSnapPointKey = 0.0;
		pTrailingSnapPointKey = 0.0;
		pLeadingMarginSnapPointKey = 0.0;
		pTrailingMarginSnapPointKey = 0.0;

		// for now we are not storing irregular keys, it make every arrange to invalidate snap points.
	}

	//-------------------------------------------------------------------------
	//
	//  Function:   ItemsPresenter::GetCommonSnapPointKeys
	//
	//  Synopsis:
	//    Determines the common keys for regular and irregular snap points.
	//    Those keys are the left/right padding for a horizontal panel,
	//    or the top/bottom padding for a vertical panel.
	//
	//-------------------------------------------------------------------------
	private void GetCommonSnapPointKeys(
		out double pLeadingSnapPointKey,
		out double pTrailingSnapPointKey,
		out double pLeadingMarginSnapPointKey,
		out double pTrailingMarginSnapPointKey)
	{
		pLeadingMarginSnapPointKey = 0.0;
		pTrailingMarginSnapPointKey = 0.0;

		var isHorizontal = IsHorizontal();

		pTrailingSnapPointKey = GetSentinelItemSize(TrailingPaddingSentinelIndex, isHorizontal);
		pLeadingSnapPointKey = GetSentinelItemSize(LeadingPaddingSentinelIndex, isHorizontal);

		if (Panel is not FrameworkElement spPanel)
		{
			throw new InvalidOperationException("The ItemsPresenter has no panel.");
		}

		var panelsMargins = spPanel.Margin;
		if (isHorizontal)
		{
			pLeadingMarginSnapPointKey = panelsMargins.Left;
			pTrailingMarginSnapPointKey = panelsMargins.Right;
		}
		else
		{
			pLeadingMarginSnapPointKey = panelsMargins.Top;
			pTrailingMarginSnapPointKey = panelsMargins.Bottom;
		}
	}

	//-------------------------------------------------------------------------
	//
	//  Function:   ItemsPresenter::NotifySnapPointsChanges
	//
	//  Synopsis:
	//    Checks if the snap point keys have changed and a notification needs
	//    to be raised.
	//
	//-------------------------------------------------------------------------
	private void NotifySnapPointsChanges()
	{
		int cSnapPointKeys = 0;
		float[]? pSnapPointKeys = null;
		float snapPointKeysOffset = 0.0f;
		float snapPointKey = 0.0f;
		double leadingSnapPointKey = 0.0;
		double trailingSnapPointKey = 0.0;
		double leadingMarginSnapPointKey = 0.0;
		double trailingMarginSnapPointKey = 0.0;
		var notifyForHorizontalSnapPoints = false;
		var notifyForVerticalSnapPoints = false;

		var isHorizontal = IsHorizontal();
		var areScrollSnapPointsRegular = AreScrollSnapPointsRegular(isHorizontal);

		if (!isHorizontal)
		{
			if (((m_regularSnapPointKey != -1.0) || (m_cIrregularSnapPointKeys != -1)) &&
				m_bAreSnapPointsKeysHorizontal && m_bNotifyHorizontalSnapPointsChanges)
			{
				// Last computed snap point keys were for horizontal orientation.
				// New orientation is vertical.
				// Consumer wants notifications for horizontal snap points.
				notifyForHorizontalSnapPoints = true;
			}
		}
		else
		{
			if (((m_regularSnapPointKey != -1.0) || (m_cIrregularSnapPointKeys != -1)) &&
				!m_bAreSnapPointsKeysHorizontal && m_bNotifyVerticalSnapPointsChanges)
			{
				// Last computed snap point keys were for vertical orientation.
				// New orientation is horizontal.
				// Consumer wants notifications for vertical snap points.
				notifyForVerticalSnapPoints = true;
			}
		}

		if ((m_bNotifyHorizontalSnapPointsChanges && isHorizontal &&
			 m_bAreSnapPointsKeysHorizontal && !m_bNotifiedHorizontalSnapPointsChanges) ||
			(m_bNotifyVerticalSnapPointsChanges && !isHorizontal &&
			 !m_bAreSnapPointsKeysHorizontal && !m_bNotifiedVerticalSnapPointsChanges))
		{
			if (m_regularSnapPointKey != -1.0)
			{
				if (areScrollSnapPointsRegular)
				{
					GetRegularSnapPointKeys(isHorizontal, out snapPointKey, out leadingSnapPointKey, out trailingSnapPointKey, out leadingMarginSnapPointKey, out trailingMarginSnapPointKey);
					if (m_regularSnapPointKey != snapPointKey ||
						m_leadingSnapPointKey != leadingSnapPointKey ||
						m_trailingSnapPointKey != trailingSnapPointKey ||
						m_leadingMarginSnapPointKey != leadingMarginSnapPointKey ||
						m_trailingMarginSnapPointKey != trailingMarginSnapPointKey)
					{
						if (m_bAreSnapPointsKeysHorizontal)
						{
							notifyForHorizontalSnapPoints = true;
						}
						else
						{
							notifyForVerticalSnapPoints = true;
						}
					}
				}
				else
				{
					if (m_bAreSnapPointsKeysHorizontal)
					{
						notifyForHorizontalSnapPoints = true;
					}
					else
					{
						notifyForVerticalSnapPoints = true;
					}
				}
			}
			else if (m_cIrregularSnapPointKeys != -1)
			{
				if (!areScrollSnapPointsRegular)
				{
					GetIrregularSnapPointKeys(isHorizontal, out pSnapPointKeys, out cSnapPointKeys, out snapPointKeysOffset, out leadingSnapPointKey, out trailingSnapPointKey, out leadingMarginSnapPointKey, out trailingMarginSnapPointKey);
					if (m_cIrregularSnapPointKeys != cSnapPointKeys ||
						m_irregularSnapPointKeysOffset != snapPointKeysOffset ||
						m_leadingSnapPointKey != leadingSnapPointKey ||
						m_trailingSnapPointKey != trailingSnapPointKey ||
						m_leadingMarginSnapPointKey != leadingMarginSnapPointKey ||
						m_trailingMarginSnapPointKey != trailingMarginSnapPointKey)
					{
						if (m_bAreSnapPointsKeysHorizontal)
						{
							notifyForHorizontalSnapPoints = true;
						}
						else
						{
							notifyForVerticalSnapPoints = true;
						}
					}
					else
					{
						for (var iSnapPointKey = 0; iSnapPointKey < cSnapPointKeys; iSnapPointKey++)
						{
							if (m_pIrregularSnapPointKeys![iSnapPointKey] != pSnapPointKeys![iSnapPointKey])
							{
								if (m_bAreSnapPointsKeysHorizontal)
								{
									notifyForHorizontalSnapPoints = true;
								}
								else
								{
									notifyForVerticalSnapPoints = true;
								}
								break;
							}
						}
					}
				}
				else
				{
					if (m_bAreSnapPointsKeysHorizontal)
					{
						notifyForHorizontalSnapPoints = true;
					}
					else
					{
						notifyForVerticalSnapPoints = true;
					}
				}
			}
		}

		if (notifyForHorizontalSnapPoints)
		{
			NotifySnapPointChanges(true /*isForHorizontalSnapPoints*/);
		}

		if (notifyForVerticalSnapPoints)
		{
			NotifySnapPointChanges(false /*isForHorizontalSnapPoints*/);
		}
	}

	//-------------------------------------------------------------------------
	//
	//  Function:   ItemsPresenter::NotifySnapPointChanges
	//
	//  Synopsis:
	//
	//
	//-------------------------------------------------------------------------
	private void NotifySnapPointChanges(bool isForHorizontalSnapPoints)
	{
		if ((isForHorizontalSnapPoints && m_bNotifyHorizontalSnapPointsChanges && !m_bNotifiedHorizontalSnapPointsChanges) ||
			(!isForHorizontalSnapPoints && m_bNotifyVerticalSnapPointsChanges && !m_bNotifiedVerticalSnapPointsChanges))
		{
			if (isForHorizontalSnapPoints)
			{
				// Raise HorizontalSnapPointsChanged event.
				m_bNotifiedHorizontalSnapPointsChanges = true;
				OnHorizontalSnapPointsChanged();
			}
			else
			{
				// Raise VerticalSnapPointsChanged event.
				m_bNotifiedVerticalSnapPointsChanges = true;
				OnVerticalSnapPointsChanged();
			}
		}
	}

#if !HAS_UNO // TODO Uno: DMMotionTypes is not ported, the hooked handlers call the raisers directly
	// Called by HorizontalSnapPointsChangedHandler and
	// VerticalSnapPointsChangedHandler when snap points changed
	private void OnSnapPointsChanged(DMMotionTypes motionType)
	{
		switch (motionType)
		{
			case DMMotionTypes.DMMotionTypePanX:
				OnHorizontalSnapPointsChanged();
				break;

			case DMMotionTypes.DMMotionTypePanY:
				OnVerticalSnapPointsChanged();
				break;

			default:
				throw new InvalidOperationException("E_UNEXPECTED");
		}
	}
#endif

	//-------------------------------------------------------------------------
	//
	//  Function:   ItemsPresenter::OnHorizontalSnapPointsChanged
	//
	//  Synopsis:
	//    Raises the HorizontalSnapPointsChanged event.
	//
	//-------------------------------------------------------------------------
	private void OnHorizontalSnapPointsChanged()
	{
		// Raise the event
		m_horizontalSnapPointsChanged?.Invoke(this, EventArgs.Empty);
	}

	//-------------------------------------------------------------------------
	//
	//  Function:   ItemsPresenter::OnVerticalSnapPointsChanged
	//
	//  Synopsis:
	//    Raises the VerticalSnapPointsChanged event.
	//
	//-------------------------------------------------------------------------
	private void OnVerticalSnapPointsChanged()
	{
		// Raise the event
		m_verticalSnapPointsChanged?.Invoke(this, EventArgs.Empty);
	}
}
