// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

// The SnapPointWrapper class has a SnapPointBase member that can be shared among multiple
// HorizontalSnapPoints, VerticalSnapPoints and ZoomSnapPoints collections.
// It also includes all the data that is specific to that snap point and a particular collection.

using System.Collections.Generic;
using Windows.UI.Composition;
using Windows.UI.Xaml.Controls.Primitives;

namespace Windows.UI.Xaml.Controls;

internal partial class SnapPointWrapper<T>
{
	private (double, double) m_actualApplicableZone = (double.NegativeInfinity, double.PositiveInfinity);
	private (double, double) m_actualImpulseApplicableZone = (double.NegativeInfinity, double.PositiveInfinity);
	private int m_combinationCount;
	private double m_ignoredValue = double.NaN; // Ignored snapping value when inertia is triggered by an impulse
	private ExpressionAnimation m_conditionExpressionAnimation;
	private ExpressionAnimation m_restingValueExpressionAnimation;
}

internal partial struct SnapPointWrapperComparator<T> : IComparer<SnapPointWrapper<T>> where T : SnapPointBase
{
	public int Compare(SnapPointWrapper<T> x, SnapPointWrapper<T> y)
	{
		var left = x.SnapPoint;
		var right = y.SnapPoint;
		if (left == right)
		{
			return 0;
		}
		else if (left < right)
		{
			return -1;
		}
		else
		{
			return 1;
		}
	}
}
