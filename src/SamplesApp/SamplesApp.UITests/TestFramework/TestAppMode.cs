using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Uno.UITest.Helpers.Queries;

namespace SamplesApp.UITests.TestFramework
{
	[AttributeUsage(AttributeTargets.Class, AllowMultiple=true, Inherited = true)]
	public class TestAppModeAttribute : Attribute
	{
		/// <summary>
		/// Builds TestMode attribute
		/// </summary>
		/// <param name="cleanEnvironment">
		/// Determines if the app should be restarted to get a clean environment before the fixture tests are started.
		/// </param>
		/// <param name="platform">Determines the target platform to be used for this attribute</param>
		public TestAppModeAttribute(bool cleanEnvironment, Platform platform)
		{
			CleanEnvironment = cleanEnvironment;
			Platform = platform;
		}

		/// <summary>
		/// Determines if the app should be restarted to get a clean environment before the fixture tests are started.
		/// </summary>
		public bool CleanEnvironment { get; }

		/// <summary>
		/// Determines the target platform to be used for this attribute
		/// </summary>
		public Platform Platform { get; }
	}
}
