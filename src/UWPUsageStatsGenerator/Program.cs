using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace UWPUsageStatsGenerator
{
	class Program
	{
		static void Main(string[] args)
		{
			var types = new List<string>();

			foreach (var file in Directory.GetFiles(@"C:\temp\UWP-Analysis", "*.txt"))
			{
				var content = File.ReadAllText(file);

				var m = Regex.Match(content, "warning Uno0001: (?<type>.*?) is not implemented in Uno", RegexOptions.Multiline);


				do
				{
					types.Add(m.Groups["type"].Value);

				} while ((m = m.NextMatch()).Success);
			}

			var results = from type in types
						  group type by type into groups
						  let count = groups.Count()
						  orderby groups.Key ascending
						  select new
						  {
							  Type = groups.Key,
							  Count = count
						  };

			using (var file = new StreamWriter("results.txt"))
			{
				foreach (var result in results)
				{
					file.WriteLine($"{result.Type};{result.Count}");
				}
			}
		}
	}
}
