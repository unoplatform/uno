using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Uno.UI.Samples.Controls
{

	[AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = false)]
	public class SampleAttribute : Attribute
	{
		/// <summary>
		/// Marks a class as a sample test control that can be browsed by the SampleChooserControl
		/// and which can be used by automated tests.
		/// </summary>
		public SampleAttribute()
		{
		}

		/// <summary>
		/// Marks a class as a sample test control that can be browsed by the SampleChooserControl
		/// and which can be used by automated tests.
		/// </summary>
		/// <param name="categories">An optional list of categories to which this sample is related to</param>
		public SampleAttribute(params string[] categories)
		{
			Categories = categories;
		}

		/// <summary>
		/// Marks a class as a sample test control that can be browsed by the SampleChooserControl
		/// and which can be used by automated tests.
		/// </summary>
		/// <param name="categories">An optional list of categories to which this sample is related to</param>
		public SampleAttribute(params Type[] categories)
		{
			Categories = categories.Select(type => type.Name).ToArray();
		}

		/// <summary>
		/// An optional list of categories to which this sample is related to
		/// </summary>
		public string[] Categories { get; }

		/// <summary>
		/// An optional name for this sample. Default will be the name of the class.
		/// </summary>
		public string Name { get; set; }

		/// <summary>
		/// Determines if this sample should be included or not in the automated screenshot comparison.
		/// If this flag is not set, the sample will be included.
		/// </summary>
		public bool IgnoreInSnapshotTests { get; set; }

		/// <summary>
		/// Determines if this test should be manually tested (e.g. animations, external/untestable dependencies)
		/// </summary>
		public bool IsManualTest { get; set; }

		/// <summary>
		/// An optional ViewModel type that will be instantiated and set as DataContext of the sample control
		/// </summary>
		public Type ViewModelType { get; set; }

		/// <summary>
		/// An optional description of the sample. A good practice is to explain the expected result of the sample.
		/// </summary>
		public string Description { get; set; }

		/// <summary>
		/// Set to true if the sample is navigated to using frame navigation.
		/// </summary>
		/// <remarks>Defaults to true.</remarks>
		public bool UsesFrame { get; set; } = true;
	}
}
