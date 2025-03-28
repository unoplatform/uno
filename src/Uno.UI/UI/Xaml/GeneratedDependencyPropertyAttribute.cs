#nullable enable

using System;
using System.Collections.Generic;
using System.Text;
using Windows.UI.Xaml;

namespace Uno.UI.Xaml
{
	/// <summary>
	/// Attribute to control the automatic generation of dependency property generation
	/// </summary>
	[System.AttributeUsage(AttributeTargets.Property, Inherited = false, AllowMultiple = false)]
	internal sealed class GeneratedDependencyPropertyAttribute : Attribute
	{
		// This is a positional argument
		public GeneratedDependencyPropertyAttribute()
		{
		}

		/// <summary>
		/// The set of <see cref="FrameworkPropertyMetadataOptions"/> options for the property
		/// </summary>
		public FrameworkPropertyMetadataOptions Options { get; set; }

		/// <summary>
		/// The DefaultValue to use for the property
		/// </summary>
		public object? DefaultValue { get; set; }

		/// <summary>
		/// Declares that the property must define a <see cref="CoerceValueCallback"/>.
		/// </summary>
		public bool CoerceCallback { get; set; }

		/// <summary>
		/// Declares that the property must define a <see cref="PropertyChangedCallback"/>.
		/// </summary>
		public bool ChangedCallback { get; set; }

		/// <summary>
		/// Declares that the property uses a local cache of the dependency property.
		/// </summary>
		public bool LocalCache { get; set; } = true;

		/// <summary>
		/// Declares that the dependency property is attached
		/// </summary>
		public bool Attached { get; set; }

		/// <summary>
		/// Declares that the dependency property is attached
		/// </summary>
		public Type? AttachedBackingFieldOwner { get; set; }

		/// <summary>
		/// Declares an optional PropertyChanged callback name
		/// </summary>
		public string? ChangedCallbackName { get; set; }
	}
}
