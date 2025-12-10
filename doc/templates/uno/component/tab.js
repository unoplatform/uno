function renderTabs() {

    const contentAttrs = {
        id: 'data-bi-id',
        name: 'data-bi-name',
        type: 'data-bi-type'
    };

    const Tab = (function () {
        function Tab(li, a, section) {
            this.li = li;
            this.a = a;
            this.section = section;
        }

        Object.defineProperty(Tab.prototype, "tabIds", {
            get: function () {
                return this.a.getAttribute('data-tab').split(' ');
            },
            enumerable: true,
            configurable: true
        });

        Object.defineProperty(Tab.prototype, "condition", {
            get: function () {
                return this.a.getAttribute('data-condition');
            },
            enumerable: true,
            configurable: true
        });

        Object.defineProperty(Tab.prototype, "visible", {
            get: function () {
                return !this.li.hasAttribute('hidden');
            },
            set: function (value) {
                if (value) {
                    this.li.removeAttribute('hidden');
                    this.li.removeAttribute('aria-hidden');
                } else {
                    this.li.setAttribute('hidden', 'hidden');
                    this.li.setAttribute('aria-hidden', 'true');
                }
            },
            enumerable: true,
            configurable: true
        });

        Object.defineProperty(Tab.prototype, "selected", {
            get: function () {
                return !this.section.hasAttribute('hidden');
            },
            set: function (value) {
                if (value) {
                    this.a.setAttribute('aria-selected', 'true');
                    this.a.tabIndex = 0;
                    this.section.removeAttribute('hidden');
                    this.section.removeAttribute('aria-hidden');
                } else {
                    this.a.setAttribute('aria-selected', 'false');
                    this.a.tabIndex = -1;
                    this.section.setAttribute('hidden', 'hidden');
                    this.section.setAttribute('aria-hidden', 'true');
                }
            },
            enumerable: true,
            configurable: true
        });

        Tab.prototype.focus = function () {
            this.a.focus();
        };

        return Tab;

    }());

    initTabs(document.body);

    function initTabs(container) {
        const queryStringTabs = readTabsQueryStringParam();
        const elements = container.querySelectorAll('.tabGroup');
        const state = {groups: [], selectedTabs: []};
        for (let i = 0; i < elements.length; i++) {
            const group = initTabGroup(elements.item(i));
            if (!group.independent) {
                updateVisibilityAndSelection(group, state);
                state.groups.push(group);
            }
        }
        container.addEventListener('click', function (event) {
            return handleClick(event, state);
        });
        if (state.groups.length === 0) {
            return state;
        }
        selectTabs(queryStringTabs, container);
        updateTabsQueryStringParam(state);
        notifyContentUpdated();
        return state;
    }

    function initTabGroup(element) {

        const group = {
            independent: element.hasAttribute('data-tab-group-independent'),
            tabs: []
        };

        let li = element.firstElementChild.firstElementChild;
        while (li) {
            const a = li.firstElementChild;
            a.setAttribute(contentAttrs.name, 'tab');

            const dataTab = a.getAttribute('data-tab').replace(/\+/g, ' ');
            a.setAttribute('data-tab', dataTab);

            const section = element.querySelector("[id=\"" + a.getAttribute('aria-controls') + "\"]");
            const tab = new Tab(li, a, section);
            group.tabs.push(tab);

            li = li.nextElementSibling;
        }

        element.setAttribute(contentAttrs.name, 'tab-group');
        element.tabGroup = group;

        return group;
    }

    function updateVisibilityAndSelection(group, state) {
        let anySelected = false;
        let firstVisibleTab;

        for (let _i = 0, _a = group.tabs; _i < _a.length; _i++) {
            let tab = _a[_i];
            tab.visible = tab.condition === null || state.selectedTabs.indexOf(tab.condition) !== -1;
            if (tab.visible) {
                if (!firstVisibleTab) {
                    firstVisibleTab = tab;
                }
            }
            tab.selected = tab.visible && arraysIntersect(state.selectedTabs, tab.tabIds);
            anySelected = anySelected || tab.selected;
        }

        if (!anySelected) {
            for (let _b = 0, _c = group.tabs; _b < _c.length; _b++) {
                const tabIds = _c[_b].tabIds;
                for (let _d = 0, tabIds_1 = tabIds; _d < tabIds_1.length; _d++) {
                    const tabId = tabIds_1[_d];
                    const index = state.selectedTabs.indexOf(tabId);
                    if (index === -1) {
                        continue;
                    }
                    state.selectedTabs.splice(index, 1);
                }
            }
            const tab = firstVisibleTab;
            tab.selected = true;
            state.selectedTabs.push(tab.tabIds[0]);
        }
    }

    function getTabInfoFromEvent(event) {

        if (!(event.target instanceof HTMLElement)) {
            return null;
        }

        const anchor = event.target.closest('a[data-tab]');

        if (anchor === null) {
            return null;
        }

        const tabIds = anchor.getAttribute('data-tab').split(' ');
        const group = anchor.parentElement.parentElement.parentElement.tabGroup;

        if (group === undefined) {
            return null;
        }

        return {tabIds: tabIds, group: group, anchor: anchor};
    }

    function handleClick(event, state) {
        const info = getTabInfoFromEvent(event);

        if (info === null) {
            return;
        }

        event.preventDefault();
        info.anchor.href = 'javascript:';

        setTimeout(function () {
            return info.anchor.href = '#' + info.anchor.getAttribute('aria-controls');
        });

        const tabIds = info.tabIds, group = info.group;
        const originalTop = info.anchor.getBoundingClientRect().top;

        if (group.independent) {
            for (let _i = 0, _a = group.tabs; _i < _a.length; _i++) {
                const tab = _a[_i];
                tab.selected = arraysIntersect(tab.tabIds, tabIds);
            }
        } else {
            if (arraysIntersect(state.selectedTabs, tabIds)) {
                return;
            }
            const previousTabId = group.tabs.filter(function (t) {
                return t.selected;
            })[0].tabIds[0];
            state.selectedTabs.splice(state.selectedTabs.indexOf(previousTabId), 1, tabIds[0]);
            for (let _b = 0, _c = state.groups; _b < _c.length; _b++) {
                const group_1 = _c[_b];
                updateVisibilityAndSelection(group_1, state);
            }
            updateTabsQueryStringParam(state);
        }
        notifyContentUpdated();
        const top = info.anchor.getBoundingClientRect().top;
        if (top !== originalTop && event instanceof MouseEvent) {
            window.scrollTo(0, window.pageYOffset + top - originalTop);
        }
    }

    function selectTabs(tabIds) {
        for (let _i = 0, tabIds_1 = tabIds; _i < tabIds_1.length; _i++) {
            const tabId = tabIds_1[_i];
            const a = document.querySelector(".tabGroup > ul > li > a[data-tab=\"" + tabId + "\"]:not([hidden])");

            if (a === null) {
                return;
            }

            a.dispatchEvent(new CustomEvent('click', {bubbles: true}));
        }
    }

    function readTabsQueryStringParam() {
        const qs = parseQueryString(location.search);
        const t = qs.tabs;

        if (t === undefined || t === '') {
            return [];
        }

        return t.split(',');
    }

    function updateTabsQueryStringParam(state) {
        const qs = parseQueryString(location.search);
        qs.tabs = state.selectedTabs.join();

        const url = location.protocol + "//" + location.host + location.pathname + "?" + toQueryString(qs) + location.hash;

        if (location.href === url) {
            return;
        }

        history.replaceState({}, document.title, url);
    }

    function toQueryString(args) {
        const parts = [];

        for (let name_1 in args) {
            if (args.hasOwnProperty(name_1) && args[name_1] !== '' && args[name_1] !== null && args[name_1] !== undefined) {
                parts.push(encodeURIComponent(name_1) + '=' + encodeURIComponent(args[name_1]));
            }
        }

        return parts.join('&');
    }

    function parseQueryString(queryString) {
        let match;
        const pl = /\+/g;
        const search = /([^&=]+)=?([^&]*)/g;

        const decode = function (s) {
            return decodeURIComponent(s.replace(pl, ' '));
        };

        if (queryString === undefined) {
            queryString = '';
        }

        queryString = queryString.substring(1);
        const urlParams = {};

        while (match = search.exec(queryString)) {
            urlParams[decode(match[1])] = decode(match[2]);
        }

        return urlParams;
    }

    function arraysIntersect(a, b) {
        for (let _i = 0, a_1 = a; _i < a_1.length; _i++) {
            const itemA = a_1[_i];

            for (let _a = 0, b_1 = b; _a < b_1.length; _a++) {
                const itemB = b_1[_a];
                if (itemA === itemB) {
                    return true;
                }
            }
        }

        return false;
    }

    function notifyContentUpdated() {
        // Dispatch this event when needed
        // window.dispatchEvent(new CustomEvent('content-update'));
    }
}
