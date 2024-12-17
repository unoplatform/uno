using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Windows.UI.Xaml.Data
{
	/// <summary>
	/// Defines a bindable class, used as a marker to generate type metadata at compilation time.
	/// </summary>
	[AttributeUsage(AttributeTargets.Class, Inherited = true, AllowMultiple = false)]
	public sealed partial class BindableAttribute : Attribute
	{
	}

	/// <summary>
	/// Defines non-bindable property, used as a marker to avoid generating type metadata at compilation time.
	/// </summary>
	[AttributeUsage(AttributeTargets.Property, Inherited = true, AllowMultiple = false)]
	public sealed class NonBindableAttribute : Attribute
	{
	}
}
