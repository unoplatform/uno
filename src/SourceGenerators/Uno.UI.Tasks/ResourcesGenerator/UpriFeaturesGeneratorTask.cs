#nullable enable

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;

namespace Uno.UI.Tasks.ResourcesGenerator
{
	public class UpriFeaturesGeneratorTask_v0 : Task
	{
		public ITaskItem[]? Languages { get; set; }

		[Output]
		public ITaskItem[]? OutputFeatures { get; set; }

		public override bool Execute()
		{
			// Debugger.Launch();

			if (Languages != null && Languages.Length > 0)
			{
				// Get all cultures except Invariant and legacy Chinese (superseded by zh-Hans and zh-Hant)
				var allCultures =
					CultureInfo
						.GetCultures(CultureTypes.NeutralCultures | CultureTypes.SpecificCultures)
						.Where(c => c.IetfLanguageTag != string.Empty && c.Name != "zh-CHS" && c.Name != "zh-CHT")
						.ToDictionary(c => c.IetfLanguageTag, c => "false");

				foreach (var language in Languages)
				{
					var culture = new CultureInfo(language.ItemSpec);

					while (culture.IetfLanguageTag != string.Empty)
					{
						allCultures[culture.IetfLanguageTag] = "true";

						culture = culture.Parent;
					}
				}

				OutputFeatures =
					allCultures
						.Select(kvp => new TaskItem($"UPRI_{kvp.Key.Replace('-', '_').ToLowerInvariant()}", new Dictionary<string, string>() { ["Value"] = kvp.Value }))
						.ToArray();
			}
			else
			{
				OutputFeatures = Array.Empty<ITaskItem>();
			}

			return true;
		}
	}
}
