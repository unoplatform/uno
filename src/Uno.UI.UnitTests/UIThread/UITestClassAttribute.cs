#nullable enable

using System;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Uno.UI.Tests;

/// <summary>
/// A test class whose tests run on <see cref="UnitTestUIThread"/>, including their async continuations.
/// Use it instead of <see cref="TestClassAttribute"/>: Uno's UI code expects a single UI thread.
/// </summary>
[AttributeUsage(AttributeTargets.Class, Inherited = false)]
public sealed class UITestClassAttribute : TestClassAttribute
{
	public override TestMethodAttribute? GetTestMethodAttribute(TestMethodAttribute testMethodAttribute)
		=> base.GetTestMethodAttribute(testMethodAttribute) is { } attribute and not UIThreadTestMethodAttribute
			? new UIThreadTestMethodAttribute(attribute)
			: testMethodAttribute;

#pragma warning disable MSTEST0057 // Wraps an existing attribute, whose caller info it reuses.
	private sealed class UIThreadTestMethodAttribute(TestMethodAttribute inner)
		: TestMethodAttribute(inner.DeclaringFilePath, inner.DeclaringLineNumber ?? -1)
	{
		public override async Task<TestResult[]> ExecuteAsync(ITestMethod testMethod)
		{
			await UnitTestUIThread.SwitchTo();
			return await inner.ExecuteAsync(testMethod).ConfigureAwait(false);
		}
	}
#pragma warning restore MSTEST0057
}
