#if HAS_UNO
using System;
using System.Collections.Generic;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Markup;

namespace Uno.UI.RuntimeTests.Tests.Windows_UI_Xaml_Markup
{
	[TestClass]
	[RunsOnUIThread]
	public partial class Given_IXamlCondition
	{
		// Reset state visible to all tests in the class. Each test uses a unique
		// IXamlCondition implementation type so the process-lifetime cache in
		// XamlPredicateService cannot leak results between tests.

		[TestMethod]
		public void When_IXamlCondition_Returns_True_Element_Included()
		{
			AlwaysTrueCondition.WasEvaluated = false;
			var conditionType = typeof(AlwaysTrueCondition).FullName;
			var xaml = $$"""
				<StackPanel xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
							xmlns:cond="http://schemas.microsoft.com/winfx/2006/xaml/presentation?{{conditionType}}(any)">
					<cond:TextBlock Text="visible" />
				</StackPanel>
				""";

			var panel = (StackPanel)XamlReader.Load(xaml);

			Assert.AreEqual(1, panel.Children.Count);
			Assert.IsTrue(AlwaysTrueCondition.WasEvaluated);
			Assert.AreEqual("visible", ((TextBlock)panel.Children[0]).Text);
		}

		[TestMethod]
		public void When_IXamlCondition_Returns_False_Element_Excluded()
		{
			AlwaysFalseCondition.WasEvaluated = false;
			var conditionType = typeof(AlwaysFalseCondition).FullName;
			// Unique argument keeps this test independent of XamlPredicateService's
			// process-wide (Type, args) cache so we can assert that Evaluate ran.
			var xaml = $$"""
				<StackPanel xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
							xmlns:cond="http://schemas.microsoft.com/winfx/2006/xaml/presentation?{{conditionType}}(returns-false-element)">
					<cond:TextBlock Text="hidden" />
					<TextBlock Text="always" />
				</StackPanel>
				""";

			var panel = (StackPanel)XamlReader.Load(xaml);

			Assert.AreEqual(1, panel.Children.Count);
			Assert.AreEqual("always", ((TextBlock)panel.Children[0]).Text);
			Assert.IsTrue(AlwaysFalseCondition.WasEvaluated);
		}

		[TestMethod]
		public void When_IXamlCondition_Argument_Forwarded()
		{
			ArgumentRecordingCondition.LastArgument = null;
			var conditionType = typeof(ArgumentRecordingCondition).FullName;
			var xaml = $$"""
				<StackPanel xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
							xmlns:cond="http://schemas.microsoft.com/winfx/2006/xaml/presentation?{{conditionType}}(MyFlag)">
					<cond:TextBlock Text="x" />
				</StackPanel>
				""";

			XamlReader.Load(xaml);

			Assert.AreEqual("MyFlag", ArgumentRecordingCondition.LastArgument);
		}

		[TestMethod]
		public void When_IXamlCondition_Result_Cached()
		{
			CountingCondition.EvaluateCount = 0;
			var conditionType = typeof(CountingCondition).FullName;
			var xaml = $$"""
				<StackPanel xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
							xmlns:cond="http://schemas.microsoft.com/winfx/2006/xaml/presentation?{{conditionType}}(SameArg)">
					<cond:TextBlock Text="a" />
				</StackPanel>
				""";

			XamlReader.Load(xaml);
			XamlReader.Load(xaml);
			XamlReader.Load(xaml);

			// XamlPredicateService caches (Type, args) results for the lifetime of the
			// process — the user's Evaluate must run at most once per (type, argument) pair.
			Assert.AreEqual(1, CountingCondition.EvaluateCount);
		}

		[TestMethod]
		public void When_IXamlCondition_Different_Arguments_Evaluated_Separately()
		{
			DifferingArgsCondition.EvaluatedArguments.Clear();
			var conditionType = typeof(DifferingArgsCondition).FullName;
			var xaml = $$"""
				<StackPanel xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
							xmlns:on="http://schemas.microsoft.com/winfx/2006/xaml/presentation?{{conditionType}}(On)"
							xmlns:off="http://schemas.microsoft.com/winfx/2006/xaml/presentation?{{conditionType}}(Off)">
					<on:TextBlock Text="on-only" />
					<off:TextBlock Text="off-only" />
				</StackPanel>
				""";

			var panel = (StackPanel)XamlReader.Load(xaml);

			Assert.AreEqual(1, panel.Children.Count);
			Assert.AreEqual("on-only", ((TextBlock)panel.Children[0]).Text);
			CollectionAssert.AreEquivalent(new[] { "On", "Off" }, DifferingArgsCondition.EvaluatedArguments);
		}

		[TestMethod]
		public void When_IXamlCondition_Conditional_Attribute_Excluded()
		{
			AlwaysFalseCondition.WasEvaluated = false;
			var conditionType = typeof(AlwaysFalseCondition).FullName;
			// When the conditional namespace evaluates false the attribute set on the
			// element through that namespace must not be applied. Unique arg avoids
			// the process-wide XamlPredicateService cache.
			var xaml = $$"""
				<TextBlock xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
						   xmlns:cond="http://schemas.microsoft.com/winfx/2006/xaml/presentation?{{conditionType}}(conditional-attribute)"
						   Text="default"
						   cond:Text="overridden" />
				""";

			var textBlock = (TextBlock)XamlReader.Load(xaml);

			Assert.AreEqual("default", textBlock.Text);
			Assert.IsTrue(AlwaysFalseCondition.WasEvaluated);
		}

		[TestMethod]
		public void When_Condition_Type_Not_Resolved_Falls_Back_To_Default_Include()
		{
			// Unknown predicate name that does not resolve to any type — the existing
			// fallback (return Default with namespace stripped) is preserved.
			var xaml = """
				<StackPanel xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
							xmlns:cond="http://schemas.microsoft.com/winfx/2006/xaml/presentation?Some.Bogus.Type(arg)">
					<cond:TextBlock Text="kept" />
				</StackPanel>
				""";

			var panel = (StackPanel)XamlReader.Load(xaml);

			Assert.AreEqual(1, panel.Children.Count);
			Assert.AreEqual("kept", ((TextBlock)panel.Children[0]).Text);
		}
	}

	public sealed partial class AlwaysTrueCondition : DependencyObject, IXamlCondition
	{
		public static bool WasEvaluated { get; set; }

		public bool Evaluate(string argument)
		{
			WasEvaluated = true;
			return true;
		}
	}

	public sealed partial class AlwaysFalseCondition : DependencyObject, IXamlCondition
	{
		public static bool WasEvaluated { get; set; }

		public bool Evaluate(string argument)
		{
			WasEvaluated = true;
			return false;
		}
	}

	public sealed partial class ArgumentRecordingCondition : DependencyObject, IXamlCondition
	{
		public static string LastArgument { get; set; }

		public bool Evaluate(string argument)
		{
			LastArgument = argument;
			return true;
		}
	}

	public sealed partial class CountingCondition : DependencyObject, IXamlCondition
	{
		public static int EvaluateCount;

		public bool Evaluate(string argument)
		{
			EvaluateCount++;
			return true;
		}
	}

	public sealed partial class DifferingArgsCondition : DependencyObject, IXamlCondition
	{
		public static List<string> EvaluatedArguments { get; } = new List<string>();

		public bool Evaluate(string argument)
		{
			EvaluatedArguments.Add(argument);
			return argument == "On";
		}
	}
}
#endif
