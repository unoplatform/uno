// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using Microsoft.UI.Composition;
using Microsoft.UI.Composition.Interactions;
using Microsoft.UI.Xaml.Controls.Primitives;

using static Microsoft.UI.Xaml.Controls._Tracing;

namespace Microsoft.UI.Xaml.Controls;

internal partial class SnapPointWrapper<T> where T : SnapPointBase
{
	public SnapPointWrapper(T snapPoint)
	{
		SnapPoint = snapPoint;
	}

	// #if DBG
	// 	~SnapPointWrapper()
	// 	{
	// 	}
	// #endif //DBG

	public T SnapPoint { get; }

	public (double, double) ActualApplicableZone()
	{
		return m_actualApplicableZone;
	}


	public int CombinationCount()
	{
		return m_combinationCount;
	}


	public bool ResetIgnoredValue()
	{
		if (!double.IsNaN(m_ignoredValue))
		{
			m_ignoredValue = double.NaN;
			return true;
		}

		return false;
	}


	public void SetIgnoredValue(double ignoredValue)
	{
		MUX_ASSERT(!double.IsNaN(ignoredValue));

		m_ignoredValue = ignoredValue;
	}


	public ExpressionAnimation CreateRestingPointExpression(
		InteractionTracker interactionTracker,
		string target,
		string scale,
		bool isInertiaFromImpulse)
	{
		m_restingValueExpressionAnimation = GetSnapPointFromWrapper(this).CreateRestingPointExpression(
			m_ignoredValue,
			m_actualImpulseApplicableZone,
			interactionTracker,
			target,
			scale,
			isInertiaFromImpulse);

		return m_restingValueExpressionAnimation;
	}


	public ExpressionAnimation CreateConditionalExpression(
		InteractionTracker interactionTracker,
		string target,
		string scale,
		bool isInertiaFromImpulse)
	{
		m_conditionExpressionAnimation = GetSnapPointFromWrapper(this).CreateConditionalExpression(
			m_actualApplicableZone,
			m_actualImpulseApplicableZone,
			interactionTracker,
			target,
			scale,
			isInertiaFromImpulse);

		return m_conditionExpressionAnimation;
	}

	// Invoked when the InteractionTracker reaches the Idle State and a new ignored value may have to be set.

	public (ExpressionAnimation, ExpressionAnimation) GetUpdatedExpressionAnimationsForImpulse()
	{
		SnapPointBase snapPoint = GetSnapPointFromWrapper(this);

		snapPoint.UpdateConditionalExpressionAnimationForImpulse(
			m_conditionExpressionAnimation,
			m_actualImpulseApplicableZone);
		snapPoint.UpdateRestingPointExpressionAnimationForImpulse(
			m_restingValueExpressionAnimation,
			m_ignoredValue,
			m_actualImpulseApplicableZone);

		return (m_conditionExpressionAnimation, m_restingValueExpressionAnimation);
	}


	public void DetermineActualApplicableZone(
		SnapPointWrapper<T> previousSnapPointWrapper,
		SnapPointWrapper<T> nextSnapPointWrapper,
		bool forImpulseOnly)
	{
		SnapPointBase snapPoint = GetSnapPointFromWrapper(this);
		SnapPointBase previousSnapPoint = GetSnapPointFromWrapper(previousSnapPointWrapper);
		SnapPointBase nextSnapPoint = GetSnapPointFromWrapper(nextSnapPointWrapper);
		double previousIgnoredValue = previousSnapPointWrapper is not null ? previousSnapPointWrapper.m_ignoredValue : double.NaN;
		double nextIgnoredValue = nextSnapPointWrapper is not null ? nextSnapPointWrapper.m_ignoredValue : double.NaN;

		if (!forImpulseOnly)
		{
			m_actualApplicableZone = snapPoint.DetermineActualApplicableZone(
				previousSnapPoint,
				nextSnapPoint);
		}

		m_actualImpulseApplicableZone = snapPoint.DetermineActualImpulseApplicableZone(
			previousSnapPoint,
			nextSnapPoint,
			m_ignoredValue,
			previousIgnoredValue,
			nextIgnoredValue);
	}


	public void Combine(SnapPointWrapper<T> snapPointWrapper)
	{
		GetSnapPointFromWrapper(this).Combine(ref m_combinationCount, snapPointWrapper.SnapPoint);
	}


	public double Evaluate(double value)
	{
		return GetSnapPointFromWrapper(this).Evaluate(m_actualApplicableZone, value);
	}


	public bool SnapsAt(double value)
	{
		return GetSnapPointFromWrapper(this).SnapsAt(m_actualApplicableZone, value);
	}

	public static SnapPointBase GetSnapPointFromWrapper(SnapPointWrapper<T> snapPointWrapper)
	{
		if (snapPointWrapper is not null)
		{
			SnapPointBase winrtPreviousSnapPoint = snapPointWrapper.SnapPoint as SnapPointBase;
			return winrtPreviousSnapPoint;
		}
		return null;
	}
}
