using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using NUnit.Framework;
using Uno.UITest.Helpers.Queries;

namespace SamplesApp.UITests.TestFramework
{
	/// <summary>
	/// Defines a list of platforms for which the test will be executed. Other platforms will mark the test as ignored.
	/// WARNING:
	/// This is supported only on UI tests, not for runtime tests.
	/// It's available for runtime tests only to ease port of UI tests to runtime test.
	/// </summary>
	[AttributeUsage(AttributeTargets.Method | AttributeTargets.Class, AllowMultiple = false)]
	internal class ActivePlatformsAttribute : PropertyAttribute
	{
		public Platform[] Platforms
		{
			get
			{
				var property = Properties["ActivePlatforms"] as IList<object>;
				return property?.FirstOrDefault() as Platform[];
			}
		}

		public Exception Error { get; }

		public ActivePlatformsAttribute(Platform platform, [CallerArgumentExpression("platform")] string expression = "")
		{
			// Errors out platform shouldn't be thrown here.
			// We must assign to an Error property and throw in SampleControlUITestBase.BeforeEachTest (specifically, the GetActivePlatforms call).
			// Otherwise, NUnit skips the test completely when it tries to load the attributes.
			if (expression.Length == 0)
			{
				Error = new NotSupportedException("'CallerArgumentExpression' is not supported?");
			}

			if (expression.Contains('|'))
			{
				// See https://github.com/unoplatform/uno/issues/9798 for info.
				Error = new ArgumentException("Don't use bitwise or in ActivePlatforms attribute.");
			}

			Properties.Add("ActivePlatforms", new[] { platform });
		}

		public ActivePlatformsAttribute(params Platform[] platforms)
		{
			Properties.Add("ActivePlatforms", platforms);
		}
	}
}
