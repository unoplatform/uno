// UNO PREAMBLE START

// Note that this license is inherited from the original Hunspell project
// 	as this is derived from that work. Original license:
//
// Version: MPL 1.1/GPL 2.0/LGPL 2.1
//
// The contents of this file are subject to the Mozilla Public License Version
// 1.1 (the "License"); you may not use this file except in compliance with
// the License. You may obtain a copy of the License at
// http://www.mozilla.org/MPL/
//
// Software distributed under the License is distributed on an "AS IS" basis,
// WITHOUT WARRANTY OF ANY KIND, either express or implied. See the License
// for the specific language governing rights and limitations under the
// License.
//
// 	The Original Code is Hunspell, based on MySpell.
//
// 	The Initial Developers of the Original Code are
// Kevin Hendricks (MySpell) and NÃ©meth LÃ¡szlÃ³ (Hunspell).
// 	Portions created by the Initial Developers are Copyright (C) 2002-2005
// the Initial Developers. All Rights Reserved.
//
// 	Contributor(s):
// David Einstein 
// Davide Prina
// Giuseppe Modugno 
// Gianluca Turconi
// Simon Brouwer
// Noll JÃ¡nos
// BÃ­rÃ³ ÃrpÃ¡d
// Goldman EleonÃ³ra
// SarlÃ³s TamÃ¡s
// BencsÃ¡th BoldizsÃ¡r
// HalÃ¡csy PÃ©ter
// Dvornik LÃ¡szlÃ³
// Gefferth AndrÃ¡s
// Nagy Viktor
// Varga DÃ¡niel
// Chris Halls
// Rene Engelhard
// Bram Moolenaar
// Dafydd Jones
// Harri PitkÃ¤nen
//
// 	Alternatively, the contents of this file may be used under the terms of
// 	either the GNU General Public License Version 2 or later (the "GPL"), or
// 	the GNU Lesser General Public License Version 2.1 or later (the "LGPL"),
// 	in which case the provisions of the GPL or the LGPL are applicable instead
// of those above. If you wish to allow use of your version of this file only
// under the terms of either the GPL or the LGPL, and not to allow others to
// use your version of this file under the terms of the MPL, indicate your
// decision by deleting the provisions above and replace them with the notice
// and other provisions required by the GPL or the LGPL. If you do not delete
// the provisions above, a recipient may use your version of this file under
// the terms of any one of the MPL, the GPL or the LGPL.

// https://github.com/aarondandy/WeCantSpell.Hunspell

#pragma warning disable IDE0055, CA1805, CA1815, CA1310,

// UNO PREAMBLE END

using System;

namespace WeCantSpell.Hunspell;

public partial class AffixReader
{
    private ref struct AffixParametersParser
    {
        public AffixParametersParser(ReadOnlySpan<char> text)
        {
            _text = text;
        }

        private ReadOnlySpan<char> _text;

        public bool TryParseNextAffixFlag(FlagParser flagParser, out FlagValue flag)
        {
            var flagSpan = ParseNextArgument();
            return flagParser.TryParseFlag(flagSpan, out flag);
        }

        public ReadOnlySpan<char> ParseNextArgument()
        {
            ReadOnlySpan<char> resultSpan;
            AdvanceThroughWhiteSpace();

            if (_text.Length > 0)
            {
                var i = _text.IndexOfTabOrSpace();

                if (i < 0)
                {
                    resultSpan = _text;
                    _text = [];
                }
                else
                {
                    resultSpan = _text.Slice(0, i);
                    _text = _text.Slice(resultSpan.Length);
                }
            }
            else
            {
                resultSpan = [];
            }

            return resultSpan;
        }

        public ReadOnlySpan<char> ParseFinalArguments()
        {
            AdvanceThroughWhiteSpace();

            var remainder = _text;

            var commentIndex = locateComments(remainder);
            if (commentIndex >= 0)
            {
                remainder = remainder.Slice(0, commentIndex);
            }

            return remainder;

            static int locateComments(ReadOnlySpan<char> span)
            {
                var i = 0;
                while (i < span.Length)
                {
                    i = span.IndexOf('#', i);

                    if (i == 0)
                    {
                        return 0;
                    }
                    else if (i < 0)
                    {
                        goto fail;
                    }

                    if (span[i - 1].IsTabOrSpace())
                    {
                        return i;
                    }

                    i++;
                }

            fail:
                return -1;
            }
        }

        public override readonly string ToString() => _text.ToString();

        private void AdvanceThroughWhiteSpace()
        {
            var i = 0;
            for (; i < _text.Length && _text[i].IsTabOrSpace(); i++) ;

            if (i > 0)
            {
                _text = _text.Slice(i);
            }
        }
    }
}

