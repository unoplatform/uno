using Microsoft.CodeAnalysis.Testing;
using Microsoft.CodeAnalysis.CSharp.Testing;
using Uno.UI.SourceGenerators.DependencyObject;
using Microsoft.CodeAnalysis.Testing.Verifiers;
using System.Collections.Immutable;

namespace Uno.UI.SourceGenerators.Tests.DependencyObjectGeneratorTests;

[TestClass]
public class Given_DependenyObjectGenerator
{
	private async Task TestAndroid(string testCode, params DiagnosticResult[] expectedDiagnostics)
	{
		var test = new CSharpSourceGeneratorTest<DependencyObjectGenerator, MSTestVerifier>()
		{
			TestState =
			{
				Sources = { testCode },
			},
			ReferenceAssemblies = ReferenceAssemblies.Net.Net60Android.AddPackages(ImmutableArray.Create(new PackageIdentity("Uno.UI", "4.4.20"))),
		};
		test.ExpectedDiagnostics.AddRange(expectedDiagnostics);
		await test.RunAsync();
	}

	[TestMethod]
	public async Task TestAndroidViewImplementingDependencyObject()
	{
		await TestAndroid(@"
using Android.Content;
using Windows.UI.Core;
using Windows.UI.Xaml;

public class C : Android.Views.View, DependencyObject
{
	public C(Context context) : base(context)
	{
	}

	public CoreDispatcher Dispatcher { get; }
	public object GetValue(DependencyProperty dp) => null;
	public void SetValue(DependencyProperty dp, object value) { }
	public void ClearValue(DependencyProperty dp) { }
	public object ReadLocalValue(DependencyProperty dp) => null;
	public object GetAnimationBaseValue(DependencyProperty dp) => null;
	public long RegisterPropertyChangedCallback(DependencyProperty dp, DependencyPropertyChangedCallback callback) => 0;
	public void UnregisterPropertyChangedCallback(DependencyProperty dp, long token) { }
}",
		// /0/Test0.cs(6,14): error Uno0003: 'Android.Views.View' shouldn't implement 'DependencyObject'. Inherit 'FrameworkElement' instead.
		DiagnosticResult.CompilerError("Uno0003").WithSpan(6, 14, 6, 15).WithArguments("Android.Views.View"));
	}
}
