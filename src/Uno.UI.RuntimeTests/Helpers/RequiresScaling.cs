using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Uno.UI.RuntimeTests;

/// <summary>
/// Marks a test which sets its test UI to run with specific scaling.
/// </summary>
public class RequiresScalingAttribute : Attribute
{
	/// <summary>
	/// Creates a new instance of the RequiresScalingAttribute.
	/// </summary>
	/// <param name="scaling">Scaling in decimal format (100% = 1.0f).</param>
	public RequiresScalingAttribute(float scaling)
	{
		Scaling = scaling;
	}

	/// <summary>
	/// Gets the requested scaling.
	/// </summary>
	public float Scaling { get; }
}
