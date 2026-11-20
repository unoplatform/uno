#nullable enable

using System.Collections.Generic;
using System.Reflection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using OpenQA.Selenium;
using SamplesApp.AppiumTests.Infrastructure;

namespace SamplesApp.AppiumTests.Tests;

[TestClass]
[TestCategory(TestCategories.HostIndependent)]
public sealed class WasmAdapterPatternTests
{
	[TestMethod]
	[DataRow("listbox", new[] { "selection" })]
	[DataRow("combobox", new[] { "expandcollapse", "selection" })]
	[DataRow("list", new string[0])]
	public void GetSupportedPatterns_Maps_Selection_Containers(string role, string[] expected)
	{
		var element = DispatchProxy.Create<IWebElement, ElementProxy>();
		((ElementProxy)(object)element).Attributes["role"] = role;

		new WasmAdapter().GetSupportedPatterns(element).Should().Equal(expected);
	}

	public class ElementProxy : DispatchProxy
	{
		public Dictionary<string, string?> Attributes { get; } = new();

		protected override object? Invoke(MethodInfo? targetMethod, object?[]? args)
			=> targetMethod?.Name == nameof(IWebElement.GetAttribute) && args?[0] is string name
				? Attributes.GetValueOrDefault(name)
				: throw new AssertFailedException($"Unexpected call: {targetMethod?.Name}.");
	}
}
