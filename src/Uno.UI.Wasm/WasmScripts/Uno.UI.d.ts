declare namespace Uno.Utils {
    class Clipboard {
        static setText(text: string): string;
    }
}
declare namespace Uno.UI {
    class HtmlDom {
        /**
         * Initialize various polyfills used by Uno
         */
        static initPolyfills(): void;
        private static isConnectedPolyfill();
    }
}
declare namespace Uno.Http {
    interface IHttpClientConfig {
        id: string;
        method: string;
        url: string;
        headers?: string[][];
        payload?: string;
        payloadType?: string;
        cacheMode?: RequestCache;
    }
    class HttpClient {
        static send(config: IHttpClientConfig): Promise<void>;
        private static blobFromBase64(base64, contentType);
        private static base64FromBlob(blob);
        private static dispatchResponse(requestId, status, headers, payload);
        private static dispatchError(requestId, error);
        private static dispatchResponseMethod;
        private static dispatchErrorMethod;
        private static initMethods();
    }
}
declare module Uno.UI {
    interface IContentDefinition {
        id: string;
        tagName: string;
        handle: number;
        type: string;
        isSvg: boolean;
        isFrameworkElement: boolean;
        isFocusable: boolean;
        classes?: string[];
    }
}
declare namespace Uno.UI {
    class WindowManager {
        private containerElementId;
        private loadingElementId;
        static current: WindowManager;
        private static _isHosted;
        /**
         * Defines if the WindowManager is running in hosted mode, and should skip the
         * initialization of WebAssembly, use this mode in conjuction with the Uno.UI.WpfHost
         * to improve debuggability.
         */
        static readonly isHosted: boolean;
        private static readonly unoRootClassName;
        private static readonly unoUnarrangedClassName;
        /**
            * Initialize the WindowManager
            * @param containerElementId The ID of the container element for the Xaml UI
            * @param loadingElementId The ID of the loading element to remove once ready
            */
        static init(localStoragePath: string, isHosted: boolean, containerElementId?: string, loadingElementId?: string): string;
        private containerElement;
        private rootContent;
        private allActiveElementsById;
        static assembly: UI.Interop.IMonoAssemblyHandle;
        private static resizeMethod;
        private static dispatchEventMethod;
        private constructor();
        /**
         * Setup the storage persistence
         *
         * */
        static setupStorage(localStoragePath: string): void;
        /**
         * Determine if IndexDB is available, some browsers and modes disable it.
         * */
        static isIndexDBAvailable(): boolean;
        /**
         * Synchronize the IDBFS memory cache back to IndexDB
         * */
        static synchronizeFileSystem(): void;
        /**
            * Creates the UWP-compatible splash screen
            *
            */
        static setupSplashScreen(): void;
        /**
            * Reads the window's search parameters
            *
            */
        static findLaunchArguments(): string;
        /**
            * Create a html DOM element representing a Xaml element.
            *
            * You need to call addView to connect it to the DOM.
            */
        createContent(contentDefinition: IContentDefinition): string;
        /**
            * Set a name for an element.
            *
            * This is mostly for diagnostic purposes.
            */
        setName(elementId: string, name: string): string;
        /**
            * Set an attribute for an element.
            */
        setAttribute(elementId: string, attributes: {
            [name: string]: string;
        }): string;
        /**
            * Get an attribute for an element.
            */
        getAttribute(elementId: string, name: string): any;
        /**
            * Set a property for an element.
            */
        setProperty(elementId: string, properties: {
            [name: string]: string;
        }): string;
        /**
            * Get a property for an element.
            */
        getProperty(elementId: string, name: string): any;
        /**
            * Set the CSS style of a html element.
            *
            * To remove a value, set it to empty string.
            * @param styles A dictionary of styles to apply on html element.
            */
        setStyle(elementId: string, styles: {
            [name: string]: string;
        }, setAsArranged?: boolean): string;
        /**
            * Set the CSS style of a html element.
            *
            * To remove a value, set it to empty string.
            * @param styles A dictionary of styles to apply on html element.
            */
        resetStyle(elementId: string, names: string[]): string;
        /**
            * Load the specified URL into a new tab or window
            * @param url URL to load
            * @returns "True" or "False", depending on whether a new window could be opened or not
            */
        open(url: string): string;
        /**
            * Issue a browser alert to user
            * @param message message to display
            */
        alert(message: string): string;
        /**
            * Sets the browser window title
            * @param message the new title
            */
        setWindowTitle(title: string): string;
        /**
            * Gets the currently set browser window title
            */
        getWindowTitle(): string;
        /**
            * Add an event handler to a html element.
            *
            * @param eventName The name of the event
            * @param onCapturePhase true means "on trickle down", false means "on bubble up". Default is false.
            */
        registerEventOnView(elementId: string, eventName: string, onCapturePhase?: boolean, eventFilter?: (event: Event) => boolean, eventExtractor?: (event: Event) => any): string;
        /**
            * Set or replace the root content element.
            */
        setRootContent(elementId?: string): string;
        /**
            * Set a view as a child of another one.
            *
            * "Loading" & "Loaded" events will be raised if nescessary.
            *
            * @param index Position in children list. Appended at end if not specified.
            */
        addView(parentId: string, childId: string, index?: number): string;
        /**
            * Remove a child from a parent element.
            *
            * "Unloading" & "Unloaded" events will be raised if nescessary.
            */
        removeView(parentId: string, childId: string): string;
        /**
            * Destroy a html element.
            *
            * The element won't be available anymore. Usually indicate the managed
            * version has been scavenged by the GC.
            */
        destroyView(viewId: string): string;
        getBoundingClientRect(elementId: string): string;
        getBBox(elementId: string): string;
        /**
            * Use the Html engine to measure the element using specified constraints.
            *
            * @param maxWidth string containing width in pixels. Empty string means infinite.
            * @param maxHeight string containing height in pixels. Empty string means infinite.
            */
        measureView(viewId: string, maxWidth: string, maxHeight: string): string;
        setImageRawData(viewId: string, dataPtr: number, width: number, height: number): string;
        setPointerCapture(viewId: string, pointerId: number): string;
        releasePointerCapture(viewId: string, pointerId: number): string;
        focusView(elementId: string): string;
        /**
            * Set the Html content for an element.
            *
            * Those html elements won't be available as XamlElement in managed code.
            * WARNING: you should avoid mixing this and `addView` for the same element.
            */
        setHtmlContent(viewId: string, html: string): string;
        /**
            * Remove the loading indicator.
            *
            * In a future version it will also handle the splashscreen.
            */
        activate(): string;
        private init();
        private static initMethods();
        private initDom();
        private removeLoading();
        private resize();
        private dispatchEvent(element, eventName, eventPayload?);
        private getMonoString(str);
        private fromMonoString(strHandle);
        private getIsConnectedToRootElement(element);
    }
}
declare module Uno.UI {
    interface IAppManifest {
        splashScreenImage: URL;
        splashScreenColor: string;
        displayName: string;
    }
}
declare module Uno.UI.Interop {
    interface IMonoAssemblyHandle {
    }
}
declare module Uno.UI.Interop {
    interface IMonoClassHandle {
    }
}
declare module Uno.UI.Interop {
    interface IMonoMethodHandle {
    }
}
declare module Uno.UI.Interop {
    interface IMonoRuntime {
        assembly_load(assemblyName: string): Interop.IMonoAssemblyHandle;
        find_class(moduleHandle: Interop.IMonoAssemblyHandle, namespace: string, typeName: string): Interop.IMonoClassHandle;
        find_method(classHandle: Interop.IMonoClassHandle, methodName: string, _: number): Interop.IMonoMethodHandle;
        call_method(methodHandle: Interop.IMonoMethodHandle, object: any, params?: any[]): any;
        mono_string(str: string): Interop.IMonoStringHandle;
        conv_string(strHandle: Interop.IMonoStringHandle): string;
    }
}
declare module Uno.UI.Interop {
    interface IMonoStringHandle {
    }
}
declare module Uno.UI.Interop {
    interface IUnoDispatch {
        resize(size: string): void;
        dispatch(htmlIdStr: string, eventNameStr: string, eventPayloadStr: string): string;
    }
}
declare module Uno.UI.Interop {
    interface IWebAssemblyApp {
        main_module: Interop.IMonoAssemblyHandle;
        main_class: Interop.IMonoClassHandle;
    }
}
declare namespace Uno.Foundation.Interop {
    class ManagedObject {
        private static assembly;
        private static dispatchMethod;
        private static init();
        static dispatch(handle: string, method: string, parameters: string): void;
    }
}
declare namespace Uno.UI.Interop {
    class Runtime {
        static readonly engine: any;
        private static init();
    }
}
declare namespace Uno.UI.Interop {
    class Xaml {
    }
}
declare const MonoRuntime: Uno.UI.Interop.IMonoRuntime;
declare const WebAssemblyApp: Uno.UI.Interop.IWebAssemblyApp;
declare const UnoAppManifest: Uno.UI.IAppManifest;
declare const UnoDispatch: Uno.UI.Interop.IUnoDispatch;
