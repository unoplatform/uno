using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.UI.Xaml;
using static Private.Infrastructure.TestServices;
using Microsoft.UI.Xaml.Controls;
using System.Threading.Tasks;
using Uno.UI.RuntimeTests.Helpers;
using Uno.UI.Extensions;

namespace Uno.UI.RuntimeTests.Tests.Uno_UI;

[TestClass]
public class Given_ResourceResolver
{
	[TestMethod]
	[RunsOnUIThread]
	public void When_Resolving_String_Resources_Should_Produce_Target_Type()
	{
		var SUT = new Resolve_String_Resources();
		WindowHelper.WindowContent = SUT;
		var expected = new Duration(TimeSpan.Parse("00:00:00.250"));

		Assert.AreEqual(expected, SUT.ReferencingStringDurationFromDP.Duration);
		Assert.AreEqual(expected, SUT.ReferenceStringDurationFromProperty.Duration);
	}

	[TestMethod]
	[RunsOnUIThread]
	public async Task When_Resolving_FrameworkElement_Local_ThemeResource()
	{
		var setup = new ResourceMarkupTest_Setup();
		await UITestHelper.Load(setup);

		const string ExpectedMatchHead = "override Page.Resources-"; // dont care if we got -light or -dark
		var doTestText = ITestable.GetTextValue(setup.SUT_DO_Themed.Content);
		var feTestText = ITestable.GetTextValue(setup.SUT_FE_Themed.Content);

		if (doTestText?.StartsWith(ExpectedMatchHead) != true || feTestText?.StartsWith(ExpectedMatchHead) != true)
		{
			string FormatContent(object content) => content switch
			{
				null => "null",
				ITestable t => $"[{t.GetType().Name}]{t.Text}",
				object o => $"[{o.GetType().Name}]",
			};

			Assert.Fail($"Expecting ITestable.Text to start with '{ExpectedMatchHead}': " +
				$"SUT_DO_Themed.Content={FormatContent(setup.SUT_DO_Themed.Content)}, " +
				$"SUT_FE_Themed.Content={FormatContent(setup.SUT_FE_Themed.Content)}");
		}
	}

	[TestMethod]
	[RunsOnUIThread]
	public async Task When_Resolving_TextBlockInlines_Local_ThemeResource()
	{
		var setup = new ResourceMarkupTest_Setup();
		await UITestHelper.Load(setup);

		const string ExpectedMatchHead = "override Page.Resources-"; // dont care if we got -light or -dark
		var run0 = setup.SUT_TextBlock_Run0;
		var run1 = setup.SUT_TextBlock_Run1;

		if (run0.Text?.StartsWith(ExpectedMatchHead) != true || run1.Text?.StartsWith(ExpectedMatchHead) != true)
		{
			Assert.Fail($"Expecting ITestable.Text to start with '{ExpectedMatchHead}': " +
				$"SUT_TextBlock_Run0.Text={run0.Text ?? "<null>"}, " +
				$"SUT_TextBlock_Run1.Text={run1.Text ?? "<null>"}");
		}
	}
}

#region other classes

public class MyCustomClass
{
	public Duration Duration { get; set; }
}

public interface ITestable
{
	string Text { get; }

	public static string GetTextValue(object maybeTest) => (maybeTest as ITestable)?.Text;
}
public partial class TestControl : Control, ITestable
{
	#region DependencyProperty: Text

	public static DependencyProperty TextProperty { get; } = DependencyProperty.Register(
		nameof(Text),
		typeof(string),
		typeof(TestControl),
		new PropertyMetadata(default(string), OnTextChanged));

	public string Text
	{
		get => (string)GetValue(TextProperty);
		set => SetValue(TextProperty, value);
	}

	#endregion

	private static void OnTextChanged(DependencyObject dependencyObject, DependencyPropertyChangedEventArgs args)
	{
		var msg = $"{dependencyObject?.GetType().Name}.OnTextChanged: []{args.OldValue} -> []{args.NewValue}";
	}
}
public partial class TestDepObj : DependencyObject, ITestable
{
	#region DependencyProperty: Text

	public static DependencyProperty TextProperty { get; } = DependencyProperty.Register(
		nameof(Text),
		typeof(string),
		typeof(TestDepObj),
		new PropertyMetadata(default(string), OnTextChanged));

	public string Text
	{
		get => (string)GetValue(TextProperty);
		set => SetValue(TextProperty, value);
	}

	#endregion

	private static void OnTextChanged(DependencyObject dependencyObject, DependencyPropertyChangedEventArgs args)
	{
		var msg = $"{dependencyObject?.GetType().Name}.OnTextChanged: []{args.OldValue} -> []{args.NewValue}";
	}
}
#endregion
