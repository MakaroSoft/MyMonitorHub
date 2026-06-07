;(function ($) {
    $.widget("ui.popupmenu", {
        options: {
            width: 800,
            dialogClass: '',
            menus: [
                    { icon: '', text: 'Option 1', href: '' },
                    { icon: '', text: 'Option 2', href: '' }
            ]
},
        _create: function () {
            var self = this;

            var title = $(this.element).attr('title');
            var deviceId = $(this.element).attr('device-id');
            
            $(this.element).click(function () {
                // show a spinner or something via css
                dialog = $('<div style="display:none;"></div>').appendTo('body');
                // open the dialog
                dialog.dialog({
                    title: title,
                    width: self.options.width,
                    // add a close listener to prevent adding multiple divs to the document
                    close: function (event, ui) {
                        // remove div with all data and events
                        dialog.remove();
                    },
                    dialogClass: self.options.dialogClass,
                    position: {
                        my: 'left top',
                        of: event
                    },
                    modal: true
                });

                var html = '<nav id="popupMenuId" class="ms-nav-menu" style="width: 200px;">    <ul>';
                var menus = self.options.menus;
                menus.forEach(function(entry) {
                    html = html + '<li class="container"><a href="'+entry.href+'/' + deviceId + '"><div><span class="' + entry.icon + '"></span><span class="valign">' + entry.text+ '</span></div></a></li>';
                });
                html = html + "</ul></nav>";
                
                dialog.html(html);
                
                $("#popupMenuId li").hover(function (hin) {
                    if (!$(this).hasClass("disabled")) {
                        $(this).addClass("ui-state-hover");
                    }
                }, function (hout) {
                    $(this).removeClass("ui-state-hover");
                });

                
                //prevent the browser to follow the link
                return false;
            });

        },
        destroy: function () {
            dialog.close();
            dialog.destroy();
        }
    });
})(jQuery);
