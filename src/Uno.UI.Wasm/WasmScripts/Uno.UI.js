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
             * initialization of WebAssembly, use this mode in conjuction with the Uno.UI.WpfHost
             * to improve debuggability.
             */
            static get isHosted() {
                return WindowManager._isHosted;
            }
            /**
                * Initialize the WindowManager
                * @param containerElementId The ID of the container element for the Xaml UI
                * @param loadingElementId The ID of the loading element to remove once ready
                */
            static init(localStoragePath, isHosted, containerElementId = "uno-body", loadingElementId = "uno-loading") {
                if (WindowManager.assembly) {
                    throw "Already initialized";
                }
                WindowManager._isHosted = isHosted;
                WindowManager.initMethods();
                UI.HtmlDom.initPolyfills();
                WindowManager.setupStorage(localStoragePath);
                this.current = new WindowManager(containerElementId, loadingElementId);
                this.current.init();
                return "ok";
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
            static setupSplashScreen() {
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
                        const unoLoadingSplash = document.createElement("div");
                        unoLoadingSplash.id = "uno-loading-splash";
                        unoLoadingSplash.classList.add("uno-splash");
                        unoLoadingSplash.style.backgroundImage = `url('${UnoAppManifest.splashScreenImage}')`;
                        unoLoading.appendChild(unoLoadingSplash);
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
                    let queryIndex = document.location.search.indexOf('?');
                    if (queryIndex != -1) {
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
                // Create the HTML element
                const element = contentDefinition.isSvg
                    ? document.createElementNS("http://www.w3.org/2000/svg", contentDefinition.tagName)
                    : document.createElement(contentDefinition.tagName);
                element.id = contentDefinition.id;
                element.setAttribute("XamlType", contentDefinition.type);
                element.setAttribute("XamlHandle", `${contentDefinition.handle}`);
                if (contentDefinition.isFrameworkElement) {
                    element.classList.add(WindowManager.unoUnarrangedClassName);
                }
                if (element.hasOwnProperty("tabindex")) {
                    element["tabindex"] = contentDefinition.isFocusable ? 0 : -1;
                }
                else {
                    element.setAttribute("tabindex", contentDefinition.isFocusable ? '0' : '-1');
                }
                if (contentDefinition) {
                    for (const className of contentDefinition.classes) {
                        element.classList.add(`uno-${className}`);
                    }
                }
                // Add the html element to list of elements
                this.allActiveElementsById[contentDefinition.id] = element;
                return "ok";
            }
            /**
                * Set a name for an element.
                *
                * This is mostly for diagnostic purposes.
                */
            setName(elementId, name) {
                const htmlElement = this.allActiveElementsById[elementId];
                if (!htmlElement) {
                    throw `Element id ${elementId} not found.`;
                }
                htmlElement.setAttribute("XamlName", name);
                return "ok";
            }
            /**
                * Set an attribute for an element.
                */
            setAttribute(elementId, attributes) {
                const htmlElement = this.allActiveElementsById[elementId];
                if (!htmlElement) {
                    throw `Element id ${elementId} not found.`;
                }
                for (const name in attributes) {
                    if (attributes.hasOwnProperty(name)) {
                        htmlElement.setAttribute(name, attributes[name]);
                    }
                }
                return "ok";
            }
            /**
                * Get an attribute for an element.
                */
            getAttribute(elementId, name) {
                const htmlElement = this.allActiveElementsById[elementId];
                if (!htmlElement) {
                    throw `Element id ${elementId} not found.`;
                }
                return htmlElement.getAttribute(name);
            }
            /**
                * Set a property for an element.
                */
            setProperty(elementId, properties) {
                const htmlElement = this.allActiveElementsById[elementId];
                if (!htmlElement) {
                    throw `Element id ${elementId} not found.`;
                }
                for (const name in properties) {
                    if (properties.hasOwnProperty(name)) {
                        htmlElement[name] = properties[name];
                    }
                }
                return "ok";
            }
            /**
                * Get a property for an element.
                */
            getProperty(elementId, name) {
                const htmlElement = this.allActiveElementsById[elementId];
                if (!htmlElement) {
                    throw `Element id ${elementId} not found.`;
                }
                return htmlElement[name] || "";
            }
            /**
                * Set the CSS style of a html element.
                *
                * To remove a value, set it to empty string.
                * @param styles A dictionary of styles to apply on html element.
                */
            setStyle(elementId, styles, setAsArranged = false) {
                const htmlElement = this.allActiveElementsById[elementId];
                if (!htmlElement) {
                    throw `Element id ${elementId} not found.`;
                }
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
            resetStyle(elementId, names) {
                const htmlElement = this.allActiveElementsById[elementId];
                if (!htmlElement) {
                    throw `Element id ${elementId} not found.`;
                }
                for (const name of names) {
                    htmlElement.style.setProperty(name, "");
                }
                return "ok";
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
                * @param onCapturePhase true means "on trickle down", false means "on bubble up". Default is false.
                */
            registerEventOnView(elementId, eventName, onCapturePhase = false, eventFilter, eventExtractor) {
                const htmlElement = this.allActiveElementsById[elementId];
                if (!htmlElement) {
                    throw `Element id ${elementId} not found.`;
                }
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
                        if (event instanceof KeyboardEvent) {
                            event.preventDefault();
                        }
                    }
                };
                htmlElement.addEventListener(eventName, eventHandler, onCapturePhase);
                return "ok";
            }
            /**
                * Set or replace the root content element.
                */
            setRootContent(elementId) {
                if (this.rootContent && this.rootContent.id === elementId) {
                    return null; // nothing to do
                }
                if (this.rootContent) {
                    // Remove existing
                    this.containerElement.removeChild(this.rootContent);
                    this.dispatchEvent(this.rootContent, "unloaded");
                    this.rootContent.classList.remove(WindowManager.unoRootClassName);
                }
                if (!elementId) {
                    return null;
                }
                // set new root
                const newRootElement = this.allActiveElementsById[elementId];
                newRootElement.classList.add(WindowManager.unoRootClassName);
                this.rootContent = newRootElement;
                this.dispatchEvent(this.rootContent, "loading");
                this.containerElement.appendChild(this.rootContent);
                this.dispatchEvent(this.rootContent, "loaded");
                newRootElement.classList.remove(WindowManager.unoUnarrangedClassName); // patch because root is not measured/arranged
                this.resize();
                return "ok";
            }
            /**
                * Set a view as a child of another one.
                *
                * "Loading" & "Loaded" events will be raised if nescessary.
                *
                * @param index Position in children list. Appended at end if not specified.
                */
            addView(parentId, childId, index) {
                const parentElement = this.allActiveElementsById[parentId];
                if (!parentElement) {
                    throw `addView: Parent element id ${parentId} not found.`;
                }
                const childElement = this.allActiveElementsById[childId];
                if (!childElement) {
                    throw `addView: Child element id ${parentId} not found.`;
                }
                const alreadyLoaded = this.getIsConnectedToRootElement(childElement);
                const isLoading = !alreadyLoaded && this.getIsConnectedToRootElement(parentElement);
                if (isLoading) {
                    this.dispatchEvent(childElement, "loading");
                }
                if (index && index < parentElement.childElementCount) {
                    const insertBeforeElement = parentElement.children[index];
                    parentElement.insertBefore(childElement, insertBeforeElement);
                }
                else {
                    parentElement.appendChild(childElement);
                }
                if (isLoading) {
                    this.dispatchEvent(childElement, "loaded");
                }
                return "ok";
            }
            /**
                * Remove a child from a parent element.
                *
                * "Unloading" & "Unloaded" events will be raised if nescessary.
                */
            removeView(parentId, childId) {
                const parentElement = this.allActiveElementsById[parentId];
                if (!parentElement) {
                    throw `removeView: Parent element id ${parentId} not found.`;
                }
                const childElement = this.allActiveElementsById[childId];
                if (!childElement) {
                    throw `removeView: Child element id ${parentId} not found.`;
                }
                const loaded = this.getIsConnectedToRootElement(childElement);
                parentElement.removeChild(childElement);
                if (loaded) {
                    this.dispatchEvent(childElement, "unloaded");
                }
                return "ok";
            }
            /**
                * Destroy a html element.
                *
                * The element won't be available anymore. Usually indicate the managed
                * version has been scavenged by the GC.
                */
            destroyView(viewId) {
                const element = this.allActiveElementsById[viewId];
                if (!element) {
                    throw `destroyView: Element id ${viewId} not found.`;
                }
                if (element.parentElement) {
                    element.parentElement.removeChild(element);
                    delete this.allActiveElementsById[viewId];
                }
                return "ok";
            }
            getBoundingClientRect(elementId) {
                const htmlElement = this.allActiveElementsById[elementId];
                if (!htmlElement) {
                    throw `Element id ${elementId} not found.`;
                }
                var bounds = htmlElement.getBoundingClientRect();
                return `${bounds.left};${bounds.top};${bounds.right - bounds.left};${bounds.bottom - bounds.top}`;
            }
            getBBox(elementId) {
                const htmlElement = this.allActiveElementsById[elementId];
                if (!htmlElement) {
                    throw `Element id ${elementId} not found.`;
                }
                var bbox = htmlElement.getBBox();
                return `${bbox.x};${bbox.y};${bbox.width};${bbox.height}`;
            }
            /**
                * Use the Html engine to measure the element using specified constraints.
                *
                * @param maxWidth string containing width in pixels. Empty string means infinite.
                * @param maxHeight string containing height in pixels. Empty string means infinite.
                */
            measureView(viewId, maxWidth, maxHeight) {
                const element = this.allActiveElementsById[viewId];
                if (!element) {
                    throw `measureView: Element id ${viewId} not found.`;
                }
                const previousWidth = element.style.width;
                const previousHeight = element.style.height;
                const previousPosition = element.style.position;
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
                    element.style.width = "";
                    element.style.height = "";
                    // This is required for an unconstrained measure (otherwise the parents size is taken into accound)
                    element.style.position = "fixed";
                    element.style.maxWidth = maxWidth ? `${maxWidth}px` : "";
                    element.style.maxHeight = maxHeight ? `${maxHeight}px` : "";
                    if (element.tagName.toUpperCase() === "IMG") {
                        const imgElement = element;
                        const size = `${imgElement.naturalWidth};${imgElement.naturalHeight}`;
                        return size;
                    }
                    else {
                        const resultWidth = element.offsetWidth ? element.offsetWidth : element.clientWidth;
                        const resultHeight = element.offsetHeight ? element.offsetHeight : element.clientHeight;
                        const size = `${resultWidth};${resultHeight}`;
                        return size;
                    }
                }
                finally {
                    element.style.width = previousWidth;
                    element.style.height = previousHeight;
                    element.style.position = previousPosition;
                    element.style.maxWidth = "";
                    element.style.maxHeight = "";
                }
            }
            setImageRawData(viewId, dataPtr, width, height) {
                const element = this.allActiveElementsById[viewId];
                if (!element) {
                    throw `setPointerCapture: Element id ${viewId} not found.`;
                }
                if (element.tagName.toUpperCase() === "IMG") {
                    const imgElement = element;
                    var rawCanvas = document.createElement("canvas");
                    rawCanvas.width = width;
                    rawCanvas.height = height;
                    var ctx = rawCanvas.getContext("2d");
                    var imgData = ctx.createImageData(width, height);
                    var bufferSize = width * height * 4;
                    for (var i = 0; i < bufferSize; i += 4) {
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
            setPointerCapture(viewId, pointerId) {
                const element = this.allActiveElementsById[viewId];
                if (!element) {
                    throw `setPointerCapture: Element id ${viewId} not found.`;
                }
                element.setPointerCapture(pointerId);
                return "ok";
            }
            releasePointerCapture(viewId, pointerId) {
                const element = this.allActiveElementsById[viewId];
                if (!element) {
                    throw `releasePointerCapture: Element id ${viewId} not found.`;
                }
                element.releasePointerCapture(pointerId);
                return "ok";
            }
            focusView(elementId) {
                const htmlElement = this.allActiveElementsById[elementId];
                if (!htmlElement) {
                    throw `Element id ${elementId} not found.`;
                }
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
                const element = this.allActiveElementsById[viewId];
                if (!element) {
                    throw `setHtmlContent: Element id ${viewId} not found.`;
                }
                element.innerHTML = html;
                return "ok";
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
                    if (!WindowManager.assembly) {
                        WindowManager.assembly = MonoRuntime.assembly_load("Uno.UI");
                        if (!WindowManager.assembly) {
                            throw `Unable to find assembly Uno.UI`;
                        }
                    }
                    if (!WindowManager.resizeMethod) {
                        const type = MonoRuntime.find_class(WindowManager.assembly, "Windows.UI.Xaml", "Window");
                        if (!type) {
                            throw `Unable to find type Windows.UI.Xaml.Window`;
                        }
                        WindowManager.resizeMethod = MonoRuntime.find_method(type, "Resize", -1);
                        if (!WindowManager.resizeMethod) {
                            throw `Unable to find Windows.UI.Xaml.Window.Resize method`;
                        }
                    }
                    if (!WindowManager.dispatchEventMethod) {
                        const type = MonoRuntime.find_class(WindowManager.assembly, "Windows.UI.Xaml", "UIElement");
                        WindowManager.dispatchEventMethod = MonoRuntime.find_method(type, "DispatchEvent", -1);
                        if (!WindowManager.dispatchEventMethod) {
                            throw `Unable to find Windows.UI.Xaml.UIElement.DispatchEvent method`;
                        }
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
                body.style.backgroundColor = '#fff';
            }
            resize() {
                if (WindowManager.isHosted) {
                    UnoDispatch.resize(`${window.innerWidth};${window.innerHeight}`);
                }
                else {
                    const sizeStr = this.getMonoString(`${window.innerWidth};${window.innerHeight}`);
                    MonoRuntime.call_method(WindowManager.resizeMethod, null, [sizeStr]);
                }
            }
            dispatchEvent(element, eventName, eventPayload = null) {
                const htmlId = element.getAttribute("XamlHandle");
                // console.debug(`${element.getAttribute("id")}: Raising event ${eventName}.`);
                if (!htmlId) {
                    throw `No attribute XamlHandle on element ${element}. Can't raise event.`;
                }
                if (WindowManager.isHosted) {
                    // Dispatch to the C# backed UnoDispatch class. Events propagated
                    // this way always succeed because synchronous calls are not possible
                    // between the host and the browser, unlike wasm.
                    UnoDispatch.dispatch(htmlId, eventName, eventPayload);
                    return true;
                }
                else {
                    const htmlIdStr = this.getMonoString(htmlId);
                    const eventNameStr = this.getMonoString(eventName);
                    const eventPayloadStr = this.getMonoString(eventPayload);
                    var handledHandle = MonoRuntime.call_method(WindowManager.dispatchEventMethod, null, [htmlIdStr, eventNameStr, eventPayloadStr]);
                    var handledStr = this.fromMonoString(handledHandle);
                    var handled = handledStr == "True";
                    return handled;
                }
            }
            getMonoString(str) {
                return str ? MonoRuntime.mono_string(str) : null;
            }
            fromMonoString(strHandle) {
                return strHandle ? MonoRuntime.conv_string(strHandle) : "";
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
        WindowManager.unoRootClassName = "uno-root-element";
        WindowManager.unoUnarrangedClassName = "uno-unarranged";
        UI.WindowManager = WindowManager;
        if (typeof define === 'function') {
            define(['AppManifest'], () => {
                if (document.readyState === "loading") {
                    document.addEventListener("DOMContentLoaded", () => WindowManager.setupSplashScreen());
                }
                else {
                    WindowManager.setupSplashScreen();
                }
            });
        }
        else {
            throw `The Uno.Wasm.Boostrap is not up to date, please upgrade to a later version`;
        }
    })(UI = Uno.UI || (Uno.UI = {}));
})(Uno || (Uno = {}));
// Ensure the "Uno" namespace is availablle globally
window.Uno = Uno;
var Uno;
(function (Uno) {
    var Foundation;
    (function (Foundation) {
        var Interop;
        (function (Interop) {
            class ManagedObject {
                static init() {
                    if (!ManagedObject.assembly) {
                        ManagedObject.assembly = MonoRuntime.assembly_load("Uno.Foundation");
                        if (!ManagedObject.assembly) {
                            throw "Unable to find assembly Uno.Foundation";
                        }
                    }
                    if (!ManagedObject.dispatchMethod) {
                        const type = MonoRuntime.find_class(ManagedObject.assembly, "Uno.Foundation.Interop", "JSObject");
                        ManagedObject.dispatchMethod = MonoRuntime.find_method(type, "Dispatch", -1);
                        if (!ManagedObject.dispatchMethod) {
                            throw "Unable to find Uno.Foundation.Interop.JSObject.Dispatch method";
                        }
                    }
                }
                static dispatch(handle, method, parameters) {
                    if (!ManagedObject.dispatchMethod) {
                        ManagedObject.init();
                    }
                    const handleStr = handle ? MonoRuntime.mono_string(handle) : null;
                    const methodStr = method ? MonoRuntime.mono_string(method) : null;
                    const parametersStr = parameters ? MonoRuntime.mono_string(parameters) : null;
                    if (methodStr == null) {
                        throw "Cannot dispatch to unknown method";
                    }
                    MonoRuntime.call_method(ManagedObject.dispatchMethod, null, [handleStr, methodStr, parametersStr]);
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
