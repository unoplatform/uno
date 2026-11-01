#nullable enable

using System;
using System.Collections.Generic;

namespace Microsoft.UI.Xaml.Media.Animation;

// TODO Uno specific: In WinUI CDoubleAnimationUsingKeyFrames derives from CDoubleAnimation; the public types don't, so the
// CDoubleAnimation members it inherits are repeated here (see DoubleAnimation.mux.cs and DoubleAnimation.components.mux.cs).
partial class DoubleAnimationUsingKeyFrames
{
	private float m_vFrom;
	private float m_vTo;
	private float m_vBy;
	private float m_vBaseValue;
	private float m_vNonAnimatedBaseValue;
	private float m_vCurrentValue;

	internal override bool IsAnimation() => true;

	private protected override IEnumerable<IKeyFrame> KeyFrames_Core => KeyFrames.Items;

	private protected override bool m_enableDependentAnimation => EnableDependentAnimation;

	private protected override Type? AnimatedPropertyType => typeof(double);

	private protected override void InterpolateCurrentValue(float rPercentEnd)
	{
		var rPercentStart = 1.0f - rPercentEnd;
		var rResult = m_vFrom * rPercentStart + rPercentEnd * m_vTo;
		m_vCurrentValue = rResult;
	}

	private protected override void ComputeToUsingFromAndBy() => m_vTo = m_vFrom + m_vBy;

	private protected override void GetAnimationBaseValue()
	{
		var targetObject = GetResolvedTargetObject();

		if (targetObject != null)
		{
			var rawValue = targetObject.GetValue(m_pTargetDependencyProperty!);

			if (rawValue is float floatValue)
			{
				m_vBaseValue = floatValue;
			}

			if (rawValue is double doubleValue)
			{
				m_vBaseValue = (float)doubleValue;
			}
		}
	}

	private protected override bool FindIndependentAnimationTargetsRecursive(
		DependencyObject pTargetObject,
		DependencyObject? pSender,
		List<DependencyObject>? pIATargets)
		=> DoubleAnimation.FindIndependentAnimationTargetsRecursive(this, m_pTargetDependencyProperty!, pTargetObject, pSender, pIATargets);

	private protected override void ValueAssign(AssignmentOperand destOperand, AssignmentOperand sourceOperand)
		=> GetInternalOperand(destOperand) = GetInternalOperand(sourceOperand);

	private protected override object? ValueAssign(AssignmentOperand sourceOperand) => (double)GetInternalOperand(sourceOperand);

	private protected override void ValueAssign(AssignmentOperand destOperand, object? sourceOperand)
		=> GetInternalOperand(destOperand) = DoubleAnimation.ToFloat(sourceOperand);

	private ref float GetInternalOperand(AssignmentOperand operand)
	{
		switch (operand)
		{
			case AssignmentOperand.From:
				return ref m_vFrom;
			case AssignmentOperand.To:
				return ref m_vTo;
			case AssignmentOperand.By:
				return ref m_vBy;
			case AssignmentOperand.BaseValue:
				return ref m_vBaseValue;
			case AssignmentOperand.NonAnimatedBaseValue:
				return ref m_vNonAnimatedBaseValue;
			case AssignmentOperand.CurrentValue:
				return ref m_vCurrentValue;
			default:
				throw new ArgumentOutOfRangeException(nameof(operand));
		}
	}
}
