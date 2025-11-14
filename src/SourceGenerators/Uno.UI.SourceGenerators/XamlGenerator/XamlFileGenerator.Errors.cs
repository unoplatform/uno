using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Uno.Extensions;

namespace Uno.UI.SourceGenerators.XamlGenerator;

internal partial class XamlFileGenerator
{
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
	private void SafeBuild<TArg>(Action<IIndentedStringBuilder, TArg> buildAction, IIndentedStringBuilder writer, TArg arg, [CallerArgumentExpression(nameof(buildAction))] string name = "")
		=> SafeBuild(w => buildAction(w, arg), writer, name);

#pragma warning disable IDE0051 // private member not used ... yet!
	/// <summary>
	/// Invokes a build block safely, capturing any exceptions as generation errors.
	/// </summary>
	private void SafeBuild<TArg1, TArg2>(Action<IIndentedStringBuilder, TArg1, TArg2> buildAction, IIndentedStringBuilder writer, TArg1 arg1, TArg2 arg2, [CallerArgumentExpression(nameof(buildAction))] string name = "")
		=> SafeBuild(w => buildAction(w, arg1, arg2), writer, name);
#pragma warning restore IDE0051

	/// <summary>
	/// Invokes a build block safely, capturing any exceptions as generation errors.
	/// </summary>
	private void SafeBuild(Action<IIndentedStringBuilder> buildAction, IIndentedStringBuilder writer, [CallerArgumentExpression(nameof(buildAction))] string name = "")
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
			_errors.Add(new XamlGenerationException($"Processing failed for an unknown reason ({name})", error, _fileDefinition));
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
