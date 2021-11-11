// ******************************************************************
// Copyright � 2015-2018 nventive inc. All rights reserved.
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
//      http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.
//
// ******************************************************************
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace Uno.Validation
{
	/// <summary>
	/// Static class containing a series of method for validating inputs
	/// </summary>
	internal static class ValidationHelper
	{
		#region Fields and Const

		/// <summary>
		/// The accepted chars used to validate the US and Canadian phones
		/// </summary>
		private static readonly char[] _acceptedCharsInUSAndCanadianPhones = new char[] { ' ', '(', ')', '.', '-', '+' };

		/// <summary>
		/// The regex options
		/// </summary>
		private const RegexOptions _regexOptions =
#if NETFX_CORE
				RegexOptions.CultureInvariant | RegexOptions.Singleline | RegexOptions.IgnoreCase;
#else
				RegexOptions.CultureInvariant | RegexOptions.Singleline | RegexOptions.IgnoreCase | RegexOptions.Compiled;
#endif

		#endregion

		#region Public Regex Properties

		/// <summary>
		/// The email regex.
		/// </summary>
		/// <remarks>
		/// Accepts any email address adhering to the w3c standard that does NOT contain non-latin characters.
		/// </remarks>
		public static readonly Regex EmailRegex = CreateEmailRegex();

		/// <summary>
		/// The Canadian postal code regex. can be lower case with no space.
		/// </summary>
		public static readonly Regex CanadianPostalCodeRegex = CreateCanadianPostalCodeRegex();

		/// <summary>
		/// The zip code regex, valid input can be in the form ddddd or ddddd-dddd
		/// </summary>
		public static readonly Regex ZipCodeRegex = CreateZipCodeRegex();

		/// <summary>
		/// Gets the us state only regex. Does not include the Territories and Military symbols
		/// </summary>
		/// <value>
		/// The us state only regex.
		/// </value>
		public static readonly Regex USStateOnlyRegex = CreateUsStateRegex(includeTerritories: false, includeMilitary: false);

		/// <summary>
		/// Gets the us state regex. Includes the Territories and Military symbols
		/// </summary>
		/// <value>
		/// The us state only regex.
		/// </value>
		public static readonly Regex USStateWithTerritoriesAndMillitaryRegex = CreateUsStateRegex(includeTerritories: true, includeMilitary: true);

		/// <summary>
		/// Gets the us state regex. Includes the Territories but not and Military symbols
		/// </summary>
		/// <value>
		/// The us state only regex.
		/// </value>
		public static readonly Regex USStateWithTerritoriesRegex = CreateUsStateRegex(includeTerritories: true, includeMilitary: false);

		/// <summary>
		/// Gets the us state regex. Includes the Military but not and Territories symbols
		/// </summary>
		/// <value>
		/// The us state only regex.
		/// </value>
		public static readonly Regex USStateWithMillitaryRegex = CreateUsStateRegex(includeTerritories: false, includeMilitary: true);

		#endregion

		#region Public methods

		/// <summary>
		/// Determines whether the specified input is a valid email.
		/// The minimum format of the email must be x@x.xx
		/// </summary>
		/// <remarks>Regex currently limited to ASCII characters.</remarks>
		/// <param name="input">The input. If <c>null</c> returns <c>false</c>.</param>
		/// <returns>
		///   <c>true</c> if the specified input is a valid email; otherwise, <c>false</c>.
		/// </returns>
		public static bool IsEmail(string input)
		{
			return EmailRegex.IsMatch(input ?? "");
		}

		/// <summary>
		/// Determines whether the specified input is a valid Canadian postal code.
		/// </summary>
		/// <param name="input">The input. If <c>null</c> returns <c>false</c>.</param>
		/// <returns>
		///   <c>true</c> if the specified input is a valid Canadian postal code; otherwise, <c>false</c>.
		/// </returns>
		public static bool IsCanadianPostalCode(string input)
		{
			return CanadianPostalCodeRegex.IsMatch(input ?? "");
		}

		/// <summary>
		/// Determines whether the specified input is a valid US zip code.
		/// Valid input can be 5 digits or 5 digits followed by 4 digits in the form of ddddd-dddd
		/// </summary>
		/// <param name="input">The input. If <c>null</c> returns <c>false</c>.</param>
		/// <returns>
		///   <c>true</c> if the specified input is a valid US zip code; otherwise, <c>false</c>.
		/// </returns>
		public static bool IsZipCode(string input)
		{
			return ZipCodeRegex.IsMatch(input ?? "");
		}

		/// <summary>
		/// Validates US States and/or Territories by @jdforsythe
		/// Case insensitive
		/// Can include US Territories or not - default does not 
		/// Can include US Military postal abbreviations(AA, AE, AP) - default does not
		/// Note: "States" always includes DC(District of Colombia)
		/// https://en.wikipedia.org/wiki/List_of_U.S._state_abbreviations
		/// </summary>
		/// <param name="input">The input. If <c>null</c> returns <c>false</c>.</param>
		/// <param name="includeTerritories"></param>
		/// <param name="includeMilitary"></param>
		/// <returns></returns>
		public static bool IsUSState(string input, bool includeTerritories = false, bool includeMilitary = false)
		{
			Regex stateRegex;
			if (!includeTerritories && !includeMilitary)
			{
				stateRegex = USStateOnlyRegex;
			}
			else if (includeTerritories && includeMilitary)
			{
				stateRegex = USStateWithTerritoriesAndMillitaryRegex;
			}
			else if (includeTerritories)
			{
				stateRegex = USStateWithTerritoriesRegex;
			}
			else
			{
				stateRegex = USStateWithMillitaryRegex;
			}

			return stateRegex.IsMatch(input ?? "");
		}

		/// <summary>
		/// Determines whether the specified input is valid currency.
		/// </summary>
		/// <param name="input">The input. If <c>null</c> returns <c>false</c>.</param>
		/// <param name="symbol">The symbol.</param>
		/// <param name="optinalSymbol">if set to <c>true</c> the symbol is optional to be considered valid.</param>
		/// <returns>
		///   <c>true</c> if the specified input is valid currency; otherwise, <c>false</c>.
		/// </returns>
		public static bool IsCurrency(string input, IFormatProvider currentCulture = null)
		{
			currentCulture = currentCulture ?? CultureInfo.CurrentCulture;
			double dummy;
			var r = double.TryParse(input ?? "", NumberStyles.Currency, currentCulture, out dummy);
			return r;
		}

		/// <summary>
		/// Determines whether the specified input is a valid north American phone number.
		/// To be valid it as to have 7 or 10 digits anything else in between is not validated i.e. separators
		/// </summary>
		/// <param name="input">The input. If <c>null</c> returns <c>false</c>.</param>
		/// <returns>
		///   <c>true</c> if the specified input is a valid north American phone; otherwise, <c>false</c>.
		/// </returns>
		public static bool IsUSCanadaPhone(string input)
		{
			input = input ?? "";
			var allCharsAreValid = input.All(
				c => char.IsDigit(c) ||
				_acceptedCharsInUSAndCanadianPhones.Contains(c)
			);

			if (!allCharsAreValid)
			{
				return false;
			}

			var phoneNumber = input.Where(c => char.IsDigit(c)).ToArray();
			return phoneNumber.Length == 7 ||
				   phoneNumber.Length == 10 ||
				   (phoneNumber.Length == 11 && phoneNumber[0] == '1');
		}

		#endregion

		#region Regex creation methods

		private static Regex CreateUsStateRegex(bool includeTerritories, bool includeMilitary)
		{
			var regex = "^(D[CE]|FL|HI|I[ADLN]|K[SY]|LA|N[CDEHJMVY]|O[HKR]|RI|S[CD]|T[NX]|UT|W[AIVY]";

			if (!includeTerritories && !includeMilitary)
			{
				regex += "|A[KLRZ]|C[AOT]|GA|M[ADEINOST]|PA|V[AT])$";
			}
			else if (includeTerritories && includeMilitary)
			{
				regex += "|A[AEKLPRSZ]|C[AOT]|G[AU]|M[ADEINOPST]|P[AR]|V[AIT])$";
			}
			else if (includeTerritories)
			{
				regex += "|A[KLRSZ]|C[AOT]|G[AU]|M[ADEINOPST]|P[AR]|V[AIT])$";
			}
			else
			{
				regex += "|A[AEKLPRZ]|C[AOT]|GA|M[ADEINOST]|PA|V[AT])$";
			}

			return new Regex(regex, _regexOptions);
		}

		private static Regex CreateZipCodeRegex()
		{
			return new Regex(
				@"\d{5}$|\d{5}-\d{4}$",
				_regexOptions
			);
		}

		private static Regex CreateCanadianPostalCodeRegex()
		{
			//Case insensitive
			/*  First or last character in a word
			*  Any character in this class: [A-z-[dDfFiIoOqQuUwWzZ]]
			*  Any digit
			*  Any character in this class: [A-z-[dDfFiIoOqQuU]]
			*   *\d
			*      Space, any number of repetitions
			*      Any digit
			*  Any character in this class: [A-z-[dDfFiIoOqQuU]]
			*  \d\b
			*      Any digit
			*      First or last character in a word
			*/
			return new Regex(
				@"[A-z-[dDfFiIoOqQuUwWzZ]]\d[A-z-[dDfFiIoOqQuU]] *\d[A-z-[dDfFiIoOqQuU]]\d\b",
				_regexOptions
			);
		}

		private static Regex CreateEmailRegex()
		{
			return new Regex(
				@"\A(?:[a-z0-9!#$%&'*+/=?^_`{|}~-]+(?:\.[a-z0-9!#$%&'*+/=?^_`{|}~-]+)*@(?:[a-z0-9](?:[a-z0-9-]*[a-z0-9])?\.)+[a-z0-9](?:[a-z0-9-]*[a-z0-9])?)\Z",
				_regexOptions);
		}

		#endregion
	}
}
