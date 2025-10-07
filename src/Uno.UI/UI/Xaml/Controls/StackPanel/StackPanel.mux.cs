using System;
using Windows.Foundation;
using static Microsoft.UI.Xaml.Controls._Tracing;

namespace Microsoft.UI.Xaml.Controls;

partial class StackPanel
{
	protected override Size MeasureOverride(Size availableSize)
	{
		var stackDesiredSize = new Size();
		var combinedThickness = Border.HelperGetCombinedThickness(this);
		var childConstraint = (Orientation == Orientation.Vertical) ?
			new Size(availableSize.Width - combinedThickness.Width, double.PositiveInfinity) :
			new Size(double.PositiveInfinity, availableSize.Height - combinedThickness.Height);

		var children = GetUnsortedChildren();
		var childrenCount = children.Count;
		int visibleChildrenCount = 0;

		for (int childIndex = 0; childIndex < childrenCount; childIndex++)
		{
			UIElement currentChild = children[childIndex];
			MUX_ASSERT(currentChild != null);

			currentChild.Measure(childConstraint);
			//currentChild.EnsureLayoutStorage();

			if (Orientation == Orientation.Vertical)
			{
				stackDesiredSize.Width = Math.Max(stackDesiredSize.Width, currentChild.DesiredSize.Width);//currentChild.GetLayoutStorage().m_desiredSize.Width);
				stackDesiredSize.Height += currentChild.DesiredSize.Height;//currentChild.GetLayoutStorage().m_desiredSize.Height;
			}
			else
			{
				stackDesiredSize.Width += currentChild.DesiredSize.Width;//currentChild.GetLayoutStorage().m_desiredSize.Width;
				stackDesiredSize.Height = Math.Max(stackDesiredSize.Height, currentChild.DesiredSize.Height);//currentChild.GetLayoutStorage().m_desiredSize.Height);
			}

			if (currentChild.IsVisible())
			{
				visibleChildrenCount++;
			}
		}

		stackDesiredSize.Width += combinedThickness.Width;
		stackDesiredSize.Height += combinedThickness.Height;

		if (visibleChildrenCount > 1)
		{
			double combinedSpacing = Spacing * (visibleChildrenCount - 1);
			if (Orientation == Orientation.Vertical)
			{
				stackDesiredSize.Height += combinedSpacing;
			}
			else
			{
				stackDesiredSize.Width += combinedSpacing;
			}
		}

		return stackDesiredSize;
	}

	protected override Size ArrangeOverride(Size finalSize)
	{
		var newFinalSize = finalSize;

		Rect arrangeRect = Border.HelperGetInnerRect(this, finalSize);
		var spacing = Spacing;

		var children = GetUnsortedChildren();
		var childrenCount = children.Count;

		for (var childIndex = 0; childIndex < childrenCount; childIndex++)
		{
			UIElement currentChild = children[childIndex];
			MUX_ASSERT(currentChild != null);

			//currentChild.EnsureLayoutStorage();

			if (Orientation == Orientation.Vertical)
			{
				arrangeRect.Height = currentChild.DesiredSize.Height;//currentChild.GetLayoutStorage().m_desiredSize.Height;
				arrangeRect.Width = Math.Max(arrangeRect.Width, currentChild.DesiredSize.Width); //currentChild.GetLayoutStorage().m_desiredSize.Width);
			}
			else
			{
				arrangeRect.Width = currentChild.DesiredSize.Width;//currentChild.GetLayoutStorage().m_desiredSize.Width;
				arrangeRect.Height = Math.Max(arrangeRect.Height, currentChild.DesiredSize.Height);//currentChild.GetLayoutStorage().m_desiredSize.Height);
			}

			currentChild.Arrange(arrangeRect);

			// Offset the rect for the next child.
			if (currentChild.IsVisible())
			{
				if (Orientation == Orientation.Vertical)
				{
					arrangeRect.Y += arrangeRect.Height + spacing;
				}
				else
				{
					arrangeRect.X += arrangeRect.Width + spacing;
				}
			}
		}

		// UIElement_NotifySnapPointsChanged might have to be called because of
		// snap point updates.
		NotifySnapPointsChanges(children);

		return newFinalSize;
	}

	/// DirectManipulation-specific implementations

	/// <summary>
	/// Used to retrieve an array of irregular snap points.
	/// </summary>
	/// <param name="horizontalOrientation">True when horizontal snap points are requested.</param>
	/// <param name="nearAligned">True when requested snap points will align to the left/top of the children.</param>
	/// <param name="farAligned">True when requested snap points will align to the right/bottom of the children.</param>
	/// <returns>Array of irregular snap points.</returns>
	private void GetIrregularSnapPoints(
		 bool horizontalOrientation,
		 bool nearAligned,
		 bool farAligned,
		 out float[] snapPoints,
		 out int snapPointsCounter)
	{
		bool bIsFirstChild = true;
		double childDim = 0.0;
		double cumulatedDim = 0.0;
		snapPointsCounter = 0;
		int snapPointKeysCounter = 0;

		snapPoints = null;
		float[] snapPointKeys = null;

		if ((Orientation == Orientation.Vertical && !horizontalOrientation) ||
			(Orientation == Orientation.Horizontal && horizontalOrientation))
		{
			if (m_bAreScrollSnapPointsRegular)
			{
				throw new InvalidOperationException(
					"Accessing the irregular snap points while AreScrollSnapPointsRegular " +
					"is True is not supported.");
			}

			ResetSnapPointKeys();

			GetCommonSnapPointKeys(out var lowerMarginSnapPointKey, out var upperMarginSnapPointKey);

			var children = GetUnsortedChildren();
			var count = children.Count;
			MUX_ASSERT(count <= int.MaxValue - 1);
			count = Math.Min(count, int.MaxValue - 1);

			if (count > 0)
			{
				snapPoints = new float[count];

				snapPointKeys = new float[count];

				for (var childIndex = 0; childIndex < count; childIndex++)
				{
					UIElement child = children[childIndex];
					if (child is not null)
					{
						// pChild.EnsureLayoutStorage();

						if (nearAligned)
						{
							// Snap points are aligned to the left/top of the children
							snapPoints[snapPointsCounter] = (float)cumulatedDim;
						}

						if (Orientation == Orientation.Vertical)
						{
							childDim = child.DesiredSize.Height;//pChild.GetLayoutStorage().m_desiredSize.Height;
						}
						else
						{
							childDim = child.DesiredSize.Width;//pChild.GetLayoutStorage().m_desiredSize.Width;
						}

						if (!nearAligned && !farAligned)
						{
							// Snap points are centered on the children
							snapPoints[snapPointsCounter] = (float)cumulatedDim + (float)childDim / 2;
						}

						cumulatedDim += childDim;

						if (farAligned)
						{
							// Snap points are aligned to the right/bottom of the children
							snapPoints[snapPointsCounter] = (float)cumulatedDim;
						}

						if (!(nearAligned && bIsFirstChild))
						{
							// Do not include the lower margin for the first child's snap point when the alignment is Near
							snapPoints[snapPointsCounter] += (float)lowerMarginSnapPointKey;
						}

						bIsFirstChild = false;

						snapPointKeys[snapPointKeysCounter] = (float)childDim;

						snapPointsCounter++;
						snapPointKeysCounter++;
					}
				}
			}

			m_pIrregularSnapPointKeys = snapPointKeys;
			m_cIrregularSnapPointKeys = snapPointKeysCounter;
			m_lowerMarginSnapPointKey = lowerMarginSnapPointKey;
			m_upperMarginSnapPointKey = upperMarginSnapPointKey;
			m_bAreSnapPointsKeysHorizontal = horizontalOrientation;

			// Next snap point change needs to raise a notification
			if (horizontalOrientation)
			{
				m_bNotifiedHorizontalSnapPointsChanges = false;
			}
			else
			{
				m_bNotifiedVerticalSnapPointsChanges = false;
			}
		}
	}

	/// <summary>
	/// Used to retrieve an offset and interval for regular snap points.
	/// </summary>
	/// <param name="horizontalOrientation">True when horizontal snap points are requested.</param>
	/// <param name="nearAligned">True when requested snap points will align to the left/top of the children.</param>
	/// <param name="farAligned">True when requested snap points will align to the right/bottom of the children.</param>
	/// <param name="offset">Placeholder for snap points offset.</param>
	/// <param name="interval">Placeholder for snap points interval.</param>
	/// <exception cref="InvalidOperationException">Thrown when snap points are not regular.</exception>
	private void GetRegularSnapPoints(
		bool horizontalOrientation,
		bool nearAligned,
		bool farAligned,
		out float offset,
		out float interval)
	{
		double childDim = 0.0;
		double lowerMarginSnapPointKey = 0.0;
		double upperMarginSnapPointKey = 0.0;

		offset = 0.0f;
		interval = 0.0f;

		if ((Orientation == Orientation.Vertical && !horizontalOrientation) ||
			(Orientation == Orientation.Horizontal && horizontalOrientation))
		{
			if (!m_bAreScrollSnapPointsRegular)
			{
				// Accessing the regular snap points while AreScrollSnapPointsRegular is False is not supported.
				throw new InvalidOperationException("Accessing the regular snap points while AreScrollSnapPointsRegular is False is not supported.");
			}

			ResetSnapPointKeys();

			GetCommonSnapPointKeys(out lowerMarginSnapPointKey, out upperMarginSnapPointKey);

			var children = GetUnsortedChildren();
			var count = children.Count;
			for (var childIndex = 0; childIndex < count; childIndex++)
			{
				UIElement child = children[childIndex];
				if (child is not null)
				{
					//child.EnsureLayoutStorage();

					if (Orientation == Orientation.Vertical)
					{
						childDim = child.DesiredSize.Height;
					}
					else
					{
						childDim = child.DesiredSize.Width;
					}

					if (nearAligned)
					{
						offset = (float)lowerMarginSnapPointKey;
					}
					else if (farAligned)
					{
						offset = (float)upperMarginSnapPointKey;
					}
					else if (!nearAligned && !farAligned)
					{
						// Snap points are centered on the children
						offset = (float)childDim / 2 + (float)lowerMarginSnapPointKey;
					}
					interval = (float)childDim;
					break;
				}
			}

			m_regularSnapPointKey = childDim;
			m_lowerMarginSnapPointKey = lowerMarginSnapPointKey;
			m_upperMarginSnapPointKey = upperMarginSnapPointKey;
			m_bAreSnapPointsKeysHorizontal = horizontalOrientation;

			// Next snap point change needs to raise a notification
			if (horizontalOrientation)
			{
				m_bNotifiedHorizontalSnapPointsChanges = false;
			}
			else
			{
				m_bNotifiedVerticalSnapPointsChanges = false;
			}
		}
	}

	////-------------------------------------------------------------------------
	////
	////  Function:   CStackPanel.GetIrregularSnapPointKeys
	////
	////  Synopsis:
	////    Determines the keys for irregular snap points.
	////
	////-------------------------------------------------------------------------

	//HRESULT
	private void GetIrregularSnapPointKeys(
		UIElementCollection children,
		out float[] ppSnapPointKeys,
		out int pcSnapPointKeys,
		out double pLowerMarginSnapPointKey,
		out double pUpperMarginSnapPointKey)
	{
		var cSnapPointKeys = 0;
		float[] pSnapPointKeys = null;
		UIElement pChild = null;
		var count = children.Count;

		ppSnapPointKeys = null;
		pcSnapPointKeys = 0;
		pLowerMarginSnapPointKey = 0.0;
		pUpperMarginSnapPointKey = 0.0;

		if (count > 0)
		{
			pSnapPointKeys = new float[count];

			for (var childIndex = 0; childIndex < count; childIndex++)
			{
				pChild = children[childIndex];
				if (pChild is not null)
				{
					//pChild.EnsureLayoutStorage();

					if (Orientation == Orientation.Vertical)
					{
						pSnapPointKeys[cSnapPointKeys] = (float)pChild.DesiredSize.Height;//pChild.GetLayoutStorage().m_desiredSize.Height;
					}
					else
					{
						pSnapPointKeys[cSnapPointKeys] = (float)pChild.DesiredSize.Width;//GetLayoutStorage().m_desiredSize.Width;
					}
					cSnapPointKeys++;
				}
			}

			ppSnapPointKeys = pSnapPointKeys;
			pcSnapPointKeys = cSnapPointKeys;
			pSnapPointKeys = null;
		}

		GetCommonSnapPointKeys(out pLowerMarginSnapPointKey, out pUpperMarginSnapPointKey);
	}

	/// <summary>
	/// Determines the keys for regular snap points.
	/// </summary>
	/// <param name="children">Children.</param>
	/// <param name="snapPointKey">Snap point key.</param>
	/// <param name="lowerMarginSnapPointKey">Lower margin snap point key.</param>
	/// <param name="upperMarginSnapPointKey">Upper margin snap point key.</param>
	private void GetRegularSnapPointKeys(
		UIElementCollection children,
		out double snapPointKey,
		out double lowerMarginSnapPointKey,
		out double upperMarginSnapPointKey)
	{
		var count = children.Count;

		snapPointKey = 0.0;

		for (var childIndex = 0; childIndex < count; childIndex++)
		{
			var child = children[childIndex];
			if (child is not null)
			{
				//pChild.EnsureLayoutStorage();

				if (Orientation == Orientation.Vertical)
				{
					snapPointKey = child.DesiredSize.Height;//pChild.GetLayoutStorage().m_desiredSize.Height;
				}
				else
				{
					snapPointKey = child.DesiredSize.Width;//pChild.GetLayoutStorage().m_desiredSize.Width;
				}
				break;
			}
		}

		GetCommonSnapPointKeys(out lowerMarginSnapPointKey, out upperMarginSnapPointKey);
	}

	/// <summary>
	/// Determines the common keys for regular and irregular snap points.
	///	Those keys are the left/right margins for a horizontal stackpanel,
	///	or the top/bottom margins for a vertical stackpanel.
	/// </summary>
	/// <param name="lowerMarginSnapPointKey">Lower margin snap point key.</param>
	/// <param name="upperMarginSnapPointKey">Upper margin snap point key.</param>
	private void GetCommonSnapPointKeys(
		out double lowerMarginSnapPointKey,
		out double upperMarginSnapPointKey)
	{
		lowerMarginSnapPointKey = 0.0;
		upperMarginSnapPointKey = 0.0;

		if (true)//m_pLayoutProperties)
		{
			if (Orientation == Orientation.Horizontal)
			{
				lowerMarginSnapPointKey = Margin.Left;// m_pLayoutProperties.m_margin.left;
				upperMarginSnapPointKey = Margin.Right;// m_pLayoutProperties.m_margin.right;
			}
			else
			{
				lowerMarginSnapPointKey = Margin.Top;// m_pLayoutProperties.m_margin.top;
				upperMarginSnapPointKey = Margin.Bottom;// m_pLayoutProperties.m_margin.bottom;
			}
		}
	}

	/// <summary>
	/// Called to let the peer know that snap points have changed.
	/// </summary>
	/// <param name="isForHorizontalSnapPoints">
	/// Value indicating whether notification
	/// is for horizonatal snap points.
	/// </param>
	private void NotifySnapPointsChanges(bool isForHorizontalSnapPoints)
	{
		if ((isForHorizontalSnapPoints && m_bNotifyHorizontalSnapPointsChanges && !m_bNotifiedHorizontalSnapPointsChanges) ||
			(!isForHorizontalSnapPoints && m_bNotifyVerticalSnapPointsChanges && !m_bNotifiedVerticalSnapPointsChanges))
		{
			NotifySnapPointsChanged(isForHorizontalSnapPoints);

			if (isForHorizontalSnapPoints)
			{
				m_bNotifiedHorizontalSnapPointsChanges = true;
			}
			else
			{
				m_bNotifiedVerticalSnapPointsChanges = true;
			}
		}
	}

	////-------------------------------------------------------------------------
	////
	////  Function:   CStackPanel.NotifySnapPointsChanges
	////
	////  Synopsis:
	////    Checks if the snap point keys have changed and a notification needs
	////    to be raised.
	////
	////-------------------------------------------------------------------------

	//HRESULT
	/// <summary>
	/// Checks if the snap point keys have changed and a notification needs
	/// to be raised. 
	/// </summary>
	/// <param name="children">Children.</param>
	private void NotifySnapPointsChanges(UIElementCollection children)
	{
		int cSnapPointKeys = 0;
		float[] pSnapPointKeys = null;
		double snapPointKey = 0.0;
		double lowerMarginSnapPointKey = 0.0;
		double upperMarginSnapPointKey = 0.0;
		bool bNotifyForHorizontalSnapPoints = false;
		bool bNotifyForVerticalSnapPoints = false;

		if (Orientation == Orientation.Vertical)
		{
			if (((m_regularSnapPointKey != -1.0) || (m_cIrregularSnapPointKeys != int.MaxValue)) &&
				m_bAreSnapPointsKeysHorizontal && m_bNotifyHorizontalSnapPointsChanges)
			{
				// Last computed snap point keys were for horizontal orientation.
				// New orientation is vertical.
				// Consumer wants notifications for horizontal snap points.
				bNotifyForHorizontalSnapPoints = true;
			}
		}
		else
		{
			if (((m_regularSnapPointKey != -1.0) || (m_cIrregularSnapPointKeys != int.MaxValue)) &&
				!m_bAreSnapPointsKeysHorizontal && m_bNotifyVerticalSnapPointsChanges)
			{
				// Last computed snap point keys were for vertical orientation.
				// New orientation is horizontal.
				// Consumer wants notifications for vertical snap points.
				bNotifyForVerticalSnapPoints = true;
			}
		}

		if ((m_bNotifyHorizontalSnapPointsChanges && Orientation == Orientation.Horizontal &&
			 m_bAreSnapPointsKeysHorizontal && !m_bNotifiedHorizontalSnapPointsChanges) ||
			 (m_bNotifyVerticalSnapPointsChanges && Orientation == Orientation.Vertical &&
			 !m_bAreSnapPointsKeysHorizontal && !m_bNotifiedVerticalSnapPointsChanges))
		{
			if (m_regularSnapPointKey != -1.0)
			{
				if (m_bAreScrollSnapPointsRegular)
				{
					GetRegularSnapPointKeys(children, out snapPointKey, out lowerMarginSnapPointKey, out upperMarginSnapPointKey);
					if (m_regularSnapPointKey != snapPointKey ||
						m_lowerMarginSnapPointKey != lowerMarginSnapPointKey ||
						m_upperMarginSnapPointKey != upperMarginSnapPointKey)
					{
						if (m_bAreSnapPointsKeysHorizontal)
						{
							bNotifyForHorizontalSnapPoints = true;
						}
						else
						{
							bNotifyForVerticalSnapPoints = true;
						}
					}
				}
				else
				{
					if (m_bAreSnapPointsKeysHorizontal)
					{
						bNotifyForHorizontalSnapPoints = true;
					}
					else
					{
						bNotifyForVerticalSnapPoints = true;
					}
				}
			}
			else if (m_cIrregularSnapPointKeys != int.MaxValue)
			{
				if (!m_bAreScrollSnapPointsRegular)
				{
					GetIrregularSnapPointKeys(children, out pSnapPointKeys, out cSnapPointKeys, out lowerMarginSnapPointKey, out upperMarginSnapPointKey);
					if (m_cIrregularSnapPointKeys != cSnapPointKeys ||
						m_lowerMarginSnapPointKey != lowerMarginSnapPointKey ||
						m_upperMarginSnapPointKey != upperMarginSnapPointKey)
					{
						if (m_bAreSnapPointsKeysHorizontal)
						{
							bNotifyForHorizontalSnapPoints = true;
						}
						else
						{
							bNotifyForVerticalSnapPoints = true;
						}
					}
					else
					{
						for (var iSnapPointKey = 0; iSnapPointKey < cSnapPointKeys; iSnapPointKey++)
						{
							if (m_pIrregularSnapPointKeys[iSnapPointKey] != pSnapPointKeys[iSnapPointKey])
							{
								if (m_bAreSnapPointsKeysHorizontal)
								{
									bNotifyForHorizontalSnapPoints = true;
								}
								else
								{
									bNotifyForVerticalSnapPoints = true;
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
						bNotifyForHorizontalSnapPoints = true;
					}
					else
					{
						bNotifyForVerticalSnapPoints = true;
					}
				}
			}
		}

		if (bNotifyForHorizontalSnapPoints)
		{
			NotifySnapPointsChanges(true /*bIsForHorizontalSnapPoints*/);
		}

		if (bNotifyForVerticalSnapPoints)
		{
			NotifySnapPointsChanges(false /*bIsForHorizontalSnapPoints*/);
		}
	}

	/// <summary>
	/// Called when the AreScrollSnapPointsRegular property changed.
	/// </summary>
	private void OnAreScrollSnapPointsRegularChanged()
	{
		var children = GetUnsortedChildren();
		NotifySnapPointsChanges(children);
	}

	/// <summary>
	/// Refreshes the m_pIrregularSnapPointKeys/m_cIrregularSnapPointKeys
	/// fields based on all children.
	/// </summary>
	private void RefreshIrregularSnapPointKeys()
	{
		var cSnapPointKeys = 0;
		float[] pSnapPointKeys = null;
		double lowerMarginSnapPointKey = 0.0;
		double upperMarginSnapPointKey = 0.0;

		MUX_ASSERT(!m_bAreScrollSnapPointsRegular);

		var children = GetUnsortedChildren();

		ResetSnapPointKeys();

		GetIrregularSnapPointKeys(
			children,
			out pSnapPointKeys,
			out cSnapPointKeys,
			out lowerMarginSnapPointKey,
			out upperMarginSnapPointKey);

		m_pIrregularSnapPointKeys = pSnapPointKeys;
		MUX_ASSERT(cSnapPointKeys <= int.MaxValue - 1);
		m_cIrregularSnapPointKeys = cSnapPointKeys;
		m_lowerMarginSnapPointKey = lowerMarginSnapPointKey;
		m_upperMarginSnapPointKey = upperMarginSnapPointKey;
		m_bAreSnapPointsKeysHorizontal = (Orientation == Orientation.Horizontal);
		pSnapPointKeys = null;
	}

	/// <summary>
	/// Refreshes the m_regularSnapPointKey field based on a single child.
	///	Refreshes also the m_lowerMarginSnapPointKey/m_upperMarginSnapPointKey fields based
	/// on the current margins.
	/// </summary>
	private void RefreshRegularSnapPointKeys()
	{
		double snapPointKey = 0.0;
		double lowerMarginSnapPointKey = 0.0;
		double upperMarginSnapPointKey = 0.0;

		MUX_ASSERT(m_bAreScrollSnapPointsRegular);

		ResetSnapPointKeys();

		m_regularSnapPointKey = 0.0;

		var children = GetUnsortedChildren();

		GetRegularSnapPointKeys(children, out snapPointKey, out lowerMarginSnapPointKey, out upperMarginSnapPointKey);

		m_bAreSnapPointsKeysHorizontal = (Orientation == Orientation.Horizontal);
		m_regularSnapPointKey = snapPointKey;
		m_lowerMarginSnapPointKey = lowerMarginSnapPointKey;
		m_upperMarginSnapPointKey = upperMarginSnapPointKey;
	}

	/// <summary>
	/// Resets both regular and irregular snap point keys.
	/// </summary>
	private void ResetSnapPointKeys()
	{
		m_pIrregularSnapPointKeys = null;
		m_cIrregularSnapPointKeys = int.MaxValue;
		m_regularSnapPointKey = -1.0;
		m_lowerMarginSnapPointKey = 0;
		m_upperMarginSnapPointKey = 0;
	}

	/// <summary>
	/// Determines whether the StackPanel must call NotifySnapPointsChanged
	/// when snap points change or not.
	/// </summary>
	/// <param name="isForHorizontalSnapPoints"></param>
	/// <param name="notifyChanges"></param>
	private void SetSnapPointsChangeNotificationsRequirement(
		 bool isForHorizontalSnapPoints,
		 bool notifyChanges)
	{
		if (isForHorizontalSnapPoints)
		{
			m_bNotifyHorizontalSnapPointsChanges = notifyChanges;
			if (Orientation == Orientation.Horizontal && notifyChanges)
			{
				if (m_bAreScrollSnapPointsRegular)
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
			if (Orientation == Orientation.Vertical && notifyChanges)
			{
				if (m_bAreScrollSnapPointsRegular)
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

	////-------------------------------------------------------------------------
	////
	////  Function:   CStackPanel.SetValue
	////
	////  Synopsis:
	////    Overridden to detect changes of the AreScrollSnapPointsRegular property.
	////
	////-------------------------------------------------------------------------
	//HRESULT
	//  CSetValue(SetValueParams& args)
	//{
	//	HRESULT hr = S_OK;

	//	ASSERT(args.m_pDP != null);

	//	(CPanel.SetValue(args));

	//	switch (args.m_pDP.GetIndex())
	//	{
	//		case KnownPropertyIndex.StackPanel_AreScrollSnapPointsRegular:
	//			(OnAreScrollSnapPointsRegularChanged());
	//			break;
	//	}

	//Cleanup:
	//	return hr;
	//}
}
