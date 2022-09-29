#nullable disable

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Text;

namespace Uno
{
	public static partial class WinRTFeatureConfiguration
	{
		public static class ArrayPool
		{
			/// <summary>The default maximum length of each array in the pool</summary>
			/// <remarks>
			/// WARNING: Altering this setting may degrade the performance of the application significantly.
			/// This value is captured automatically after App(), any subsequent change is ignored.
			/// </remarks>
			public static int DefaultMaxArrayLength { get; set; } = 1024 * 1024;

			/// <summary>The default maximum number of arrays per bucket that are available for rent when using automatic memory management.</summary>
			/// <remarks>
			/// WARNING: Altering this setting may degrade the performance of the application significantly.
			/// This value is captured automatically after App(), any subsequent change is ignored.
			/// </remarks>
			public static int DefaultAutomaticMaxNumberOfArraysPerBucket { get; set; } = 8192;

			/// <summary>The default maximum number of arrays per bucket that are available for rent when automatic memory management is disabled.</summary>
			/// <remarks>
			/// WARNING: Altering this setting may degrade the performance of the application significantly.
			/// This value is captured automatically after App(), any subsequent change is ignored.
			/// </remarks>
			public static int DefaultMaxNumberOfArraysPerBucket { get; set; } = 100;

			/// <summary>Determines if automatic memory management is used for the ArrayPool.</summary>
			/// <remarks>
			/// WARNING: Altering this setting may degrade the performance of the application significantly.
			/// This value is captured automatically after App(), any subsequent change is ignored.
			///
			/// Automatic memory management uses <see cref="Windows.System.MemoryManager"/> to determine the acceptable
			/// memory consumption for pools, and uses GC triggers to reduce the pressure.
			/// </remarks>
			public static bool EnableAutomaticMemoryManagement { get; set; } = true;
		}
	}
}
