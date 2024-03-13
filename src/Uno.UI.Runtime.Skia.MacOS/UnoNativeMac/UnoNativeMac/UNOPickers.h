//
//  UNOPickers.h
//

#pragma once

#import "UnoNativeMac.h"

NS_ASSUME_NONNULL_BEGIN

// https://learn.microsoft.com/en-us/uwp/api/windows.storage.pickers.pickerlocationid?view=winrt-22621
typedef NS_ENUM(sint32, PickerLocationId) {
    PickerLocationIdDocumentsLibrary = 0,
    PickerLocationIdComputerFolder = 1,
    PickerLocationIdDesktop = 2,
    PickerLocationIdDownloads = 3,
    PickerLocationIdHomeGroup = 4,
    PickerLocationIdMusicLibrary = 5,
    PickerLocationIdPicturesLibrary = 6,
    PickerLocationIdVideosLibrary = 7,
    PickerLocationIdObjects3D = 8,
    PickerLocationIdUnspecified = 9,
};

char* _Nullable uno_pick_single_folder(const char* _Nullable prompt, const char* _Nullable identifier, PickerLocationId suggestedStartLocation);
char* _Nullable uno_pick_single_file(const char* _Nullable prompt, const char* _Nullable identifier, PickerLocationId suggestedStartLocation, char* _Nonnull filters[_Nullable], int filterSize);
char* _Nullable uno_pick_save_file(const char* _Nullable prompt, const char* _Nullable identifier, const char* _Nullable suggestedFileName, PickerLocationId suggestedStartLocation, char* _Nonnull filters[_Nullable], int filterSize);
char* _Nullable * _Nullable uno_pick_multiple_files(const char* _Nullable prompt, const char* _Nullable identifier, PickerLocationId suggestedStartLocation, char* _Nonnull filters[_Nullable], int filterSize);

NS_ASSUME_NONNULL_END
