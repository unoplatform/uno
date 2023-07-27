using System;
using System.Collections.Generic;
using System.Text;

namespace Uno.UI.RemoteControl
{
	[System.AttributeUsage(AttributeTargets.Assembly, Inherited = false, AllowMultiple = false)]
	public sealed class ProjectConfigurationAttribute : Attribute
	{
		public ProjectConfigurationAttribute(string projectPath, string[] xamlPaths, string[] msbuildProperties)
		{
			ProjectPath = projectPath;
			XamlPaths = xamlPaths;
			MSBuildProperties = msbuildProperties;
		}

		public string ProjectPath { get; }

		public string[] MSBuildProperties { get; }

		public string[] XamlPaths { get; }
	}
}
