using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;
using Uno.Extensions;

namespace Uno.UI.SourceGenerators.XamlGenerator;

/*
 * About error handling in XamlFileGenerator:
 *
 *  *** We should raise/generate only XamlGenerationException from code generation logic. ***
 *
 * This allows to properly capture location information and context about the error and proper reporting to the user.
 * Other kind of exception should strictly reserve to internal logic errors that should never happen during normal operation (including with invalid XAML file).
 *
 * Messages:
 * ** Error messages are reported to the end-user. **
 * - They must not explain internal implementation details (e.g. Value of member is null),
 *   but rather focus on what the user did wrong and how to fix it (e.g. The 'TargetType' is not set on 'Style').
 *   (Reminder: errors are reported with location in the XAML file, prefer simple error message avoiding fullname/namespace of types for instance).
 * - Variables are expected to be surrounded by simple quotes (e.g. 'MyElement') to improve readability (_follows MS logic_).
 * - They must not end with a dot ('.'), as the dot is added when generating the error comment in the generated code (_follows MS logic_).
 *
 * When it's possible to continue to generate valid C# code, prefer to use AddError(), GenerateError() instead of throwing.
 * This allows user to get all errors of a XAML file in a single build instead of fixing them one by one.
 *
 * The Safely methods allows to properly handle errors in code generation blocks.
 * They should be used to wrap any code generation block that can raise exceptions.
 * Like for the AddError and GenerateError, they give ability to the user to get all errors in a single build by continuing generation of the end of the XAML file.
 *
 */


internal partial class XamlFileGenerator
{
	private static readonly Regex _safeMethodNameRegex = new(@"\(\) =\> (?<method>\w+)");

	/// <summary>
	/// List of all errors encountered during generation.
	/// </summary>
	/// <remarks>This is cleared at the beginning of the generation.</remarks>
	private readonly List<XamlGenerationException> _errors = new();

	public const bool DefaultShouldWriteErrorOnInvalidXaml = true;
	/// <summary>
	/// **For recoverable errors**, indicates if the code generation should report them (and break at compile time) or write a // Warning, which would be silent.
	/// </summary>
	/// <remarks>Initial behavior was to write // Warning, this is now defaulting to true.</remarks>
	public bool ShouldWriteErrorOnInvalidXaml { get; }

	/// <summary>
	/// Invokes a build block safely, capturing any exceptions as generation errors.
	/// </summary>
	private void Safely(Action<IIndentedStringBuilder> buildAction, IIndentedStringBuilder writer, [CallerArgumentExpression(nameof(buildAction))] string name = "", [CallerLineNumber] int line = -1)
	{
		try
		{
			buildAction(writer);
		}
		catch (XamlGenerationException xamlGenError)
		{
			_errors.Add(xamlGenError);
		}
		catch (Exception error) when (error is not OperationCanceledException)
		{
			_errors.Add(new XamlGenerationException($"Processing failed for an unknown reason ({name}@{line})", error, _fileDefinition));
		}
	}

	/// <summary>
	/// Invokes a build block safely, capturing any exceptions as generation errors.
	/// </summary>
	/// <remarks>The <paramref name="action"/> is expected to be a single method call. Avoid bodied delegates!</remarks>
	private void Safely(Action action, [CallerArgumentExpression(nameof(action))] string name = "", [CallerLineNumber] int line = -1)
	{
		Debug.Assert(_safeMethodNameRegex.IsMatch(name));

		try
		{
			action();
		}
		catch (XamlGenerationException xamlGenError)
		{
			_errors.Add(xamlGenError);
		}
		catch (Exception error) when (error is not OperationCanceledException)
		{
			var method = _safeMethodNameRegex.Match(name) is { Success: true } match
				? match.Groups["method"].Value
				: nameof(XamlFileGenerator);

			_errors.Add(new XamlGenerationException($"Processing failed for an unknown reason ({method}@{line})", error, _fileDefinition));
		}
	}

	/// <summary>
	/// Add an error to the generation error list.
	/// </summary>
	/// <remarks>
	/// Use this instead of throwing <see cref="XamlGenerationException"/> when you can safely continue code generation.
	/// </remarks>
	private void AddError(string message, IXamlLocation location)
	{
		_errors.Add(new XamlGenerationException(message, location));
	}

	/// <summary>
	/// Add an error to the generation error list and dump error message in place in the generated code for debug purposes.
	/// </summary>
	/// <remarks>
	/// Use this instead of throwing <see cref="XamlGenerationException"/> when you can safely continue code generation.
	/// </remarks>
	private void GenerateError(IIndentedStringBuilder writer, string message, IXamlLocation location)
	{
		if (ShouldWriteErrorOnInvalidXaml)
		{
			_errors.Add(new XamlGenerationException(message, location));
		}

		writer.AppendLineIndented(string.Empty); // Make sure to have comment on a dedicated line
		writer.AppendIndented($"// [ERROR] {location.FilePath}({location.LineNumber},{location.LinePosition}): ");
		writer.Append(message);
		writer.Append("."); // Error message are expected to **NOT** end with a dot.
		writer.AppendLine();
	}

	/// <summary>
	/// Dump error message in place in the generated code.
	/// Note: This DOES NOT add any error in the generation error list.
	/// </summary>
	/// <remarks>
	/// Use this instead of <see cref="GenerateError"/> when error can be ignored without causing trouble in final application.
	/// </remarks>
	private void GenerateSilentWarning(IIndentedStringBuilder writer, string message, IXamlLocation location)
	{
		writer.AppendLineIndented(string.Empty); // Make sure to have comment on a dedicated line
		writer.AppendIndented($"// [WARNING] {location.FilePath}({location.LineNumber},{location.LinePosition}): ");
		writer.Append(message);
		writer.Append("."); // Error message are expected to **NOT** end with a dot.
		writer.AppendLine();
	}
}
