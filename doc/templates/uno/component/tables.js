/**
 * Styling for tables in conceptual documents using Bootstrap.
 * See http://getbootstrap.com/css/#tables
 */
function renderTables() {
    $('table').addClass('table table-bordered table-striped table-condensed').wrap('<div class=\"table-responsive\"></div>');
}
