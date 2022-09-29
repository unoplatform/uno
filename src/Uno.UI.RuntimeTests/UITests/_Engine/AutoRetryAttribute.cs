#nullable disable

using System;
using System.Collections.Generic;
using System.Text;

namespace SamplesApp.UITests.TestFramework;

[AttributeUsage(AttributeTargets.Method | AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
internal class AutoRetryAttribute : Attribute
{
	public AutoRetryAttribute(int tryCount = 3)
	{
	}
}
