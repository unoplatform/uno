#nullable enable

// Mostly based on https://github.com/dotnet/runtime/blob/907eff84ef204a2d71c10e7cd726b76951b051bd/src/libraries/System.Private.CoreLib/src/System/Diagnostics/Debug.cs

// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;

namespace System.Diagnostics;

/// <summary>
/// Provides a set of properties and methods for debugging code.
/// </summary>
/// <remarks>
/// This was modified for Uno to be a "CI assert" rather than a "Debug assert".
/// In CI, we build in Release for performance reasons.
/// Still, we want to catch assertion failures.
/// Also, we keep this available on Debug.
/// </remarks>
internal static partial class CI
{
	private sealed class CIAssertException : Exception
	{
		internal CIAssertException(string? message, string? detailMessage) :
			base(message + Environment.NewLine + detailMessage)
		{
		}
	}

	[Conditional("IS_CI_OR_DEBUG")]
	public static void Assert([DoesNotReturnIf(false)] bool condition) =>
		Assert(condition, string.Empty, string.Empty);

	[Conditional("IS_CI_OR_DEBUG")]
	public static void Assert([DoesNotReturnIf(false)] bool condition, string? message) =>
		Assert(condition, message, string.Empty);

	[Conditional("IS_CI_OR_DEBUG")]
	public static void Assert([DoesNotReturnIf(false)] bool condition, [InterpolatedStringHandlerArgument(nameof(condition))] ref AssertInterpolatedStringHandler message) =>
		Assert(condition, message.ToStringAndClear());

	[Conditional("IS_CI_OR_DEBUG")]
	public static void Assert([DoesNotReturnIf(false)] bool condition, string? message, string? detailMessage)
	{
		if (!condition)
		{
			Fail(message, detailMessage);
		}
	}

	[Conditional("IS_CI_OR_DEBUG")]
	public static void Assert([DoesNotReturnIf(false)] bool condition, [InterpolatedStringHandlerArgument(nameof(condition))] ref AssertInterpolatedStringHandler message, [InterpolatedStringHandlerArgument(nameof(condition))] ref AssertInterpolatedStringHandler detailMessage) =>
		Assert(condition, message.ToStringAndClear(), detailMessage.ToStringAndClear());

	[Conditional("IS_CI_OR_DEBUG")]
#pragma warning disable CA1305 // Specify IFormatProvider
	public static void Assert([DoesNotReturnIf(false)] bool condition, string? message, [StringSyntax(StringSyntaxAttribute.CompositeFormat)] string detailMessageFormat, params object?[] args) =>
		Assert(condition, message, string.Format(detailMessageFormat, args));
#pragma warning restore CA1305 // Specify IFormatProvider

	[Conditional("IS_CI_OR_DEBUG")]
	[DoesNotReturn]
	public static void Fail(string? message) =>
		Fail(message, string.Empty);

	[Conditional("IS_CI_OR_DEBUG")]
	[DoesNotReturn]
	[MethodImpl(MethodImplOptions.NoInlining)] // Preserve the frame for debugger
	public static void Fail(string? message, string? detailMessage) =>
		throw new CIAssertException(message, detailMessage);

	/// <summary>Provides an interpolated string handler for CI.Assert that only performs formatting if the assert fails.</summary>
	[EditorBrowsable(EditorBrowsableState.Never)]
	[InterpolatedStringHandler]
	public struct AssertInterpolatedStringHandler
	{
		/// <summary>The handler we use to perform the formatting.</summary>
		private StringBuilder.AppendInterpolatedStringHandler _stringBuilderHandler;
		private StringBuilder? _stringBuilder;

		/// <summary>Creates an instance of the handler..</summary>
		/// <param name="literalLength">The number of constant characters outside of interpolation expressions in the interpolated string.</param>
		/// <param name="formattedCount">The number of interpolation expressions in the interpolated string.</param>
		/// <param name="condition">The condition Boolean passed to the <see cref="CI"/> method.</param>
		/// <param name="shouldAppend">A value indicating whether formatting should proceed.</param>
		/// <remarks>This is intended to be called only by compiler-generated code. Arguments are not validated as they'd otherwise be for members intended to be used directly.</remarks>
		public AssertInterpolatedStringHandler(int literalLength, int formattedCount, bool condition, out bool shouldAppend)
		{
			if (condition)
			{
				_stringBuilderHandler = default;
				shouldAppend = false;
			}
			else
			{
				// Only used when failing an assert.  Additional allocation here doesn't matter; just create a new StringBuilder.
				_stringBuilder = new StringBuilder();
				_stringBuilderHandler = new StringBuilder.AppendInterpolatedStringHandler(literalLength, formattedCount, _stringBuilder);
				shouldAppend = true;
			}
		}

		/// <summary>Extracts the built string from the handler.</summary>
		internal string ToStringAndClear()
		{
			string s = _stringBuilder is StringBuilder sb ?
				sb.ToString() :
				string.Empty;
			_stringBuilderHandler = default;
			return s;
		}

		/// <summary>Writes the specified string to the handler.</summary>
		/// <param name="value">The string to write.</param>
		public void AppendLiteral(string value) => _stringBuilderHandler.AppendLiteral(value);

		/// <summary>Writes the specified value to the handler.</summary>
		/// <param name="value">The value to write.</param>
		/// <typeparam name="T">The type of the value to write.</typeparam>
		public void AppendFormatted<T>(T value) => _stringBuilderHandler.AppendFormatted(value);

		/// <summary>Writes the specified value to the handler.</summary>
		/// <param name="value">The value to write.</param>
		/// <param name="format">The format string.</param>
		/// <typeparam name="T">The type of the value to write.</typeparam>
		public void AppendFormatted<T>(T value, string? format) => _stringBuilderHandler.AppendFormatted(value, format);

		/// <summary>Writes the specified value to the handler.</summary>
		/// <param name="value">The value to write.</param>
		/// <param name="alignment">Minimum number of characters that should be written for this value.  If the value is negative, it indicates left-aligned and the required minimum is the absolute value.</param>
		/// <typeparam name="T">The type of the value to write.</typeparam>
		public void AppendFormatted<T>(T value, int alignment) => _stringBuilderHandler.AppendFormatted(value, alignment);

		/// <summary>Writes the specified value to the handler.</summary>
		/// <param name="value">The value to write.</param>
		/// <param name="format">The format string.</param>
		/// <param name="alignment">Minimum number of characters that should be written for this value.  If the value is negative, it indicates left-aligned and the required minimum is the absolute value.</param>
		/// <typeparam name="T">The type of the value to write.</typeparam>
		public void AppendFormatted<T>(T value, int alignment, string? format) => _stringBuilderHandler.AppendFormatted(value, alignment, format);

		/// <summary>Writes the specified character span to the handler.</summary>
		/// <param name="value">The span to write.</param>
		public void AppendFormatted(ReadOnlySpan<char> value) => _stringBuilderHandler.AppendFormatted(value);

		/// <summary>Writes the specified string of chars to the handler.</summary>
		/// <param name="value">The span to write.</param>
		/// <param name="alignment">Minimum number of characters that should be written for this value.  If the value is negative, it indicates left-aligned and the required minimum is the absolute value.</param>
		/// <param name="format">The format string.</param>
		public void AppendFormatted(ReadOnlySpan<char> value, int alignment = 0, string? format = null) => _stringBuilderHandler.AppendFormatted(value, alignment, format);

		/// <summary>Writes the specified value to the handler.</summary>
		/// <param name="value">The value to write.</param>
		public void AppendFormatted(string? value) => _stringBuilderHandler.AppendFormatted(value);

		/// <summary>Writes the specified value to the handler.</summary>
		/// <param name="value">The value to write.</param>
		/// <param name="alignment">Minimum number of characters that should be written for this value.  If the value is negative, it indicates left-aligned and the required minimum is the absolute value.</param>
		/// <param name="format">The format string.</param>
		public void AppendFormatted(string? value, int alignment = 0, string? format = null) => _stringBuilderHandler.AppendFormatted(value, alignment, format);

		/// <summary>Writes the specified value to the handler.</summary>
		/// <param name="value">The value to write.</param>
		/// <param name="alignment">Minimum number of characters that should be written for this value.  If the value is negative, it indicates left-aligned and the required minimum is the absolute value.</param>
		/// <param name="format">The format string.</param>
		public void AppendFormatted(object? value, int alignment = 0, string? format = null) => _stringBuilderHandler.AppendFormatted(value, alignment, format);
	}
}
