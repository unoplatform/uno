// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// MUX reference PipsPagerTemplateSettings.properties.cpp, commit 3a84856

using System.Collections.Generic;
using Windows.UI.Xaml;

namespace Microsoft.UI.Xaml.Controls
{
	public sealed partial class PipsPagerTemplateSettings : DependencyObject
	{
		public IList<int> PipsPagerItems
		{
			get => (IList<int>)GetValue(PipsPagerItemsProperty);
			set => SetValue(PipsPagerItemsProperty, value);
		}

		public static DependencyProperty PipsPagerItemsProperty { get; } =
			DependencyProperty.Register(
				nameof(PipsPagerItems),
				typeof(IList<int>),
				typeof(PipsPagerTemplateSettings),
				new FrameworkPropertyMetadata(null));
	}
}
