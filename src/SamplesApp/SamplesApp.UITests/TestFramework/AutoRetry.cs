using System;
using System.Drawing;
using NUnit.Framework;
using NUnit.Framework.Interfaces;
using NUnit.Framework.Internal;
using NUnit.Framework.Internal.Commands;

namespace SamplesApp.UITests.TestFramework
{
	/// <summary>
	/// Specifies that a test method should be rerun on failure up to the specified 
	/// maximum number of times.
	/// </summary>
	[AttributeUsage(AttributeTargets.Method | AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
	public class AutoRetryAttribute : RetryAttribute
	{

		/// <summary>
		/// Construct a <see cref="RetryAttribute" />
		/// </summary>
		/// <param name="tryCount">The maximum number of times the test should be run if it fails</param>
		public AutoRetryAttribute(int tryCount = 3) : base(tryCount)
		{
		}
	}
}
