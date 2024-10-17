// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System;
using Windows.UI.Composition;
using Windows.UI.Composition.Interactions;
using Uno.UI.Helpers.WinUI;
using Windows.UI;

using static Microsoft/* UWP don't rename */.UI.Xaml.Controls._Tracing;

namespace Windows.UI.Xaml.Controls.Primitives;

public partial class SnapPointBase
{
	internal SnapPointBase()
	{
	}

	private protected string GetTargetExpression(string target)
	{
		return StringUtil.FormatString("this.Target.%1!s!", target);
	}

	private protected string GetIsInertiaFromImpulseExpression(string target)
	{
		// Returns 't.IsInertiaFromImpulse' or 'this.Target.IsInertiaFromImpulse'.
		return StringUtil.FormatString("%1!s!.IsInertiaFromImpulse", target);
	}

	// Uno docs: This operator is not public in WinUI. However, C# requires operators to be public (https://learn.microsoft.com/dotnet/csharp/misc/cs0558).
	public static bool operator <(SnapPointBase @this, SnapPointBase snapPoint)
	{
		SnapPointSortPredicate mySortPredicate = @this.SortPredicate();
		SnapPointSortPredicate theirSortPredicate = snapPoint.SortPredicate();
		if (mySortPredicate.primary < theirSortPredicate.primary)
		{
			return true;
		}
		if (theirSortPredicate.primary < mySortPredicate.primary)
		{
			return false;
		}

		if (mySortPredicate.secondary < theirSortPredicate.secondary)
		{
			return true;
		}
		if (theirSortPredicate.secondary < mySortPredicate.secondary)
		{
			return false;
		}

		if (mySortPredicate.tertiary < theirSortPredicate.tertiary)
		{
			return true;
		}
		return false;
	}

	public static bool operator ==(SnapPointBase @this, SnapPointBase snapPoint)
	{
		SnapPointSortPredicate mySortPredicate = @this.SortPredicate();
		SnapPointSortPredicate theirSortPredicate = snapPoint.SortPredicate();
		if (Math.Abs(mySortPredicate.primary - theirSortPredicate.primary) < s_equalityEpsilon
			&& Math.Abs(mySortPredicate.secondary - theirSortPredicate.secondary) < s_equalityEpsilon
			&& mySortPredicate.tertiary == theirSortPredicate.tertiary)
		{
			return true;
		}
		return false;
	}

	// Uno specific: C# requires defining these operators.
	public static bool operator !=(SnapPointBase @this, SnapPointBase snapPoint) => !(@this == snapPoint);
	public static bool operator >(SnapPointBase @this, SnapPointBase snapPoint) => !(@this < snapPoint || @this == snapPoint);

	// Uno specific: C# requires overriding Equals and GetHashCode when implementing equality operators.
	public override bool Equals(object obj) => obj is SnapPointBase other && this == other;
	public override int GetHashCode() => SortPredicate().GetHashCode();


#if ApplicableRangeType // UNO TODO
	private double ApplicableRange()
	{
		return m_specifiedApplicableRange;
	}

	private SnapPointApplicableRangeType ApplicableRangeType()
	{
		return m_applicableRangeType;
	}
#endif

#if DEBUG
	internal Color VisualizationColor { get; set; } = Colors.Black;
#endif // DBG

	// Returns True if this snap point snaps around the provided value.
	internal bool SnapsAt(
		(double, double) actualApplicableZone,
		double value)
	{
		if (actualApplicableZone.Item1 <= value &&
			actualApplicableZone.Item2 >= value)
		{
			double snappedValue = Evaluate(actualApplicableZone, (float)value);

			return Math.Abs(value - snappedValue) < s_equalityEpsilon;
		}

		return false;
	}

	// Uno docs: This doesn't seem to be used.
	//void SetBooleanParameter(
	//	ExpressionAnimation expressionAnimation,
	//	string booleanName,
	//	bool booleanValue)
	//{
	//	// SCROLLPRESENTER_TRACE_VERBOSE(nullptr, TRACE_MSG_METH_STR_INT, METH_NAME, this, booleanName, booleanValue);

	//	expressionAnimation.SetBooleanParameter(booleanName, booleanValue);
	//}

	internal void SetScalarParameter(
		ExpressionAnimation expressionAnimation,
		string scalarName,
		float scalarValue)
	{
		// SCROLLPRESENTER_TRACE_VERBOSE(nullptr, TRACE_MSG_METH_STR_FLT, METH_NAME, this, scalarName, scalarValue);

		expressionAnimation.SetScalarParameter(scalarName, scalarValue);
	}
}

public partial class ScrollSnapPointBase : SnapPointBase
{
	private protected double m_alignmentAdjustment = 0.0; // Non-zero adjustment based on viewport size, when the alignment is Center or Far.

	/////////////////////////////////////////////////////////////////////
	/////////////////      Scroll Snap Points     ///////////////////////
	/////////////////////////////////////////////////////////////////////

	// Required for Modern Idl bug, should never be called.
	internal ScrollSnapPointBase()
	{
		// throw (ERROR_CALL_NOT_IMPLEMENTED);
	}

	public ScrollSnapPointsAlignment Alignment { get; private protected set; } = ScrollSnapPointsAlignment.Near;


	// Returns True when this snap point is sensitive to the viewport size and is interested in future updates.
	internal override bool OnUpdateViewport(double newViewport)
	{
		switch (Alignment)
		{
			case ScrollSnapPointsAlignment.Near:
				MUX_ASSERT(m_alignmentAdjustment == 0.0);
				return false;
			case ScrollSnapPointsAlignment.Center:
				m_alignmentAdjustment = -newViewport / 2.0;
				break;
			case ScrollSnapPointsAlignment.Far:
				m_alignmentAdjustment = -newViewport;
				break;
		}
		return true;
	}
}

public partial class ScrollSnapPoint : ScrollSnapPointBase
{
	// Uno docs: In SnapPoint.h in WinUI code, the second parameter has a default value of ScrollSnapPointsAlignment::Near
	// HOWEVER, the API seen in a real WinUI app says that the parameter is mandatory.
	public ScrollSnapPoint(
		double snapPointValue,
		ScrollSnapPointsAlignment alignment)
	{
		Value = snapPointValue;
		Alignment = alignment;
	}

#if ApplicableRangeType // UNO TODO:
	public ScrollSnapPoint(
		double snapPointValue,
		double applicableRange,
		ScrollSnapPointsAlignment alignment)
	{
		if (applicableRange <= 0)
		{
			throw new InvalidArgumentException("'applicableRange' must be strictly positive.");
		}

		Value = snapPointValue;
		Alignment = alignment;
		m_specifiedApplicableRange = applicableRange;
		m_actualApplicableZone = (double, double){ snapPointValue - applicableRange, snapPointValue + applicableRange};
		m_applicableRangeType = SnapPointApplicableRangeType.Optional;
	}
#endif

	public double Value { get; }

	internal override ExpressionAnimation CreateRestingPointExpression(
		double ignoredValue,
		(double, double) actualImpulseApplicableZone,
		InteractionTracker interactionTracker,
		string target,
		string scale,
		bool isInertiaFromImpulse)
	{
		string expression = StringUtil.FormatString("%1!s!*%2!s!", s_snapPointValue, scale);

		// SCROLLPRESENTER_TRACE_VERBOSE(nullptr, TRACE_MSG_METH_STR, METH_NAME, this, expression.c_str());

		var restingPointExpressionAnimation = interactionTracker.Compositor.CreateExpressionAnimation(expression);

		SetScalarParameter(restingPointExpressionAnimation, s_snapPointValue, (float)(ActualValue()));

		return restingPointExpressionAnimation;
	}

	internal override ExpressionAnimation CreateConditionalExpression(
		(double, double) actualApplicableZone,
		(double, double) actualImpulseApplicableZone,
		InteractionTracker interactionTracker,
		string target,
		string scale,
		bool isInertiaFromImpulse)
	{
		const string scaledValue = "(%1!s!*%2!s!)";
		string isInertiaFromImpulseExpression = GetIsInertiaFromImpulseExpression("this.Target");
		string targetExpression = GetTargetExpression(target);
		string scaledMinApplicableRange = StringUtil.FormatString(
			scaledValue,
			s_minApplicableValue,
			scale);
		string scaledMaxApplicableRange = StringUtil.FormatString(
			scaledValue,
			s_maxApplicableValue,
			scale);
		string scaledMinImpulseApplicableRange = StringUtil.FormatString(
			scaledValue,
			s_minImpulseApplicableValue,
			scale);
		string scaledMaxImpulseApplicableRange = StringUtil.FormatString(
			scaledValue,
			s_maxImpulseApplicableValue,
			scale);
		string expression = StringUtil.FormatString(
			"%1!s!?(%2!s!>=%5!s!&&%2!s!<=%6!s!):(%2!s!>=%3!s!&&%2!s!<= %4!s!)",
			isInertiaFromImpulseExpression,
			targetExpression,
			scaledMinApplicableRange,
			scaledMaxApplicableRange,
			scaledMinImpulseApplicableRange,
			scaledMaxImpulseApplicableRange);

		// SCROLLPRESENTER_TRACE_VERBOSE(nullptr, TRACE_MSG_METH_STR, METH_NAME, this, expression.c_str());

		var conditionExpressionAnimation = interactionTracker.Compositor.CreateExpressionAnimation(expression);

		SetScalarParameter(conditionExpressionAnimation, s_minApplicableValue, (float)actualApplicableZone.Item1);
		SetScalarParameter(conditionExpressionAnimation, s_maxApplicableValue, (float)actualApplicableZone.Item2);

		UpdateConditionalExpressionAnimationForImpulse(
			conditionExpressionAnimation,
			actualImpulseApplicableZone);

		return conditionExpressionAnimation;
	}

	internal override void UpdateConditionalExpressionAnimationForImpulse(
		ExpressionAnimation conditionExpressionAnimation,
		(double, double) actualImpulseApplicableZone)
	{
		SetScalarParameter(conditionExpressionAnimation, s_minImpulseApplicableValue, (float)actualImpulseApplicableZone.Item1);
		SetScalarParameter(conditionExpressionAnimation, s_maxImpulseApplicableValue, (float)actualImpulseApplicableZone.Item2);
	}

	internal override void UpdateRestingPointExpressionAnimationForImpulse(
		ExpressionAnimation restingValueExpressionAnimation,
		double ignoredValue,
		(double, double) actualImpulseApplicableZone)
	{
		// An irregular snap point like ScrollSnapPoint is either completely ignored in impulse mode or not ignored at all, unlike repeated snap points
		// which can be partially ignored. Its conditional expression depends on the impulse mode, whereas its resting point expression does not,
		// thus this method has no job to do.
	}

	internal override SnapPointSortPredicate SortPredicate()
	{
		double actualValue = ActualValue();

		// Irregular snap point should be sorted before repeated snap points so it gives a tertiary sort value of 0 (repeated snap points get 1)
		return new SnapPointSortPredicate
		{
			primary = actualValue,
			secondary = actualValue,
			tertiary = 0
		};
	}

	internal override (double, double) DetermineActualApplicableZone(
		SnapPointBase previousSnapPoint,
		SnapPointBase nextSnapPoint)
	{
		return (
			DetermineMinActualApplicableZone(previousSnapPoint),
			DetermineMaxActualApplicableZone(nextSnapPoint));
	}

	internal override (double, double) DetermineActualImpulseApplicableZone(
		SnapPointBase previousSnapPoint,
		SnapPointBase nextSnapPoint,
		double currentIgnoredValue,
		double previousIgnoredValue,
		double nextIgnoredValue)
	{
		return (
			DetermineMinActualImpulseApplicableZone(
				previousSnapPoint,
				currentIgnoredValue,
				previousIgnoredValue),
			DetermineMaxActualImpulseApplicableZone(
				nextSnapPoint,
				currentIgnoredValue,
				nextIgnoredValue));
	}

	private double ActualValue()
	{
		return Value + m_alignmentAdjustment;
	}

	private double DetermineMinActualApplicableZone(
		SnapPointBase previousSnapPoint)
	{
		// If we are not passed a previousSnapPoint it means we are the first in the list, see if we expand to negative Infinity or stay put.
		if (previousSnapPoint is null)
		{
#if ApplicableRangeType // UNO TODO
			if (applicableRangeType != SnapPointApplicableRangeType.Optional)
			{
				return -INFINITY;
			}
			else
			{
				return ActualValue() - m_specifiedApplicableRange;
			}
#else
			return double.NegativeInfinity;
#endif
		}
		// If we are passed a previousSnapPoint then we need to account for its influence on us.
		else
		{
			double previousMaxInfluence = previousSnapPoint.Influence(ActualValue());

#if ApplicableRangeType // UNO TODO
			switch (m_applicableRangeType)
			{
				case SnapPointApplicableRangeType.Optional:
					return Math.Max(previousMaxInfluence, ActualValue() - m_specifiedApplicableRange);
				case SnapPointApplicableRangeType.Mandatory:
					return previousMaxInfluence;
				default:
					MUX_ASSERT(false);
					return 0.0;
			}
#else
			return previousMaxInfluence;
#endif
		}
	}

	private double DetermineMinActualImpulseApplicableZone(
		SnapPointBase previousSnapPoint,
		double currentIgnoredValue,
		double previousIgnoredValue)
	{
		if (previousSnapPoint is null)
		{
			return double.NegativeInfinity;
		}
		else
		{
			double previousMaxInfluence = previousSnapPoint.ImpulseInfluence(ActualValue(), previousIgnoredValue);

			if (double.IsNaN(currentIgnoredValue))
			{
				return previousMaxInfluence;
			}
			else
			{
				return Math.Max(previousMaxInfluence, ActualValue());
			}
		}
	}

	private double DetermineMaxActualApplicableZone(
		SnapPointBase nextSnapPoint)
	{
		// If we are not passed a nextSnapPoint it means we are the last in the list, see if we expand to Infinity or stay put.
		if (nextSnapPoint is null)
		{
#if ApplicableRangeType // UNO TODO
			if (m_applicableRangeType != SnapPointApplicableRangeType.Optional)
			{
				return INFINITY;
			}
			else
			{
				return ActualValue() + m_specifiedApplicableRange;
			}
#else
			return double.PositiveInfinity;
#endif
		}
		// If we are passed a nextSnapPoint then we need to account for its influence on us.
		else
		{
			double nextMinInfluence = nextSnapPoint.Influence(ActualValue());

#if ApplicableRangeType // UNO TODO
			switch (m_applicableRangeType)
			{
				case SnapPointApplicableRangeType.Optional:
					return Math.Min(ActualValue() + m_specifiedApplicableRange, nextMinInfluence);
				case SnapPointApplicableRangeType.Mandatory:
					return nextMinInfluence;
				default:
					MUX_ASSERT(false);
					return 0.0;
			}
#else
			return nextMinInfluence;
#endif
		}
	}

	private double DetermineMaxActualImpulseApplicableZone(
		SnapPointBase nextSnapPoint,
		double currentIgnoredValue,
		double nextIgnoredValue)
	{
		if (nextSnapPoint is null)
		{
			return double.PositiveInfinity;
		}
		else
		{
			double nextMinInfluence = nextSnapPoint.ImpulseInfluence(ActualValue(), nextIgnoredValue);

			if (double.IsNaN(currentIgnoredValue))
			{
				return nextMinInfluence;
			}
			else
			{
				return Math.Min(ActualValue(), nextMinInfluence);
			}
		}
	}

	internal override double Influence(double edgeOfMidpoint)
	{
		double actualValue = ActualValue();
		double midPoint = (actualValue + edgeOfMidpoint) / 2;

#if ApplicableRangeType // UNO TODO
		switch (m_applicableRangeType)
		{
			case SnapPointApplicableRangeType.Optional:
				if (actualValue <= edgeOfMidpoint)
				{
					return Math.Min(actualValue + m_specifiedApplicableRange, midPoint);
				}
				else
				{
					return Math.Max(actualValue - m_specifiedApplicableRange, midPoint);
				}
			case SnapPointApplicableRangeType.Mandatory:
				return midPoint;
			default:
				MUX_ASSERT(false);
				return 0.0;
		}
#else
		return midPoint;
#endif
	}

	internal override double ImpulseInfluence(double edgeOfMidpoint, double ignoredValue)
	{
		double actualValue = ActualValue();
		double midPoint = (actualValue + edgeOfMidpoint) / 2.0;

		if (double.IsNaN(ignoredValue))
		{
			return midPoint;
		}
		else
		{
			if (actualValue <= edgeOfMidpoint)
			{
				return Math.Min(actualValue, midPoint);
			}
			else
			{
				return Math.Max(actualValue, midPoint);
			}
		}
	}

	internal override void Combine(
		ref int combinationCount,
		SnapPointBase snapPoint)
	{
		var snapPointAsIrregular = snapPoint as ScrollSnapPoint;
		if (snapPointAsIrregular is not null)
		{
#if ApplicableRangeType // UNO TODO
			//TODO: The m_specifiedApplicableRange field is never expected to change after creation. A correction will be needed here.
			m_specifiedApplicableRange = Math.Max(snapPointAsIrregular.ApplicableRange(), m_specifiedApplicableRange);
#else
			MUX_ASSERT(m_specifiedApplicableRange == double.PositiveInfinity);
#endif
			combinationCount++;
		}
		else
		{
			// TODO: Provide custom error message
			throw new ArgumentException();
		}
	}

	internal override int SnapCount()
	{
		return 1;
	}

	internal override double Evaluate(
		(double, double) actualApplicableZone,
		double value)
	{
		if (value >= actualApplicableZone.Item1 && value <= actualApplicableZone.Item2)
		{
			return ActualValue();
		}
		return value;
	}
}

public partial class RepeatedScrollSnapPoint : ScrollSnapPointBase
{
	public RepeatedScrollSnapPoint(
		double offset,
		double interval,
		double start,
		double end,
		ScrollSnapPointsAlignment alignment)
	{
		ValidateConstructorParameters(
#if ApplicableRangeType // UNO TODO
			false /*applicableRangeToo*/,
			0 /*applicableRange*/,
#endif
			offset,
			interval,
			start,
			end);

		Offset = offset;
		Interval = interval;
		Start = start;
		End = end;
		Alignment = alignment;
	}

#if ApplicableRangeType // UNO TODO
	RepeatedScrollSnapPoint::RepeatedScrollSnapPoint(
		double offset,
		double interval,
		double start,
		double end,
		double applicableRange,
		ScrollSnapPointsAlignment alignment)
	{
		ValidateConstructorParameters(
			true /*applicableRangeToo*/,
			applicableRange,
			offset,
			interval,
			start,
			end);
		
		m_offset = offset;
		m_interval = interval;
		m_start = start;
		m_end = end;
		m_specifiedApplicableRange = applicableRange;
		m_applicableRangeType = SnapPointApplicableRangeType.Optional;
		m_alignment = alignment;
	}
#endif

	public double Offset { get; }

	public double Interval { get; }

	private double Start { get; }

	private double End { get; }

	internal override ExpressionAnimation CreateRestingPointExpression(
		double ignoredValue,
		(double, double) actualImpulseApplicableZone,
		InteractionTracker interactionTracker,
		string target,
		string scale,
		bool isInertiaFromImpulse)
	{
		/*
		fracTarget = (target / scale - first) / interval       // Unsnapped value in fractional unscaled intervals from first snapping value
		prevSnap = ((Floor(fracTarget) * interval) + first)    // First unscaled snapped value before unsnapped value
		nextSnap = ((Ceil(fracTarget) * interval) + first)     // First unscaled snapped value after unsnapped value
		effectiveEnd = (IsInertiaFromImpulse ? impEnd : end)   // Regular or impulse upper bound of unscaled applicable zone
		
		Expression:
		((Abs(target / scale - prevSnap) >= Abs(target / scale - nextSnap)) && (nextSnap <= effectiveEnd))
		?
		// nextSnap value is closer to unsnapped value and within applicable zone.
		(
		IsInertiaFromImpulse
		?
		// Impulse mode.
		(
		nextSnap == impIgn
		?
		(
			// Next snapped value is ignored. Pick the previous snapped value if any, else the ignored value.
			(impIgn == first ? first * scale : (impIgn - interval) * scale)
		)
		:
		// Pick next snapped value.
		nextSnap * scale
		)
		:
		// Regular mode. Pick next snapped value.
		nextSnap * scale
		)
		:
		// prevSnap value is closer to unsnapped value.
		(
		IsInertiaFromImpulse
		?
		// Impulse mode.
		(
		prevSnap == impIgn
		?
		// Previous snapped value is ignored. Pick the next snapped value if any, else the ignored value.
		(impIgn + interval <= effectiveEnd ? (impIgn + interval) * scale : impIgn * scale)
		:
		(
			prevSnap < first i.e. fracTarget < -0.5
			?
			// Pick next snapped value as previous snapped value is outside applicable zone.
			nextSnap * scale
			:
			// Pick previous snapped value as it is within applicable zone.
			prevSnap * scale
		)
		)
		:
		// Regular mode.
		(
		prevSnap < first i.e. fracTarget < -0.5
		?
		// Pick next snapped value as previous snapped value is outside applicable zone.
		nextSnap * scale
		:
		// Pick previous snapped value as it is within applicable zone.
		prevSnap * scale
		)
		)
		*/

		string isInertiaFromImpulseExpression = GetIsInertiaFromImpulseExpression(s_interactionTracker);
		string expression = StringUtil.FormatString(
			"((Abs(T.%2!s!/T.Scale-(Floor((T.%2!s!/T.Scale-P)/V)*V+P))>=Abs(T.%2!s!/T.Scale-(Ceil((T.%2!s!/T.Scale-P)/V)*V+P)))&&((Ceil((T.%2!s!/T.Scale-P)/V)*V+P)<=(%1!s!?iE:E)))?(%1!s!?((Ceil((T.%2!s!/T.Scale-P)/V)*V+P)==M?((M==P?P*T.Scale:(M-V)*T.Scale)):(Ceil((T.%2!s!/T.Scale-P)/V)*V+P)*T.Scale):(Ceil((T.%2!s!/T.Scale-P)/V)*V+P)*T.Scale):(%1!s!?((Floor((T.%2!s!/T.Scale-P)/V)*V+P)==M?(M+V<=(%1!s!?iE:E)?(M+V)*T.Scale:M*T.Scale):(T.%2!s!/T.Scale<P-0.5*V?(Ceil((T.%2!s!/T.Scale-P)/V)*V+P)*T.Scale:(Floor((T.%2!s!/T.Scale-P)/V)*V+P)*T.Scale)):(T.%2!s!/T.Scale<P-0.5*V?(Ceil((T.%2!s!/T.Scale-P)/V)*V+P)*T.Scale:(Floor((T.%2!s!/T.Scale-P)/V)*V+P)*T.Scale))",
			isInertiaFromImpulseExpression,
			target);

		// SCROLLPRESENTER_TRACE_VERBOSE(nullptr, TRACE_MSG_METH_STR, METH_NAME, this, expression.c_str());

		var restingPointExpressionAnimation = interactionTracker.Compositor.CreateExpressionAnimation(expression);

		SetScalarParameter(restingPointExpressionAnimation, s_interval, (float)Interval);
		SetScalarParameter(restingPointExpressionAnimation, s_end, (float)ActualEnd());
		SetScalarParameter(restingPointExpressionAnimation, s_first, (float)DetermineFirstRepeatedSnapPointValue());
		restingPointExpressionAnimation.SetReferenceParameter(s_interactionTracker, interactionTracker);

		UpdateRestingPointExpressionAnimationForImpulse(
			restingPointExpressionAnimation,
			ignoredValue,
			actualImpulseApplicableZone);

		return restingPointExpressionAnimation;
	}

	internal override ExpressionAnimation CreateConditionalExpression(
		(double, double) actualApplicableZone,
		(double, double) actualImpulseApplicableZone,
		InteractionTracker interactionTracker,
		string target,
		string scale,
		bool isInertiaFromImpulse)
	{
		MUX_ASSERT(actualApplicableZone.Item1 == ActualStart());
		MUX_ASSERT(actualApplicableZone.Item2 == ActualEnd());

		/*
		fracTarget = (target / scale - first) / interval       // Unsnapped value in fractional unscaled intervals from first snapping value
		prevSnap = ((Floor(fracTarget) * interval) + first)    // First unscaled snapped value before unsnapped value
		nextSnap = ((Ceil(fracTarget) * interval) + first)     // First unscaled snapped value after unsnapped value
		effectiveEnd = (IsInertiaFromImpulse ? impEnd : end)   // Regular or impulse upper bound of unscaled applicable zone

		Expression:
		(
		(!IsInertiaFromImpulse && target / scale >= start && target / scale <= end)       // If we are within the start and end in non-impulse mode
		||
		(IsInertiaFromImpulse && target / scale >= impStart && target / scale <= impEnd)  // or we are within the impulse start and end in impulse mode
		)
		&&                                                                                 // and...
		(                                                                                  // The location of the repeated snap point just before the natural resting point
		(prevSnap + appRange >= target / scale)                                           // Plus the applicable range is greater than the natural resting point
		||                                                                                // or...
		(                                                                                 // The location of the repeated snap point just after the natural resting point
		(nextSnap - appRange <= target / scale) &&                                       // Minus the applicable range is less than the natural resting point.
		(nextSnap <= effectiveEnd)                                                       // And the snap point after the natural resting point is less than or equal to the effective end value
		)
		)
		*/

		string isInertiaFromImpulseExpression = GetIsInertiaFromImpulseExpression(s_interactionTracker);
		string expression = StringUtil.FormatString(
			"((!%1!s!&&T.%2!s!/T.Scale>=S&&T.%2!s!/T.Scale<=E)||(%1!s!&&T.%2!s!/T.Scale>=iS&&T.%2!s!/T.Scale<=iE))&&(((Floor((T.%2!s!/T.Scale-P)/V)*V)+P+aR>=T.%2!s!/T.Scale)||(((Ceil((T.%2!s!/T.Scale-P)/V)*V)+P-aR<=T.%2!s!/T.Scale)&&((Ceil((T.%2!s!/T.Scale-P)/V)*V)+P<=(%1!s!?iE:E))))",
			isInertiaFromImpulseExpression,
			target);

		//SCROLLPRESENTER_TRACE_VERBOSE(nullptr, TRACE_MSG_METH_STR, METH_NAME, this, expression.c_str());

		var conditionExpressionAnimation = interactionTracker.Compositor.CreateExpressionAnimation(expression);

		SetScalarParameter(conditionExpressionAnimation, s_interval, (float)Interval);
		SetScalarParameter(conditionExpressionAnimation, s_first, (float)DetermineFirstRepeatedSnapPointValue());
		SetScalarParameter(conditionExpressionAnimation, s_start, (float)ActualStart());
		SetScalarParameter(conditionExpressionAnimation, s_end, (float)ActualEnd());
		SetScalarParameter(conditionExpressionAnimation, s_applicableRange, (float)m_specifiedApplicableRange);
		conditionExpressionAnimation.SetReferenceParameter(s_interactionTracker, interactionTracker);

		UpdateConditionalExpressionAnimationForImpulse(
			conditionExpressionAnimation,
			actualImpulseApplicableZone);

		return conditionExpressionAnimation;
	}

	internal override void UpdateConditionalExpressionAnimationForImpulse(
		ExpressionAnimation conditionExpressionAnimation,
		(double, double) actualImpulseApplicableZone)
	{
		SetScalarParameter(conditionExpressionAnimation, s_impulseStart, (float)actualImpulseApplicableZone.Item1);
		SetScalarParameter(conditionExpressionAnimation, s_impulseEnd, (float)actualImpulseApplicableZone.Item2);
	}

	internal override void UpdateRestingPointExpressionAnimationForImpulse(
		ExpressionAnimation restingValueExpressionAnimation,
		double ignoredValue,
		(double, double) actualImpulseApplicableZone)
	{
		SetScalarParameter(restingValueExpressionAnimation, s_impulseEnd, (float)actualImpulseApplicableZone.Item2);
		SetScalarParameter(restingValueExpressionAnimation, s_impulseIgnoredValue, (float)ignoredValue);
	}

	internal override SnapPointSortPredicate SortPredicate()
	{
		// Repeated snap points should be sorted after irregular snap points, so give it a tertiary sort value of 1 (irregular snap points get 0)
		return new SnapPointSortPredicate
		{
			primary = ActualStart(),
			secondary = ActualEnd(),
			tertiary = 1
		};
	}

	internal override (double, double) DetermineActualApplicableZone(
		SnapPointBase previousSnapPoint,
		SnapPointBase nextSnapPoint)
	{
		(double, double) actualApplicableZoneReturned = (
			DetermineMinActualApplicableZone(previousSnapPoint),
			DetermineMaxActualApplicableZone(nextSnapPoint));

		// Influence() will not have thrown if either of the adjacent snap points are also repeated snap points which have the same start and end, however this is not allowed.
		// We only need to check the nextSnapPoint because of the symmetry in the algorithm.
		if (nextSnapPoint is not null && this == nextSnapPoint)
		{
			// TODO: Provide custom error message
			throw new ArgumentException();
		}

		return actualApplicableZoneReturned;
	}

	internal override (double, double) DetermineActualImpulseApplicableZone(
		SnapPointBase previousSnapPoint,
		SnapPointBase nextSnapPoint,
		double currentIgnoredValue,
		double previousIgnoredValue,
		double nextIgnoredValue)
	{
		return (
			DetermineMinActualImpulseApplicableZone(
				previousSnapPoint,
				currentIgnoredValue,
				previousIgnoredValue),
			DetermineMaxActualImpulseApplicableZone(
				nextSnapPoint,
				currentIgnoredValue,
				nextIgnoredValue));
	}

	private double ActualOffset()
	{
		return Offset + m_alignmentAdjustment;
	}

	private double ActualStart()
	{
		return Start + m_alignmentAdjustment;
	}

	private double ActualEnd()
	{
		return End + m_alignmentAdjustment;
	}

	private double DetermineFirstRepeatedSnapPointValue()
	{
		double actualOffset = ActualOffset();
		double actualStart = ActualStart();

		MUX_ASSERT(actualOffset >= actualStart);
		MUX_ASSERT(Interval > 0.0);

		return actualOffset - Math.Floor((actualOffset - actualStart) / Interval) * Interval;
	}

	private double DetermineLastRepeatedSnapPointValue()
	{
		double actualOffset = ActualOffset();
		double actualEnd = ActualEnd();

		MUX_ASSERT(actualOffset <= End);
		MUX_ASSERT(Interval > 0.0);

		return actualOffset + Math.Floor((actualEnd - actualOffset) / Interval) * Interval;
	}

	private double DetermineMinActualApplicableZone(
		SnapPointBase previousSnapPoint)
	{
		double actualStart = ActualStart();

		// The Influence() method of repeated snap points has a check to ensure the value does not fall within its range.
		// This call will ensure that we are not in the range of the previous snap point if it is.
		if (previousSnapPoint is not null)
		{
			previousSnapPoint.Influence(actualStart);
		}
		return actualStart;
	}

	private double DetermineMinActualImpulseApplicableZone(
		SnapPointBase previousSnapPoint,
		double currentIgnoredValue,
		double previousIgnoredValue)
	{
		if (previousSnapPoint is not null)
		{
			if (currentIgnoredValue == DetermineFirstRepeatedSnapPointValue())
			{
				return currentIgnoredValue;
			}

			if (!double.IsNaN(previousIgnoredValue))
			{
				return previousSnapPoint.ImpulseInfluence(ActualStart(), previousIgnoredValue);
			}
		}
		return ActualStart();
	}

	private double DetermineMaxActualApplicableZone(
		SnapPointBase nextSnapPoint)
	{
		double actualEnd = ActualEnd();

		// The Influence() method of repeated snap points has a check to ensure the value does not fall within its range.
		// This call will ensure that we are not in the range of the next snap point if it is.
		if (nextSnapPoint is not null)
		{
			nextSnapPoint.Influence(actualEnd);
		}
		return actualEnd;
	}

	private double DetermineMaxActualImpulseApplicableZone(
		SnapPointBase nextSnapPoint,
		double currentIgnoredValue,
		double nextIgnoredValue)
	{
		if (nextSnapPoint is not null)
		{
			if (currentIgnoredValue == DetermineLastRepeatedSnapPointValue())
			{
				return currentIgnoredValue;
			}

			if (!double.IsNaN(nextIgnoredValue))
			{
				return nextSnapPoint.ImpulseInfluence(ActualEnd(), nextIgnoredValue);
			}
		}
		return ActualEnd();
	}

	private void ValidateConstructorParameters(
#if ApplicableRangeType // UNO TODO
		bool applicableRangeToo,
		double applicableRange,
#endif
		double offset,
		double interval,
		double start,
		double end)
	{
		if (end <= start)
		{
			throw new ArgumentException("'end' must be greater than 'start'.");
		}

		if (offset < start)
		{
			throw new ArgumentException("'offset' must be greater than or equal to 'start'.");
		}

		if (offset > end)
		{
			throw new ArgumentException("'offset' must be smaller than or equal to 'end'.");
		}

		if (interval <= 0)
		{
			throw new ArgumentException("'interval' must be strictly positive.");
		}

#if ApplicableRangeType // UNO TODO
		if (applicableRangeToo && applicableRange <= 0)
		{
			throw new ArgumentException("'applicableRange' must be strictly positive.");
		}
#endif
	}

	internal override double Influence(double edgeOfMidpoint)
	{
		double actualStart = ActualStart();
		double actualEnd = ActualEnd();

		if (edgeOfMidpoint <= actualStart)
		{
			return actualStart;
		}
		else if (edgeOfMidpoint >= actualEnd)
		{
			return actualEnd;
		}
		else
		{
			// Snap points are not allowed within the bounds (Start thru End) of repeated snap points
			// TODO: Provide custom error message
			throw new ArgumentException();
		}
		//return 0.0;
	}

	internal override double ImpulseInfluence(double edgeOfMidpoint, double ignoredValue)
	{
		if (edgeOfMidpoint <= ActualStart())
		{
			if (ignoredValue == DetermineFirstRepeatedSnapPointValue())
			{
				return ignoredValue;
			}
			return ActualStart();
		}
		else if (edgeOfMidpoint >= ActualEnd())
		{
			if (ignoredValue == DetermineLastRepeatedSnapPointValue())
			{
				return ignoredValue;
			}
			return ActualEnd();
		}
		else
		{
			MUX_ASSERT(false);
			return 0.0;
		}
	}

	internal override void Combine(
		ref int combinationCount,
		SnapPointBase snapPoint)
	{
		// Snap points are not allowed within the bounds (Start thru End) of repeated snap points
		// TODO: Provide custom error message
		throw new ArgumentException();
	}

	internal override int SnapCount()
	{
		return (int)((End - Start) / Interval);
	}

	internal override double Evaluate(
		(double, double) actualApplicableZone,
		double value)
	{
		if (value >= ActualStart() && value <= ActualEnd())
		{
			double firstSnapPointValue = DetermineFirstRepeatedSnapPointValue();
			double passedSnapPoints = Math.Floor((value - firstSnapPointValue) / Interval);
			double previousSnapPointValue = (passedSnapPoints * Interval) + firstSnapPointValue;
			double nextSnapPointValue = previousSnapPointValue + Interval;

			if ((value - previousSnapPointValue) <= (nextSnapPointValue - value))
			{
				if (previousSnapPointValue + m_specifiedApplicableRange >= value)
				{
					return previousSnapPointValue;
				}
			}
			else
			{
				if (nextSnapPointValue - m_specifiedApplicableRange <= value)
				{
					return nextSnapPointValue;
				}
			}
		}
		return value;
	}
}

public partial class ZoomSnapPointBase : SnapPointBase
{
	/////////////////////////////////////////////////////////////////////
	/////////////////       Zoom Snap Points      ///////////////////////
	/////////////////////////////////////////////////////////////////////

	// Required for Modern Idl bug, should never be called.
	internal ZoomSnapPointBase()
	{
		// throw (ERROR_CALL_NOT_IMPLEMENTED);
	}

	internal override bool OnUpdateViewport(double newViewport)
	{
		return false;
	}

}

public partial class ZoomSnapPoint : ZoomSnapPointBase
{
	public ZoomSnapPoint(
		double snapPointValue)
	{
		Value = snapPointValue;
	}

#if ApplicableRangeType // UNO TODO
	public ZoomSnapPoint(
		double snapPointValue,
		double applicableRange)
	{
		if (applicableRange <= 0)
		{
			throw new InvalidArgumentException("'applicableRange' must be strictly positive.");
		}

		Value = snapPointValue;
		m_specifiedApplicableRange = applicableRange;
		m_actualApplicableZone = (double, double){ snapPointValue - applicableRange, snapPointValue + applicableRange };
		m_applicableRangeType = SnapPointApplicableRangeType.Optional;
	}
#endif

	public double Value { get; }

	// For zoom snap points scale == L"1.0".
	internal override ExpressionAnimation CreateRestingPointExpression(
		double ignoredValue,
		(double, double) actualImpulseApplicableZone,
		InteractionTracker interactionTracker,
		string target,
		string scale,
		bool isInertiaFromImpulse)
	{
		// SCROLLPRESENTER_TRACE_VERBOSE(nullptr, TRACE_MSG_METH, METH_NAME, this);

		var restingPointExpressionAnimation = interactionTracker.Compositor.CreateExpressionAnimation(s_snapPointValue);

		SetScalarParameter(restingPointExpressionAnimation, s_snapPointValue, (float)Value);

		return restingPointExpressionAnimation;
	}

	// For zoom snap points scale == L"1.0".
	internal override ExpressionAnimation CreateConditionalExpression(
		(double, double) actualApplicableZone,
		(double, double) actualImpulseApplicableZone,
		InteractionTracker interactionTracker,
		string target,
		string scale,
		bool isInertiaFromImpulse)
	{
		string isInertiaFromImpulseExpression = GetIsInertiaFromImpulseExpression("this.Target");
		string targetExpression = GetTargetExpression(target);
		string expression = StringUtil.FormatString(
			"%1!s!?(%2!s!>=%5!s!&&%2!s!<=%6!s!):(%2!s!>=%3!s!&&%2!s!<=%4!s!)",
			isInertiaFromImpulseExpression,
			targetExpression,
			s_minApplicableValue,
			s_maxApplicableValue,
			s_minImpulseApplicableValue,
			s_maxImpulseApplicableValue);

		// SCROLLPRESENTER_TRACE_VERBOSE(nullptr, TRACE_MSG_METH_STR, METH_NAME, this, expression.c_str());

		var conditionExpressionAnimation = interactionTracker.Compositor.CreateExpressionAnimation(expression);

		SetScalarParameter(conditionExpressionAnimation, s_minApplicableValue, (float)actualApplicableZone.Item1);
		SetScalarParameter(conditionExpressionAnimation, s_maxApplicableValue, (float)actualApplicableZone.Item2);

		UpdateConditionalExpressionAnimationForImpulse(
			conditionExpressionAnimation,
			actualImpulseApplicableZone);

		return conditionExpressionAnimation;
	}

	internal override void UpdateConditionalExpressionAnimationForImpulse(
		ExpressionAnimation conditionExpressionAnimation,
		(double, double) actualImpulseApplicableZone)
	{
		SetScalarParameter(conditionExpressionAnimation, s_minImpulseApplicableValue, (float)actualImpulseApplicableZone.Item1);
		SetScalarParameter(conditionExpressionAnimation, s_maxImpulseApplicableValue, (float)actualImpulseApplicableZone.Item2);
	}

	internal override void UpdateRestingPointExpressionAnimationForImpulse(
		ExpressionAnimation restingValueExpressionAnimation,
		double ignoredValue,
		(double, double) actualImpulseApplicableZone)
	{
		// An irregular snap point like ZoomSnapPoint is either completely ignored in impulse mode or not ignored at all, unlike repeated snap points
		// which can be partially ignored. Its conditional expression depends on the impulse mode, whereas its resting point expression does not,
		// thus this method has no job to do.
	}

	internal override SnapPointSortPredicate SortPredicate()
	{
		// Irregular snap point should be sorted before repeated snap points so it gives a tertiary sort value of 0 (repeated snap points get 1)
		return new SnapPointSortPredicate
		{
			primary = Value,
			secondary = Value,
			tertiary = 0
		};
	}

	internal override (double, double) DetermineActualApplicableZone(
		SnapPointBase previousSnapPoint,
		SnapPointBase nextSnapPoint)
	{
		return (
			DetermineMinActualApplicableZone(previousSnapPoint),
			DetermineMaxActualApplicableZone(nextSnapPoint));
	}

	internal override (double, double) DetermineActualImpulseApplicableZone(
		SnapPointBase previousSnapPoint,
		SnapPointBase nextSnapPoint,
		double currentIgnoredValue,
		double previousIgnoredValue,
		double nextIgnoredValue)
	{
		return (
			DetermineMinActualImpulseApplicableZone(
				previousSnapPoint,
				currentIgnoredValue,
				previousIgnoredValue),
			DetermineMaxActualImpulseApplicableZone(
				nextSnapPoint,
				currentIgnoredValue,
				nextIgnoredValue));
	}

	private double DetermineMinActualApplicableZone(
		SnapPointBase previousSnapPoint)
	{
		// If we are not passed a previousSnapPoint it means we are the first in the list, see if we expand to negative Infinity or stay put.
		if (previousSnapPoint is null)
		{
#if ApplicableRangeType // UNO TODO
			if (applicableRangeType != SnapPointApplicableRangeType.Optional)
			{
				return -INFINITY;
			}
			else
			{
				return m_value - m_specifiedApplicableRange;
			}
#else
			return double.NegativeInfinity;
#endif
		}
		// If we are passed a previousSnapPoint then we need to account for its influence on us.
		else
		{
			double previousMaxInfluence = previousSnapPoint.Influence(Value);

#if ApplicableRangeType // UNO TODO
			switch (m_applicableRangeType)
			{
				case SnapPointApplicableRangeType.Optional:
					return Math.Max(previousMaxInfluence, m_value - m_specifiedApplicableRange);
				case SnapPointApplicableRangeType.Mandatory:
					return previousMaxInfluence;
				default:
					MUX_ASSERT(false);
					return 0.0;
			}
#else
			return previousMaxInfluence;
#endif
		}
	}

	private double DetermineMinActualImpulseApplicableZone(
		SnapPointBase previousSnapPoint,
		double currentIgnoredValue,
		double previousIgnoredValue)
	{
		if (previousSnapPoint is null)
		{
			return double.NegativeInfinity;
		}
		else
		{
			double previousMaxInfluence = previousSnapPoint.ImpulseInfluence(Value, previousIgnoredValue);

			if (double.IsNaN(currentIgnoredValue))
			{
				return previousMaxInfluence;
			}
			else
			{
				return Math.Max(previousMaxInfluence, Value);
			}
		}
	}

	private double DetermineMaxActualApplicableZone(
		SnapPointBase nextSnapPoint)
	{
		// If we are not passed a nextSnapPoint it means we are the last in the list, see if we expand to Infinity or stay put.
		if (nextSnapPoint is null)
		{
#if ApplicableRangeType // UNO TODO
			if (m_applicableRangeType != SnapPointApplicableRangeType.Optional)
			{
				return INFINITY;
			}
			else
			{
				return m_value + m_specifiedApplicableRange;
			}
#else
			return double.PositiveInfinity;
#endif
		}
		// If we are passed a nextSnapPoint then we need to account for its influence on us.
		else
		{
			double nextMinInfluence = nextSnapPoint.Influence(Value);

#if ApplicableRangeType // UNO TODO
			switch (m_applicableRangeType)
			{
				case SnapPointApplicableRangeType.Optional:
					return Math.Min(m_value + m_specifiedApplicableRange, nextMinInfluence);
				case SnapPointApplicableRangeType.Mandatory:
					return nextMinInfluence;
				default:
					MUX_ASSERT(false);
					return 0.0;
			}
#else
			return nextMinInfluence;
#endif
		}
	}

	private double DetermineMaxActualImpulseApplicableZone(
		SnapPointBase nextSnapPoint,
		double currentIgnoredValue,
		double nextIgnoredValue)
	{
		if (nextSnapPoint is null)
		{
			return double.PositiveInfinity;
		}
		else
		{
			double nextMinInfluence = nextSnapPoint.ImpulseInfluence(Value, nextIgnoredValue);

			if (double.IsNaN(currentIgnoredValue))
			{
				return nextMinInfluence;
			}
			else
			{
				return Math.Min(Value, nextMinInfluence);
			}
		}
	}

	internal override double Influence(double edgeOfMidpoint)
	{
		double midPoint = (Value + edgeOfMidpoint) / 2;

#if ApplicableRangeType // UNO TODO
		switch (m_applicableRangeType)
		{
			case SnapPointApplicableRangeType.Optional:
				if (Value <= edgeOfMidpoint)
				{
					return Math.Min(Value + m_specifiedApplicableRange, midPoint);
				}
				else
				{
					return Math.Max(Value - m_specifiedApplicableRange, midPoint);
				}
			case SnapPointApplicableRangeType.Mandatory:
				return midPoint;
			default:
				MUX_ASSERT(false);
				return 0.0;
		}
#else
		return midPoint;
#endif
	}

	internal override double ImpulseInfluence(double edgeOfMidpoint, double ignoredValue)
	{
		double midPoint = (Value + edgeOfMidpoint) / 2.0;

		if (double.IsNaN(ignoredValue))
		{
			return midPoint;
		}
		else
		{
			if (Value <= edgeOfMidpoint)
			{
				return Math.Min(Value, midPoint);
			}
			else
			{
				return Math.Max(Value, midPoint);
			}
		}
	}

	internal override void Combine(
		ref int combinationCount,
		SnapPointBase snapPoint)
	{
		var snapPointAsIrregular = snapPoint as ZoomSnapPoint;
		if (snapPointAsIrregular is not null)
		{
#if ApplicableRangeType // UNO TODO
			//TODO: The m_specifiedApplicableRange field is never expected to change after creation. A correction will be needed here.
			m_specifiedApplicableRange = Math.Max(snapPointAsIrregular.ApplicableRange(), m_specifiedApplicableRange);
#else
			MUX_ASSERT(m_specifiedApplicableRange == double.PositiveInfinity);
#endif
			combinationCount++;
		}
		else
		{
			// TODO: Provide custom error message
			throw new ArgumentException();
		}
	}

	internal override int SnapCount()
	{
		return 1;
	}

	internal override double Evaluate(
		(double, double) actualApplicableZone,
		double value)
	{
		if (value >= actualApplicableZone.Item1 && value <= actualApplicableZone.Item2)
		{
			return Value;
		}
		return value;
	}
}

public partial class RepeatedZoomSnapPoint : ZoomSnapPointBase
{
	public RepeatedZoomSnapPoint(
		double offset,
		double interval,
		double start,
		double end)
	{
		ValidateConstructorParameters(
#if ApplicableRangeType // UNO TODO
			false /*applicableRangeToo*/,
			0 /*applicableRange*/,
#endif
			offset,
			interval,
			start,
			end);

		Offset = offset;
		Interval = interval;
		Start = start;
		End = end;
	}

#if ApplicableRangeType // UNO TODO
	RepeatedZoomSnapPoint(
		double offset,
		double interval,
		double start,
		double end,
		double applicableRange)
	{
		ValidateConstructorParameters(
			true /*applicableRangeToo*/,
			applicableRange,
			offset,
			interval,
			start,
			end);

		Offset = offset;
		Interval = interval;
		Start = start;
		End = end;
		m_specifiedApplicableRange = applicableRange;
		m_applicableRangeType = SnapPointApplicableRangeType.Optional;
	}
#endif

	public double Offset { get; }

	public double Interval { get; }

	public double Start { get; }

	public double End { get; }

	// For zoom snap points scale == L"1.0".
	internal override ExpressionAnimation CreateRestingPointExpression(
		double ignoredValue,
		(double, double) actualImpulseApplicableZone,
		InteractionTracker interactionTracker,
		string target,
		string scale,
		bool isInertiaFromImpulse)
	{
		/*
		fracTarget = (target - first) / interval               // Unsnapped value in fractional intervals from first snapping value
		prevSnap = ((Floor(fracTarget) * interval) + first)    // First snapped value before unsnapped value
		nextSnap = ((Ceil(fracTarget) * interval) + first)     // First snapped value after unsnapped value
		effectiveEnd = (IsInertiaFromImpulse ? impEnd : end)   // Regular or impulse upper bound of applicable zone

		Expression:
		((Abs(target - prevSnap) >= Abs(target - nextSnap)) && (nextSnap <= effectiveEnd))
		?
		// nextSnap value is closer to unsnapped value and within applicable zone.
		(
		IsInertiaFromImpulse
		?
		// Impulse mode.
		(
		nextSnap == impIgn
		?
		(
			// Next snapped value is ignored. Pick the previous snapped value if any, else the ignored value.
			(impIgn == first ? first : impIgn - interval)
		)
		:
		// Pick next snapped value.
		nextSnap
		)
		:
		// Regular mode. Pick next snapped value.
		nextSnap
		)
		:
		// prevSnap value is closer to unsnapped value.
		(
		IsInertiaFromImpulse
		?
		// Impulse mode.
		(
		prevSnap == impIgn
		?
		// Previous snapped value is ignored. Pick the next snapped value if any, else the ignored value.
		(impIgn + interval <= effectiveEnd ? impIgn + interval : impIgn)
		:
		(
			prevSnap < first i.e. fracTarget < -0.5
			?
			// Pick next snapped value as previous snapped value is outside applicable zone.
			nextSnap
			:
			// Pick previous snapped value as it is within applicable zone.
			prevSnap
		)
		)
		:
		// Regular mode.
		(
		prevSnap < first i.e. fracTarget < -0.5
		?
		// Pick next snapped value as previous snapped value is outside applicable zone.
		nextSnap
		:
		// Pick previous snapped value as it is within applicable zone.
		prevSnap
		)
		)
		*/

		string isInertiaFromImpulseExpression = GetIsInertiaFromImpulseExpression(s_interactionTracker);
		string expression = StringUtil.FormatString(
			"((Abs(T.%2!s!-(Floor((T.%2!s!-P)/V)*V+P))>=Abs(T.%2!s!-(Ceil((T.%2!s!-P)/V)*V+P)))&&(Ceil((T.%2!s!-P)/V)*V+P<=(%1!s!?iE:E)))?(%1!s!?(Ceil((T.%2!s!-P)/V)*V+P==M?(M==P?P:M-V):Ceil((T.%2!s!-P)/V)*V+P):Ceil((T.%2!s!-P)/V)*V+P):(%1!s!?(Floor((T.%2!s!-P)/V)*V+P==M?(M+V<=(%1!s!?iE:E)?(M+V):M):(T.%2!s!<P-0.5*V?Ceil((T.%2!s!-P)/V)*V+P:Floor((T.%2!s!-P)/V)*V+P)):(T.%2!s!<P-0.5*V?Ceil((T.%2!s!-P)/V)*V+P:Floor((T.%2!s!-P)/V)*V+P))",
			isInertiaFromImpulseExpression,
			target);

		// SCROLLPRESENTER_TRACE_VERBOSE(nullptr, TRACE_MSG_METH_STR, METH_NAME, this, expression.c_str());

		var restingPointExpressionAnimation = interactionTracker.Compositor.CreateExpressionAnimation(expression);

		SetScalarParameter(restingPointExpressionAnimation, s_interval, (float)Interval);
		SetScalarParameter(restingPointExpressionAnimation, s_end, (float)End);
		SetScalarParameter(restingPointExpressionAnimation, s_first, (float)DetermineFirstRepeatedSnapPointValue());
		restingPointExpressionAnimation.SetReferenceParameter(s_interactionTracker, interactionTracker);

		UpdateRestingPointExpressionAnimationForImpulse(
			restingPointExpressionAnimation,
			ignoredValue,
			actualImpulseApplicableZone);

		return restingPointExpressionAnimation;
	}

	// For zoom snap points scale == L"1.0".
	internal override ExpressionAnimation CreateConditionalExpression(
		(double, double) actualApplicableZone,
		(double, double) actualImpulseApplicableZone,
		InteractionTracker interactionTracker,
		string target,
		string scale,
		bool isInertiaFromImpulse)
	{
		MUX_ASSERT(actualApplicableZone.Item1 == Start);
		MUX_ASSERT(actualApplicableZone.Item2 == End);

		/*
		fracTarget = (target - first) / interval               // Unsnapped value in fractional intervals from first snapping value
		prevSnap = ((Floor(fracTarget) * interval) + first)    // First snapped value before unsnapped value
		nextSnap = ((Ceil(fracTarget) * interval) + first)     // First snapped value after unsnapped value
		effectiveEnd = (IsInertiaFromImpulse ? impEnd : end)   // Regular or impulse upper bound of applicable zone

		Expression:
		(
		(!IsInertiaFromImpulse && target >= start && target <= end)       // If we are within the start and end in non-impulse mode
		||
		(IsInertiaFromImpulse && target >= impStart && target <= impEnd)  // or we are within the impulse start and end in impulse mode
		)
		&&                                                                 // and...
		(                                                                  // The location of the repeated snap point just before the natural resting point
		(prevSnap + appRange >= target)                                   // Plus the applicable range is greater than the natural resting point
		||                                                                // or...
		(                                                                 // The location of the repeated snap point just after the natural resting point
		(nextSnap - appRange <= target) &&                               // Minus the applicable range is less than the natural resting point.
		(nextSnap <= effectiveEnd)                                       // And the snap point after the natural resting point is less than or equal to the effective end value
		)
		)
		*/

		string isInertiaFromImpulseExpression = GetIsInertiaFromImpulseExpression(s_interactionTracker);
		string expression = StringUtil.FormatString(
			"((!%1!s!&&T.%2!s!>=S&&T.%2!s!<=E)||(%1!s!&&T.%2!s!>=iS&&T.%2!s!<=iE))&&((Floor((T.%2!s!-P)/V)*V+P+aR>=T.%2!s!)||((Ceil((T.%2!s!-P)/V)*V+P-aR<=T.%2!s!)&&(Ceil((T.%2!s!-P)/V)*V+P<=(%1!s!?iE:E))))",
			isInertiaFromImpulseExpression,
			target);

		// SCROLLPRESENTER_TRACE_VERBOSE(nullptr, TRACE_MSG_METH_STR, METH_NAME, this, expression.c_str());

		var conditionExpressionAnimation = interactionTracker.Compositor.CreateExpressionAnimation(expression);

		SetScalarParameter(conditionExpressionAnimation, s_interval, (float)Interval);
		SetScalarParameter(conditionExpressionAnimation, s_first, (float)DetermineFirstRepeatedSnapPointValue());
		SetScalarParameter(conditionExpressionAnimation, s_start, (float)Start);
		SetScalarParameter(conditionExpressionAnimation, s_end, (float)End);
		SetScalarParameter(conditionExpressionAnimation, s_applicableRange, (float)m_specifiedApplicableRange);
		conditionExpressionAnimation.SetReferenceParameter(s_interactionTracker, interactionTracker);

		UpdateConditionalExpressionAnimationForImpulse(
			conditionExpressionAnimation,
			actualImpulseApplicableZone);

		return conditionExpressionAnimation;
	}

	internal override void UpdateConditionalExpressionAnimationForImpulse(
		ExpressionAnimation conditionExpressionAnimation,
		(double, double) actualImpulseApplicableZone)
	{
		SetScalarParameter(conditionExpressionAnimation, s_impulseStart, (float)actualImpulseApplicableZone.Item1);
		SetScalarParameter(conditionExpressionAnimation, s_impulseEnd, (float)actualImpulseApplicableZone.Item2);
	}

	internal override void UpdateRestingPointExpressionAnimationForImpulse(
		ExpressionAnimation restingValueExpressionAnimation,
		double ignoredValue,
		(double, double) actualImpulseApplicableZone)
	{
		SetScalarParameter(restingValueExpressionAnimation, s_impulseEnd, (float)actualImpulseApplicableZone.Item2);
		SetScalarParameter(restingValueExpressionAnimation, s_impulseIgnoredValue, (float)ignoredValue);
	}

	internal override SnapPointSortPredicate SortPredicate()
	{
		// Repeated snap points should be sorted after irregular snap points, so give it a tertiary sort value of 1 (irregular snap points get 0)
		return new SnapPointSortPredicate
		{
			primary = Start,
			secondary = End,
			tertiary = 1,
		};
	}

	internal override (double, double) DetermineActualApplicableZone(
		SnapPointBase previousSnapPoint,
		SnapPointBase nextSnapPoint)
	{
		(double, double) actualApplicableZoneReturned = (
			DetermineMinActualApplicableZone(previousSnapPoint),
			DetermineMaxActualApplicableZone(nextSnapPoint));

		// Influence() will not have thrown if either of the adjacent snap points are also repeated snap points which have the same start and end, however this is not allowed.
		// We only need to check the nextSnapPoint because of the symmetry in the algorithm.
		if (nextSnapPoint is not null && this == nextSnapPoint)
		{
			// TODO: Provide custom error message
			throw new ArgumentException();
		}

		return actualApplicableZoneReturned;
	}

	internal override (double, double) DetermineActualImpulseApplicableZone(
		SnapPointBase previousSnapPoint,
		SnapPointBase nextSnapPoint,
		double currentIgnoredValue,
		double previousIgnoredValue,
		double nextIgnoredValue)
	{
		return (
			DetermineMinActualImpulseApplicableZone(
				previousSnapPoint,
				currentIgnoredValue,
				previousIgnoredValue),
			DetermineMaxActualImpulseApplicableZone(
				nextSnapPoint,
				currentIgnoredValue,
				nextIgnoredValue));
	}

	private double DetermineFirstRepeatedSnapPointValue()
	{
		MUX_ASSERT(Offset >= Start);
		MUX_ASSERT(Interval > 0.0);

		return Offset - Math.Floor((Offset - Start) / Interval) * Interval;
	}

	private double DetermineLastRepeatedSnapPointValue()
	{
		MUX_ASSERT(Offset <= End);
		MUX_ASSERT(Interval > 0.0);

		return Offset + Math.Floor((End - Offset) / Interval) * Interval;
	}

	private double DetermineMinActualApplicableZone(
		SnapPointBase previousSnapPoint)
	{
		// The Influence() method of repeated snap points has a check to ensure the value does not fall within its range.
		// This call will ensure that we are not in the range of the previous snap point if it is.
		if (previousSnapPoint is not null)
		{
			previousSnapPoint.Influence(Start);
		}
		return Start;
	}

	private double DetermineMinActualImpulseApplicableZone(
		SnapPointBase previousSnapPoint,
		double currentIgnoredValue,
		double previousIgnoredValue)
	{
		if (previousSnapPoint is not null)
		{
			if (currentIgnoredValue == DetermineFirstRepeatedSnapPointValue())
			{
				return currentIgnoredValue;
			}

			if (!double.IsNaN(previousIgnoredValue))
			{
				return previousSnapPoint.ImpulseInfluence(Start, previousIgnoredValue);
			}
		}
		return Start;
	}

	private double DetermineMaxActualApplicableZone(
		SnapPointBase nextSnapPoint)
	{
		// The Influence() method of repeated snap points has a check to ensure the value does not fall within its range.
		// This call will ensure that we are not in the range of the next snap point if it is.
		if (nextSnapPoint is not null)
		{
			nextSnapPoint.Influence(End);
		}
		return End;
	}

	private double DetermineMaxActualImpulseApplicableZone(
		SnapPointBase nextSnapPoint,
		double currentIgnoredValue,
		double nextIgnoredValue)
	{
		if (nextSnapPoint is not null)
		{
			if (currentIgnoredValue == DetermineLastRepeatedSnapPointValue())
			{
				return currentIgnoredValue;
			}

			if (!double.IsNaN(nextIgnoredValue))
			{
				return nextSnapPoint.ImpulseInfluence(End, nextIgnoredValue);
			}
		}
		return End;
	}

	void ValidateConstructorParameters(
#if ApplicableRangeType // UNO TODO
		bool applicableRangeToo,
		double applicableRange,
#endif
		double offset,
		double interval,
		double start,
		double end)
	{
		if (end <= start)
		{
			throw new ArgumentException("'end' must be greater than 'start'.");
		}

		if (offset < start)
		{
			throw new ArgumentException("'offset' must be greater than or equal to 'start'.");
		}

		if (offset > end)
		{
			throw new ArgumentException("'offset' must be smaller than or equal to 'end'.");
		}

		if (interval <= 0)
		{
			throw new ArgumentException("'interval' must be strictly positive.");
		}

#if ApplicableRangeType // UNO TODO
		if (applicableRangeToo && applicableRange <= 0)
		{
			throw new InvalidArgumentException("'applicableRange' must be strictly positive.");
		}
#endif
	}

	internal override double Influence(double edgeOfMidpoint)
	{
		if (edgeOfMidpoint <= Start)
		{
			return Start;
		}
		else if (edgeOfMidpoint >= End)
		{
			return End;
		}
		else
		{
			// Snap points are not allowed within the bounds (Start thru End) of repeated snap points
			// TODO: Provide custom error message
			throw new ArgumentException();
		}
		//return 0.0;
	}

	internal override double ImpulseInfluence(double edgeOfMidpoint, double ignoredValue)
	{
		if (edgeOfMidpoint <= Start)
		{
			if (ignoredValue == DetermineFirstRepeatedSnapPointValue())
			{
				return ignoredValue;
			}
			return Start;
		}
		else if (edgeOfMidpoint >= Start)
		{
			if (ignoredValue == DetermineLastRepeatedSnapPointValue())
			{
				return ignoredValue;
			}
			return Start;
		}
		else
		{
			MUX_ASSERT(false);
			return 0.0;
		}
	}

	internal override void Combine(
		ref int combinationCount,
		SnapPointBase snapPoint)
	{
		// Snap points are not allowed within the bounds (Start thru End) of repeated snap points
		// TODO: Provide custom error message
		throw new ArgumentException();
	}

	internal override int SnapCount()
	{
		return (int)((End - Start) / Interval);
	}

	internal override double Evaluate(
		(double, double) actualApplicableZone,
		double value)
	{
		if (value >= Start && value <= End)
		{
			double firstSnapPointValue = DetermineFirstRepeatedSnapPointValue();
			double passedSnapPoints = Math.Floor((value - firstSnapPointValue) / Interval);
			double previousSnapPointValue = (passedSnapPoints * Interval) + firstSnapPointValue;
			double nextSnapPointValue = previousSnapPointValue + Interval;

			if ((value - previousSnapPointValue) <= (nextSnapPointValue - value))
			{
				if (previousSnapPointValue + m_specifiedApplicableRange >= value)
				{
					return previousSnapPointValue;
				}
			}
			else
			{
				if (nextSnapPointValue - m_specifiedApplicableRange <= value)
				{
					return nextSnapPointValue;
				}
			}
		}
		return value;
	}
}
