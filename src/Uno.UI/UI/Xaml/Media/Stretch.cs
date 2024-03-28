using System;
using System.Collections.Generic;
using System.Text;

namespace Windows.UI.Xaml.Media
{
	/// <summary>Describes how content is resized to fill its allocated space.</summary>
	public enum Stretch
	{
		/// <summary>The content preserves its original size.</summary>
		None,
		/// <summary>The content is resized to fill the destination dimensions. The aspect ratio is not preserved.</summary>
		Fill,
		/// <summary>The content is resized to fit in the destination dimensions while it preserves its native aspect ratio.</summary>
		Uniform,
		/// <summary>
		/// The content is resized to fill the destination dimensions while it preserves its native aspect ratio. 
		/// If the aspect ratio of the destination rectangle differs from the source, the source 
		/// content is clipped to fit in the destination dimensions.
		/// </summary>
		UniformToFill
	}
}
