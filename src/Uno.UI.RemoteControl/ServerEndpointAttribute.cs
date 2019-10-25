using System;
using System.Collections.Generic;
using System.Text;

namespace Uno.UI.RemoteControl
{
	[System.AttributeUsage(AttributeTargets.Assembly, Inherited = false, AllowMultiple = true)]
	public sealed class ServerEndpointAttribute : Attribute
	{
		public ServerEndpointAttribute(string endpoint, int port)
		{
			Endpoint = endpoint;
			Port = port;
		}

		public string Endpoint { get; }
		public int Port { get; }
	}
}
