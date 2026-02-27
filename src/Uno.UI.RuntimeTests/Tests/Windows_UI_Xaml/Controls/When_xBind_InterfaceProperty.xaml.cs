// Copyright (c) Microsoft Corporation. All rights reserved. Licensed under the MIT License. See LICENSE in the project root for license information.

using System;
using System.Collections.Generic;
using Microsoft.UI.Xaml.Controls;

namespace Uno.UI.RuntimeTests.Tests.Windows_UI_Xaml.Controls;

/// <summary>
/// Test page for x:Bind with interface properties
/// </summary>
public sealed partial class When_xBind_InterfaceProperty : Page
{
	public When_xBind_InterfaceProperty()
	{
		Model = new TestViewModel();
		this.InitializeComponent();
	}

	public TestViewModel Model { get; }
}

public class TestViewModel
{
	// Property returning array (which implements IReadOnlyList<T> but doesn't have Count property directly)
	public IReadOnlyList<string> Items { get; } = new[] { "Item1", "Item2", "Item3" };

	// Property returning List<T> (which has Count property directly)
	public IReadOnlyList<string> ListItems { get; } = new List<string> { "ListItem1", "ListItem2" };
}
