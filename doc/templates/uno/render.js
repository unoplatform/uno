workAroundFixedHeaderForAnchors();
highlight();
enableSearch();

renderTables();
renderAlerts();
renderLinks();
renderSidebar();
renderAffix();

if (!iframed) {
    renderNavbar();
    renderLogo();
    renderFooter();
}

breakText();
renderTabs();
updateLogo();
