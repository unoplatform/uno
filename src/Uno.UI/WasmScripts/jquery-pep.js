/*!
 * PEP v0.4.3 | https://github.com/jquery/PEP
 * Copyright jQuery Foundation and other contributors | http://jquery.org/license
 */

(function (global, factory) {
    typeof exports === 'object' && typeof module !== 'undefined' ? module.exports = factory() :
        typeof define === 'function' && define.amd ? define(factory) :
            (global.PointerEventsPolyfill = factory());
}(this, function () {
    'use strict';

    /**
     * This is the constructor for new PointerEvents.
     *
     * New Pointer Events must be given a type, and an optional dictionary of
     * initialization properties.
     *
     * Due to certain platform requirements, events returned from the constructor
     * identify as MouseEvents.
     *
     * @constructor
     * @param {String} inType The type of the event to create.
     * @param {Object} [inDict] An optional dictionary of initial event properties.
     * @return {Event} A new PointerEvent of type `inType`, initialized with properties from `inDict`.
     */
    var MOUSE_PROPS = [
        'bubbles',
        'cancelable',
        'view',
        'detail',
        'screenX',
        'screenY',
        'clientX',
        'clientY',
        'ctrlKey',
        'altKey',
        'shiftKey',
        'metaKey',
        'button',
        'relatedTarget',
        'pageX',
        'pageY'
    ];

    var MOUSE_DEFAULTS = [
        false,
        false,
        null,
        null,
        0,
        0,
        0,
        0,
        false,
        false,
        false,
        false,
        0,
        null,
        0,
        0
    ];

    function PointerEvent(inType, inDict) {
        inDict = inDict || Object.create(null);

        var e = document.createEvent('Event');
        e.initEvent(inType, inDict.bubbles || false, inDict.cancelable || false);

        // define inherited MouseEvent properties
        // skip bubbles and cancelable since they're set above in initEvent()
        for (var i = 2, p; i < MOUSE_PROPS.length; i++) {
            p = MOUSE_PROPS[i];
            e[p] = inDict[p] || MOUSE_DEFAULTS[i];
        }
        e.buttons = inDict.buttons || 0;

        // Spec requires that pointers without pressure specified use 0.5 for down
        // state and 0 for up state.
        var pressure = 0;

        if (inDict.pressure && e.buttons) {
            pressure = inDict.pressure;
        } else {
            pressure = e.buttons ? 0.5 : 0;
        }

        // add x/y properties aliased to clientX/Y
        e.x = e.clientX;
        e.y = e.clientY;

        // define the properties of the PointerEvent interface
        e.pointerId = inDict.pointerId || 0;
        e.width = inDict.width || 0;
        e.height = inDict.height || 0;
        e.pressure = pressure;
        e.tiltX = inDict.tiltX || 0;
        e.tiltY = inDict.tiltY || 0;
        e.twist = inDict.twist || 0;
        e.tangentialPressure = inDict.tangentialPressure || 0;
        e.pointerType = inDict.pointerType || '';
        e.hwTimestamp = inDict.hwTimestamp || 0;
        e.isPrimary = inDict.isPrimary || false;
        return e;
    }

    /**
     * This module implements a map of pointer states
     */
    var USE_MAP = window.Map && window.Map.prototype.forEach;
    var PointerMap = USE_MAP ? Map : SparseArrayMap;

    function SparseArrayMap() {
        this.array = [];
        this.size = 0;
    }

    SparseArrayMap.prototype = {
        set: function (k, v) {
            if (v === undefined) {
                return this.delete(k);
            }
            if (!this.has(k)) {
                this.size++;
            }
            this.array[k] = v;
        },
        has: function (k) {
            return this.array[k] !== undefined;
        },
        delete: function (k) {
            if (this.has(k)) {
                delete this.array[k];
                this.size--;
            }
        },
        get: function (k) {
            return this.array[k];
        },
        clear: function () {
            this.array.length = 0;
            this.size = 0;
        },

        // return value, key, map
        forEach: function (callback, thisArg) {
            return this.array.forEach(function (v, k) {
                callback.call(thisArg, v, k, this);
            }, this);
        }
    };

    var CLONE_PROPS = [

        // MouseEvent
        'bubbles',
        'cancelable',
        'view',
        'detail',
        'screenX',
        'screenY',
        'clientX',
        'clientY',
        'ctrlKey',
        'altKey',
        'shiftKey',
        'metaKey',
        'button',
        'relatedTarget',

        // DOM Level 3
        'buttons',

        // PointerEvent
        'pointerId',
        'width',
        'height',
        'pressure',
        'tiltX',
        'tiltY',
        'pointerType',
        'hwTimestamp',
        'isPrimary',

        // event instance
        'type',
        'target',
        'currentTarget',
        'which',
        'pageX',
        'pageY',
        'timeStamp'
    ];

    var CLONE_DEFAULTS = [

        // MouseEvent
        false,
        false,
        null,
        null,
        0,
        0,
        0,
        0,
        false,
        false,
        false,
        false,
        0,
        null,

        // DOM Level 3
        0,

        // PointerEvent
        0,
        0,
        0,
        0,
        0,
        0,
        '',
        0,
        false,

        // event instance
        '',
        null,
        null,
        0,
        0,
        0,
        0
    ];

    var BOUNDARY_EVENTS = {
        'pointerover': 1,
        'pointerout': 1,
        'pointerenter': 1,
        'pointerleave': 1
    };

    var HAS_SVG_INSTANCE = (typeof SVGElementInstance !== 'undefined');

    /**
     * This module is for normalizing events. Mouse and Touch events will be
     * collected here, and fire PointerEvents that have the same semantics, no
     * matter the source.
     * Events fired:
     *   - pointerdown: a pointing is added
     *   - pointerup: a pointer is removed
     *   - pointermove: a pointer is moved
     *   - pointerover: a pointer crosses into an element
     *   - pointerout: a pointer leaves an element
     *   - pointercancel: a pointer will no longer generate events
     */
    var dispatcher = {
        pointermap: new PointerMap(),
        eventMap: Object.create(null),
        captureInfo: Object.create(null),

        // Scope objects for native events.
        // This exists for ease of testing.
        eventSources: Object.create(null),
        eventSourceList: [],
        /**
         * Add a new event source that will generate pointer events.
         *
         * `inSource` must contain an array of event names named `events`, and
         * functions with the names specified in the `events` array.
         * @param {string} name A name for the event source
         * @param {Object} source A new source of platform events.
         */
        registerSource: function (name, source) {
            var s = source;
            var newEvents = s.events;
            if (newEvents) {
                newEvents.forEach(function (e) {
                    if (s[e]) {
                        this.eventMap[e] = s[e].bind(s);
                    }
                }, this);
                this.eventSources[name] = s;
                this.eventSourceList.push(s);
            }
        },
        register: function (element) {
            var l = this.eventSourceList.length;
            for (var i = 0, es; (i < l) && (es = this.eventSourceList[i]); i++) {

                // call eventsource register
                es.register.call(es, element);
            }
        },
        unregister: function (element) {
            var l = this.eventSourceList.length;
            for (var i = 0, es; (i < l) && (es = this.eventSourceList[i]); i++) {

                // call eventsource register
                es.unregister.call(es, element);
            }
        },
        contains: /*scope.external.contains || */function (container, contained) {
            try {
                return container.contains(contained);
            } catch (ex) {

                // most likely: https://bugzilla.mozilla.org/show_bug.cgi?id=208427
                return false;
            }
        },

        // EVENTS
        down: function (inEvent) {
            inEvent.bubbles = true;
            this.fireEvent('pointerdown', inEvent);
        },
        move: function (inEvent) {
            inEvent.bubbles = true;
            this.fireEvent('pointermove', inEvent);
        },
        up: function (inEvent) {
            inEvent.bubbles = true;
            this.fireEvent('pointerup', inEvent);
        },
        enter: function (inEvent) {
            inEvent.bubbles = false;
            this.fireEvent('pointerenter', inEvent);
        },
        leave: function (inEvent) {
            inEvent.bubbles = false;
            this.fireEvent('pointerleave', inEvent);
        },
        over: function (inEvent) {
            inEvent.bubbles = true;
            this.fireEvent('pointerover', inEvent);
        },
        out: function (inEvent) {
            inEvent.bubbles = true;
            this.fireEvent('pointerout', inEvent);
        },
        cancel: function (inEvent) {
            inEvent.bubbles = true;
            this.fireEvent('pointercancel', inEvent);
        },
        leaveOut: function (event) {
            this.out(event);
            this.propagate(event, this.leave, false);
        },
        enterOver: function (event) {
            this.over(event);
            this.propagate(event, this.enter, true);
        },

        // LISTENER LOGIC
        eventHandler: function (inEvent) {

            // This is used to prevent multiple dispatch of pointerevents from
            // platform events. This can happen when two elements in different scopes
            // are set up to create pointer events, which is relevant to Shadow DOM.
            if (inEvent._handledByPE) {
                return;
            }
            var type = inEvent.type;
            var fn = this.eventMap && this.eventMap[type];
            if (fn) {
                fn(inEvent);
            }
            inEvent._handledByPE = true;
        },

        // set up event listeners
        listen: function (target, events) {
            events.forEach(function (e) {
                this.addEvent(target, e);
            }, this);
        },

        // remove event listeners
        unlisten: function (target, events) {
            events.forEach(function (e) {
                this.removeEvent(target, e);
            }, this);
        },
        addEvent: /*scope.external.addEvent || */function (target, eventName) {
            target.addEventListener(eventName, this.boundHandler);
        },
        removeEvent: /*scope.external.removeEvent || */function (target, eventName) {
            target.removeEventListener(eventName, this.boundHandler);
        },

        // EVENT CREATION AND TRACKING
        /**
         * Creates a new Event of type `inType`, based on the information in
         * `inEvent`.
         *
         * @param {string} inType A string representing the type of event to create
         * @param {Event} inEvent A platform event with a target
         * @return {Event} A PointerEvent of type `inType`
         */
        makeEvent: function (inType, inEvent) {

            // relatedTarget must be null if pointer is captured
            if (this.captureInfo[inEvent.pointerId]) {
                inEvent.relatedTarget = null;
            }
            var e = new PointerEvent(inType, inEvent);
            if (inEvent.preventDefault) {
                e.preventDefault = inEvent.preventDefault;
            }
            e._target = e._target || inEvent.target;
            return e;
        },

        // make and dispatch an event in one call
        fireEvent: function (inType, inEvent) {
            var e = this.makeEvent(inType, inEvent);
            return this.dispatchEvent(e);
        },
        /**
         * Returns a snapshot of inEvent, with writable properties.
         *
         * @param {Event} inEvent An event that contains properties to copy.
         * @return {Object} An object containing shallow copies of `inEvent`'s
         *    properties.
         */
        cloneEvent: function (inEvent) {
            var eventCopy = Object.create(null);
            var p;
            for (var i = 0; i < CLONE_PROPS.length; i++) {
                p = CLONE_PROPS[i];
                eventCopy[p] = inEvent[p] || CLONE_DEFAULTS[i];

                // Work around SVGInstanceElement shadow tree
                // Return the <use> element that is represented by the instance for Safari, Chrome, IE.
                // This is the behavior implemented by Firefox.
                if (HAS_SVG_INSTANCE && (p === 'target' || p === 'relatedTarget')) {
                    if (eventCopy[p] instanceof SVGElementInstance) {
                        eventCopy[p] = eventCopy[p].correspondingUseElement;
                    }
                }
            }

            // keep the semantics of preventDefault
            if (inEvent.preventDefault) {
                eventCopy.preventDefault = function () {
                    inEvent.preventDefault();
                };
            }
            return eventCopy;
        },
        getTarget: function (inEvent) {
            var capture = this.captureInfo[inEvent.pointerId];
            if (!capture) {
                return inEvent._target;
            }
            if (inEvent._target === capture || !(inEvent.type in BOUNDARY_EVENTS)) {
                return capture;
            }
        },
        propagate: function (event, fn, propagateDown) {
            var target = event.target;
            var targets = [];

            // Order of conditions due to document.contains() missing in IE.
            while (target !== document && !target.contains(event.relatedTarget)) {
                targets.push(target);
                target = target.parentNode;

                // Touch: Do not propagate if node is detached.
                if (!target) {
                    return;
                }
            }
            if (propagateDown) {
                targets.reverse();
            }
            targets.forEach(function (target) {
                event.target = target;
                fn.call(this, event);
            }, this);
        },
        setCapture: function (inPointerId, inTarget, skipDispatch) {
            if (this.captureInfo[inPointerId]) {
                this.releaseCapture(inPointerId, skipDispatch);
            }

            this.captureInfo[inPointerId] = inTarget;
            this.implicitRelease = this.releaseCapture.bind(this, inPointerId, skipDispatch);
            document.addEventListener('pointerup', this.implicitRelease);
            document.addEventListener('pointercancel', this.implicitRelease);

            var e = new PointerEvent('gotpointercapture');
            e.pointerId = inPointerId;
            e._target = inTarget;

            if (!skipDispatch) {
                this.asyncDispatchEvent(e);
            }
        },
        releaseCapture: function (inPointerId, skipDispatch) {
            var t = this.captureInfo[inPointerId];
            if (!t) {
                return;
            }

            this.captureInfo[inPointerId] = undefined;
            document.removeEventListener('pointerup', this.implicitRelease);
            document.removeEventListener('pointercancel', this.implicitRelease);

            var e = new PointerEvent('lostpointercapture');
            e.pointerId = inPointerId;
            e._target = t;

            if (!skipDispatch) {
                this.asyncDispatchEvent(e);
            }
        },
        /**
         * Dispatches the event to its target.
         *
         * @param {Event} inEvent The event to be dispatched.
         * @return {Boolean} True if an event handler returns true, false otherwise.
         */
        dispatchEvent: /*scope.external.dispatchEvent || */function (inEvent) {
            var t = this.getTarget(inEvent);
            if (t) {
                return t.dispatchEvent(inEvent);
            }
        },
        asyncDispatchEvent: function (inEvent) {
            requestAnimationFrame(this.dispatchEvent.bind(this, inEvent));
        }
    };
    dispatcher.boundHandler = dispatcher.eventHandler.bind(dispatcher);

    var targeting = {
        shadow: function (inEl) {
            if (inEl) {
                return inEl.shadowRoot || inEl.webkitShadowRoot;
            }
        },
        canTarget: function (shadow) {
            return shadow && Boolean(shadow.elementFromPoint);
        },
        targetingShadow: function (inEl) {
            var s = this.shadow(inEl);
            if (this.canTarget(s)) {
                return s;
            }
        },
        olderShadow: function (shadow) {
            var os = shadow.olderShadowRoot;
            if (!os) {
                var se = shadow.querySelector('shadow');
                if (se) {
                    os = se.olderShadowRoot;
                }
            }
            return os;
        },
        allShadows: function (element) {
            var shadows = [];
            var s = this.shadow(element);
            while (s) {
                shadows.push(s);
                s = this.olderShadow(s);
            }
            return shadows;
        },
        searchRoot: function (inRoot, x, y) {
            if (inRoot) {
                var t = inRoot.elementFromPoint(x, y);
                var st, sr;

                // is element a shadow host?
                sr = this.targetingShadow(t);
                while (sr) {

                    // find the the element inside the shadow root
                    st = sr.elementFromPoint(x, y);
                    if (!st) {

                        // check for older shadows
                        sr = this.olderShadow(sr);
                    } else {

                        // shadowed element may contain a shadow root
                        var ssr = this.targetingShadow(st);
                        return this.searchRoot(ssr, x, y) || st;
                    }
                }

                // light dom element is the target
                return t;
            }
        },
        owner: function (element) {
            var s = element;

            // walk up until you hit the shadow root or document
            while (s.parentNode) {
                s = s.parentNode;
            }

            // the owner element is expected to be a Document or ShadowRoot
            if (s.nodeType !== Node.DOCUMENT_NODE && s.nodeType !== Node.DOCUMENT_FRAGMENT_NODE) {
                s = document;
            }
            return s;
        },
        findTarget: function (inEvent) {
            var x = inEvent.clientX;
            var y = inEvent.clientY;

            // if the listener is in the shadow root, it is much faster to start there
            var s = this.owner(inEvent.target);

            // if x, y is not in this root, fall back to document search
            if (!s.elementFromPoint(x, y)) {
                s = document;
            }
            return this.searchRoot(s, x, y);
        }
    };

    var forEach = Array.prototype.forEach.call.bind(Array.prototype.forEach);
    var map = Array.prototype.map.call.bind(Array.prototype.map);
    var toArray = Array.prototype.slice.call.bind(Array.prototype.slice);
    var filter = Array.prototype.filter.call.bind(Array.prototype.filter);
    var MO = window.MutationObserver || window.WebKitMutationObserver;
    var SELECTOR = '[touch-action]';
    var OBSERVER_INIT = {
        subtree: true,
        childList: true,
        attributes: true,
        attributeOldValue: true,
        attributeFilter: ['touch-action']
    };

    function Installer(add, remove, changed, binder) {
        this.addCallback = add.bind(binder);
        this.removeCallback = remove.bind(binder);
        this.changedCallback = changed.bind(binder);
        if (MO) {
            this.observer = new MO(this.mutationWatcher.bind(this));
        }
    }

    Installer.prototype = {
        watchSubtree: function (target) {

            // Only watch scopes that can target find, as these are top-level.
            // Otherwise we can see duplicate additions and removals that add noise.
            //
            // TODO(dfreedman): For some instances with ShadowDOMPolyfill, we can see
            // a removal without an insertion when a node is redistributed among
            // shadows. Since it all ends up correct in the document, watching only
            // the document will yield the correct mutations to watch.
            if (this.observer && targeting.canTarget(target)) {
                this.observer.observe(target, OBSERVER_INIT);
            }
        },
        enableOnSubtree: function (target) {
            this.watchSubtree(target);
            if (target === document && document.readyState !== 'complete') {
                this.installOnLoad();
            } else {
                this.installNewSubtree(target);
            }
        },
        installNewSubtree: function (target) {
            forEach(this.findElements(target), this.addElement, this);
        },
        findElements: function (target) {
            if (target.querySelectorAll) {
                return target.querySelectorAll(SELECTOR);
            }
            return [];
        },
        removeElement: function (el) {
            this.removeCallback(el);
        },
        addElement: function (el) {
            this.addCallback(el);
        },
        elementChanged: function (el, oldValue) {
            this.changedCallback(el, oldValue);
        },
        concatLists: function (accum, list) {
            return accum.concat(toArray(list));
        },

        // register all touch-action = none nodes on document load
        installOnLoad: function () {
            document.addEventListener('readystatechange', function () {
                if (document.readyState === 'complete') {
                    this.installNewSubtree(document);
                }
            }.bind(this));
        },
        isElement: function (n) {
            return n.nodeType === Node.ELEMENT_NODE;
        },
        flattenMutationTree: function (inNodes) {

            // find children with touch-action
            var tree = map(inNodes, this.findElements, this);

            // make sure the added nodes are accounted for
            tree.push(filter(inNodes, this.isElement));

            // flatten the list
            return tree.reduce(this.concatLists, []);
        },
        mutationWatcher: function (mutations) {
            mutations.forEach(this.mutationHandler, this);
        },
        mutationHandler: function (m) {
            if (m.type === 'childList') {
                var added = this.flattenMutationTree(m.addedNodes);
                added.forEach(this.addElement, this);
                var removed = this.flattenMutationTree(m.removedNodes);
                removed.forEach(this.removeElement, this);
            } else if (m.type === 'attributes') {
                this.elementChanged(m.target, m.oldValue);
            }
        }
    };

    function shadowSelector(v) {
        return 'body /shadow-deep/ ' + selector(v);
    }
    function selector(v) {
        return '[touch-action="' + v + '"]';
    }
    function rule(v) {
        return '{ -ms-touch-action: ' + v + '; touch-action: ' + v + '; }';
    }
    var attrib2css = [
        'none',
        'auto',
        'pan-x',
        'pan-y',
        {
            rule: 'pan-x pan-y',
            selectors: [
                'pan-x pan-y',
                'pan-y pan-x'
            ]
        }
    ];
    var styles = '';

    // only install stylesheet if the browser has touch action support
    var hasNativePE = window.PointerEvent || window.MSPointerEvent;

    // only add shadow selectors if shadowdom is supported
    var hasShadowRoot = !window.ShadowDOMPolyfill && document.head.createShadowRoot;

    function applyAttributeStyles() {
        if (hasNativePE) {
            attrib2css.forEach(function (r) {
                if (String(r) === r) {
                    styles += selector(r) + rule(r) + '\n';
                    if (hasShadowRoot) {
                        styles += shadowSelector(r) + rule(r) + '\n';
                    }
                } else {
                    styles += r.selectors.map(selector) + rule(r.rule) + '\n';
                    if (hasShadowRoot) {
                        styles += r.selectors.map(shadowSelector) + rule(r.rule) + '\n';
                    }
                }
            });

            var el = document.createElement('style');
            el.textContent = styles;
            document.head.appendChild(el);
        }
    }

    var pointermap = dispatcher.pointermap;

    // radius around touchend that swallows mouse events
    var DEDUP_DIST = 25;

    // left, middle, right, back, forward
    var BUTTON_TO_BUTTONS = [1, 4, 2, 8, 16];

    var HAS_BUTTONS = false;
    try {
        HAS_BUTTONS = new MouseEvent('test', { buttons: 1 }).buttons === 1;
    } catch (e) { }

    // handler block for native mouse events
    var mouseEvents = {
        POINTER_ID: 1,
        POINTER_TYPE: 'mouse',
        events: [
            'mousedown',
            'mousemove',
            'mouseup',
            'mouseover',
            'mouseout'
        ],
        register: function (target) {
            dispatcher.listen(target, this.events);
        },
        unregister: function (target) {
            dispatcher.unlisten(target, this.events);
        },
        lastTouches: [],

        // collide with the global mouse listener
        isEventSimulatedFromTouch: function (inEvent) {
            var lts = this.lastTouches;
            var x = inEvent.clientX;
            var y = inEvent.clientY;
            for (var i = 0, l = lts.length, t; i < l && (t = lts[i]); i++) {

                // simulated mouse events will be swallowed near a primary touchend
                var dx = Math.abs(x - t.x);
                var dy = Math.abs(y - t.y);
                if (dx <= DEDUP_DIST && dy <= DEDUP_DIST) {
                    return true;
                }
            }
        },
        prepareEvent: function (inEvent) {
            var e = dispatcher.cloneEvent(inEvent);

            // forward mouse preventDefault
            var pd = e.preventDefault;
            e.preventDefault = function () {
                inEvent.preventDefault();
                pd();
            };
            e.pointerId = this.POINTER_ID;
            e.isPrimary = true;
            e.pointerType = this.POINTER_TYPE;
            return e;
        },
        prepareButtonsForMove: function (e, inEvent) {
            var p = pointermap.get(this.POINTER_ID);

            // Update buttons state after possible out-of-document mouseup.
            if (inEvent.which === 0 || !p) {
                e.buttons = 0;
            } else {
                e.buttons = p.buttons;
            }
            inEvent.buttons = e.buttons;
        },
        mousedown: function (inEvent) {
            if (!this.isEventSimulatedFromTouch(inEvent)) {
                var p = pointermap.get(this.POINTER_ID);
                var e = this.prepareEvent(inEvent);
                if (!HAS_BUTTONS) {
                    e.buttons = BUTTON_TO_BUTTONS[e.button];
                    if (p) { e.buttons |= p.buttons; }
                    inEvent.buttons = e.buttons;
                }
                pointermap.set(this.POINTER_ID, inEvent);
                if (!p || p.buttons === 0) {
                    dispatcher.down(e);
                } else {
                    dispatcher.move(e);
                }
            }
        },
        mousemove: function (inEvent) {
            if (!this.isEventSimulatedFromTouch(inEvent)) {
                var e = this.prepareEvent(inEvent);
                if (!HAS_BUTTONS) { this.prepareButtonsForMove(e, inEvent); }
                e.button = -1;
                pointermap.set(this.POINTER_ID, inEvent);
                dispatcher.move(e);
            }
        },
        mouseup: function (inEvent) {
            if (!this.isEventSimulatedFromTouch(inEvent)) {
                var p = pointermap.get(this.POINTER_ID);
                var e = this.prepareEvent(inEvent);
                if (!HAS_BUTTONS) {
                    var up = BUTTON_TO_BUTTONS[e.button];

                    // Produces wrong state of buttons in Browsers without `buttons` support
                    // when a mouse button that was pressed outside the document is released
                    // inside and other buttons are still pressed down.
                    e.buttons = p ? p.buttons & ~up : 0;
                    inEvent.buttons = e.buttons;
                }
                pointermap.set(this.POINTER_ID, inEvent);

                // Support: Firefox <=44 only
                // FF Ubuntu includes the lifted button in the `buttons` property on
                // mouseup.
                // https://bugzilla.mozilla.org/show_bug.cgi?id=1223366
                e.buttons &= ~BUTTON_TO_BUTTONS[e.button];
                if (e.buttons === 0) {
                    dispatcher.up(e);
                } else {
                    dispatcher.move(e);
                }
            }
        },
        mouseover: function (inEvent) {
            if (!this.isEventSimulatedFromTouch(inEvent)) {
                var e = this.prepareEvent(inEvent);
                if (!HAS_BUTTONS) { this.prepareButtonsForMove(e, inEvent); }
                e.button = -1;
                pointermap.set(this.POINTER_ID, inEvent);
                dispatcher.enterOver(e);
            }
        },
        mouseout: function (inEvent) {
            if (!this.isEventSimulatedFromTouch(inEvent)) {
                var e = this.prepareEvent(inEvent);
                if (!HAS_BUTTONS) { this.prepareButtonsForMove(e, inEvent); }
                e.button = -1;
                dispatcher.leaveOut(e);
            }
        },
        cancel: function (inEvent) {
            var e = this.prepareEvent(inEvent);
            dispatcher.cancel(e);
            this.deactivateMouse();
        },
        deactivateMouse: function () {
            pointermap.delete(this.POINTER_ID);
        }
    };

    var captureInfo = dispatcher.captureInfo;
    var findTarget = targeting.findTarget.bind(targeting);
    var allShadows = targeting.allShadows.bind(targeting);
    var pointermap$1 = dispatcher.pointermap;

    // This should be long enough to ignore compat mouse events made by touch
    var DEDUP_TIMEOUT = 2500;
    var CLICK_COUNT_TIMEOUT = 200;
    var ATTRIB = 'touch-action';
    var INSTALLER;

    // handler block for native touch events
    var touchEvents = {
        events: [
            'touchstart',
            'touchmove',
            'touchend',
            'touchcancel'
        ],
        register: function (target) {
            INSTALLER.enableOnSubtree(target);
        },
        unregister: function () {

            // TODO(dfreedman): is it worth it to disconnect the MO?
        },
        elementAdded: function (el) {
            var a = el.getAttribute(ATTRIB);
            var st = this.touchActionToScrollType(a);
            if (st) {
                el._scrollType = st;
                dispatcher.listen(el, this.events);

                // set touch-action on shadows as well
                allShadows(el).forEach(function (s) {
                    s._scrollType = st;
                    dispatcher.listen(s, this.events);
                }, this);
            }
        },
        elementRemoved: function (el) {
            el._scrollType = undefined;
            dispatcher.unlisten(el, this.events);

            // remove touch-action from shadow
            allShadows(el).forEach(function (s) {
                s._scrollType = undefined;
                dispatcher.unlisten(s, this.events);
            }, this);
        },
        elementChanged: function (el, oldValue) {
            var a = el.getAttribute(ATTRIB);
            var st = this.touchActionToScrollType(a);
            var oldSt = this.touchActionToScrollType(oldValue);

            // simply update scrollType if listeners are already established
            if (st && oldSt) {
                el._scrollType = st;
                allShadows(el).forEach(function (s) {
                    s._scrollType = st;
                }, this);
            } else if (oldSt) {
                this.elementRemoved(el);
            } else if (st) {
                this.elementAdded(el);
            }
        },
        scrollTypes: {
            EMITTER: 'none',
            XSCROLLER: 'pan-x',
            YSCROLLER: 'pan-y',
            SCROLLER: /^(?:pan-x pan-y)|(?:pan-y pan-x)|auto$/
        },
        touchActionToScrollType: function (touchAction) {
            var t = touchAction;
            var st = this.scrollTypes;
            if (t === 'none') {
                return 'none';
            } else if (t === st.XSCROLLER) {
                return 'X';
            } else if (t === st.YSCROLLER) {
                return 'Y';
            } else if (st.SCROLLER.exec(t)) {
                return 'XY';
            }
        },
        POINTER_TYPE: 'touch',
        firstTouch: null,
        isPrimaryTouch: function (inTouch) {
            return this.firstTouch === inTouch.identifier;
        },
        setPrimaryTouch: function (inTouch) {

            // set primary touch if there no pointers, or the only pointer is the mouse
            if (pointermap$1.size === 0 || (pointermap$1.size === 1 && pointermap$1.has(1))) {
                this.firstTouch = inTouch.identifier;
                this.firstXY = { X: inTouch.clientX, Y: inTouch.clientY };
                this.scrolling = false;
                this.cancelResetClickCount();
            }
        },
        removePrimaryPointer: function (inPointer) {
            if (inPointer.isPrimary) {
                this.firstTouch = null;
                this.firstXY = null;
                this.resetClickCount();
            }
        },
        clickCount: 0,
        resetId: null,
        resetClickCount: function () {
            var fn = function () {
                this.clickCount = 0;
                this.resetId = null;
            }.bind(this);
            this.resetId = setTimeout(fn, CLICK_COUNT_TIMEOUT);
        },
        cancelResetClickCount: function () {
            if (this.resetId) {
                clearTimeout(this.resetId);
            }
        },
        typeToButtons: function (type) {
            var ret = 0;
            if (type === 'touchstart' || type === 'touchmove') {
                ret = 1;
            }
            return ret;
        },
        touchToPointer: function (inTouch) {
            var cte = this.currentTouchEvent;
            var e = dispatcher.cloneEvent(inTouch);

            // We reserve pointerId 1 for Mouse.
            // Touch identifiers can start at 0.
            // Add 2 to the touch identifier for compatibility.
            var id = e.pointerId = inTouch.identifier + 2;
            e.target = captureInfo[id] || findTarget(e);
            e.bubbles = true;
            e.cancelable = true;
            e.detail = this.clickCount;
            e.button = 0;
            e.buttons = this.typeToButtons(cte.type);
            e.width = (inTouch.radiusX || inTouch.webkitRadiusX || 0) * 2;
            e.height = (inTouch.radiusY || inTouch.webkitRadiusY || 0) * 2;
            e.pressure = inTouch.force || inTouch.webkitForce || 0.5;
            e.isPrimary = this.isPrimaryTouch(inTouch);
            e.pointerType = this.POINTER_TYPE;

            // forward modifier keys
            e.altKey = cte.altKey;
            e.ctrlKey = cte.ctrlKey;
            e.metaKey = cte.metaKey;
            e.shiftKey = cte.shiftKey;

            // forward touch preventDefaults
            var self = this;
            e.preventDefault = function () {
                self.scrolling = false;
                self.firstXY = null;
                cte.preventDefault();
            };
            return e;
        },
        processTouches: function (inEvent, inFunction) {
            var tl = inEvent.changedTouches;
            this.currentTouchEvent = inEvent;
            for (var i = 0, t; i < tl.length; i++) {
                t = tl[i];
                inFunction.call(this, this.touchToPointer(t));
            }
        },

        // For single axis scrollers, determines whether the element should emit
        // pointer events or behave as a scroller
        shouldScroll: function (inEvent) {
            if (this.firstXY) {
                var ret;
                var scrollAxis = inEvent.currentTarget._scrollType;
                if (scrollAxis === 'none') {

                    // this element is a touch-action: none, should never scroll
                    ret = false;
                } else if (scrollAxis === 'XY') {

                    // this element should always scroll
                    ret = true;
                } else {
                    var t = inEvent.changedTouches[0];

                    // check the intended scroll axis, and other axis
                    var a = scrollAxis;
                    var oa = scrollAxis === 'Y' ? 'X' : 'Y';
                    var da = Math.abs(t['client' + a] - this.firstXY[a]);
                    var doa = Math.abs(t['client' + oa] - this.firstXY[oa]);

                    // if delta in the scroll axis > delta other axis, scroll instead of
                    // making events
                    ret = da >= doa;
                }
                this.firstXY = null;
                return ret;
            }
        },
        findTouch: function (inTL, inId) {
            for (var i = 0, l = inTL.length, t; i < l && (t = inTL[i]); i++) {
                if (t.identifier === inId) {
                    return true;
                }
            }
        },

        // In some instances, a touchstart can happen without a touchend. This
        // leaves the pointermap in a broken state.
        // Therefore, on every touchstart, we remove the touches that did not fire a
        // touchend event.
        // To keep state globally consistent, we fire a
        // pointercancel for this "abandoned" touch
        vacuumTouches: function (inEvent) {
            var tl = inEvent.touches;

            // pointermap.size should be < tl.length here, as the touchstart has not
            // been processed yet.
            if (pointermap$1.size >= tl.length) {
                var d = [];
                pointermap$1.forEach(function (value, key) {

                    // Never remove pointerId == 1, which is mouse.
                    // Touch identifiers are 2 smaller than their pointerId, which is the
                    // index in pointermap.
                    if (key !== 1 && !this.findTouch(tl, key - 2)) {
                        var p = value.out;
                        d.push(p);
                    }
                }, this);
                d.forEach(this.cancelOut, this);
            }
        },
        touchstart: function (inEvent) {
            this.vacuumTouches(inEvent);
            this.setPrimaryTouch(inEvent.changedTouches[0]);
            this.dedupSynthMouse(inEvent);
            if (!this.scrolling) {
                this.clickCount++;
                this.processTouches(inEvent, this.overDown);
            }
        },
        overDown: function (inPointer) {
            pointermap$1.set(inPointer.pointerId, {
                target: inPointer.target,
                out: inPointer,
                outTarget: inPointer.target
            });
            dispatcher.enterOver(inPointer);
            dispatcher.down(inPointer);
        },
        touchmove: function (inEvent) {
            if (!this.scrolling) {
                if (this.shouldScroll(inEvent)) {
                    this.scrolling = true;
                    this.touchcancel(inEvent);
                } else {
                    inEvent.preventDefault();
                    this.processTouches(inEvent, this.moveOverOut);
                }
            }
        },
        moveOverOut: function (inPointer) {
            var event = inPointer;
            var pointer = pointermap$1.get(event.pointerId);

            // a finger drifted off the screen, ignore it
            if (!pointer) {
                return;
            }
            var outEvent = pointer.out;
            var outTarget = pointer.outTarget;
            dispatcher.move(event);
            if (outEvent && outTarget !== event.target) {
                outEvent.relatedTarget = event.target;
                event.relatedTarget = outTarget;

                // recover from retargeting by shadow
                outEvent.target = outTarget;
                if (event.target) {
                    dispatcher.leaveOut(outEvent);
                    dispatcher.enterOver(event);
                } else {

                    // clean up case when finger leaves the screen
                    event.target = outTarget;
                    event.relatedTarget = null;
                    this.cancelOut(event);
                }
            }
            pointer.out = event;
            pointer.outTarget = event.target;
        },
        touchend: function (inEvent) {
            this.dedupSynthMouse(inEvent);
            this.processTouches(inEvent, this.upOut);
        },
        upOut: function (inPointer) {
            if (!this.scrolling) {
                dispatcher.up(inPointer);
                dispatcher.leaveOut(inPointer);
            }
            this.cleanUpPointer(inPointer);
        },
        touchcancel: function (inEvent) {
            this.processTouches(inEvent, this.cancelOut);
        },
        cancelOut: function (inPointer) {
            dispatcher.cancel(inPointer);
            dispatcher.leaveOut(inPointer);
            this.cleanUpPointer(inPointer);
        },
        cleanUpPointer: function (inPointer) {
            pointermap$1.delete(inPointer.pointerId);
            this.removePrimaryPointer(inPointer);
        },

        // prevent synth mouse events from creating pointer events
        dedupSynthMouse: function (inEvent) {
            var lts = mouseEvents.lastTouches;
            var t = inEvent.changedTouches[0];

            // only the primary finger will synth mouse events
            if (this.isPrimaryTouch(t)) {

                // remember x/y of last touch
                var lt = { x: t.clientX, y: t.clientY };
                lts.push(lt);
                var fn = (function (lts, lt) {
                    var i = lts.indexOf(lt);
                    if (i > -1) {
                        lts.splice(i, 1);
                    }
                }).bind(null, lts, lt);
                setTimeout(fn, DEDUP_TIMEOUT);
            }
        }
    };

    INSTALLER = new Installer(touchEvents.elementAdded, touchEvents.elementRemoved,
        touchEvents.elementChanged, touchEvents);

    var pointermap$2 = dispatcher.pointermap;
    var HAS_BITMAP_TYPE = window.MSPointerEvent &&
        typeof window.MSPointerEvent.MSPOINTER_TYPE_MOUSE === 'number';
    var msEvents = {
        events: [
            'MSPointerDown',
            'MSPointerMove',
            'MSPointerUp',
            'MSPointerOut',
            'MSPointerOver',
            'MSPointerCancel',
            'MSGotPointerCapture',
            'MSLostPointerCapture'
        ],
        register: function (target) {
            dispatcher.listen(target, this.events);
        },
        unregister: function (target) {
            dispatcher.unlisten(target, this.events);
        },
        POINTER_TYPES: [
            '',
            'unavailable',
            'touch',
            'pen',
            'mouse'
        ],
        prepareEvent: function (inEvent) {
            var e = inEvent;
            if (HAS_BITMAP_TYPE) {
                e = dispatcher.cloneEvent(inEvent);
                e.pointerType = this.POINTER_TYPES[inEvent.pointerType];
            }
            return e;
        },
        cleanup: function (id) {
            pointermap$2.delete(id);
        },
        MSPointerDown: function (inEvent) {
            pointermap$2.set(inEvent.pointerId, inEvent);
            var e = this.prepareEvent(inEvent);
            dispatcher.down(e);
        },
        MSPointerMove: function (inEvent) {
            var e = this.prepareEvent(inEvent);
            dispatcher.move(e);
        },
        MSPointerUp: function (inEvent) {
            var e = this.prepareEvent(inEvent);
            dispatcher.up(e);
            this.cleanup(inEvent.pointerId);
        },
        MSPointerOut: function (inEvent) {
            var e = this.prepareEvent(inEvent);
            dispatcher.leaveOut(e);
        },
        MSPointerOver: function (inEvent) {
            var e = this.prepareEvent(inEvent);
            dispatcher.enterOver(e);
        },
        MSPointerCancel: function (inEvent) {
            var e = this.prepareEvent(inEvent);
            dispatcher.cancel(e);
            this.cleanup(inEvent.pointerId);
        },
        MSLostPointerCapture: function (inEvent) {
            var e = dispatcher.makeEvent('lostpointercapture', inEvent);
            dispatcher.dispatchEvent(e);
        },
        MSGotPointerCapture: function (inEvent) {
            var e = dispatcher.makeEvent('gotpointercapture', inEvent);
            dispatcher.dispatchEvent(e);
        }
    };

    function applyPolyfill() {

        // only activate if this platform does not have pointer events
        if (!window.PointerEvent) {
            window.PointerEvent = PointerEvent;

            if (window.navigator.msPointerEnabled) {
                var tp = window.navigator.msMaxTouchPoints;
                Object.defineProperty(window.navigator, 'maxTouchPoints', {
                    value: tp,
                    enumerable: true
                });
                dispatcher.registerSource('ms', msEvents);
            } else {
                Object.defineProperty(window.navigator, 'maxTouchPoints', {
                    value: 0,
                    enumerable: true
                });
                dispatcher.registerSource('mouse', mouseEvents);
                if (window.ontouchstart !== undefined) {
                    dispatcher.registerSource('touch', touchEvents);
                }
            }

            dispatcher.register(document);
        }
    }

    var n = window.navigator;
    var s;
    var r;
    var h;
    function assertActive(id) {
        if (!dispatcher.pointermap.has(id)) {
            var error = new Error('InvalidPointerId');
            error.name = 'InvalidPointerId';
            throw error;
        }
    }
    function assertConnected(elem) {
        var parent = elem.parentNode;
        while (parent && parent !== elem.ownerDocument) {
            parent = parent.parentNode;
        }
        if (!parent) {
            var error = new Error('InvalidStateError');
            error.name = 'InvalidStateError';
            throw error;
        }
    }
    function inActiveButtonState(id) {
        var p = dispatcher.pointermap.get(id);
        return p.buttons !== 0;
    }
    if (n.msPointerEnabled) {
        s = function (pointerId) {
            assertActive(pointerId);
            assertConnected(this);
            if (inActiveButtonState(pointerId)) {
                dispatcher.setCapture(pointerId, this, true);
                this.msSetPointerCapture(pointerId);
            }
        };
        r = function (pointerId) {
            assertActive(pointerId);
            dispatcher.releaseCapture(pointerId, true);
            this.msReleasePointerCapture(pointerId);
        };
    } else {
        s = function setPointerCapture(pointerId) {
            assertActive(pointerId);
            assertConnected(this);
            if (inActiveButtonState(pointerId)) {
                dispatcher.setCapture(pointerId, this);
            }
        };
        r = function releasePointerCapture(pointerId) {
            assertActive(pointerId);
            dispatcher.releaseCapture(pointerId);
        };
    }
    h = function hasPointerCapture(pointerId) {
        return !!dispatcher.captureInfo[pointerId];
    };

    function applyPolyfill$1() {
        if (window.Element && !Element.prototype.setPointerCapture) {
            Object.defineProperties(Element.prototype, {
                'setPointerCapture': {
                    value: s
                },
                'releasePointerCapture': {
                    value: r
                },
                'hasPointerCapture': {
                    value: h
                }
            });
        }
    }

    applyAttributeStyles();
    applyPolyfill();
    applyPolyfill$1();

    var pointerevents = {
        dispatcher: dispatcher,
        Installer: Installer,
        PointerEvent: PointerEvent,
        PointerMap: PointerMap,
        targetFinding: targeting
    };

    return pointerevents;

}));
