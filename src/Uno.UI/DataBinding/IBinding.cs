#nullable disable

using System;

namespace Uno.Presentation
{
	public interface IBinding
	{
		/// <summary>
		/// An optional value converter
		/// </summary>
		object Converter { get; set; }

		/// <summary>
		/// The source data context
		/// </summary>
		object DataContext { get; set; }

		/// <summary>
		/// The source path in the current DataContext
		/// </summary>
		string Path { get; }

		/// <summary>
		/// The bound view property
		/// </summary>
		string TargetName { get; }

		/// <summary>
		/// Sets the source value in the data context
		/// </summary>
		/// <param name="value"></param>
		void SetSourceValue(object value);
	}
}
