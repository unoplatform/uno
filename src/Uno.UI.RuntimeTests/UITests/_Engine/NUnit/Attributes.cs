using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Text;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace NUnit.Framework;

internal class TestFixtureAttribute : TestClassAttribute
{
}

internal class TestAttribute : TestMethodAttribute
{
	public TestAttribute([CallerFilePath] string callerFilePath = "", [CallerLineNumber] int callerLineNumber = -1)
		: base(callerFilePath, callerLineNumber)
	{
	}
}

internal class PropertyAttribute : Attribute
{
	public Dictionary<string, IList> Properties { get; } = new();
}
