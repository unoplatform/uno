var Uno;
(function (Uno) {
    var Utils;
    (function (Utils) {
        class Clipboard {
            static setText(text) {
                const nav = navigator;
                if (nav.clipboard) {
                    // Use clipboard object when available
                    nav.clipboard.setText(text);
                }
                else {
                    // Hack when the clipboard is not available
                    const textarea = document.createElement("textarea");
                    textarea.value = text;
                    document.body.appendChild(textarea);
                    textarea.select();
                    document.execCommand("copy");
                    document.body.removeChild(textarea);
                }
                return "ok";
            }
        }
        Utils.Clipboard = Clipboard;
    })(Utils = Uno.Utils || (Uno.Utils = {}));
})(Uno || (Uno = {}));
var Windows;
(function (Windows) {
    var UI;
    (function (UI) {
        var Core;
        (function (Core) {
            /**
             * Support file for the Windows.UI.Core
             * */
            class CoreDispatcher {
                static init(isReady) {
                    MonoSupport.jsCallDispatcher.registerScope("CoreDispatcher", Windows.UI.Core.CoreDispatcher);
                    CoreDispatcher.initMethods();
                    CoreDispatcher._isReady = isReady;
                    CoreDispatcher._isIOS = /iPad|iPhone|iPod/.test(navigator.userAgent) && !window.MSStream;
                }
                /**
                 * Enqueues a core dispatcher callback on the javascript's event loop
                 *
                 * */
                static WakeUp() {
                    // Is there a Ready promise ?
                    if (CoreDispatcher._isReady) {
                        // Are we already waiting for a Ready promise ?
                        if (!CoreDispatcher._isWaitingReady) {
                            CoreDispatcher._isReady
                                .then(() => {
                                CoreDispatcher.InnerWakeUp();
                                CoreDispatcher._isReady = null;
                            });
                            CoreDispatcher._isWaitingReady = true;
                        }
                    }
                    else {
                        CoreDispatcher.InnerWakeUp();
                    }
                    return true;
                }
                static InnerWakeUp() {
                    if (CoreDispatcher._isIOS && CoreDispatcher._isFirstCall) {
                        //
                        // This is a workaround for the available call stack during the first 5 (?) seconds
                        // of the startup of an application. See https://github.com/mono/mono/issues/12357 for
                        // more details.
                        //
                        CoreDispatcher._isFirstCall = false;
                        console.debug("Detected iOS, delaying first CoreDispatched dispatch for 5s (see https://github.com/mono/mono/issues/12357)");
                        window.setTimeout(() => this.WakeUp(), 5000);
                    }
                    else {
                        window.setImmediate(() => {
                            try {
                                CoreDispatcher._coreDispatcherCallback();
                            }
                            catch (e) {
                                console.error(`Unhandled dispatcher exception: ${e} (${e.stack})`);
                                throw e;
                            }
                        });
                    }
                }
                static initMethods() {
                    if (Uno.UI.WindowManager.isHosted) {
                        console.debug("Hosted Mode: Skipping CoreDispatcher initialization ");
                    }
                    else {
                        if (!CoreDispatcher._coreDispatcherCallback) {
                            CoreDispatcher._coreDispatcherCallback = Module.mono_bind_static_method("[Uno] Windows.UI.Core.CoreDispatcher:DispatcherCallback");
                        }
                    }
                }
            }
            CoreDispatcher._isFirstCall = true;
            Core.CoreDispatcher = CoreDispatcher;
        })(Core = UI.Core || (UI.Core = {}));
    })(UI = Windows.UI || (Windows.UI = {}));
})(Windows || (Windows = {}));
var Uno;
(function (Uno) {
    var UI;
    (function (UI) {
        class HtmlDom {
            /**
             * Initialize various polyfills used by Uno
             */
            static initPolyfills() {
                this.isConnectedPolyfill();
            }
            static isConnectedPolyfill() {
                function get() {
                    // polyfill implementation
                    return document.contains(this);
                }
                (supported => {
                    if (!supported) {
                        Object.defineProperty(Node.prototype, "isConnected", { get });
                    }
                })("isConnected" in Node.prototype);
            }
        }
        UI.HtmlDom = HtmlDom;
    })(UI = Uno.UI || (Uno.UI = {}));
})(Uno || (Uno = {}));
var __awaiter = (this && this.__awaiter) || function (thisArg, _arguments, P, generator) {
    return new (P || (P = Promise))(function (resolve, reject) {
        function fulfilled(value) { try { step(generator.next(value)); } catch (e) { reject(e); } }
        function rejected(value) { try { step(generator["throw"](value)); } catch (e) { reject(e); } }
        function step(result) { result.done ? resolve(result.value) : new P(function (resolve) { resolve(result.value); }).then(fulfilled, rejected); }
        step((generator = generator.apply(thisArg, _arguments || [])).next());
    });
};
var Uno;
(function (Uno) {
    var Http;
    (function (Http) {
        class HttpClient {
            static send(config) {
                return __awaiter(this, void 0, void 0, function* () {
                    const params = {
                        method: config.method,
                        cache: config.cacheMode || "default",
                        headers: new Headers(config.headers)
                    };
                    if (config.payload) {
                        params.body = yield this.blobFromBase64(config.payload, config.payloadType);
                    }
                    try {
                        const response = yield fetch(config.url, params);
                        let responseHeaders = "";
                        response.headers.forEach((v, k) => responseHeaders += `${k}:${v}\n`);
                        const responseBlob = yield response.blob();
                        const responsePayload = responseBlob ? yield this.base64FromBlob(responseBlob) : "";
                        this.dispatchResponse(config.id, response.status, responseHeaders, responsePayload);
                    }
                    catch (error) {
                        this.dispatchError(config.id, `${error.message || error}`);
                        console.error(error);
                    }
                });
            }
            static blobFromBase64(base64, contentType) {
                return __awaiter(this, void 0, void 0, function* () {
                    contentType = contentType || "application/octet-stream";
                    const url = `data:${contentType};base64,${base64}`;
                    return yield (yield fetch(url)).blob();
                });
            }
            static base64FromBlob(blob) {
                return new Promise(resolve => {
                    const reader = new FileReader();
                    reader.onloadend = () => {
                        const dataUrl = reader.result;
                        const base64 = dataUrl.split(",", 2)[1];
                        resolve(base64);
                    };
                    reader.readAsDataURL(blob);
                });
            }
            static dispatchResponse(requestId, status, headers, payload) {
                this.initMethods();
                const requestIdStr = MonoRuntime.mono_string(requestId);
                const statusStr = MonoRuntime.mono_string("" + status);
                const headersStr = MonoRuntime.mono_string(headers);
                const payloadStr = MonoRuntime.mono_string(payload);
                MonoRuntime.call_method(this.dispatchResponseMethod, null, [requestIdStr, statusStr, headersStr, payloadStr]);
            }
            static dispatchError(requestId, error) {
                this.initMethods();
                const requestIdStr = MonoRuntime.mono_string(requestId);
                const errorStr = MonoRuntime.mono_string(error);
                MonoRuntime.call_method(this.dispatchErrorMethod, null, [requestIdStr, errorStr]);
            }
            static initMethods() {
                if (this.dispatchResponseMethod) {
                    return; // already initialized.
                }
                const asm = MonoRuntime.assembly_load("Uno.UI.Wasm");
                const httpClass = MonoRuntime.find_class(asm, "Uno.UI.Wasm", "WasmHttpHandler");
                this.dispatchResponseMethod = MonoRuntime.find_method(httpClass, "DispatchResponse", -1);
                this.dispatchErrorMethod = MonoRuntime.find_method(httpClass, "DispatchError", -1);
            }
        }
        Http.HttpClient = HttpClient;
    })(Http = Uno.Http || (Uno.Http = {}));
})(Uno || (Uno = {}));
var MonoSupport;
(function (MonoSupport) {
    /**
     * This class is used by https://github.com/mono/mono/blob/fa726d3ac7153d87ed187abd422faa4877f85bb5/sdks/wasm/dotnet_support.js#L88 to perform
     * unmarshaled invocation of javascript from .NET code.
     * */
    class jsCallDispatcher {
        /**
         * Registers a instance for a specified identier
         * @param identifier the scope name
         * @param instance the instance to use for the scope
         */
        static registerScope(identifier, instance) {
            jsCallDispatcher.registrations.set(identifier, instance);
        }
        static findJSFunction(identifier) {
            if (!jsCallDispatcher._isUnoRegistered) {
                jsCallDispatcher.registerScope("UnoStatic", Uno.UI.WindowManager);
                jsCallDispatcher._isUnoRegistered = true;
            }
            var knownMethod = jsCallDispatcher.methodMap.get(identifier);
            if (knownMethod) {
                return knownMethod;
            }
            const { ns, methodName } = jsCallDispatcher.parseIdentifier(identifier);
            var instance = jsCallDispatcher.registrations.get(ns);
            if (instance) {
                var boundMethod = instance[methodName].bind(instance);
                jsCallDispatcher.cacheMethod(identifier, boundMethod);
                return boundMethod;
            }
            else {
                throw `Unknown scope ${ns}`;
            }
        }
        /**
         * Parses the method identifier
         * @param identifier
         */
        static parseIdentifier(identifier) {
            var parts = identifier.split(':');
            const ns = parts[0];
            const methodName = parts[1];
            return { ns, methodName };
        }
        /**
         * Adds the a resolved method for a given identifier
         * @param identifier the findJSFunction identifier
         * @param boundMethod the method to call
         */
        static cacheMethod(identifier, boundMethod) {
            jsCallDispatcher.methodMap.set(identifier, boundMethod);
        }
    }
    jsCallDispatcher.registrations = new Map();
    jsCallDispatcher.methodMap = new Map();
    MonoSupport.jsCallDispatcher = jsCallDispatcher;
})(MonoSupport || (MonoSupport = {}));
// Export the DotNet helper for WebAssembly.JSInterop.InvokeJSUnmarshalled
window.DotNet = MonoSupport;
var Uno;
(function (Uno) {
    var UI;
    (function (UI) {
        class WindowManager {
            constructor(containerElementId, loadingElementId) {
                this.containerElementId = containerElementId;
                this.loadingElementId = loadingElementId;
                this.allActiveElementsById = {};
                this.initDom();
            }
            /**
             * Defines if the WindowManager is running in hosted mode, and should skip the
             * initialization of WebAssembly, use this mode in conjunction with the Uno.UI.WpfHost
             * to improve debuggability.
             */
            static get isHosted() {
                return WindowManager._isHosted;
            }
            /**
             * Defines if the WindowManager is responsible to raise the loading, loaded and unloaded events,
             * or if they are raised directly by the managed code to reduce interop.
             */
            static get isLoadEventsEnabled() {
                return WindowManager._isLoadEventsEnabled;
            }
            /**
                * Initialize the WindowManager
                * @param containerElementId The ID of the container element for the Xaml UI
                * @param loadingElementId The ID of the loading element to remove once ready
                */
            static init(localStoragePath, isHosted, isLoadEventsEnabled, containerElementId = "uno-body", loadingElementId = "uno-loading") {
                WindowManager._isHosted = isHosted;
                WindowManager._isLoadEventsEnabled = isLoadEventsEnabled;
                Windows.UI.Core.CoreDispatcher.init(WindowManager.buildReadyPromise());
                WindowManager.setupStorage(localStoragePath);
                this.current = new WindowManager(containerElementId, loadingElementId);
                MonoSupport.jsCallDispatcher.registerScope("Uno", this.current);
                this.current.init();
                return "ok";
            }
            /**
             * Builds a promise that will signal the ability for the dispatcher
             * to initiate work.
             * */
            static buildReadyPromise() {
                return new Promise(resolve => {
                    Promise.all([WindowManager.buildSplashScreen()]).then(() => resolve(true));
                });
            }
            /**
             * Build the splashscreen image eagerly
             * */
            static buildSplashScreen() {
                return new Promise(resolve => {
                    const img = new Image();
                    let loaded = false;
                    let loadingDone = () => {
                        if (!loaded) {
                            loaded = true;
                            if (img.width !== 0 && img.height !== 0) {
                                // Materialize the image content so it shows immediately
                                // even if the dispatcher is blocked thereafter by all
                                // the Uno initialization work. The resulting canvas is not used.
                                //
                                // If the image fails to load, setup the splashScreen anyways with the
                                // proper sample.
                                let canvas = document.createElement('canvas');
                                canvas.width = img.width;
                                canvas.height = img.height;
                                let ctx = canvas.getContext("2d");
                                ctx.drawImage(img, 0, 0);
                            }
                            if (document.readyState === "loading") {
                                document.addEventListener("DOMContentLoaded", () => {
                                    WindowManager.setupSplashScreen(img);
                                    resolve(true);
                                });
                            }
                            else {
                                WindowManager.setupSplashScreen(img);
                                resolve(true);
                            }
                        }
                    };
                    // Preload the splash screen so the image element
                    // created later on 
                    img.onload = loadingDone;
                    img.onerror = loadingDone;
                    img.src = String(UnoAppManifest.splashScreenImage);
                    // If there's no response, skip the loading
                    setTimeout(loadingDone, 2000);
                });
            }
            /**
                * Initialize the WindowManager
                * @param containerElementId The ID of the container element for the Xaml UI
                * @param loadingElementId The ID of the loading element to remove once ready
                */
            static initNative(pParams) {
                const params = WindowManagerInitParams.unmarshal(pParams);
                WindowManager.init(params.LocalFolderPath, params.IsHostedMode, params.IsLoadEventsEnabled);
                return true;
            }
            /**
             * Setup the storage persistence
             *
             * */
            static setupStorage(localStoragePath) {
                if (WindowManager.isHosted) {
                    console.debug("Hosted Mode: skipping IndexDB initialization");
                }
                else {
                    if (WindowManager.isIndexDBAvailable()) {
                        FS.mkdir(localStoragePath);
                        FS.mount(IDBFS, {}, localStoragePath);
                        FS.syncfs(true, err => {
                            if (err) {
                                console.error(`Error synchronizing filsystem from IndexDB: ${err}`);
                            }
                        });
                        window.addEventListener("beforeunload", () => WindowManager.synchronizeFileSystem());
                        setInterval(() => WindowManager.synchronizeFileSystem(), 10000);
                    }
                    else {
                        console.warn("IndexedDB is not available (private mode?), changed will not be persisted.");
                    }
                }
            }
            /**
             * Determine if IndexDB is available, some browsers and modes disable it.
             * */
            static isIndexDBAvailable() {
                try {
                    // IndexedDB may not be available in private mode
                    window.indexedDB;
                    return true;
                }
                catch (err) {
                    return false;
                }
            }
            /**
             * Synchronize the IDBFS memory cache back to IndexDB
             * */
            static synchronizeFileSystem() {
                FS.syncfs(err => {
                    if (err) {
                        console.error(`Error synchronizing filsystem from IndexDB: ${err}`);
                    }
                });
            }
            /**
                * Creates the UWP-compatible splash screen
                *
                */
            static setupSplashScreen(splashImage) {
                if (UnoAppManifest && UnoAppManifest.splashScreenImage) {
                    const loading = document.getElementById("loading");
                    if (loading) {
                        loading.remove();
                    }
                    const unoBody = document.getElementById("uno-body");
                    if (unoBody) {
                        const unoLoading = document.createElement("div");
                        unoLoading.id = "uno-loading";
                        if (UnoAppManifest.splashScreenColor) {
                            const body = document.getElementsByTagName("body")[0];
                            body.style.backgroundColor = UnoAppManifest.splashScreenColor;
                        }
                        splashImage.id = "uno-loading-splash";
                        splashImage.classList.add("uno-splash");
                        unoLoading.appendChild(splashImage);
                        unoBody.appendChild(unoLoading);
                    }
                }
            }
            /**
                * Reads the window's search parameters
                *
                */
            static findLaunchArguments() {
                if (typeof URLSearchParams === "function") {
                    return new URLSearchParams(window.location.search).toString();
                }
                else {
                    const queryIndex = document.location.search.indexOf('?');
                    if (queryIndex !== -1) {
                        return document.location.search.substring(queryIndex + 1);
                    }
                    return "";
                }
            }
            /**
                * Create a html DOM element representing a Xaml element.
                *
                * You need to call addView to connect it to the DOM.
                */
            createContent(contentDefinition) {
                this.createContentInternal(contentDefinition);
                return "ok";
            }
            /**
                * Create a html DOM element representing a Xaml element.
                *
                * You need to call addView to connect it to the DOM.
                */
            createContentNative(pParams) {
                const params = WindowManagerCreateContentParams.unmarshal(pParams);
                const def = {
                    id: params.HtmlId,
                    handle: params.Handle,
                    isFocusable: params.IsFocusable,
                    isFrameworkElement: params.IsFrameworkElement,
                    isSvg: params.IsSvg,
                    tagName: params.TagName,
                    type: params.Type,
                    classes: params.Classes
                };
                this.createContentInternal(def);
                return true;
            }
            createContentInternal(contentDefinition) {
                // Create the HTML element
                const element = contentDefinition.isSvg
                    ? document.createElementNS("http://www.w3.org/2000/svg", contentDefinition.tagName)
                    : document.createElement(contentDefinition.tagName);
                element.id = String(contentDefinition.id);
                element.setAttribute("XamlType", contentDefinition.type);
                element.setAttribute("XamlHandle", `${contentDefinition.handle}`);
                if (contentDefinition.isFrameworkElement) {
                    element.classList.add(WindowManager.unoUnarrangedClassName);
                }
                if (element.hasOwnProperty("tabindex")) {
                    element["tabindex"] = contentDefinition.isFocusable ? 0 : -1;
                }
                else {
                    element.setAttribute("tabindex", contentDefinition.isFocusable ? "0" : "-1");
                }
                if (contentDefinition) {
                    for (const className of contentDefinition.classes) {
                        element.classList.add(`uno-${className}`);
                    }
                }
                // Add the html element to list of elements
                this.allActiveElementsById[contentDefinition.id] = element;
            }
            getView(elementHandle) {
                const element = this.allActiveElementsById[elementHandle];
                if (!element) {
                    throw `Element id ${elementHandle} not found.`;
                }
                return element;
            }
            /**
                * Set a name for an element.
                *
                * This is mostly for diagnostic purposes.
                */
            setName(elementId, name) {
                this.setNameInternal(elementId, name);
                return "ok";
            }
            /**
                * Set a name for an element.
                *
                * This is mostly for diagnostic purposes.
                */
            setNameNative(pParam) {
                const params = WindowManagerSetNameParams.unmarshal(pParam);
                this.setNameInternal(params.HtmlId, params.Name);
                return true;
            }
            setNameInternal(elementId, name) {
                this.getView(elementId).setAttribute("XamlName", name);
            }
            /**
                * Set an attribute for an element.
                */
            setAttribute(elementId, attributes) {
                const htmlElement = this.getView(elementId);
                for (const name in attributes) {
                    if (attributes.hasOwnProperty(name)) {
                        htmlElement.setAttribute(name, attributes[name]);
                    }
                }
                return "ok";
            }
            /**
                * Set an attribute for an element.
                */
            setAttributeNative(pParams) {
                const params = WindowManagerSetAttributeParams.unmarshal(pParams);
                const htmlElement = this.getView(params.HtmlId);
                for (let i = 0; i < params.Pairs_Length; i += 2) {
                    htmlElement.setAttribute(params.Pairs[i], params.Pairs[i + 1]);
                }
                return true;
            }
            /**
                * Get an attribute for an element.
                */
            getAttribute(elementId, name) {
                return this.getView(elementId).getAttribute(name);
            }
            /**
                * Set a property for an element.
                */
            setProperty(elementId, properties) {
                const htmlElement = this.getView(elementId);
                for (const name in properties) {
                    if (properties.hasOwnProperty(name)) {
                        htmlElement[name] = properties[name];
                    }
                }
                return "ok";
            }
            /**
                * Set a property for an element.
                */
            setPropertyNative(pParams) {
                const params = WindowManagerSetPropertyParams.unmarshal(pParams);
                const htmlElement = this.getView(params.HtmlId);
                for (let i = 0; i < params.Pairs_Length; i += 2) {
                    htmlElement[params.Pairs[i]] = params.Pairs[i + 1];
                }
                return true;
            }
            /**
                * Get a property for an element.
                */
            getProperty(elementId, name) {
                const htmlElement = this.getView(elementId);
                return htmlElement[name] || "";
            }
            /**
                * Set the CSS style of a html element.
                *
                * To remove a value, set it to empty string.
                * @param styles A dictionary of styles to apply on html element.
                */
            setStyle(elementId, styles, setAsArranged = false) {
                const htmlElement = this.getView(elementId);
                for (const style in styles) {
                    if (styles.hasOwnProperty(style)) {
                        htmlElement.style.setProperty(style, styles[style]);
                    }
                }
                if (setAsArranged) {
                    htmlElement.classList.remove(WindowManager.unoUnarrangedClassName);
                }
                return "ok";
            }
            /**
            * Set the CSS style of a html element.
            *
            * To remove a value, set it to empty string.
            * @param styles A dictionary of styles to apply on html element.
            */
            setStyleNative(pParams) {
                const params = WindowManagerSetStylesParams.unmarshal(pParams);
                const htmlElement = this.getView(params.HtmlId);
                const elementStyle = htmlElement.style;
                const pairs = params.Pairs;
                for (let i = 0; i < params.Pairs_Length; i += 2) {
                    const key = pairs[i];
                    const value = pairs[i + 1];
                    elementStyle.setProperty(key, value);
                }
                if (params.SetAsArranged) {
                    htmlElement.classList.remove(WindowManager.unoUnarrangedClassName);
                }
                return true;
            }
            /**
            * Set a single CSS style of a html element
            *
            */
            setStyleDoubleNative(pParams) {
                const params = WindowManagerSetStyleDoubleParams.unmarshal(pParams);
                const htmlElement = this.getView(params.HtmlId);
                htmlElement.style.setProperty(params.Name, String(params.Value));
                return true;
            }
            /**
                * Set the CSS style of a html element.
                *
                * To remove a value, set it to empty string.
                * @param styles A dictionary of styles to apply on html element.
                */
            resetStyle(elementId, names) {
                this.resetStyleInternal(elementId, names);
                return "ok";
            }
            /**
                * Set the CSS style of a html element.
                *
                * To remove a value, set it to empty string.
                * @param styles A dictionary of styles to apply on html element.
                */
            resetStyleNative(pParams) {
                const params = WindowManagerResetStyleParams.unmarshal(pParams);
                this.resetStyleInternal(params.HtmlId, params.Styles);
                return true;
            }
            resetStyleInternal(elementId, names) {
                const htmlElement = this.getView(elementId);
                for (const name of names) {
                    htmlElement.style.setProperty(name, "");
                }
            }
            /**
            * Arrange and clips a native elements
            *
            */
            arrangeElementNative(pParams) {
                const params = WindowManagerArrangeElementParams.unmarshal(pParams);
                const htmlElement = this.getView(params.HtmlId);
                var style = htmlElement.style;
                style.position = "absolute";
                style.top = params.Top + "px";
                style.left = params.Left + "px";
                style.width = params.Width == NaN ? "auto" : params.Width + "px";
                style.height = params.Height == NaN ? "auto" : params.Height + "px";
                if (params.Clip) {
                    style.clip = `rect(${params.ClipTop}px, ${params.ClipRight}px, ${params.ClipBottom}px, ${params.ClipLeft}px)`;
                }
                else {
                    style.clip = "";
                }
                htmlElement.classList.remove(WindowManager.unoUnarrangedClassName);
                return true;
            }
            /**
            * Sets the transform matrix of an element
            *
            */
            setElementTransformNative(pParams) {
                const params = WindowManagerSetElementTransformParams.unmarshal(pParams);
                const htmlElement = this.getView(params.HtmlId);
                var style = htmlElement.style;
                style.transform = `matrix(${params.M11},${params.M12},${params.M21},${params.M22},${params.M31},${params.M32})`;
                htmlElement.classList.remove(WindowManager.unoUnarrangedClassName);
                return true;
            }
            /**
                * Load the specified URL into a new tab or window
                * @param url URL to load
                * @returns "True" or "False", depending on whether a new window could be opened or not
                */
            open(url) {
                const newWindow = window.open(url, "_blank");
                return newWindow != null
                    ? "True"
                    : "False";
            }
            /**
                * Issue a browser alert to user
                * @param message message to display
                */
            alert(message) {
                window.alert(message);
                return "ok";
            }
            /**
                * Sets the browser window title
                * @param message the new title
                */
            setWindowTitle(title) {
                document.title = title || UnoAppManifest.displayName;
                return "ok";
            }
            /**
                * Gets the currently set browser window title
                */
            getWindowTitle() {
                return document.title || UnoAppManifest.displayName;
            }
            /**
                * Add an event handler to a html element.
                *
                * @param eventName The name of the event
                * @param onCapturePhase true means "on trickle down" (going down to target), false means "on bubble up" (bubbling back to ancestors). Default is false.
                */
            registerEventOnView(elementId, eventName, onCapturePhase = false, eventFilterName, eventExtractorName) {
                this.registerEventOnViewInternal(elementId, eventName, onCapturePhase, eventFilterName, eventExtractorName);
                return "ok";
            }
            /**
                * Add an event handler to a html element.
                *
                * @param eventName The name of the event
                * @param onCapturePhase true means "on trickle down", false means "on bubble up". Default is false.
                */
            registerEventOnViewNative(pParams) {
                const params = WindowManagerRegisterEventOnViewParams.unmarshal(pParams);
                this.registerEventOnViewInternal(params.HtmlId, params.EventName, params.OnCapturePhase, params.EventFilterName, params.EventExtractorName);
                return true;
            }
            /**
                * Add an event handler to a html element.
                *
                * @param eventName The name of the event
                * @param onCapturePhase true means "on trickle down", false means "on bubble up". Default is false.
                */
            registerEventOnViewInternal(elementId, eventName, onCapturePhase = false, eventFilterName, eventExtractorName) {
                const htmlElement = this.getView(elementId);
                const eventFilter = this.getEventFilter(eventFilterName);
                const eventExtractor = this.getEventExtractor(eventExtractorName);
                const eventHandler = (event) => {
                    if (eventFilter && !eventFilter(event)) {
                        return;
                    }
                    const eventPayload = eventExtractor
                        ? `${eventExtractor(event)}`
                        : "";
                    var handled = this.dispatchEvent(htmlElement, eventName, eventPayload);
                    if (handled) {
                        event.stopPropagation();
                    }
                };
                htmlElement.addEventListener(eventName, eventHandler, onCapturePhase);
            }
            /**
             * left pointer event filter to be used with registerEventOnView
             * @param evt
             */
            leftPointerEventFilter(evt) {
                return evt ? evt.eventPhase === 2 || evt.eventPhase === 3 && (!evt.button || evt.button === 0) : false;
            }
            /**
             * default event filter to be used with registerEventOnView to
             * use for most routed events
             * @param evt
             */
            defaultEventFilter(evt) {
                return evt ? evt.eventPhase === 2 || evt.eventPhase === 3 : false;
            }
            /**
             * Gets the event filter function. See UIElement.HtmlEventFilter
             * @param eventFilterName an event filter name.
             */
            getEventFilter(eventFilterName) {
                if (eventFilterName) {
                    switch (eventFilterName) {
                        case "LeftPointerEventFilter":
                            return this.leftPointerEventFilter;
                        case "Default":
                            return this.defaultEventFilter;
                    }
                    throw `Event filter ${eventFilterName} is not supported`;
                }
                return null;
            }
            /**
             * pointer event extractor to be used with registerEventOnView
             * @param evt
             */
            pointerEventExtractor(evt) {
                return evt
                    ? `${evt.pointerId};${evt.clientX};${evt.clientY};${(evt.ctrlKey ? "1" : "0")};${(evt.shiftKey ? "1" : "0")};${evt.button};${evt.pointerType}`
                    : "";
            }
            /**
             * keyboard event extractor to be used with registerEventOnView
             * @param evt
             */
            keyboardEventExtractor(evt) {
                return (evt instanceof KeyboardEvent) ? evt.key : "0";
            }
            /**
             * tapped (mouse clicked / double clicked) event extractor to be used with registerEventOnView
             * @param evt
             */
            tappedEventExtractor(evt) {
                return evt
                    ? `0;${evt.clientX};${evt.clientY};${(evt.ctrlKey ? "1" : "0")};${(evt.shiftKey ? "1" : "0")};${evt.button};mouse`
                    : "";
            }
            /**
             * tapped (mouse clicked / double clicked) event extractor to be used with registerEventOnView
             * @param evt
             */
            focusEventExtractor(evt) {
                if (evt) {
                    const targetElement = evt.target;
                    if (targetElement) {
                        const targetXamlHandle = targetElement.getAttribute("XamlHandle");
                        if (targetXamlHandle) {
                            return `${targetXamlHandle}`;
                        }
                    }
                }
                return "";
            }
            /**
             * Gets the event extractor function. See UIElement.HtmlEventExtractor
             * @param eventExtractorName an event extractor name.
             */
            getEventExtractor(eventExtractorName) {
                if (eventExtractorName) {
                    switch (eventExtractorName) {
                        case "PointerEventExtractor":
                            return this.pointerEventExtractor;
                        case "KeyboardEventExtractor":
                            return this.keyboardEventExtractor;
                        case "TappedEventExtractor":
                            return this.tappedEventExtractor;
                        case "FocusEventExtractor":
                            return this.focusEventExtractor;
                    }
                    throw `Event filter ${eventExtractorName} is not supported`;
                }
                return null;
            }
            /**
                * Set or replace the root content element.
                */
            setRootContent(elementId) {
                if (this.rootContent && Number(this.rootContent.id) === elementId) {
                    return null; // nothing to do
                }
                if (this.rootContent) {
                    // Remove existing
                    this.containerElement.removeChild(this.rootContent);
                    if (WindowManager.isLoadEventsEnabled) {
                        this.dispatchEvent(this.rootContent, "unloaded");
                    }
                    this.rootContent.classList.remove(WindowManager.unoRootClassName);
                }
                if (!elementId) {
                    return null;
                }
                // set new root
                const newRootElement = this.getView(elementId);
                newRootElement.classList.add(WindowManager.unoRootClassName);
                this.rootContent = newRootElement;
                if (WindowManager.isLoadEventsEnabled) {
                    this.dispatchEvent(this.rootContent, "loading");
                }
                this.containerElement.appendChild(this.rootContent);
                if (WindowManager.isLoadEventsEnabled) {
                    this.dispatchEvent(this.rootContent, "loaded");
                }
                newRootElement.classList.remove(WindowManager.unoUnarrangedClassName); // patch because root is not measured/arranged
                this.resize();
                return "ok";
            }
            /**
                * Set a view as a child of another one.
                *
                * "Loading" & "Loaded" events will be raised if necessary.
                *
                * @param index Position in children list. Appended at end if not specified.
                */
            addView(parentId, childId, index) {
                this.addViewInternal(parentId, childId, index);
                return "ok";
            }
            /**
                * Set a view as a child of another one.
                *
                * "Loading" & "Loaded" events will be raised if necessary.
                *
                * @param pParams Pointer to a WindowManagerAddViewParams native structure.
                */
            addViewNative(pParams) {
                const params = WindowManagerAddViewParams.unmarshal(pParams);
                this.addViewInternal(params.HtmlId, params.ChildView, params.Index != -1 ? params.Index : null);
                return true;
            }
            addViewInternal(parentId, childId, index) {
                const parentElement = this.getView(parentId);
                const childElement = this.getView(childId);
                let shouldRaiseLoadEvents = false;
                if (WindowManager.isLoadEventsEnabled) {
                    const alreadyLoaded = this.getIsConnectedToRootElement(childElement);
                    shouldRaiseLoadEvents = !alreadyLoaded && this.getIsConnectedToRootElement(parentElement);
                    if (shouldRaiseLoadEvents) {
                        this.dispatchEvent(childElement, "loading");
                    }
                }
                if (index && index < parentElement.childElementCount) {
                    const insertBeforeElement = parentElement.children[index];
                    parentElement.insertBefore(childElement, insertBeforeElement);
                }
                else {
                    parentElement.appendChild(childElement);
                }
                if (shouldRaiseLoadEvents) {
                    this.dispatchEvent(childElement, "loaded");
                }
            }
            /**
                * Remove a child from a parent element.
                *
                * "Unloading" & "Unloaded" events will be raised if necessary.
                */
            removeView(parentId, childId) {
                this.removeViewInternal(parentId, childId);
                return "ok";
            }
            /**
                * Remove a child from a parent element.
                *
                * "Unloading" & "Unloaded" events will be raised if necessary.
                */
            removeViewNative(pParams) {
                const params = WindowManagerRemoveViewParams.unmarshal(pParams);
                this.removeViewInternal(params.HtmlId, params.ChildView);
                return true;
            }
            removeViewInternal(parentId, childId) {
                const parentElement = this.getView(parentId);
                const childElement = this.getView(childId);
                const shouldRaiseLoadEvents = WindowManager.isLoadEventsEnabled
                    && this.getIsConnectedToRootElement(childElement);
                parentElement.removeChild(childElement);
                // Mark the element as unarranged, so if it gets measured while being
                // disconnected from the root element, it won't be visible.
                childElement.classList.add(WindowManager.unoUnarrangedClassName);
                if (shouldRaiseLoadEvents) {
                    this.dispatchEvent(childElement, "unloaded");
                }
            }
            /**
                * Destroy a html element.
                *
                * The element won't be available anymore. Usually indicate the managed
                * version has been scavenged by the GC.
                */
            destroyView(elementId) {
                this.destroyViewInternal(elementId);
                return "ok";
            }
            /**
                * Destroy a html element.
                *
                * The element won't be available anymore. Usually indicate the managed
                * version has been scavenged by the GC.
                */
            destroyViewNative(pParams) {
                const params = WindowManagerDestroyViewParams.unmarshal(pParams);
                this.destroyViewInternal(params.HtmlId);
                return true;
            }
            destroyViewInternal(elementId) {
                const htmlElement = this.getView(elementId);
                if (htmlElement.parentElement) {
                    htmlElement.parentElement.removeChild(htmlElement);
                }
                delete this.allActiveElementsById[elementId];
            }
            getBoundingClientRect(elementId) {
                const bounds = this.getView(elementId).getBoundingClientRect();
                return `${bounds.left};${bounds.top};${bounds.right - bounds.left};${bounds.bottom - bounds.top}`;
            }
            getBBox(elementId) {
                const bbox = this.getBBoxInternal(elementId);
                return `${bbox.x};${bbox.y};${bbox.width};${bbox.height}`;
            }
            getBBoxNative(pParams, pReturn) {
                const params = WindowManagerGetBBoxParams.unmarshal(pParams);
                const bbox = this.getBBoxInternal(params.HtmlId);
                const ret = new WindowManagerGetBBoxReturn();
                ret.X = bbox.x;
                ret.Y = bbox.y;
                ret.Width = bbox.width;
                ret.Height = bbox.height;
                ret.marshal(pReturn);
                return true;
            }
            getBBoxInternal(elementId) {
                return this.getView(elementId).getBBox();
            }
            /**
                * Use the Html engine to measure the element using specified constraints.
                *
                * @param maxWidth string containing width in pixels. Empty string means infinite.
                * @param maxHeight string containing height in pixels. Empty string means infinite.
                */
            measureView(viewId, maxWidth, maxHeight) {
                const ret = this.measureViewInternal(Number(viewId), maxWidth ? Number(maxWidth) : NaN, maxHeight ? Number(maxHeight) : NaN);
                return `${ret[0]};${ret[1]}`;
            }
            /**
                * Use the Html engine to measure the element using specified constraints.
                *
                * @param maxWidth string containing width in pixels. Empty string means infinite.
                * @param maxHeight string containing height in pixels. Empty string means infinite.
                */
            measureViewNative(pParams, pReturn) {
                const params = WindowManagerMeasureViewParams.unmarshal(pParams);
                const ret = this.measureViewInternal(params.HtmlId, params.AvailableWidth, params.AvailableHeight);
                const ret2 = new WindowManagerMeasureViewReturn();
                ret2.DesiredWidth = ret[0];
                ret2.DesiredHeight = ret[1];
                ret2.marshal(pReturn);
                return true;
            }
            measureViewInternal(viewId, maxWidth, maxHeight) {
                const element = this.getView(viewId);
                const elementStyle = element.style;
                const originalStyleCssText = elementStyle.cssText;
                try {
                    if (!element.isConnected) {
                        // If the element is not connected to the DOM, we need it
                        // to be connected for the measure to provide a meaningful value.
                        let unconnectedRoot = element;
                        while (unconnectedRoot.parentElement) {
                            // Need to find the top most "unconnected" parent
                            // of this element
                            unconnectedRoot = unconnectedRoot.parentElement;
                        }
                        this.containerElement.appendChild(unconnectedRoot);
                    }
                    var updatedStyles = {};
                    for (var i = 0; i < elementStyle.length; i++) {
                        const key = elementStyle[i];
                        updatedStyles[key] = elementStyle.getPropertyValue(key);
                    }
                    updatedStyles.width = "";
                    updatedStyles.height = "";
                    // This is required for an unconstrained measure (otherwise the parents size is taken into account)
                    updatedStyles.position = "fixed";
                    updatedStyles.maxWidth = Number.isFinite(maxWidth) ? maxWidth + "px" : "";
                    updatedStyles.maxHeight = Number.isFinite(maxHeight) ? maxHeight + "px" : "";
                    var updatedStyleString = "";
                    for (var key in updatedStyles) {
                        updatedStyleString += key + ": " + updatedStyles[key] + "; ";
                    }
                    elementStyle.cssText = updatedStyleString;
                    if (element instanceof HTMLImageElement) {
                        const imgElement = element;
                        return [imgElement.naturalWidth, imgElement.naturalHeight];
                    }
                    else {
                        const offsetWidth = element.offsetWidth;
                        const offsetHeight = element.offsetHeight;
                        const resultWidth = offsetWidth ? offsetWidth : element.clientWidth;
                        const resultHeight = offsetHeight ? offsetHeight : element.clientHeight;
                        /* +0.5 is added to take rounding into account */
                        return [resultWidth + 0.5, resultHeight];
                    }
                }
                finally {
                    elementStyle.cssText = originalStyleCssText;
                }
            }
            setImageRawData(viewId, dataPtr, width, height) {
                const element = this.getView(viewId);
                if (element.tagName.toUpperCase() === "IMG") {
                    const imgElement = element;
                    const rawCanvas = document.createElement("canvas");
                    rawCanvas.width = width;
                    rawCanvas.height = height;
                    const ctx = rawCanvas.getContext("2d");
                    const imgData = ctx.createImageData(width, height);
                    const bufferSize = width * height * 4;
                    for (let i = 0; i < bufferSize; i += 4) {
                        imgData.data[i + 0] = Module.HEAPU8[dataPtr + i + 2];
                        imgData.data[i + 1] = Module.HEAPU8[dataPtr + i + 1];
                        imgData.data[i + 2] = Module.HEAPU8[dataPtr + i + 0];
                        imgData.data[i + 3] = Module.HEAPU8[dataPtr + i + 3];
                    }
                    ctx.putImageData(imgData, 0, 0);
                    imgElement.src = rawCanvas.toDataURL();
                    return "ok";
                }
            }
            /**
             * Sets the provided image with a mono-chrome version of the provided url.
             * @param viewId the image to manipulate
             * @param url the source image
             * @param color the color to apply to the monochrome pixels
             */
            setImageAsMonochrome(viewId, url, color) {
                const element = this.getView(viewId);
                if (element.tagName.toUpperCase() === "IMG") {
                    const imgElement = element;
                    var img = new Image();
                    img.onload = buildMonochromeImage;
                    img.src = url;
                    function buildMonochromeImage() {
                        // create a colored version of img
                        const c = document.createElement('canvas');
                        const ctx = c.getContext('2d');
                        c.width = img.width;
                        c.height = img.height;
                        ctx.drawImage(img, 0, 0);
                        ctx.globalCompositeOperation = 'source-atop';
                        ctx.fillStyle = color;
                        ctx.fillRect(0, 0, img.width, img.height);
                        ctx.globalCompositeOperation = 'source-over';
                        imgElement.src = c.toDataURL();
                    }
                    return "ok";
                }
                else {
                    throw `setImageAsMonochrome: Element id ${viewId} is not an Img.`;
                }
            }
            setPointerCapture(viewId, pointerId) {
                this.getView(viewId).setPointerCapture(pointerId);
                return "ok";
            }
            releasePointerCapture(viewId, pointerId) {
                this.getView(viewId).releasePointerCapture(pointerId);
                return "ok";
            }
            focusView(elementId) {
                const htmlElement = this.getView(elementId);
                if (!(htmlElement instanceof HTMLElement)) {
                    throw `Element id ${elementId} is not focusable.`;
                }
                htmlElement.focus();
                return "ok";
            }
            /**
                * Set the Html content for an element.
                *
                * Those html elements won't be available as XamlElement in managed code.
                * WARNING: you should avoid mixing this and `addView` for the same element.
                */
            setHtmlContent(viewId, html) {
                this.setHtmlContentInternal(viewId, html);
                return "ok";
            }
            /**
                * Set the Html content for an element.
                *
                * Those html elements won't be available as XamlElement in managed code.
                * WARNING: you should avoid mixing this and `addView` for the same element.
                */
            setHtmlContentNative(pParams) {
                const params = WindowManagerSetContentHtmlParams.unmarshal(pParams);
                this.setHtmlContentInternal(params.HtmlId, params.Html);
                return true;
            }
            setHtmlContentInternal(viewId, html) {
                this.getView(viewId).innerHTML = html;
            }
            /**
                * Remove the loading indicator.
                *
                * In a future version it will also handle the splashscreen.
                */
            activate() {
                this.removeLoading();
                return "ok";
            }
            init() {
                if (UnoAppManifest.displayName) {
                    document.title = UnoAppManifest.displayName;
                }
            }
            static initMethods() {
                if (WindowManager.isHosted) {
                    console.debug("Hosted Mode: Skipping MonoRuntime initialization ");
                }
                else {
                    if (!WindowManager.resizeMethod) {
                        WindowManager.resizeMethod = Module.mono_bind_static_method("[Uno.UI] Windows.UI.Xaml.Window:Resize");
                    }
                    if (!WindowManager.dispatchEventMethod) {
                        WindowManager.dispatchEventMethod = Module.mono_bind_static_method("[Uno.UI] Windows.UI.Xaml.UIElement:DispatchEvent");
                    }
                }
            }
            initDom() {
                this.containerElement = document.getElementById(this.containerElementId);
                if (!this.containerElement) {
                    // If not found, we simply create a new one.
                    this.containerElement = document.createElement("div");
                    document.body.appendChild(this.containerElement);
                }
                window.addEventListener("resize", x => this.resize());
            }
            removeLoading() {
                if (!this.loadingElementId) {
                    return;
                }
                const element = document.getElementById(this.loadingElementId);
                if (element) {
                    element.parentElement.removeChild(element);
                }
                // UWP Window's default background is white.
                const body = document.getElementsByTagName("body")[0];
                body.style.backgroundColor = "#fff";
            }
            resize() {
                if (WindowManager.isHosted) {
                    UnoDispatch.resize(`${document.documentElement.clientWidth};${document.documentElement.clientHeight}`);
                }
                else {
                    WindowManager.resizeMethod(document.documentElement.clientWidth, document.documentElement.clientHeight);
                }
            }
            dispatchEvent(element, eventName, eventPayload = null) {
                const htmlId = Number(element.getAttribute("XamlHandle"));
                // console.debug(`${element.getAttribute("id")}: Raising event ${eventName}.`);
                if (!htmlId) {
                    throw `No attribute XamlHandle on element ${element}. Can't raise event.`;
                }
                if (WindowManager.isHosted) {
                    // Dispatch to the C# backed UnoDispatch class. Events propagated
                    // this way always succeed because synchronous calls are not possible
                    // between the host and the browser, unlike wasm.
                    UnoDispatch.dispatch(String(htmlId), eventName, eventPayload);
                    return true;
                }
                else {
                    return WindowManager.dispatchEventMethod(htmlId, eventName, eventPayload || "");
                }
            }
            getIsConnectedToRootElement(element) {
                const rootElement = this.rootContent;
                if (!rootElement) {
                    return false;
                }
                return rootElement === element || rootElement.contains(element);
            }
        }
        WindowManager._isHosted = false;
        WindowManager._isLoadEventsEnabled = false;
        WindowManager.unoRootClassName = "uno-root-element";
        WindowManager.unoUnarrangedClassName = "uno-unarranged";
        WindowManager._cctor = (() => {
            WindowManager.initMethods();
            UI.HtmlDom.initPolyfills();
        })();
        UI.WindowManager = WindowManager;
        if (typeof define === "function") {
            define(["AppManifest"], () => {
            });
        }
        else {
            throw `The Uno.Wasm.Boostrap is not up to date, please upgrade to a later version`;
        }
    })(UI = Uno.UI || (Uno.UI = {}));
})(Uno || (Uno = {}));
// Ensure the "Uno" namespace is availablle globally
window.Uno = Uno;
/* TSBindingsGenerator Generated code -- this code is regenerated on each build */
class WindowManagerAddViewParams {
    static unmarshal(pData) {
        let ret = new WindowManagerAddViewParams();
        {
            ret.HtmlId = Number(Module.getValue(pData + 0, "*"));
        }
        {
            ret.ChildView = Number(Module.getValue(pData + 4, "*"));
        }
        {
            ret.Index = Number(Module.getValue(pData + 8, "i32"));
        }
        return ret;
    }
}
/* TSBindingsGenerator Generated code -- this code is regenerated on each build */
class WindowManagerArrangeElementParams {
    static unmarshal(pData) {
        let ret = new WindowManagerArrangeElementParams();
        {
            ret.Top = Number(Module.getValue(pData + 0, "double"));
        }
        {
            ret.Left = Number(Module.getValue(pData + 8, "double"));
        }
        {
            ret.Width = Number(Module.getValue(pData + 16, "double"));
        }
        {
            ret.Height = Number(Module.getValue(pData + 24, "double"));
        }
        {
            ret.ClipTop = Number(Module.getValue(pData + 32, "double"));
        }
        {
            ret.ClipLeft = Number(Module.getValue(pData + 40, "double"));
        }
        {
            ret.ClipBottom = Number(Module.getValue(pData + 48, "double"));
        }
        {
            ret.ClipRight = Number(Module.getValue(pData + 56, "double"));
        }
        {
            ret.HtmlId = Number(Module.getValue(pData + 64, "*"));
        }
        {
            ret.Clip = Boolean(Module.getValue(pData + 68, "i32"));
        }
        return ret;
    }
}
/* TSBindingsGenerator Generated code -- this code is regenerated on each build */
class WindowManagerCreateContentParams {
    static unmarshal(pData) {
        let ret = new WindowManagerCreateContentParams();
        {
            ret.HtmlId = Number(Module.getValue(pData + 0, "*"));
        }
        {
            var ptr = Module.getValue(pData + 4, "*");
            if (ptr !== 0) {
                ret.TagName = String(Module.UTF8ToString(ptr));
            }
            else {
                ret.TagName = null;
            }
        }
        {
            ret.Handle = Number(Module.getValue(pData + 8, "*"));
        }
        {
            var ptr = Module.getValue(pData + 12, "*");
            if (ptr !== 0) {
                ret.Type = String(Module.UTF8ToString(ptr));
            }
            else {
                ret.Type = null;
            }
        }
        {
            ret.IsSvg = Boolean(Module.getValue(pData + 16, "i32"));
        }
        {
            ret.IsFrameworkElement = Boolean(Module.getValue(pData + 20, "i32"));
        }
        {
            ret.IsFocusable = Boolean(Module.getValue(pData + 24, "i32"));
        }
        {
            ret.Classes_Length = Number(Module.getValue(pData + 28, "i32"));
        }
        {
            var pArray = Module.getValue(pData + 32, "*");
            if (pArray !== 0) {
                ret.Classes = new Array();
                for (var i = 0; i < ret.Classes_Length; i++) {
                    var value = Module.getValue(pArray + i * 4, "*");
                    if (value !== 0) {
                        ret.Classes.push(String(MonoRuntime.conv_string(value)));
                    }
                    else {
                        ret.Classes.push(null);
                    }
                }
            }
            else {
                ret.Classes = null;
            }
        }
        return ret;
    }
}
/* TSBindingsGenerator Generated code -- this code is regenerated on each build */
class WindowManagerDestroyViewParams {
    static unmarshal(pData) {
        let ret = new WindowManagerDestroyViewParams();
        {
            ret.HtmlId = Number(Module.getValue(pData + 0, "*"));
        }
        return ret;
    }
}
/* TSBindingsGenerator Generated code -- this code is regenerated on each build */
class WindowManagerGetBBoxParams {
    static unmarshal(pData) {
        let ret = new WindowManagerGetBBoxParams();
        {
            ret.HtmlId = Number(Module.getValue(pData + 0, "*"));
        }
        return ret;
    }
}
/* TSBindingsGenerator Generated code -- this code is regenerated on each build */
class WindowManagerGetBBoxReturn {
    marshal(pData) {
        Module.setValue(pData + 0, this.X, "double");
        Module.setValue(pData + 8, this.Y, "double");
        Module.setValue(pData + 16, this.Width, "double");
        Module.setValue(pData + 24, this.Height, "double");
    }
}
/* TSBindingsGenerator Generated code -- this code is regenerated on each build */
class WindowManagerInitParams {
    static unmarshal(pData) {
        let ret = new WindowManagerInitParams();
        {
            var ptr = Module.getValue(pData + 0, "*");
            if (ptr !== 0) {
                ret.LocalFolderPath = String(Module.UTF8ToString(ptr));
            }
            else {
                ret.LocalFolderPath = null;
            }
        }
        {
            ret.IsHostedMode = Boolean(Module.getValue(pData + 4, "i32"));
        }
        {
            ret.IsLoadEventsEnabled = Boolean(Module.getValue(pData + 8, "i32"));
        }
        return ret;
    }
}
/* TSBindingsGenerator Generated code -- this code is regenerated on each build */
class WindowManagerMeasureViewParams {
    static unmarshal(pData) {
        let ret = new WindowManagerMeasureViewParams();
        {
            ret.HtmlId = Number(Module.getValue(pData + 0, "*"));
        }
        {
            ret.AvailableWidth = Number(Module.getValue(pData + 8, "double"));
        }
        {
            ret.AvailableHeight = Number(Module.getValue(pData + 16, "double"));
        }
        return ret;
    }
}
/* TSBindingsGenerator Generated code -- this code is regenerated on each build */
class WindowManagerMeasureViewReturn {
    marshal(pData) {
        Module.setValue(pData + 0, this.DesiredWidth, "double");
        Module.setValue(pData + 8, this.DesiredHeight, "double");
    }
}
/* TSBindingsGenerator Generated code -- this code is regenerated on each build */
class WindowManagerRegisterEventOnViewParams {
    static unmarshal(pData) {
        let ret = new WindowManagerRegisterEventOnViewParams();
        {
            ret.HtmlId = Number(Module.getValue(pData + 0, "*"));
        }
        {
            var ptr = Module.getValue(pData + 4, "*");
            if (ptr !== 0) {
                ret.EventName = String(Module.UTF8ToString(ptr));
            }
            else {
                ret.EventName = null;
            }
        }
        {
            ret.OnCapturePhase = Boolean(Module.getValue(pData + 8, "i32"));
        }
        {
            var ptr = Module.getValue(pData + 12, "*");
            if (ptr !== 0) {
                ret.EventFilterName = String(Module.UTF8ToString(ptr));
            }
            else {
                ret.EventFilterName = null;
            }
        }
        {
            var ptr = Module.getValue(pData + 16, "*");
            if (ptr !== 0) {
                ret.EventExtractorName = String(Module.UTF8ToString(ptr));
            }
            else {
                ret.EventExtractorName = null;
            }
        }
        return ret;
    }
}
/* TSBindingsGenerator Generated code -- this code is regenerated on each build */
class WindowManagerRemoveViewParams {
    static unmarshal(pData) {
        let ret = new WindowManagerRemoveViewParams();
        {
            ret.HtmlId = Number(Module.getValue(pData + 0, "*"));
        }
        {
            ret.ChildView = Number(Module.getValue(pData + 4, "*"));
        }
        return ret;
    }
}
/* TSBindingsGenerator Generated code -- this code is regenerated on each build */
class WindowManagerResetStyleParams {
    static unmarshal(pData) {
        let ret = new WindowManagerResetStyleParams();
        {
            ret.HtmlId = Number(Module.getValue(pData + 0, "*"));
        }
        {
            ret.Styles_Length = Number(Module.getValue(pData + 4, "i32"));
        }
        {
            var pArray = Module.getValue(pData + 8, "*");
            if (pArray !== 0) {
                ret.Styles = new Array();
                for (var i = 0; i < ret.Styles_Length; i++) {
                    var value = Module.getValue(pArray + i * 4, "*");
                    if (value !== 0) {
                        ret.Styles.push(String(MonoRuntime.conv_string(value)));
                    }
                    else {
                        ret.Styles.push(null);
                    }
                }
            }
            else {
                ret.Styles = null;
            }
        }
        return ret;
    }
}
/* TSBindingsGenerator Generated code -- this code is regenerated on each build */
class WindowManagerSetAttributeParams {
    static unmarshal(pData) {
        let ret = new WindowManagerSetAttributeParams();
        {
            ret.HtmlId = Number(Module.getValue(pData + 0, "*"));
        }
        {
            ret.Pairs_Length = Number(Module.getValue(pData + 4, "i32"));
        }
        {
            var pArray = Module.getValue(pData + 8, "*");
            if (pArray !== 0) {
                ret.Pairs = new Array();
                for (var i = 0; i < ret.Pairs_Length; i++) {
                    var value = Module.getValue(pArray + i * 4, "*");
                    if (value !== 0) {
                        ret.Pairs.push(String(MonoRuntime.conv_string(value)));
                    }
                    else {
                        ret.Pairs.push(null);
                    }
                }
            }
            else {
                ret.Pairs = null;
            }
        }
        return ret;
    }
}
/* TSBindingsGenerator Generated code -- this code is regenerated on each build */
class WindowManagerSetContentHtmlParams {
    static unmarshal(pData) {
        let ret = new WindowManagerSetContentHtmlParams();
        {
            ret.HtmlId = Number(Module.getValue(pData + 0, "*"));
        }
        {
            var ptr = Module.getValue(pData + 4, "*");
            if (ptr !== 0) {
                ret.Html = String(Module.UTF8ToString(ptr));
            }
            else {
                ret.Html = null;
            }
        }
        return ret;
    }
}
/* TSBindingsGenerator Generated code -- this code is regenerated on each build */
class WindowManagerSetElementTransformParams {
    static unmarshal(pData) {
        let ret = new WindowManagerSetElementTransformParams();
        {
            ret.M11 = Number(Module.getValue(pData + 0, "double"));
        }
        {
            ret.M12 = Number(Module.getValue(pData + 8, "double"));
        }
        {
            ret.M21 = Number(Module.getValue(pData + 16, "double"));
        }
        {
            ret.M22 = Number(Module.getValue(pData + 24, "double"));
        }
        {
            ret.M31 = Number(Module.getValue(pData + 32, "double"));
        }
        {
            ret.M32 = Number(Module.getValue(pData + 40, "double"));
        }
        {
            ret.HtmlId = Number(Module.getValue(pData + 48, "*"));
        }
        return ret;
    }
}
/* TSBindingsGenerator Generated code -- this code is regenerated on each build */
class WindowManagerSetNameParams {
    static unmarshal(pData) {
        let ret = new WindowManagerSetNameParams();
        {
            ret.HtmlId = Number(Module.getValue(pData + 0, "*"));
        }
        {
            var ptr = Module.getValue(pData + 4, "*");
            if (ptr !== 0) {
                ret.Name = String(Module.UTF8ToString(ptr));
            }
            else {
                ret.Name = null;
            }
        }
        return ret;
    }
}
/* TSBindingsGenerator Generated code -- this code is regenerated on each build */
class WindowManagerSetPropertyParams {
    static unmarshal(pData) {
        let ret = new WindowManagerSetPropertyParams();
        {
            ret.HtmlId = Number(Module.getValue(pData + 0, "*"));
        }
        {
            ret.Pairs_Length = Number(Module.getValue(pData + 4, "i32"));
        }
        {
            var pArray = Module.getValue(pData + 8, "*");
            if (pArray !== 0) {
                ret.Pairs = new Array();
                for (var i = 0; i < ret.Pairs_Length; i++) {
                    var value = Module.getValue(pArray + i * 4, "*");
                    if (value !== 0) {
                        ret.Pairs.push(String(MonoRuntime.conv_string(value)));
                    }
                    else {
                        ret.Pairs.push(null);
                    }
                }
            }
            else {
                ret.Pairs = null;
            }
        }
        return ret;
    }
}
/* TSBindingsGenerator Generated code -- this code is regenerated on each build */
class WindowManagerSetStyleDoubleParams {
    static unmarshal(pData) {
        let ret = new WindowManagerSetStyleDoubleParams();
        {
            ret.HtmlId = Number(Module.getValue(pData + 0, "*"));
        }
        {
            var ptr = Module.getValue(pData + 4, "*");
            if (ptr !== 0) {
                ret.Name = String(Module.UTF8ToString(ptr));
            }
            else {
                ret.Name = null;
            }
        }
        {
            ret.Value = Number(Module.getValue(pData + 8, "double"));
        }
        return ret;
    }
}
/* TSBindingsGenerator Generated code -- this code is regenerated on each build */
class WindowManagerSetStylesParams {
    static unmarshal(pData) {
        let ret = new WindowManagerSetStylesParams();
        {
            ret.HtmlId = Number(Module.getValue(pData + 0, "*"));
        }
        {
            ret.SetAsArranged = Boolean(Module.getValue(pData + 4, "i32"));
        }
        {
            ret.Pairs_Length = Number(Module.getValue(pData + 8, "i32"));
        }
        {
            var pArray = Module.getValue(pData + 12, "*");
            if (pArray !== 0) {
                ret.Pairs = new Array();
                for (var i = 0; i < ret.Pairs_Length; i++) {
                    var value = Module.getValue(pArray + i * 4, "*");
                    if (value !== 0) {
                        ret.Pairs.push(String(MonoRuntime.conv_string(value)));
                    }
                    else {
                        ret.Pairs.push(null);
                    }
                }
            }
            else {
                ret.Pairs = null;
            }
        }
        return ret;
    }
}
var Uno;
(function (Uno) {
    var Foundation;
    (function (Foundation) {
        var Interop;
        (function (Interop) {
            class ManagedObject {
                static init() {
                    ManagedObject.dispatchMethod = Module.mono_bind_static_method("[Uno.Foundation] Uno.Foundation.Interop.JSObject:Dispatch");
                }
                static dispatch(handle, method, parameters) {
                    if (!ManagedObject.dispatchMethod) {
                        ManagedObject.init();
                    }
                    ManagedObject.dispatchMethod(handle, method, parameters || "");
                }
            }
            Interop.ManagedObject = ManagedObject;
        })(Interop = Foundation.Interop || (Foundation.Interop = {}));
    })(Foundation = Uno.Foundation || (Uno.Foundation = {}));
})(Uno || (Uno = {}));
var Uno;
(function (Uno) {
    var UI;
    (function (UI) {
        var Interop;
        (function (Interop) {
            class Runtime {
                static init() {
                    return "";
                }
            }
            Runtime.engine = Runtime.init();
            Interop.Runtime = Runtime;
        })(Interop = UI.Interop || (UI.Interop = {}));
    })(UI = Uno.UI || (Uno.UI = {}));
})(Uno || (Uno = {}));
var Uno;
(function (Uno) {
    var UI;
    (function (UI) {
        var Interop;
        (function (Interop) {
            class Xaml {
            }
            Interop.Xaml = Xaml;
        })(Interop = UI.Interop || (UI.Interop = {}));
    })(UI = Uno.UI || (Uno.UI = {}));
})(Uno || (Uno = {}));
// ReSharper disable InconsistentNaming
