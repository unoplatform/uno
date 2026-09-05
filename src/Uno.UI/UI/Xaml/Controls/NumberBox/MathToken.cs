// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// MUX Reference controls\dev\NumberBox\NumberBoxParser.h, commit b8cfb849061c00df624ebb29ac4727b9e58ea99c
namespace Microsoft.UI.Xaml.Controls;

internal struct MathToken
{
	public MathToken(MathTokenType t, char c)
	{
		Type = t;
		Char = c;
		Value = double.NaN;
	}

	public MathToken(MathTokenType t, double d)
	{
		Type = t;
		Char = '\0';
		Value = d;
	}

	public MathTokenType Type { get; }

	public char Char { get; }

	public double Value { get; }
}
