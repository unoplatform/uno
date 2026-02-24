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
using System.Diagnostics;
using System.Threading;

namespace WeCantSpell.Hunspell;

[DebuggerDisplay("HasBeenCanceled = {HasBeenCanceled}")]
internal struct OperationTimedLimiter
{
    public OperationTimedLimiter(TimeSpan timeLimit, CancellationToken cancellationToken)
    {
        _timer = new ExpirationTimer(timeLimit);
        _cancellationToken = cancellationToken;
        _hasTriggeredCancellation = false;
    }

    private readonly ExpirationTimer _timer;
    private readonly CancellationToken _cancellationToken;
    private bool _hasTriggeredCancellation;

    public readonly bool HasBeenCanceled => _hasTriggeredCancellation || _cancellationToken.IsCancellationRequested;

    public bool QueryForCancellation()
    {
        if (!_hasTriggeredCancellation)
        {
            if (_cancellationToken.IsCancellationRequested || _timer.QueryForExpiration())
            {
                _hasTriggeredCancellation = true;
            }
        }

        return _hasTriggeredCancellation;
    }
}

struct OperationTimedCountLimiter
{
    /// <summary>
    /// This is the number of operations that are added to a limiter if it runs out of operations before the time limit has expired.
    /// </summary>
    /// <remarks>
    /// The prupose of this mechanism seems to be a reduction in the number of slow queries made against the clock.
    /// </remarks>
    private const int MaxPlusTimer = 100;

    public OperationTimedCountLimiter(TimeSpan timeLimit, int countLimit, CancellationToken cancellationToken)
    {
        _timer = new ExpirationTimer(timeLimit);
        _cancellationToken = cancellationToken;
        _counter = countLimit;
        _hasTriggeredCancellation = false;
    }

    private readonly ExpirationTimer _timer;
    private readonly CancellationToken _cancellationToken;
    private int _counter;
    private bool _hasTriggeredCancellation;

    public readonly bool HasBeenCanceled => _hasTriggeredCancellation || _cancellationToken.IsCancellationRequested;

    public bool QueryForCancellation()
    {
        if (!_hasTriggeredCancellation)
        {
            if (_cancellationToken.IsCancellationRequested)
            {
                _hasTriggeredCancellation = true;
            }
            else if (_counter > 1)
            {
                _counter--;
            }
            else if (_timer.QueryForExpiration())
            {
                _counter = 0;
                _hasTriggeredCancellation = true;
            }
            else
            {
                _counter = MaxPlusTimer;
            }
        }

        return _hasTriggeredCancellation;
    }
}

readonly struct ExpirationTimer
{
    private static readonly DateTime DisabledSentinelValue = DateTime.MinValue;

    internal ExpirationTimer(TimeSpan timeLimit)
    {
        if (timeLimit < TimeSpan.Zero)
        {
            _expiresAt = DisabledSentinelValue;
        }
        else
        {
            _expiresAt = DateTime.UtcNow + timeLimit;
            if (DateTime.MinValue >= _expiresAt || DateTime.MaxValue <= _expiresAt)
            {
                _expiresAt = DisabledSentinelValue;
            }
        }
    }

    private readonly DateTime _expiresAt;

    public readonly bool QueryForExpiration() => _expiresAt != DisabledSentinelValue && DateTime.UtcNow >= _expiresAt;
}

