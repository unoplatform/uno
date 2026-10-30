#nullable enable

using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Uno.UI.SourceGenerators.DependencyObject;

/// <summary>
/// The named arguments of a <c>[GeneratedDependencyProperty]</c> application.
/// </summary>
internal sealed class AttributeArguments
{
	public const string OptionsName = "Options";
	public const string DefaultValueName = "DefaultValue";
	public const string CoerceCallbackName = "CoerceCallback";
	public const string ChangedCallbackName = "ChangedCallback";
	public const string LocalCacheName = "LocalCache";
	public const string AttachedBackingFieldOwnerName = "AttachedBackingFieldOwner";
	public const string ChangedCallbackNameName = "ChangedCallbackName";

	private readonly AttributeSyntax? _syntax;

	private AttributeArguments(AttributeSyntax? syntax)
	{
		_syntax = syntax;
	}

	public TypedConstant? Options { get; private set; }

	public TypedConstant? DefaultValue { get; private set; }

	public bool CoerceCallback { get; private set; }

	public bool ChangedCallback { get; private set; }

	/// <summary>
	/// The explicit LocalCache value, or <see langword="null"/> when it isn't set.
	/// </summary>
	public bool? LocalCache { get; private set; }

	public ITypeSymbol? AttachedBackingFieldOwner { get; private set; }

	public string? ChangedCallbackMethodName { get; private set; }

	public static AttributeArguments Read(AttributeData attribute, CancellationToken cancellationToken)
	{
		var arguments = new AttributeArguments(attribute.ApplicationSyntaxReference?.GetSyntax(cancellationToken) as AttributeSyntax);

		foreach (var argument in attribute.NamedArguments)
		{
			var value = argument.Value;
			switch (argument.Key)
			{
				case OptionsName:
					arguments.Options = value;
					break;
				case DefaultValueName:
					arguments.DefaultValue = value;
					break;
				case CoerceCallbackName:
					arguments.CoerceCallback = value.Value is true;
					break;
				case ChangedCallbackName:
					arguments.ChangedCallback = value.Value is true;
					break;
				case LocalCacheName when value.Value is bool localCache:
					arguments.LocalCache = localCache;
					break;
				case AttachedBackingFieldOwnerName:
					arguments.AttachedBackingFieldOwner = value.Value as ITypeSymbol;
					break;
				case ChangedCallbackNameName:
					arguments.ChangedCallbackMethodName = value.Value as string;
					break;
			}
		}

		return arguments;
	}

	public Location? GetLocation(string argumentName)
		=> FindArgument(argumentName)?.GetLocation();

	/// <summary>
	/// The source text of an argument value, for diagnostics.
	/// </summary>
	public string? GetExpressionText(string argumentName)
		=> FindArgument(argumentName)?.Expression.ToString();

	private AttributeArgumentSyntax? FindArgument(string argumentName)
	{
		if (_syntax?.ArgumentList is { } argumentList)
		{
			foreach (var argument in argumentList.Arguments)
			{
				if (argument.NameEquals?.Name.Identifier.ValueText == argumentName)
				{
					return argument;
				}
			}
		}

		return null;
	}
}
