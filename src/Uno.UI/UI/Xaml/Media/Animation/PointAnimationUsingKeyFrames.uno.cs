#nullable enable

using System;
using System.Collections.Generic;
using Windows.Foundation;
using static Microsoft.UI.Xaml.Media.Animation.PointAnimation;

namespace Microsoft.UI.Xaml.Media.Animation;

// TODO Uno specific: In WinUI CPointAnimationUsingKeyFrames derives from CPointAnimation; the public types don't, so the
// CPointAnimation members it inherits are repeated here (see PointAnimation.mux.cs).
partial class PointAnimationUsingKeyFrames
{
	private XPOINTF m_ptFrom;
	private XPOINTF m_ptTo;
	private XPOINTF m_ptBy;
	private XPOINTF m_ptBaseValue;
	private XPOINTF m_ptNonAnimatedBaseValue;
	private XPOINTF m_ptCurrentValue;

	internal override bool IsAnimation() => true;

	private protected override IEnumerable<IKeyFrame> KeyFrames_Core => KeyFrames.Items;

	private protected override bool m_enableDependentAnimation => EnableDependentAnimation;

	private protected override Type? AnimatedPropertyType => typeof(Point);

	private protected override void InterpolateCurrentValue(float rPercentEnd)
		=> m_ptCurrentValue = PointAnimation.InterpolateCurrentValue(m_ptFrom, m_ptTo, rPercentEnd);

	private protected override void ComputeToUsingFromAndBy()
	{
		m_ptTo.x = m_ptFrom.x + m_ptBy.x;
		m_ptTo.y = m_ptFrom.y + m_ptBy.y;
	}

	private protected override void GetAnimationBaseValue()
	{
		if (GetResolvedTargetObject() is { } targetObject
			&& targetObject.GetValue(m_pTargetDependencyProperty!) is Point rawValue)
		{
			m_ptBaseValue = ToXPointF(rawValue);
		}
	}

	private protected override void ValueAssign(AssignmentOperand destOperand, AssignmentOperand sourceOperand)
		=> GetInternalOperand(destOperand) = GetInternalOperand(sourceOperand);

	private protected override object? ValueAssign(AssignmentOperand sourceOperand)
		=> FromXPointF(GetInternalOperand(sourceOperand));

	private protected override void ValueAssign(AssignmentOperand destOperand, object? sourceOperand)
		=> GetInternalOperand(destOperand) = sourceOperand is Point point ? ToXPointF(point) : default;

	private ref XPOINTF GetInternalOperand(AssignmentOperand operand)
	{
		switch (operand)
		{
			case AssignmentOperand.From:
				return ref m_ptFrom;
			case AssignmentOperand.To:
				return ref m_ptTo;
			case AssignmentOperand.By:
				return ref m_ptBy;
			case AssignmentOperand.BaseValue:
				return ref m_ptBaseValue;
			case AssignmentOperand.NonAnimatedBaseValue:
				return ref m_ptNonAnimatedBaseValue;
			case AssignmentOperand.CurrentValue:
				return ref m_ptCurrentValue;
			default:
				throw new ArgumentOutOfRangeException(nameof(operand));
		}
	}
}
