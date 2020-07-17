using System;
using System.Collections.Generic;
using System.Text;

namespace Uno.UI.Runtime.Wasm
{
	/// <summary>
	/// Defines the HtmlElement properties to use for the WebAssembly+Html target.
	/// </summary>
	[System.AttributeUsage(AttributeTargets.Class, Inherited = true, AllowMultiple = false)]
	public sealed class HtmlElementAttribute : Attribute
	{
		public HtmlElementAttribute(string tag)
		{
			Tag = tag;
		}

		public string Tag { get; }
	}
}
