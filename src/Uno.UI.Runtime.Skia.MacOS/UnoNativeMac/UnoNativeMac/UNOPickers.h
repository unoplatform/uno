//
//  UNOPickers.h
//

#pragma once

#import <AppKit/AppKit.h>
#import <Foundation/Foundation.h>

NS_ASSUME_NONNULL_BEGIN

char* _Nullable uno_pick_single_folder(void);
char* _Nullable uno_pick_single_file(const char* _Nullable prompt);
char* _Nullable uno_pick_save_file(const char* _Nullable prompt);
char* _Nullable * _Nullable uno_pick_multiple_files(const char* _Nullable prompt);

NS_ASSUME_NONNULL_END
