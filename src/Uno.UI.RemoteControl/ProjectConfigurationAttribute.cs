using System;
using System.Collections.Generic;
using System.Text;

namespace Uno.UI.RemoteControl
{
	[System.AttributeUsage(AttributeTargets.Assembly, Inherited = false, AllowMultiple = false)]
	public sealed class ProjectConfigurationAttribute : Attribute
	{
		public ProjectConfigurationAttribute(string projectPath, string[] xamlPaths)
		{
			ProjectPath = projectPath;
			XamlPaths = xamlPaths;
		}

		public string ProjectPath { get; }

		public string[] XamlPaths { get; }
	}
}
