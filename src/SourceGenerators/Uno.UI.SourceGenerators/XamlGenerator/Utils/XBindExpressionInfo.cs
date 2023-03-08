#nullable enable

using System;
using System.Collections.Generic;
using System.Text;

namespace Uno.UI.SourceGenerators.XamlGenerator.Utils
{
	internal sealed class XBindExpressionInfo
	{
		private readonly int _xBindCounter;
		private readonly string _contextType;
		private readonly string _contextName;

		public XBindExpressionInfo(int xBindCounter, string contextType, string contextName)
		{
			_xBindCounter = xBindCounter;
			_contextType = contextType;
			_contextName = contextName;
		}

		/// <summary>
		/// For an x:Bind that is semantically equivalent to `A.B?.C?.D.E`,
		/// InstanceSubexpression1 is A.B?.C?.D (ie, stop on last null access).
		/// This will be the whole expression if there are no nulls involved in the expression.
		/// </summary>
		/// <remarks>
		/// InstanceSubexpression1 can include method names for invocations.
		/// </remarks>
		public string InstanceSubexpression1 { get; set; } = null!;

		/// <summary>
		/// For an x:Bind that is semantically equivalent to `A.B?.C?.D.E`,
		/// InstanceSubexpression2 will be `E`.
		/// This property will be null if there are no nulls involved in the expression.
		/// </summary>
		/// <remarks>
		/// InstanceSubexpression2 can include method names for invocations.
		/// </remarks>
		public string? InstanceSubexpression2 { get; set; }

		/// <summary>
		/// If the x:Bind expression represents an invocation, this will represent the arguments.
		/// For a parameterless call, this will be an empty list.
		/// If this instance doesn't represent an invocation, this will be null.
		/// </summary>
		public List<XBindExpressionInfo>? Arguments { get; set; }

		public override string ToString()
		{
			// Consider using pool when https://github.com/unoplatform/uno/pull/11531 is merged.
			var builder = new StringBuilder();
			builder.AppendLine($"private bool TryGetInstance_xBind_{_xBindCounter}({_contextType} {_contextName}, out object o)");
			builder.AppendLine("{");
			builder.AppendLine("	o = null;");

			if (InstanceSubexpression2 is not null)
			{
				builder.AppendLine($"	var sub1 = {InstanceSubexpression1};");
				builder.AppendLine("	if (sub1 == null) return false;");
			}

			if (Arguments is null)
			{
				if (InstanceSubexpression2 is null)
				{
					builder.AppendLine($"	o = {InstanceSubexpression1};");
				}
				else
				{
					builder.AppendLine($"	o = sub1.{InstanceSubexpression2};");
				}

				builder.AppendLine("	return true;");
			}
			else
			{
				for (int i = 0; i < Arguments.Count; i++)
				{
					var arg = Arguments[i];
					if (arg.InstanceSubexpression2 is null)
					{
						if (!arg.InstanceSubexpression1.Equals("null", StringComparison.Ordinal))
						{
							builder.AppendLine($"	var arg{i} = {arg.InstanceSubexpression1};");
						}
					}
					else
					{
						builder.AppendLine($"	var arg{i}_sub = {arg.InstanceSubexpression1};");
						builder.AppendLine($"	if (arg{i}_sub == null) return false;");
						builder.AppendLine($"	var arg{i} = arg{i}_sub.{arg.InstanceSubexpression2};");
					}
				}

				if (InstanceSubexpression2 is null)
				{
					builder.Append($"	o = {InstanceSubexpression1}(");
				}
				else
				{
					builder.Append($"	o = sub1.{InstanceSubexpression2}(");
				}

				for (int i = 0; i < Arguments.Count; i++)
				{
					if (Arguments[i].InstanceSubexpression1.Equals("null", StringComparison.Ordinal) && Arguments[i].InstanceSubexpression2 is null)
					{
						builder.Append("null");
					}
					else
					{
						builder.Append($"arg{i}");
					}

					if (i < Arguments.Count - 1)
					{
						builder.Append(", ");
					}
				}

				builder.AppendLine(");");

				builder.AppendLine("	return true;");
			}

			builder.AppendLine("}");
			return builder.ToString();
		}
	}
}
