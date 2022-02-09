workAroundFixedHeaderForAnchors();
highlight();
enableSearch();

renderTables();
renderAlerts();
renderLinks();
renderSidebar();
renderAffix();

if (iframed) {
    fixNavbarSpacing();
} else {
    renderNavbar();
    renderLogo();
}
renderFooter();
breakText();
renderTabs();
updateLogo();
