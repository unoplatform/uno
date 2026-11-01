#nullable enable

using System.Collections.Generic;
using Uno.UI.DataBinding;

namespace Microsoft.UI.Xaml.Media.Animation;

partial class ObjectAnimationUsingKeyFrames
{
	internal override bool IsAnimation() => true;

	private protected override IEnumerable<IKeyFrame> KeyFrames_Core => KeyFrames.Items;

	private protected override bool m_enableDependentAnimation => EnableDependentAnimation;

	internal override void OnBegin()
	{
		base.OnBegin();

		EnsureKeyFrameThemeResources();
	}

	// CValue::CopyConverted: converts the value (e.g. a string from markup) to the type of the targeted property.
	private object? CopyConverted(object? value)
		=> m_pTargetDependencyProperty is { } property
			? BindingPropertyHelper.Convert(property.Type, value)
			: value;

	/// <summary>
	/// Resolves the key frames' theme resources against the target element's effective theme, so they match it even when
	/// they were materialized in another theme context.
	/// </summary>
	private void EnsureKeyFrameThemeResources()
	{
		if (GetResolvedTargetObject() is not FrameworkElement targetElement)
		{
			return;
		}

		// Resolve against the target's effective (inherited) theme, not its own per-object theme: a template child is
		// usually None even inside a themed subtree. preferAppResourceOverride lets an app-level key override win over the
		// generic template default (WinUI GetKeyOverrideFromApplicationResourcesNoRef).
		foreach (var keyFrame in KeyFrames.Items)
		{
			keyFrame.UpdateAllThemeReferences(targetElement, preferAppResourceOverride: true);
		}
	}
}
