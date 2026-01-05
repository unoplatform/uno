using System;

namespace Uno.UI.Runtime.WebAssembly
{
	/// <summary>
	/// Defines the HTML element tag to use for the WebAssembly + HTML target.
	/// </summary>
	[System.AttributeUsage(AttributeTargets.Class, Inherited = true, AllowMultiple = false)]
	public sealed class HtmlElementAttribute : Attribute
	{
		/// <summary>
		/// Initializes a new instance of the <see cref="HtmlElementAttribute"/> class.
		/// </summary>
		/// <param name="tag">The HTML tag name associated with the element.</param>
		/// <exception cref="ArgumentException">
		/// Thrown when <paramref name="tag"/> is null or empty.
		/// </exception>
		public HtmlElementAttribute(string tag)
		{
			Tag = tag;
		}

		/// <summary>
		/// Gets the HTML tag name associated with the element.
		/// </summary>
		public string Tag { get; }
	}
}