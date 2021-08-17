using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static System.Net.Mime.MediaTypeNames;

namespace ActionRunner
{
	internal static class GitHubLogger
	{
		public static void LogInformation(string text) => Console.WriteLine(text);

		public static void LogError(string text) => Console.WriteLine($"::error::{text}");

		public static void LogWarning(string text) => Console.WriteLine($"::warning::{text}");
	}
}
