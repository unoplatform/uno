// workaround for gulp-uglify changing order of execution on $.fn func assignments
Object.assign($.fn, { breakWord });

workAroundFixedHeaderForAnchors();
highlight();
enableSearch();

renderTables();
renderAlerts();
updateAlertHeightOnResize();
renderLinks();
renderSidebar();
renderAffix();

renderNavbar();
renderLogo();
updateLogo()
updateLogoOnResize();
updateTocHeightOnResize();
updateSidenavTopOnResize();
renderFooter();
breakText();
renderTabs();
updateLogo();
