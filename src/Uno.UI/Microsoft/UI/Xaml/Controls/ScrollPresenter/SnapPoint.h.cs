// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using Microsoft.UI.Composition;
using Microsoft.UI.Composition.Interactions;

namespace Microsoft.UI.Xaml.Controls.Primitives;

internal struct SnapPointSortPredicate
{
	//Sorting of snap points goes as follows:
	//We first sort on the primary value which for repeated snap points are their actualStart values, and for irregular snap points their values.
	//We then sort by the secondary value which for repeated snap points are their actualEnd values, and for irregular snap points are again their values.
	//We then sort by the tertiary value which for repeated snap points is 1 and for irregular snap points is 0.

	//Since repeated snap points' actualEnd is always greater than or equal to their actualStart the tertiary value is only used if these values are exactly equal
	//for a repeated snap point which also shares a primary (and subsequently secondary) value with an irregular snap point's value.

	//The result of this sorting is irregular snap points will always be sorted before repeated snap points when ambiguity occurs.
	//This allows us to address some corner cases in a more elegant fashion.
	public double primary;
	public double secondary;
	public short tertiary;
}

public partial class SnapPointBase
{
#if ApplicableRangeType // UNO TODO
	public double ApplicableRange();
	public SnapPointApplicableRangeType ApplicableRangeType();
#endif

	internal virtual ExpressionAnimation CreateRestingPointExpression(
		double ignoredValue,
		(double, double) actualImpulseApplicableZone,
		InteractionTracker interactionTracker,
		string target,
		string scale,
		bool isInertiaFromImpulse) => default;
	internal virtual ExpressionAnimation CreateConditionalExpression(
		(double, double) actualApplicableZone,
		(double, double) actualImpulseApplicableZone,
		InteractionTracker interactionTracker,
		string target,
		string scale,
		bool isInertiaFromImpulse) => default;
	internal virtual void UpdateConditionalExpressionAnimationForImpulse(
		ExpressionAnimation conditionExpressionAnimation,
		(double, double) actualImpulseApplicableZone)
	{
	}

	internal virtual void UpdateRestingPointExpressionAnimationForImpulse(
		ExpressionAnimation restingValueExpressionAnimation,
		double ignoredValue,
		(double, double) actualImpulseApplicableZone)
	{
	}
	internal virtual SnapPointSortPredicate SortPredicate() => default;
	internal virtual (double, double) DetermineActualApplicableZone(
		SnapPointBase previousSnapPoint,
		SnapPointBase nextSnapPoint) => default;
	internal virtual (double, double) DetermineActualImpulseApplicableZone(
		SnapPointBase previousSnapPoint,
		SnapPointBase nextSnapPoint,
		double currentIgnoredValue,
		double previousIgnoredValue,
		double nextIgnoredValue) => default;
	internal virtual double Influence(double edgeOfMidpoint) => default;
	internal virtual double ImpulseInfluence(double edgeOfMidpoint, double ignoredValue) => default;
	internal virtual void Combine(
		ref int combinationCount,
		SnapPointBase snapPoint)
	{
	}

	internal virtual int SnapCount() => default;
	internal virtual double Evaluate((double, double) actualApplicableZone, double value) => default;

	// Returns True when this snap point is sensitive to the viewport size and is interested in future updates.
	internal virtual bool OnUpdateViewport(double newViewport) => default;

	private protected double m_specifiedApplicableRange = double.PositiveInfinity;

#if ApplicableRangeType // UNO TODO
	// Only mandatory snap points are supported at this point.
	SnapPointApplicableRangeType m_applicableRangeType = SnapPointApplicableRangeType.Mandatory;
#endif


	// Maximum difference for snap points to be considered equal
	private const double s_equalityEpsilon = 0.00001;

	// Constants used in composition expressions
	private protected const string s_snapPointValue = "snapPointValue";
	private protected const string s_minApplicableValue = "minAppValue";
	private protected const string s_maxApplicableValue = "maxAppValue";
	private protected const string s_minImpulseApplicableValue = "minImpAppValue";
	private protected const string s_maxImpulseApplicableValue = "maxImpAppValue";
	private protected const string s_interval = "V";
	private protected const string s_start = "S";
	private protected const string s_end = "E";
	private protected const string s_first = "P";
	private protected const string s_applicableRange = "aR";
	private protected const string s_impulseStart = "iS";
	private protected const string s_impulseEnd = "iE";
	private protected const string s_impulseIgnoredValue = "M";
	private protected const string s_interactionTracker = "T";
}

public partial class ScrollSnapPoint
{


	// private:
	//     double ActualValue() const;
	//     double DetermineMinActualApplicableZone(
	//         SnapPointBase previousSnapPoint) const;
	//     double DetermineMinActualImpulseApplicableZone(
	//         SnapPointBase previousSnapPoint,
	//         double currentIgnoredValue,
	//         double previousIgnoredValue) const;
	//     double DetermineMaxActualApplicableZone(
	//         SnapPointBase nextSnapPoint) const;
	//     double DetermineMaxActualImpulseApplicableZone(
	//         SnapPointBase nextSnapPoint,
	//         double currentIgnoredValue,
	//         double nextIgnoredValue) const;

	//     double m_value{ 0.0 };
}

public partial class RepeatedScrollSnapPoint
{

	//Internal
	// ExpressionAnimation CreateRestingPointExpression(
	//     double ignoredValue,
	//     (double, double) actualImpulseApplicableZone,
	//     InteractionTracker interactionTracker,
	//     hstring target,
	//     hstring scale,
	//     bool isInertiaFromImpulse);
	// ExpressionAnimation CreateConditionalExpression(
	//     (double, double) actualApplicableZone,
	//     (double, double) actualImpulseApplicableZone,
	//     InteractionTracker interactionTracker,
	//     hstring target,
	//     hstring scale,
	//     bool isInertiaFromImpulse);
	// void UpdateConditionalExpressionAnimationForImpulse(
	//     ExpressionAnimation conditionExpressionAnimation,
	//     (double, double) actualImpulseApplicableZone) const;
	// void UpdateRestingPointExpressionAnimationForImpulse(
	//     ExpressionAnimation restingValueExpressionAnimation,
	//     double ignoredValue,
	//     (double, double) actualImpulseApplicableZone) const;
	// SnapPointSortPredicate SortPredicate() const;
	// (double, double) DetermineActualApplicableZone(
	//     SnapPointBase previousSnapPoint,
	//     SnapPointBase nextSnapPoint);
	// (double, double) DetermineActualImpulseApplicableZone(
	//     SnapPointBase previousSnapPoint,
	//     SnapPointBase nextSnapPoint,
	//     double currentIgnoredValue,
	//     double previousIgnoredValue,
	//     double nextIgnoredValue);
	// double Influence(
	//     double edgeOfMidpoint) const;
	// double ImpulseInfluence(
	//     double edgeOfMidpoint,
	//     double ignoredValue) const;
	// void Combine(
	//     int& combinationCount,
	//     SnapPointBase snapPoint) const;
	// int SnapCount() const;
	// double Evaluate(
	//     (double, double) actualApplicableZone,
	//     double value) const;

	// private:
	//     double ActualOffset() const;
	//     double ActualStart() const;
	//     double ActualEnd() const;
	//     double DetermineFirstRepeatedSnapPointValue() const;
	//     double DetermineLastRepeatedSnapPointValue() const;
	//     double DetermineMinActualApplicableZone(
	//         SnapPointBase previousSnapPoint) const;
	//     double DetermineMinActualImpulseApplicableZone(
	//         SnapPointBase previousSnapPoint,
	//         double currentIgnoredValue,
	//         double previousIgnoredValue) const;
	//     double DetermineMaxActualApplicableZone(
	//         SnapPointBase nextSnapPoint) const;
	//     double DetermineMaxActualImpulseApplicableZone(
	//         SnapPointBase nextSnapPoint,
	//         double currentIgnoredValue,
	//         double nextIgnoredValue) const;
	//     void ValidateConstructorParameters(
	// #ifdef ApplicableRangeType
	//         bool applicableRangeToo,
	//         double applicableRange,
	// #endif
	//         double offset,
	//         double interval,
	//         double start,
	//         double end) const;

	//     double m_offset{ 0.0f };
	//     double m_interval{ 0.0f };
	//     double m_start{ 0.0f };
	//     double m_end{ 0.0f };
}

public partial class ZoomSnapPointBase
{
	//bool OnUpdateViewport(double newViewport);

	//protected:
	//// Needed as work around for Modern Idl inheritance bug
	//ZoomSnapPointBase();
};

public partial class ZoomSnapPoint : ZoomSnapPointBase
{

	//Internal
	//     ExpressionAnimation CreateRestingPointExpression(
	//         double ignoredValue,
	//         (double, double) actualImpulseApplicableZone,
	//         InteractionTracker interactionTracker,
	//         hstring target,
	//         hstring scale,
	//         bool isInertiaFromImpulse);
	//     ExpressionAnimation CreateConditionalExpression(
	//         (double, double) actualApplicableZone,
	//         (double, double) actualImpulseApplicableZone,
	//         InteractionTracker interactionTracker,
	//         hstring target,
	//         hstring scale,
	//         bool isInertiaFromImpulse);
	//     void UpdateConditionalExpressionAnimationForImpulse(
	//         ExpressionAnimation conditionExpressionAnimation,
	//         (double, double) actualImpulseApplicableZone) const;
	//     void UpdateRestingPointExpressionAnimationForImpulse(
	//         ExpressionAnimation restingValueExpressionAnimation,
	//         double ignoredValue,
	//         (double, double) actualImpulseApplicableZone) const;
	//     SnapPointSortPredicate SortPredicate() const;
	//     (double, double) DetermineActualApplicableZone(
	//         SnapPointBase previousSnapPoint,
	//         SnapPointBase nextSnapPoint);
	//     (double, double) DetermineActualImpulseApplicableZone(
	//         SnapPointBase previousSnapPoint,
	//         SnapPointBase nextSnapPoint,
	//         double currentIgnoredValue,
	//         double previousIgnoredValue,
	//         double nextIgnoredValue);
	//     double Influence(
	//         double edgeOfMidpoint) const;
	//     double ImpulseInfluence(
	//         double edgeOfMidpoint,
	//         double ignoredValue) const;
	//     void Combine(
	//         int& combinationCount,
	//         SnapPointBase snapPoint) const;
	//     int SnapCount() const;
	//     double Evaluate(
	//         (double, double) actualApplicableZone,
	//         double value) const;

	// private:
	//     double DetermineMinActualApplicableZone(
	//         SnapPointBase previousSnapPoint) const;
	//     double DetermineMinActualImpulseApplicableZone(
	//         SnapPointBase previousSnapPoint,
	//         double currentIgnoredValue,
	//         double previousIgnoredValue) const;
	//     double DetermineMaxActualApplicableZone(
	//         SnapPointBase nextSnapPoint) const;
	//     double DetermineMaxActualImpulseApplicableZone(
	//         SnapPointBase nextSnapPoint,
	//         double currentIgnoredValue,
	//         double nextIgnoredValue) const;

	//     double m_value{ 0.0 };
};

public partial class RepeatedZoomSnapPoint
{

	//RepeatedZoomSnapPoint(
	//	double offset,
	//	double interval,
	//	double start,
	//	double end);

#if ApplicableRangeType
	RepeatedZoomSnapPoint(
		double offset,
		double interval,
		double start,
		double end,
		double applicableRange);
#endif

	//double Offset();
	//double Interval();
	//double Start();
	//double End();

	//Internal
	//     ExpressionAnimation CreateRestingPointExpression(
	//         double ignoredValue,
	//         (double, double) actualImpulseApplicableZone,
	//         InteractionTracker interactionTracker,
	//         hstring target,
	//         hstring scale,
	//         bool isInertiaFromImpulse);
	//     ExpressionAnimation CreateConditionalExpression(
	//         (double, double) actualApplicableZone,
	//         (double, double) actualImpulseApplicableZone,
	//         InteractionTracker interactionTracker,
	//         hstring target,
	//         hstring scale,
	//         bool isInertiaFromImpulse);
	//     void UpdateConditionalExpressionAnimationForImpulse(
	//         ExpressionAnimation conditionExpressionAnimation,
	//         (double, double) actualImpulseApplicableZone) const;
	//     void UpdateRestingPointExpressionAnimationForImpulse(
	//         ExpressionAnimation restingValueExpressionAnimation,
	//         double ignoredValue,
	//         (double, double) actualImpulseApplicableZone) const;
	//     SnapPointSortPredicate SortPredicate() const;
	//     (double, double) DetermineActualApplicableZone(
	//         SnapPointBase previousSnapPoint,
	//         SnapPointBase nextSnapPoint);
	//     (double, double) DetermineActualImpulseApplicableZone(
	//         SnapPointBase previousSnapPoint,
	//         SnapPointBase nextSnapPoint,
	//         double currentIgnoredValue,
	//         double previousIgnoredValue,
	//         double nextIgnoredValue);
	//     double Influence(
	//         double edgeOfMidpoint) const;
	//     double ImpulseInfluence(
	//         double edgeOfMidpoint,
	//         double ignoredValue) const;
	//     void Combine(
	//         int& combinationCount,
	//         SnapPointBase snapPoint) const;
	//     int SnapCount() const;
	//     double Evaluate(
	//         (double, double) actualApplicableZone,
	//         double value) const;

	// private:
	//     double DetermineFirstRepeatedSnapPointValue() const;
	//     double DetermineLastRepeatedSnapPointValue() const;
	//     double DetermineMinActualApplicableZone(
	//         SnapPointBase previousSnapPoint) const;
	//     double DetermineMinActualImpulseApplicableZone(
	//         SnapPointBase previousSnapPoint,
	//         double currentIgnoredValue,
	//         double previousIgnoredValue) const;
	//     double DetermineMaxActualApplicableZone(
	//         SnapPointBase nextSnapPoint) const;
	//     double DetermineMaxActualImpulseApplicableZone(
	//         SnapPointBase nextSnapPoint,
	//         double currentIgnoredValue,
	//         double nextIgnoredValue) const;
	//     void ValidateConstructorParameters(
	// #ifdef ApplicableRangeType
	//         bool applicableRangeToo,
	//         double applicableRange,
	// #endif
	//         double offset,
	//         double interval,
	//         double start,
	//         double end) const;

	//     double m_offset{ 0.0f };
	//     double m_interval{ 0.0f };
	//     double m_start{ 0.0f };
	//     double m_end{ 0.0f };
}
