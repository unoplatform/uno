using System;
using System.Collections.Generic;
using System.Text;

namespace Uno.UI.RemoteControl
{
	[System.AttributeUsage(AttributeTargets.Assembly, Inherited = false, AllowMultiple = true)]
	public sealed class ServerProcessorsConfigurationAttribute : Attribute
	{
		public ServerProcessorsConfigurationAttribute(string processorsPath)
		{
			ProcessorsPath = processorsPath;
		}

		public string ProcessorsPath { get; }
	}
}
