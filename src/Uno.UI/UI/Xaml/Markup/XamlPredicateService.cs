// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// MUX Reference XamlPredicateService.cpp, commit 9d6fb15c0

#nullable enable

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;

namespace Microsoft.UI.Xaml.Markup
{
	/// <summary>
	/// Caches and dispatches XAML conditional namespace predicate evaluations.
	/// </summary>
	/// <remarks>
	/// Mirrors the responsibilities of WinUI's internal <c>XamlPredicateService</c>
	/// singleton. Both the built-in predicates (<c>IsApiContractPresent</c> et al.)
	/// and user-supplied <see cref="IXamlCondition"/> implementations are evaluated
	/// at most once per (type, arguments) pair for the lifetime of the process,
	/// matching the WinUI behavior documented for parse-time conditional XAML.
	/// </remarks>
	internal static class XamlPredicateService
	{
		// Format constants mirror those defined in XamlPredicateService.cpp:
		//     CONDITIONAL_PREDICATE_DELIMITER       L'?'
		//     CONDITIONAL_PREDICATE_ARGS_BEGIN      L'('
		//     CONDITIONAL_PREDICATE_ARGS_END        L')'
		//     CONDITIONAL_PREDICATE_ARG_DELIMITER   L','
		internal const char ConditionalPredicateDelimiter = '?';
		internal const char ConditionalPredicateArgsBegin = '(';
		internal const char ConditionalPredicateArgsEnd = ')';
		internal const char ConditionalPredicateArgDelimiter = ',';

		// The C++ implementation keys the cache on (XamlType token, args string) pairs
		// so different types with the same logical name still get isolated entries.
		// In C# the System.Type identity already provides that isolation.
		private static readonly ConcurrentDictionary<(Type Type, string Arguments), bool> _evaluationCache
			= new ConcurrentDictionary<(Type Type, string Arguments), bool>();

		/// <summary>
		/// Splits a comma-delimited predicate argument list into its components.
		/// </summary>
		/// <remarks>
		/// Mirrors <c>SplitArgumentsString</c> from XamlPredicateService.cpp. Used by
		/// the legacy <c>IXamlPredicate</c>-style built-in predicates which accept
		/// multiple arguments.
		/// </remarks>
		internal static IReadOnlyList<string> SplitArgumentsString(string args)
		{
			if (string.IsNullOrEmpty(args))
			{
				return Array.Empty<string>();
			}

			return args.Split(ConditionalPredicateArgDelimiter);
		}

		/// <summary>
		/// Splits a conditional XML namespace string of the form
		/// <c>&lt;baseXmlns&gt;?&lt;predicate&gt;</c>.
		/// </summary>
		/// <remarks>
		/// Mirrors <c>XamlPredicateService::CrackConditionalXmlns</c>.
		/// </remarks>
		internal static void CrackConditionalXmlns(
			string xmlns,
			out string baseXmlns,
			out string conditionalPredicateSubstring)
		{
			// The format for a conditional XML namespace string is '<xmlns string>?<conditional predicate>'
			var delimiterIndex = xmlns.IndexOf(ConditionalPredicateDelimiter);
			if (delimiterIndex >= 0)
			{
				baseXmlns = xmlns.Substring(0, delimiterIndex);
				conditionalPredicateSubstring = xmlns.Substring(delimiterIndex + 1);
			}
			else
			{
				baseXmlns = xmlns;
				conditionalPredicateSubstring = string.Empty;
			}
		}

		/// <summary>
		/// Splits a conditional predicate substring of the form
		/// <c>TypeName(arg1,arg2,...)</c> into its type name and argument list.
		/// </summary>
		/// <remarks>
		/// Mirrors <c>XamlPredicateService::CrackConditionalPredicate</c>, except
		/// type resolution is deferred to the caller (the caller already has the
		/// metadata context required to resolve the predicate type).
		/// </remarks>
		internal static bool TryCrackConditionalPredicate(
			string conditionalPredicateSubstring,
			out string typeName,
			out string arguments)
		{
			typeName = string.Empty;
			arguments = string.Empty;

			// The format for a conditional predicate is '<xmlnsprefix>:<predicate name>(<arg1>,<arg2>,...,<argn>)'
			// where the substring '<xmlnsprefix>:<predicate name>' represents a type name
			var parenIndex = conditionalPredicateSubstring.IndexOf(ConditionalPredicateArgsBegin);
			if (parenIndex >= 0
				&& conditionalPredicateSubstring.Length > 0
				&& conditionalPredicateSubstring[conditionalPredicateSubstring.Length - 1] == ConditionalPredicateArgsEnd)
			{
				typeName = conditionalPredicateSubstring.Substring(0, parenIndex);
				arguments = conditionalPredicateSubstring.Substring(parenIndex + 1, conditionalPredicateSubstring.Length - parenIndex - 2);
				return true;
			}

			return false;
		}

		/// <summary>
		/// Evaluates a user-supplied <see cref="IXamlCondition"/> for the given
		/// argument string, caching the result for subsequent lookups.
		/// </summary>
		/// <summary>
		/// Evaluates a user-supplied <see cref="IXamlCondition"/> for the given
		/// argument string, caching the result for subsequent lookups.
		/// </summary>
		[UnconditionalSuppressMessage(
			"Trimming",
			"IL2026:Members attributed with RequiresUnreferencedCode may break when trimming",
			Justification = "Custom IXamlCondition implementations are referenced from XAML and cannot be statically discovered.")]
		[UnconditionalSuppressMessage(
			"Trimming",
			"IL2070:'this' argument does not satisfy 'DynamicallyAccessedMemberTypes' requirements",
			Justification = "Custom IXamlCondition implementations are referenced from XAML and cannot be statically discovered.")]
		[UnconditionalSuppressMessage(
			"Trimming",
			"IL2077",
			Justification = "Custom IXamlCondition implementations are referenced from XAML and cannot be statically discovered.")]
		[UnconditionalSuppressMessage(
			"Trimming",
			"IL2080",
			Justification = "Custom IXamlCondition implementations are referenced from XAML and cannot be statically discovered.")]
		internal static bool Evaluate(
			[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicParameterlessConstructor | DynamicallyAccessedMemberTypes.PublicMethods)]
			Type conditionType,
			string argument)
		{
			if (conditionType is null)
			{
				throw new ArgumentNullException(nameof(conditionType));
			}

			// We use a tuple key matching the C++ (predicateToken, args) cache key.
			var cacheKey = (conditionType, argument ?? string.Empty);

			return _evaluationCache.GetOrAdd(cacheKey, EvaluateUncached);
		}

		[UnconditionalSuppressMessage(
			"Trimming",
			"IL2026:Members attributed with RequiresUnreferencedCode may break when trimming",
			Justification = "Custom IXamlCondition implementations are referenced from XAML and cannot be statically discovered.")]
		[UnconditionalSuppressMessage(
			"Trimming",
			"IL2077",
			Justification = "Custom IXamlCondition implementations are referenced from XAML and cannot be statically discovered.")]
		[UnconditionalSuppressMessage(
			"Trimming",
			"IL2080",
			Justification = "Custom IXamlCondition implementations are referenced from XAML and cannot be statically discovered.")]
		private static bool EvaluateUncached((Type Type, string Arguments) key)
		{
			// Insertion, so need to calculate and cache value (mirrors comment in
			// XamlPredicateService::EvaluatePredicate).
			var instance = Activator.CreateInstance(key.Type)
				?? throw new InvalidOperationException(
					$"Failed to create instance of XAML condition '{key.Type.FullName}'.");

			if (instance is IXamlCondition condition)
			{
				return condition.Evaluate(key.Arguments);
			}

			// Fall-back for the legacy IXamlPredicate-shaped contract used by the
			// built-in predicates. They expose a public `bool Evaluate(IReadOnlyList<string>)`
			// method via a non-public interface, so we look it up reflectively.
			var evaluateMethod = key.Type.GetMethod(
				"Evaluate",
				BindingFlags.Public | BindingFlags.Instance,
				binder: null,
				types: new[] { typeof(IReadOnlyList<string>) },
				modifiers: null);

			if (evaluateMethod is not null)
			{
				return (bool)evaluateMethod.Invoke(instance, new object?[] { SplitArgumentsString(key.Arguments) })!;
			}

			throw new InvalidOperationException(
				$"Type '{key.Type.FullName}' does not implement IXamlCondition or a compatible Evaluate method.");
		}

		/// <summary>
		/// Clears the evaluation cache. Intended for tests that need to observe
		/// per-page parse behavior without process-wide caching interfering.
		/// </summary>
		internal static void ClearEvaluationCacheForTests()
		{
			_evaluationCache.Clear();
		}
	}
}
