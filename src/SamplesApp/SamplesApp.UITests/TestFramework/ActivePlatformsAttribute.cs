using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NUnit.Framework;
using Uno.UITest.Helpers.Queries;

namespace SamplesApp.UITests.TestFramework
{
	/// <summary>
	/// Defines a list of platforms for which the test will be executed. Other platforms will mark the test as ignored.
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

		public ActivePlatformsAttribute(params Platform[] platforms)
		{
			Properties.Add("ActivePlatforms", platforms);
		}
	}
}
