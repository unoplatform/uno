//
//  UNOPickers.m
//

#import "UNOPickers.h"

char* uno_pick_single_folder(void)
{
    NSOpenPanel *panel = [NSOpenPanel openPanel];
    // based on settings from uno/src/Uno.UWP/Storage/Pickers/FolderPicker.macOS.cs
    // note: `allowsOtherFileTypes` is only used on NSSavePanel (and does nothing on NSOpenPanel)
    panel.allowedFileTypes = [NSArray arrayWithObject:@"none"];
    panel.canChooseDirectories = true;
    panel.canChooseFiles = false;
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

char* uno_pick_single_file(const char *prompt)
{
    NSOpenPanel *panel = [NSOpenPanel openPanel];
    // based on settings from uno/src/Uno.UWP/Storage/Pickers/FileOpenPicker.macOS.cs
    // note: `allowsOtherFileTypes` is only used on NSSavePanel (and does nothing on NSOpenPanel)
    panel.allowedFileTypes = nil; // FIXME (nil means every types)
    panel.canChooseDirectories = false;
    panel.canChooseFiles = true;
    panel.allowsMultipleSelection = false;
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

char** uno_pick_multiple_files(const char *prompt)
{
    NSOpenPanel *panel = [NSOpenPanel openPanel];
    // based on settings from uno/src/Uno.UWP/Storage/Pickers/FileOpenPicker.macOS.cs
    // note: `allowsOtherFileTypes` is only used on NSSavePanel (and does nothing on NSOpenPanel)
    panel.allowedFileTypes = nil; // FIXME (nil means every types)
    panel.canChooseDirectories = false;
    panel.canChooseFiles = true;
    panel.allowsMultipleSelection = true;
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

char* uno_pick_save_file(const char *prompt)
{
    NSOpenPanel *panel = [NSOpenPanel openPanel];
    // based on settings from uno/src/Uno.UWP/Storage/Pickers/FileSavePicker.macOS.cs
    panel.allowsOtherFileTypes = true;
    panel.allowedFileTypes = nil; // FIXME (nil means every types)
    panel.canChooseFiles = true;
    if (prompt) {
        panel.prompt = [NSString stringWithUTF8String:prompt];
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
