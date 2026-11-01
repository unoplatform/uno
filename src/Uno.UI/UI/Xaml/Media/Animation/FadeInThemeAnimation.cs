#nullable enable

using System;
using System.Collections.Generic;
using Microsoft.UI.Xaml.Markup;

namespace Microsoft.UI.Xaml.Media.Animation;

/// <summary>
/// Represents the preconfigured opacity animation that applies to controls when they are first shown.
/// </summary>
/// <remarks>
/// TODO Uno: WinUI implements the theme animations as dynamic timelines (CDynamicTimeline) that generate their child
/// animations from the theme, animating (UIElement.TransitionTarget).Opacity. Here it animates the stand-in
/// UIElement.TransitionTargetOpacity to 1 with the default animation duration, leaving Opacity to the storyboard.
/// </remarks>
public partial class FadeInThemeAnimation : Timeline
{
	private float m_vFrom;
	private float m_vTo = 1f;
	private float m_vBaseValue;
	private float m_vNonAnimatedBaseValue;
	private float m_vCurrentValue;

	public FadeInThemeAnimation()
	{
	}

	/// <summary>
	/// Identifies the TargetName dependency property.
	/// </summary>
	public static DependencyProperty TargetNameProperty { get; } = DependencyProperty.Register(
		"TargetName", typeof(string), typeof(FadeInThemeAnimation), new FrameworkPropertyMetadata(string.Empty));

	/// <summary>
	/// Gets or sets the reference name of the control element being targeted.
	/// </summary>
	public string TargetName
	{
		get => (string)GetValue(TargetNameProperty);
		set => SetValue(TargetNameProperty, value);
	}

	internal override bool IsAnimation() => true;

	private protected override bool IsToDefault => false;

	internal override void OnBegin()
	{
		if (!string.IsNullOrEmpty(TargetName)
			&& (NameScope.GetNameScope(this)?.FindName(TargetName) ?? ResolveName(TargetName, GetTimingParent())) is DependencyObject target)
		{
			Target = target;
		}

		SetTargetProperty(UIElement.TransitionTargetOpacityProperty);

		base.OnBegin();
	}

	private protected override void InterpolateCurrentValue(float rPercentEnd)
		=> m_vCurrentValue = m_vFrom * (1.0f - rPercentEnd) + rPercentEnd * m_vTo;

	private protected override void GetAnimationBaseValue()
	{
		if (GetResolvedTargetObject()?.GetValue(UIElement.TransitionTargetOpacityProperty) is double value)
		{
			m_vBaseValue = (float)value;
		}
	}

	private protected override bool FindIndependentAnimationTargetsRecursive(DependencyObject pTargetObject, DependencyObject? pSender, List<DependencyObject>? pIATargets)
		=> DoubleAnimation.FindIndependentAnimationTargetsRecursive(this, UIElement.TransitionTargetOpacityProperty, pTargetObject, pSender, pIATargets);

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
			case AssignmentOperand.By:
				return ref m_vTo;
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
