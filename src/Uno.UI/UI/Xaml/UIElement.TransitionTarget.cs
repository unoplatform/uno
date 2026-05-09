// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// MUX Reference src\dxaml\xcp\core\inc\TransitionTarget.h, commit 978ab6363

#nullable enable

namespace Microsoft.UI.Xaml;

public partial class UIElement
{
	/// <summary>
	/// Gets the <see cref="TransitionTarget"/> for this element, lazily creating it on first access.
	/// </summary>
	internal TransitionTarget TransitionTarget
	{
		get
		{
			if (GetValue(TransitionTargetProperty) is TransitionTarget existing)
			{
				return existing;
			}

			var created = new TransitionTarget(this);
			SetValue(TransitionTargetProperty, created);
			return created;
		}
	}

	/// <summary>
	/// Returns the cached <see cref="TransitionTarget"/> without creating one if absent.
	/// </summary>
	internal TransitionTarget? GetTransitionTargetOrNull() => GetValue(TransitionTargetProperty) as TransitionTarget;

	internal static DependencyProperty TransitionTargetProperty { get; } =
		DependencyProperty.Register(
			"TransitionTarget",
			typeof(TransitionTarget),
			typeof(UIElement),
			new FrameworkPropertyMetadata(null));
}
