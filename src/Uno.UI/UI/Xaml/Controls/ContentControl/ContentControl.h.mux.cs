// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

// MUX Reference ContentControl.h, tag winui3/release/1.8.2, commit bac7a9c33

using System;

namespace Microsoft.UI.Xaml.Controls;

partial class ContentControl
{
	/// <summary>
	/// The ContentPresenter of the applied template that presents this control's own Content.
	/// </summary>
	private WeakReference<ContentPresenter> m_pTemplatePresenter;

	private bool m_bInOnApplyTemplate;

	private const string c_strTextTemplateStorage = """
		<ControlTemplate xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation">
			<Grid Background="{TemplateBinding Background}">
				<TextBlock Text="{Binding}" HorizontalAlignment="Left" VerticalAlignment="Top" TextAlignment="Left" TextWrapping="NoWrap" />
			</Grid>
		</ControlTemplate>
		""";
}
