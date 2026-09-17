#nullable enable

using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.UI.Xaml;

namespace Uno.UI.Xaml
{
	/// <summary>
	/// Generates a dependency property: put it on a partial property definition for an instance property, or on a
	/// static partial <c>Get{Name}</c> method definition for an attached property. The generator implements the partial
	/// members and declares the <c>{Name}Property</c> identifier.
	/// </summary>
	[System.AttributeUsage(AttributeTargets.Property | AttributeTargets.Method, Inherited = false, AllowMultiple = false)]
	internal sealed class GeneratedDependencyPropertyAttribute : Attribute
	{
		/// <summary>
		/// The <see cref="FrameworkPropertyMetadataOptions"/> of the property.
		/// </summary>
		public FrameworkPropertyMetadataOptions Options { get; set; }

		/// <summary>
		/// The default value of the property. When not set, a static parameterless <c>Get{Name}DefaultValue()</c>
		/// method provides it, or <c>default(T)</c> when there is none.
		/// </summary>
		public object? DefaultValue { get; set; }

		/// <summary>
		/// Requires a <c>Coerce{Name}</c> coerce callback. A method with that name is used even when this is not set.
		/// </summary>
		public bool CoerceCallback { get; set; }

		/// <summary>
		/// Requires an <c>On{Name}Changed</c> property changed callback. A method with that name is used even when this is not set.
		/// </summary>
		public bool ChangedCallback { get; set; }

		/// <summary>
		/// Whether the value is cached in a backing field, which is kept up to date by the property system.
		/// Attached properties are only cached when <see cref="AttachedBackingFieldOwner"/> is set.
		/// </summary>
		public bool LocalCache { get; set; } = true;

		/// <summary>
		/// The partial type that holds the local cache of an attached property; the cache is used for targets of that type.
		/// </summary>
		public Type? AttachedBackingFieldOwner { get; set; }

		/// <summary>
		/// The name of the property changed callback, when it isn't <c>On{Name}Changed</c>.
		/// </summary>
		public string? ChangedCallbackName { get; set; }
	}
}
