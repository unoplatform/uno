// Repro for https://github.com/unoplatform/uno/issues/4032
// Named entries in Page.Resources.ResourceDictionary must generate named fields.
//
// BUG: The XAML source generator generates code in InitializeComponent() that
// tries to USE "MyTopLevelBrush" (assigning it) but doesn't DECLARE it as a field.
// This causes CS0103 in the GENERATED file itself — the bug is in the code generator.
//
// If this file compiles, the bug is fixed. If build fails with CS0103 in the .g.cs file,
// the bug is confirmed.

using Microsoft.UI.Xaml.Controls;

namespace Uno.UI.RuntimeTests.Tests;

public sealed partial class PageResourceNameTest : Page
{
	public PageResourceNameTest()
	{
		InitializeComponent();
	}
}
