using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NUnit.Framework;

namespace SamplesApp.UITests.TestFramework
{
	/// <summary>
	/// Tentative workaround for https://github.com/microsoft/appcenter/issues/723
	/// </summary>
	public class AutoRetryAttribute : RetryAttribute
	{
		public AutoRetryAttribute() : base(3)
		{
		}
	}
}
