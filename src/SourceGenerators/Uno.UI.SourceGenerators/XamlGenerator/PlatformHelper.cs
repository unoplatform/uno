using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Uno.SourceGeneration;

namespace Uno.UI.SourceGenerators.XamlGenerator
{
	public class PlatformHelper
	{
		public static bool IsValidPlatform(SourceGeneratorContext context)
		{
			string evaluatedValue = context
				.GetProjectInstance()
				.GetProperty("TargetPlatformIdentifier")
				?.EvaluatedValue ?? "";

			return !evaluatedValue.Equals("UAP", StringComparison.OrdinalIgnoreCase);
		}
	}
}
