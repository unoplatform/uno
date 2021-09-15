using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Testing;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Uno.NonShippingAnalyzers;

namespace Uno.Analyzers.Tests.Verifiers
{
	using VerifyCS = CSharpCodeFixVerifier<UnoStringDependencyPropertyAnalyzer, EmptyCodeFixProvider>;

	[TestClass]
	public class UnoStringDependencyPropertyAnalyzerTests
	{
		private const string Stub = @"
using System;

namespace Windows.UI.Xaml
{
	public sealed class DependencyProperty
	{
		public static DependencyProperty Register(string name, Type propertyType, Type ownerType, PropertyMetadata typeMetadata) => throw new System.NotImplementedException();
		internal static DependencyProperty Register(string name, Type propertyType, Type ownerType, FrameworkPropertyMetadata typeMetadata) => throw new System.NotImplementedException();
		public static DependencyProperty RegisterAttached(string name, Type propertyType, Type ownerType, PropertyMetadata typeMetadata) => throw new System.NotImplementedException();
		internal static DependencyProperty RegisterAttached(string name, Type propertyType, Type ownerType, FrameworkPropertyMetadata typeMetadata) => throw new System.NotImplementedException();
	}

	public class PropertyMetadata
	{
		public PropertyMetadata() { }
		public PropertyMetadata(object defaultValue) { }
	}

	public class FrameworkPropertyMetadata
	{
		public FrameworkPropertyMetadata() { }
		public FrameworkPropertyMetadata(object defaultValue) { }
	}
}
";
		private static async Task VerifyAsync(string code)
		{
			await new VerifyCS.Test
			{
				TestState =
				{
					Sources = { Stub, code },
				}
			}.RunAsync();
		}

		[TestMethod]
		public async Task When_No_Default_Value_Is_Given()
		{
			await VerifyAsync(@"
using Windows.UI.Xaml;

public class C
{
    public static DependencyProperty MyProperty { get; } = DependencyProperty.Register(string.Empty, typeof(string), typeof(C), [|new PropertyMetadata()|]);
	public static DependencyProperty MySecondProperty { get; } = DependencyProperty.RegisterAttached(string.Empty, typeof(string), typeof(C), [|new PropertyMetadata()|]);

    public static DependencyProperty MyThirdProperty { get; } = DependencyProperty.Register(string.Empty, typeof(string), typeof(C), [|new FrameworkPropertyMetadata()|]);
	public static DependencyProperty MyForthProperty { get; } = DependencyProperty.RegisterAttached(string.Empty, typeof(string), typeof(C), [|new FrameworkPropertyMetadata()|]);
}
");
		}

		[TestMethod]
		public async Task When_Null_Default_Value_Is_Given()
		{
			await VerifyAsync(@"
using Windows.UI.Xaml;

public class C
{
    public static DependencyProperty MyProperty { get; } = DependencyProperty.Register(string.Empty, typeof(string), typeof(C), [|new PropertyMetadata(defaultValue: null)|]);
	public static DependencyProperty MySecondProperty { get; } = DependencyProperty.RegisterAttached(string.Empty, typeof(string), typeof(C), [|new PropertyMetadata(defaultValue: null)|]);

    public static DependencyProperty MyThirdProperty { get; } = DependencyProperty.Register(string.Empty, typeof(string), typeof(C), [|new FrameworkPropertyMetadata(defaultValue: null)|]);
	public static DependencyProperty MyForthProperty { get; } = DependencyProperty.RegisterAttached(string.Empty, typeof(string), typeof(C), [|new FrameworkPropertyMetadata(defaultValue: null)|]);
}
");
		}

		[TestMethod]
		public async Task When_Constant_Default_Value_Is_Given()
		{
			await VerifyAsync(@"
using Windows.UI.Xaml;

public class C
{
    public static DependencyProperty MyProperty { get; } = DependencyProperty.Register(string.Empty, typeof(string), typeof(C), new PropertyMetadata(defaultValue: """"));
	public static DependencyProperty MySecondProperty { get; } = DependencyProperty.RegisterAttached(string.Empty, typeof(string), typeof(C), new PropertyMetadata(defaultValue: """"));

    public static DependencyProperty MyThirdProperty { get; } = DependencyProperty.Register(string.Empty, typeof(string), typeof(C), new FrameworkPropertyMetadata(defaultValue: """"));
	public static DependencyProperty MyForthProperty { get; } = DependencyProperty.RegisterAttached(string.Empty, typeof(string), typeof(C), new FrameworkPropertyMetadata(defaultValue: """"));
}
");
		}

		[TestMethod]
		public async Task When_Non_Constant_Default_Value_Is_Given()
		{
			await VerifyAsync(@"
using Windows.UI.Xaml;

public class C
{
    public static DependencyProperty MyProperty { get; } = DependencyProperty.Register(string.Empty, typeof(string), typeof(C), new PropertyMetadata(defaultValue: string.Empty));
	public static DependencyProperty MySecondProperty { get; } = DependencyProperty.RegisterAttached(string.Empty, typeof(string), typeof(C), new PropertyMetadata(defaultValue: string.Empty));

    public static DependencyProperty MyThirdProperty { get; } = DependencyProperty.Register(string.Empty, typeof(string), typeof(C), new FrameworkPropertyMetadata(defaultValue: string.Empty));
	public static DependencyProperty MyForthProperty { get; } = DependencyProperty.RegisterAttached(string.Empty, typeof(string), typeof(C), new FrameworkPropertyMetadata(defaultValue: string.Empty));
}
");
		}
	}
}
