// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// MUX Reference BackdropMaterial.properties.cpp, commit 0db5d03

using System.Diagnostics.CodeAnalysis;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace Microsoft.UI.Xaml.Controls
{
	public partial class BackdropMaterial
	{
		/// <summary>
		/// Gets the value of the BackdropMaterial.ApplyToRootOrPageBackground XAML attached property for the target element.
		/// </summary>
		/// <param name="element">The object from which the property value is read.</param>
		/// <returns>The BackdropMaterial.ApplyToRootOrPageBackground XAML attached property value of the requested object.</returns>
		public static bool GetApplyToRootOrPageBackground(Control element) =>
			(bool)element.GetValue(ApplyToRootOrPageBackgroundProperty);

		/// <summary>
		/// Sets the value of the BackdropMaterial.ApplyToRootOrPageBackground XAML attached property for a target element.
		/// </summary>
		/// <param name="element">The object to which the property value is written.</param>
		/// <param name="value">The value to set.</param>
		public static void SetApplyToRootOrPageBackground(Control element, bool value) =>
			element.SetValue(ApplyToRootOrPageBackgroundProperty, value);

		/// <summary>
		/// Applies the backdrop material to the root or background of the XAML content.
		/// </summary>
		public static DependencyProperty ApplyToRootOrPageBackgroundProperty
		{
			[DynamicDependency(nameof(GetApplyToRootOrPageBackground))]
			[DynamicDependency(nameof(SetApplyToRootOrPageBackground))]
			get;
		} = DependencyProperty.RegisterAttached(
				"ApplyToRootOrPageBackground",
				typeof(bool),
				typeof(BackdropMaterial),
				new FrameworkPropertyMetadata(false, OnApplyToRootOrPageBackgroundChanged));
	}
}
