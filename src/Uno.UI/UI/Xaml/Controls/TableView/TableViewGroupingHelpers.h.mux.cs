// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// MUX Reference controls\dev\TableView\TableViewGroupingHelpers.h, tag winui3/release/2.5.4-experimental, commit 7b127093475

#nullable enable

using System;
using System.Collections.Generic;
using System.Globalization;
using Windows.Globalization.NumberFormatting;
using Windows.System.UserProfile;

namespace Microsoft.UI.Xaml.Controls.Tabular;

internal static partial class TableViewDetails
{
	// Culture-aware integer formatting shared by the group-key stringifier (TableViewRow) and
	// the group-header count (TableViewGroupInfo).
	//
	// Only the *locale resolution* is cached -- GetUserDefaultLocaleName + Language::IsWellFormed
	// + GlobalizationPreferences::HomeGeographicRegion were previously re-run per call, on the UI
	// thread during measure. The DecimalFormatter itself is NOT shared: callers mutate
	// FractionDigits, so a shared instance would let one call site's digits leak into another's
	// output (and would race across XAML threads).
	// TODO Uno: a class rather than a struct, so GetGroupingLocale can hand out the cached instance
	// by reference as the C++ `GroupingLocale const&` does.
	internal sealed class GroupingLocale
	{
		public List<string> Languages = new();
		public string Region = "";
		public bool HasLanguages = false;
	}

	internal static GroupingLocale GetGroupingLocale()
	{
		// TODO Uno: Original C++ function-local `static GroupingLocale s_locale;` + `static std::once_flag s_onceFlag;`
		// with std::call_once become the s_groupingLocale Lazy<T> below (ExecutionAndPublication == call_once).
		return s_groupingLocale.Value;
	}

	private static readonly Lazy<GroupingLocale> s_groupingLocale = new(
		() =>
		{
			GroupingLocale s_locale = new();

			try
			{
				// TODO Uno: Original C++:
				// WCHAR currentLocale[LOCALE_NAME_MAX_LENGTH] = {};
				// if (GetUserDefaultLocaleName(currentLocale, LOCALE_NAME_MAX_LENGTH) != 0)
				// GetUserDefaultLocaleName is Win32-only; the current culture name stands in for the user default
				// locale, and an empty name stands in for the API failing.
				var currentLocale = CultureInfo.CurrentCulture.Name;
				if (!string.IsNullOrEmpty(currentLocale))
				{
					// Strip any sort-order suffix (e.g. de-DE_phoneb), which is not a valid tag.
					var underscore = currentLocale.IndexOf('_');
					if (underscore >= 0)
					{
						currentLocale = currentLocale.Substring(0, underscore);
					}

					if (global::Windows.Globalization.Language.IsWellFormed(currentLocale))
					{
						s_locale.Languages.Add(currentLocale);
						s_locale.Region = GlobalizationPreferences.HomeGeographicRegion;
						s_locale.HasLanguages = true;
					}
				}
			}
			catch (Exception)
			{
				s_locale.Languages.Clear();
				s_locale.HasLanguages = false;
			}

			return s_locale;
		});

	// Returns a fresh formatter each call: the caller owns its FractionDigits/IntegerDigits.
	internal static DecimalFormatter? CreateCurrentCultureDecimalFormatter()
	{
		try
		{
			var locale = GetGroupingLocale();

			var formatter = locale.HasLanguages
				? new DecimalFormatter(locale.Languages, locale.Region)
				: new DecimalFormatter();

			formatter.IntegerDigits = 1;
			return formatter;
		}
		catch (Exception)
		{
			return null;
		}
	}

	internal static string FormatIntegerForCurrentCulture(long value)
	{
		try
		{
			if (CreateCurrentCultureDecimalFormatter() is { } formatter)
			{
				formatter.FractionDigits = 0;
				return formatter.FormatInt(value);
			}
		}
		catch (Exception)
		{
		}

		return value.ToString(CultureInfo.InvariantCulture);
	}
}
