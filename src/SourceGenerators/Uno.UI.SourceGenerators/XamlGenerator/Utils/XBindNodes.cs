using System.Text;

namespace Uno.UI.SourceGenerators.XamlGenerator.Utils;

/// <summary>
/// The base class for all XBind nodes.
/// </summary>
public abstract class XBindNode
{
	public string PrettyPrint()
	{
		var sb = new StringBuilder();
		PrettyPrint(sb, 0);
		return sb.ToString();
	}

	public abstract void PrettyPrint(StringBuilder sb, int indent);
}

/// <summary>
/// The root of the tree produced when parsing an x:Bind expression
/// </summary>
/// <remarks>
/// <c>xbind_root → xbind_invocation | xbind_path | '\0'</c>
/// </remarks>
public abstract class XBindRoot : XBindNode
{
}

/// <summary>
/// An invocation expression on the form xbind_path(xbind_path_arg1, xbind_path_arg2, ...)
/// </summary>
/// <remarks>
/// <c>xbind_invocation → xbind_path '(' (xbind_argument (',' xbind_argument)*)? ')'</c>
/// </remarks>
public sealed class XBindInvocation : XBindRoot
{
	public XBindPath Path { get; set; }
	public XBindArgument[] Arguments { get; set; }

	public override void PrettyPrint(StringBuilder sb, int indent)
	{
		sb.Append('\t', indent);
		sb.AppendLine("XBindInvocation");

		indent++;

		sb.Append('\t', indent);
		sb.AppendLine("Path:");
		Path.PrettyPrint(sb, indent + 1);

		sb.Append('\t', indent);
		sb.AppendLine("Arguments:");
		foreach (var arg in Arguments)
		{
			arg.PrettyPrint(sb, indent + 1);
		}
	}
}

/// <summary>
/// Represents an argument to <see cref="XBindInvocation"/>
/// </summary>
/// <remarks>
/// xbind_argument → xbind_path_argument | xbind_literal_argument
/// </remarks>
public abstract class XBindArgument : XBindNode
{
}

public sealed class XBindPathArgument : XBindArgument
{
	public XBindPath Path { get; set; }

	public override void PrettyPrint(StringBuilder sb, int indent)
	{
		sb.Append('\t', indent);
		sb.AppendLine("XBindPathArgument");

		indent++;

		sb.Append('\t', indent);
		sb.AppendLine("Path:");
		Path.PrettyPrint(sb, indent + 1);
	}
}


public sealed class XBindLiteralArgument : XBindArgument
{
	public string LiteralArgument { get; set; }

	public override void PrettyPrint(StringBuilder sb, int indent)
	{
		sb.Append('\t', indent);
		sb.AppendLine($"LiteralArgument: {LiteralArgument}");
	}
}


/// <summary>
/// The base class for different xbind_path subtypes.
/// </summary>
/// <remarks>
/// <c>xbind_path → xbind_member_access | xbind_indexer_access | xbind_identifier | xbind_attached_property_access | xbind_parenthesized_expression | xbind_cast</c>
/// </remarks>
public abstract class XBindPath : XBindRoot
{

}

/// <summary>
/// An XBind member access with '.'
/// </summary>
/// <remarks>
/// <c>xbind_member_access → xbind_path '.' xbind_identifier</c>
/// </remarks>
public sealed class XBindMemberAccess : XBindPath
{
	public XBindPath Path { get; set; }
	public XBindIdentifier Identifier { get; set; }

	public override void PrettyPrint(StringBuilder sb, int indent)
	{
		sb.Append('\t', indent);
		sb.AppendLine("XBindMemberAccess");

		indent++;

		sb.Append('\t', indent);
		sb.AppendLine("Path:");
		Path.PrettyPrint(sb, indent + 1);

		sb.Append('\t', indent);
		sb.AppendLine("Identifier:");
		Identifier.PrettyPrint(sb, indent + 1);
	}
}

/// <summary>
/// An XBind index access with '[ ]'
/// </summary>
/// <remarks>
/// <c>xbind_indexer_access → xbind_path '[' .+? ']'</c>
/// </remarks>
public sealed class XBindIndexerAccess : XBindPath
{
	public XBindPath Path { get; set; }
	public string Index { get; set; }

	public override void PrettyPrint(StringBuilder sb, int indent)
	{
		sb.Append('\t', indent);
		sb.AppendLine("XBindIndexerAccess");

		indent++;

		sb.Append('\t', indent);
		sb.AppendLine("Path:");
		Path.PrettyPrint(sb, indent + 1);

		sb.Append('\t', indent);
		sb.AppendLine($"Index: {Index}");
	}
}

/// <summary>
/// An XBind identifier. This consists of letters, digits, and underscores.
/// </summary>
public sealed class XBindIdentifier : XBindPath
{
	public string IdentifierText { get; set; }

	public override void PrettyPrint(StringBuilder sb, int indent)
	{
		sb.Append('\t', indent);
		sb.AppendLine("XBindIdentifier");

		indent++;

		sb.Append('\t', indent);
		sb.AppendLine($"IdentifierText: {IdentifierText}");
	}
}

/// <summary>
/// An XBind attached property access
/// </summary>
/// <remarks>
/// <c>xbind_attached_property_access → xbind_path.(xbind_path.xbind_identifier)</c>
/// </remarks>
public sealed class XBindAttachedPropertyAccess : XBindPath
{
	public XBindPath Member { get; set; }
	public XBindPath PropertyClass { get; set; }
	public XBindIdentifier PropertyName { get; set; }

	public override void PrettyPrint(StringBuilder sb, int indent)
	{
		sb.Append('\t', indent);
		sb.AppendLine("XBindAttachedPropertyAccess");

		indent++;

		sb.Append('\t', indent);
		sb.AppendLine("Member:");
		Member.PrettyPrint(sb, indent + 1);

		sb.Append('\t', indent);
		sb.AppendLine("PropertyClass:");
		PropertyClass.PrettyPrint(sb, indent + 1);

		sb.Append('\t', indent);
		sb.AppendLine("PropertyName:");
		PropertyName.PrettyPrint(sb, indent + 1);
	}
}

/// <summary>
/// An XBind parenthesized expression.
/// </summary>
/// <remarks>
/// <c>xbind_parenthesized_expression → '(' xbind_path ')'</c>
/// </remarks>
public sealed class XBindParenthesizedExpression : XBindPath
{
	public XBindPath Expression { get; set; }
	public bool IsPathlessCast { get; set; }

	public override void PrettyPrint(StringBuilder sb, int indent)
	{
		sb.Append('\t', indent);
		sb.AppendLine("XBindParenthesizedExpression");

		indent++;

		sb.Append('\t', indent);
		sb.AppendLine("Expression:");
		Expression.PrettyPrint(sb, indent + 1);

		sb.Append('\t', indent);
		sb.AppendLine($"IsPathlessCast: {IsPathlessCast}");
	}
}

/// <summary>
/// An XBind parenthesized expression.
/// </summary>
/// <remarks>
/// <c>xbind_parenthesized_expression → '(' xbind_path ')'</c>
/// </remarks>
public sealed class XBindCast : XBindPath
{
	public XBindPath Expression { get; set; }
	public XBindPath Type { get; set; }

	public override void PrettyPrint(StringBuilder sb, int indent)
	{
		sb.Append('\t', indent);
		sb.AppendLine("XBindCast");

		indent++;

		sb.Append('\t', indent);
		sb.AppendLine("Type:");
		Type.PrettyPrint(sb, indent + 1);

		sb.Append('\t', indent);
		sb.AppendLine("Expression:");
		Expression.PrettyPrint(sb, indent + 1);
	}
}
