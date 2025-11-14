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

	// LEGACY - to be removed when all error generation sites have been migrated to use XamlGenerationException
	private void GenerateError(IIndentedStringBuilder writer, string message)
	{
		GenerateError(writer, message.Replace("{", "{{").Replace("}", "}}"), Array.Empty<object>());
	}

	private void GenerateError(IIndentedStringBuilder writer, string message, params object?[] options)
	{
		TryAnnotateWithGeneratorSource(writer);
		if (ShouldWriteErrorOnInvalidXaml)
		{
			// it's important to add a new line to make sure #error is on its own line.
			writer.AppendLineIndented(string.Empty);
			writer.AppendLineInvariantIndented("#error " + message, options);
		}
		else
		{
			GenerateSilentWarning(writer, message, options);
		}
	}

	private void GenerateSilentWarning(IIndentedStringBuilder writer, string message, params object?[] options)
	{
		TryAnnotateWithGeneratorSource(writer);
		// it's important to add a new line to make sure #error is on its own line.
		writer.AppendLineIndented(string.Empty);
		writer.AppendLineInvariantIndented("// WARNING " + message, options);
	}
}
