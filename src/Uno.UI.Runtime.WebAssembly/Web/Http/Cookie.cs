#nullable enable

using System;

namespace Uno.Web.Http
{
	/// <summary>
	/// Contains basic cookie attributes.
	/// </summary>
	public class Cookie
	{
		/// <summary>
		/// Creates a cookie with given name and value.
		/// </summary>
		/// <param name="name">Name.</param>
		/// <param name="value">Value.</param>
		public Cookie(string name, string value)
		{
			Name = name ?? throw new ArgumentNullException(nameof(name));
			Value = value ?? throw new ArgumentNullException(nameof(value));
		}

		/// <summary>
		/// Gets the cookie name.
		/// </summary>
		public string Name { get; }

		/// <summary>
		/// Gets the cookie value.
		/// </summary>
		public string Value { get; }
	}
}
