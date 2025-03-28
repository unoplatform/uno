using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Windows.UI.Xaml
{
	public static class FrameworkPropertyMetadataOptionsExtensions
	{
		/// <summary>
		/// Determines if the specified options have the <see cref="FrameworkPropertyMetadataOptions.Inherits"/> set.
		/// </summary>
		/// <param name="options"></param>
		/// <returns></returns>
		public static bool HasInherits(this FrameworkPropertyMetadataOptions options)
			=> (options & FrameworkPropertyMetadataOptions.Inherits) != 0;

		/// <summary>
		/// Determines if the specified options have the <see cref="FrameworkPropertyMetadataOptions.ValueInheritsDataContext"/> set.
		/// </summary>
		/// <param name="options"></param>
		/// <returns></returns>
		public static bool HasValueInheritsDataContext(this FrameworkPropertyMetadataOptions options)
			=> (options & FrameworkPropertyMetadataOptions.ValueInheritsDataContext) != 0;

		/// <summary>
		/// Determines if the conversion of a set value to the type of a <see cref="DependencyProperty"/> should be performed.
		/// </summary>
		public static bool HasAutoConvert(this FrameworkPropertyMetadataOptions options)
			=> (options & FrameworkPropertyMetadataOptions.AutoConvert) != 0;

		/// <summary>
		/// Determines if the specified options have the <see cref="FrameworkPropertyMetadataOptions.ValueDoesNotInheritDataContext"/> set.
		/// </summary>
		/// <param name="options"></param>
		/// <returns></returns>
		public static bool HasValueDoesNotInheritDataContext(this FrameworkPropertyMetadataOptions options)
			=> (options & FrameworkPropertyMetadataOptions.ValueDoesNotInheritDataContext) != 0;

		/// <summary>
		/// Determines if the specified options have <see cref="FrameworkPropertyMetadataOptions.AffectsRender"/> set.
		/// </summary>
		public static bool HasAffectsRender(this FrameworkPropertyMetadataOptions options)
			=> (options & FrameworkPropertyMetadataOptions.AffectsRender) != 0;

		/// <summary>
		/// Determines if the specified options have <see cref="FrameworkPropertyMetadataOptions.AffectsArrange"/> set.
		/// </summary>
		public static bool HasAffectsArrange(this FrameworkPropertyMetadataOptions options)
			=> (options & FrameworkPropertyMetadataOptions.AffectsArrange) != 0;

		/// <summary>
		/// Determines if the specified options have <see cref="FrameworkPropertyMetadataOptions.AffectsMeasure"/> set.
		/// </summary>
		public static bool HasAffectsMeasure(this FrameworkPropertyMetadataOptions options)
			=> (options & FrameworkPropertyMetadataOptions.AffectsMeasure) != 0;

		/// <summary>
		/// Determines if the specified options have <see cref="FrameworkPropertyMetadataOptions.LogicalChild"/> set.
		/// </summary>
		public static bool HasLogicalChild(this FrameworkPropertyMetadataOptions options)
			=> (options & FrameworkPropertyMetadataOptions.LogicalChild) != 0;

		/// <summary>
		/// Determines if the specified options have <see cref="FrameworkPropertyMetadataOptions.WeakStorage"/> set.
		/// </summary>
		public static bool HasWeakStorage(this FrameworkPropertyMetadataOptions options)
			=> (options & FrameworkPropertyMetadataOptions.WeakStorage) != 0;

		/// <summary>
		/// Defines the default flags for a FrameworkPropertyMetadata if not explicitly opt-out (cf. <see cref="FrameworkPropertyMetadataOptions.Default"/>).
		/// </summary>
		internal static FrameworkPropertyMetadataOptions WithDefault(this FrameworkPropertyMetadataOptions options)
			=> (options & (FrameworkPropertyMetadataOptions.ValueDoesNotInheritDataContext | FrameworkPropertyMetadataOptions.ValueInheritsDataContext)) == default
				? options | FrameworkPropertyMetadataOptions.ValueInheritsDataContext // == FrameworkPropertyMetadataOptions.Default
				: options;
	}
}
