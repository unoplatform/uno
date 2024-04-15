//
//  UNOPickers.m
//

#import "UNOPickers.h"

NSURL* get_best_location(int32_t suggestedStartLocation)
{
    NSSearchPathDirectory path;

    switch (suggestedStartLocation) {
        case PickerLocationIdDocumentsLibrary:
            path = NSDocumentDirectory;
            break;
        case PickerLocationIdComputerFolder:
            return [NSURL URLWithString:@"file:///"];
        case PickerLocationIdDesktop:
            path = NSDesktopDirectory;
            break;
        case PickerLocationIdDownloads:
            path = NSDownloadsDirectory;
            break;
        case PickerLocationIdHomeGroup:
            path = NSSharedPublicDirectory;
            break;
        case PickerLocationIdMusicLibrary:
            path = NSMusicDirectory;
            break;
        case PickerLocationIdPicturesLibrary:
            path = NSPicturesDirectory;
            break;
        case PickerLocationIdVideosLibrary:
            path = NSMoviesDirectory;
            break;
        case PickerLocationIdObjects3D:
#if DEBUG
            NSLog(@"get_best_location %d -> no extact match, suggesting Home directory", suggestedStartLocation);
#endif
            return [NSURL URLWithString:NSHomeDirectory()];
        case PickerLocationIdUnspecified:
#if DEBUG
            NSLog(@"get_best_location %d -> unspecified", suggestedStartLocation);
#endif
            return nil;
        default:
#if DEBUG
            NSLog(@"get_best_location %d -> unknown value", suggestedStartLocation);
#endif
            return nil;
    }

    return [[NSFileManager defaultManager] URLsForDirectory:path inDomains:NSUserDomainMask][0];
}

NSMutableArray<NSString*>* get_allowed(char* filters[], int filterSize)
{
    NSMutableArray<NSString*> *allowed = [[NSMutableArray alloc] initWithCapacity:filterSize];
    for (int i=0; i < filterSize; i++) {
        NSString *s = [NSString stringWithUTF8String:filters[i]];
        [allowed addObject:s];
    }
    return allowed;
}

char* uno_pick_single_folder(const char* _Nullable prompt, const char* _Nullable identifier, int32_t suggestedStartLocation)
{
    NSOpenPanel *panel = [NSOpenPanel openPanel];
    // based on settings from uno/src/Uno.UWP/Storage/Pickers/FolderPicker.macOS.cs
    // filters are not applied in WinUI so we don't set them up here
    panel.allowedFileTypes = [NSArray arrayWithObject:@"none"];
    panel.canChooseDirectories = true;
    panel.canChooseFiles = false;
    panel.directoryURL = get_best_location(suggestedStartLocation);
    if (identifier) {
        panel.identifier = [NSString stringWithUTF8String:identifier];
    }
    if (prompt) {
        panel.prompt = [NSString stringWithUTF8String:prompt];
    }

    if ([panel runModal] == NSModalResponseOK) {
        NSURL *url = panel.URL;
        if (url) {
#if DEBUG
            NSLog(@"uno_pick_single_folder -> %s", url.path.UTF8String);
#endif
            // we need to dupe since it's NS_RETURNS_INNER_POINTER and dotnet will (try to) free it
            return strdup(url.path.UTF8String);
        }
    }
#if DEBUG
    NSLog(@"uno_pick_single_folder -> nil");
#endif
    return nil;
}

char* uno_pick_single_file(const char* _Nullable prompt, const char* _Nullable identifier, PickerLocationId suggestedStartLocation, char* filters[], int filterSize)
{
    NSOpenPanel *panel = [NSOpenPanel openPanel];
    // based on settings from uno/src/Uno.UWP/Storage/Pickers/FileOpenPicker.macOS.cs
    panel.allowedFileTypes = get_allowed(filters, filterSize);
    panel.canChooseDirectories = false;
    panel.canChooseFiles = true;
    panel.allowsMultipleSelection = false;
    panel.directoryURL = get_best_location(suggestedStartLocation);
    if (identifier) {
        panel.identifier = [NSString stringWithUTF8String:identifier];
    }
    if (prompt) {
        panel.prompt = [NSString stringWithUTF8String:prompt];
    }

    if ([panel runModal] == NSModalResponseOK) {
        NSURL *url = panel.URL;
        if (url) {
#if DEBUG
            NSLog(@"uno_pick_single_open_file -> %s", url.path.UTF8String);
#endif
            // we need to dupe since it's NS_RETURNS_INNER_POINTER and dotnet will (try to) free it
            return strdup(url.path.UTF8String);
        }
    }
#if DEBUG
    NSLog(@"uno_pick_single_open_file -> nil");
#endif
    return nil;
}

char** uno_pick_multiple_files(const char* _Nullable prompt, const char* _Nullable identifier, PickerLocationId suggestedStartLocation, char* filters[], int filterSize)
{
    NSOpenPanel *panel = [NSOpenPanel openPanel];
    // based on settings from uno/src/Uno.UWP/Storage/Pickers/FileOpenPicker.macOS.cs
    panel.allowedFileTypes = get_allowed(filters, filterSize);
    panel.canChooseDirectories = false;
    panel.canChooseFiles = true;
    panel.allowsMultipleSelection = true;
    panel.directoryURL = get_best_location(suggestedStartLocation);
    if (identifier) {
        panel.identifier = [NSString stringWithUTF8String:identifier];
    }
    if (prompt) {
        panel.prompt = [NSString stringWithUTF8String:prompt];
    }

    if ([panel runModal] == NSModalResponseOK) {
        NSArray<NSURL*> *urls = panel.URLs;
        if (urls) {
            NSUInteger count = urls.count;
            char **array = (char **)malloc((count + 1) * sizeof(char*));
            for (NSUInteger i = 0; i < count; i++) {
                // we need to dupe since it's NS_RETURNS_INNER_POINTER and dotnet will (try to) free it
                char *url = strdup([urls objectAtIndex:i].path.UTF8String);
                array[i] = url;
#if DEBUG
                NSLog(@"uno_pick_multiple_files -> %lu %s", i, url);
#endif
            }
            array[count] = nil;
            return array;
        }
    }
#if DEBUG
    NSLog(@"uno_pick_multiple_files -> nil");
#endif
    return nil;
}

char* uno_pick_save_file(const char* _Nullable prompt, const char* _Nullable identifier, const char* _Nullable suggestedFileName, PickerLocationId suggestedStartLocation, char* filters[], int filterSize)
{
    NSSavePanel *panel = [NSSavePanel savePanel];
    // based on settings from uno/src/Uno.UWP/Storage/Pickers/FileSavePicker.macOS.cs
    panel.allowsOtherFileTypes = true;
    panel.allowedFileTypes = get_allowed(filters, filterSize);
    panel.directoryURL = get_best_location(suggestedStartLocation);
    if (identifier) {
        panel.identifier = [NSString stringWithUTF8String:identifier];
    }
    if (prompt) {
        panel.prompt = [NSString stringWithUTF8String:prompt];
    }
    if (suggestedFileName) {
        panel.nameFieldStringValue = [NSString stringWithUTF8String:suggestedFileName];
    }
    if ([panel runModal] == NSModalResponseOK) {
        NSURL *url = panel.URL;
        if (url) {
#if DEBUG
            NSLog(@"uno_pick_single_save_file -> %s", url.path.UTF8String);
#endif
            // we need to dupe since it's NS_RETURNS_INNER_POINTER and dotnet will (try to) free it
            return strdup(url.path.UTF8String);
        }
    }
#if DEBUG
    NSLog(@"uno_pick_single_save_file -> nil");
#endif
    return nil;
}
