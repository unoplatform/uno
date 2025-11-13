#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AwesomeAssertions.Execution;

namespace SamplesApp.UITests
{
	public static class FluentExtensions
	{
		public static T Apply<T>(this T target, Action<T> action)
		{
			action?.Invoke(target);

			return target;
		}

		public static T ApplyIf<T>(this T target, bool condition, Action<T>? action)
		{
			if (condition)
			{
				action?.Invoke(target);
			}

			return target;
		}

		public static void FailWithText(this AssertionChain scope, string text)
		{
			var t = text.Replace("{", "{{").Replace("}", "}}");

			scope.FailWith(t);
		}
	}
}
