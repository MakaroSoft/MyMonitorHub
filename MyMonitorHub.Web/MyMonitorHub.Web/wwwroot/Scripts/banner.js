$(document).ready(function () {
    $('.banner').hover(function (hoverIn) {
        var imageContainer = $(this).children('div:first');
        imageContainer.addClass('ui-state-hover');
    }, function (hoverOut) {
        var imageContainer = $(this).children('div:first');
        imageContainer.removeClass('ui-state-hover');
    });
    $('.banner').click(function (event) {
        var imageContainer = $(this).children('div:first');        
        var image = imageContainer.children('div:first');
        if (image.hasClass('ui-icon-circle-triangle-n')) {
            image.removeClass('ui-icon-circle-triangle-n').addClass('ui-icon-circle-triangle-s');
        } else {
            image.removeClass('ui-icon-circle-triangle-s').addClass('ui-icon-circle-triangle-n');
        }
        var controlBody = $(this).attr('controlBody');
        $(controlBody).slideToggle("normal");
    });
});
