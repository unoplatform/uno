// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

#nullable enable

namespace Microsoft.UI.Xaml.Documents;

// TODO Uno (Stage 4): temporary placeholders for the WinUI font/inherited-property
// element-model types that the ported run model (ParagraphTextSource.GetTextRun and
// BlockLayoutHelpers) reference. The basic-text render path uses ParsedText via
// ISkiaParagraphSource and does not touch these; they are stubbed so the faithful
// run-model code compiles and will be replaced by the real ports when the element
// model (ITextContainer / GetRun / font context) lands.

// WinUI CFontContext — owner font-resolution context. On Uno font resolution flows
// through FontDetailsCache directly; this is a marker until that bridge is wired.
internal sealed class FontContext
{
}

// WinUI FontTypeface — a resolved typeface (family/weight/style/stretch/language).
// On Uno this maps onto FontDetails / SKTypeface.
internal sealed class FontTypeface
{
}
