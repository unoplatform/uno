//
//  UNOCameraCapture.h
//

#pragma once

#import "UnoNativeMac.h"

NS_ASSUME_NONNULL_BEGIN

char* _Nullable uno_capture_photo(int64_t operationId, bool useJpeg);
char* _Nullable uno_capture_video(int64_t operationId);
void uno_capture_cancel(int64_t operationId);

void uno_perform_on_main_runloop(void* context, void (*callback)(void* context));

NS_ASSUME_NONNULL_END
