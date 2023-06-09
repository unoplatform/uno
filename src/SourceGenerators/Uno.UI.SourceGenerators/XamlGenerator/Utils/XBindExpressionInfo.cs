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
		/// ExpressionBeforeLastNullAccess is A.B?.C?.D (ie, stop on last null access).
		/// This will be the whole expression if there are no nulls involved in the expression.
		/// </summary>
		/// <remarks>
		/// ExpressionBeforeLastNullAccess can include method names for invocations.
		/// </remarks>
		public string ExpressionBeforeLastNullAccess { get; set; } = null!;

		/// <summary>
		/// For an x:Bind that is semantically equivalent to `A.B?.C?.D.E`,
		/// ExpressionAfterLastNullAccess will be `E`.
		/// This property will be null if there are no nulls involved in the expression.
		/// </summary>
		/// <remarks>
		/// ExpressionAfterLastNullAccess can include method names for invocations.
		/// </remarks>
		public string? ExpressionAfterLastNullAccess { get; set; }

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

			if (ExpressionAfterLastNullAccess is not null)
			{
				builder.AppendLine($"	var sub1 = {ExpressionBeforeLastNullAccess};");
				builder.AppendLine("	if (sub1 == null) return false;");
			}

			if (Arguments is null)
			{
				if (ExpressionAfterLastNullAccess is null)
				{
					builder.AppendLine($"	o = {ExpressionBeforeLastNullAccess};");
				}
				else
				{
					builder.AppendLine($"	o = sub1.{ExpressionAfterLastNullAccess};");
				}

				builder.AppendLine("	return true;");
			}
			else
			{
				for (int i = 0; i < Arguments.Count; i++)
				{
					var arg = Arguments[i];
					if (arg.ExpressionAfterLastNullAccess is null)
					{
						if (!arg.ExpressionBeforeLastNullAccess.Equals("null", StringComparison.Ordinal))
						{
							builder.AppendLine($"	var arg{i} = {arg.ExpressionBeforeLastNullAccess};");
						}
					}
					else
					{
						builder.AppendLine($"	var arg{i}_sub = {arg.ExpressionBeforeLastNullAccess};");
						builder.AppendLine($"	if (arg{i}_sub == null) return false;");
						builder.AppendLine($"	var arg{i} = arg{i}_sub.{arg.ExpressionAfterLastNullAccess};");
					}
				}

				if (ExpressionAfterLastNullAccess is null)
				{
					builder.Append($"	o = {ExpressionBeforeLastNullAccess}(");
				}
				else
				{
					builder.Append($"	o = sub1.{ExpressionAfterLastNullAccess}(");
				}

				for (int i = 0; i < Arguments.Count; i++)
				{
					if (Arguments[i].ExpressionBeforeLastNullAccess.Equals("null", StringComparison.Ordinal) && Arguments[i].ExpressionAfterLastNullAccess is null)
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
