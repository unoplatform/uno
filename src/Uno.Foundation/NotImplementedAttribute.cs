#nullable enable
using System;
using System.Collections.Generic;
using System.Text;

namespace Uno
{
	[System.AttributeUsage(AttributeTargets.All, Inherited = false, AllowMultiple = false)]
	public sealed class NotImplementedAttribute : Attribute
	{
		public NotImplementedAttribute() { }

		public NotImplementedAttribute(params string[] platforms)
		{
			Platforms = platforms;
		}

		public string[]? Platforms { get; }
	}
}
