#nullable enable

using System;
using System.Collections.Generic;
using Windows.UI;

namespace Microsoft.UI.Xaml.Media.Animation;

// TODO Uno specific: In WinUI CColorAnimationUsingKeyFrames derives from CColorAnimation; the public types don't, so the
// CColorAnimation members it inherits are repeated here (see ColorAnimation.mux.cs and ColorAnimation.components.mux.cs).
partial class ColorAnimationUsingKeyFrames
{
	private uint m_vFrom;
	private uint m_vTo;
	private uint m_vBy;
	private uint m_vBaseValue;
	private uint m_vNonAnimatedBaseValue;
	private uint m_vCurrentValue;

	internal override bool IsAnimation() => true;

	private protected override IEnumerable<IKeyFrame> KeyFrames_Core => KeyFrames.Items;

	private protected override bool m_enableDependentAnimation => EnableDependentAnimation;

	private protected override Type? AnimatedPropertyType => typeof(Color);

	private protected override void InterpolateCurrentValue(float rPercentEnd)
		=> m_vCurrentValue = ColorAnimation.InterpolateCurrentValue(m_vFrom, m_vTo, rPercentEnd);

	private protected override void ComputeToUsingFromAndBy()
		=> m_vTo = ColorAnimation.ComputeToUsingFromAndBy(m_vFrom, m_vBy);

	private protected override void GetAnimationBaseValue()
	{
		if (GetResolvedTargetObject() is { } targetObject
			&& targetObject.GetValue(m_pTargetDependencyProperty!) is Color rawValue)
		{
			m_vBaseValue = ColorAnimation.ToArgb(rawValue);
		}
	}

	private protected override bool FindIndependentAnimationTargetsRecursive(
		DependencyObject pTargetObject,
		DependencyObject? pSender,
		List<DependencyObject>? pIATargets)
		=> ColorAnimation.FindIndependentAnimationTargetsRecursive(this, m_pTargetDependencyProperty!, pTargetObject, pSender, pIATargets);

	private protected override void ValueAssign(AssignmentOperand destOperand, AssignmentOperand sourceOperand)
		=> GetInternalOperand(destOperand) = GetInternalOperand(sourceOperand);

	private protected override object? ValueAssign(AssignmentOperand sourceOperand)
		=> ColorAnimation.FromArgb(GetInternalOperand(sourceOperand));

	private protected override void ValueAssign(AssignmentOperand destOperand, object? sourceOperand)
		=> GetInternalOperand(destOperand) = sourceOperand is Color color ? ColorAnimation.ToArgb(color) : 0;

	private ref uint GetInternalOperand(AssignmentOperand operand)
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
