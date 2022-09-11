// Styling for alerts.
function setAlertHeight(){
    var maxHeight = Math.max.apply(null, $(".col-md-6 div.alert").map(function ()
    {
        return $(this).outerHeight();
    }).get());

    $('.alert').css('height', maxHeight);
    
}

function updateAlertHeightOnResize() {
    $(window).on('resize', function () {
        $('.alert').css('height', 'auto');
        setAlertHeight();
    });
}

function renderAlerts() {
    $('.NOTE, .TIP').addClass('alert alert-info');
    $('.WARNING').addClass('alert alert-warning');
    $('.IMPORTANT, .CAUTION').addClass('alert alert-danger');
    setAlertHeight();

}
