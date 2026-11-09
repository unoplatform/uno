// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

#nullable enable

using Microsoft.UI.Xaml;

namespace MUXControlsTestApp;

// TODO Uno: stands in for the WinUI sample's Application.Current.Resources, which SamplesApp shares with every other sample.
public sealed partial class TableViewSampleResources : ResourceDictionary
{
	private static TableViewSampleResources? s_instance;

	public TableViewSampleResources()
	{
		this.InitializeComponent();
	}

	internal static TableViewSampleResources Instance => s_instance ??= new TableViewSampleResources();

	internal static DataTemplate Template(string key) => (DataTemplate)Instance[key];
}
