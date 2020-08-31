// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

#pragma once
#include "RatingItemInfo.h"

#include "RatingItemFontInfo.g.h"
#include "RatingItemFontInfo.properties.h"

class RatingItemFontInfo :
    public implementation.RatingItemFontInfoT<RatingItemFontInfo, RatingItemInfo>,
    public RatingItemFontInfoProperties
{
public:
    ForwardRefToBaseReferenceTracker(RatingItemInfo)
};