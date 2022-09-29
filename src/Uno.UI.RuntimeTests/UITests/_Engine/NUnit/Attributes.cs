#nullable disable

using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace NUnit.Framework;

internal class TestFixtureAttribute : TestClassAttribute
{
}

internal class TestAttribute : TestMethodAttribute
{
}

internal class PropertyAttribute : Attribute
{
	public Dictionary<string, IList> Properties { get; } = new();
}
