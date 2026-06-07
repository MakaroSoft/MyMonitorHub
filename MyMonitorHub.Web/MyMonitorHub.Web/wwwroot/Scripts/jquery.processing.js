;(function ($) {
    $.widget("ui.processing", {
        options: {
            css: {
                border: '1px solid red',
                height: '80px'
            }
        },

        _create: function () {
            var self = this;
            var test = "<table><tr><td><img alt='' src='/Content/ajax-loader.gif'/></td><td>Processing...</td></tr></table>";
            this.element.append(test);
            this.element.children('table:first').css(self.options.css);
            this.element.hide();
        },
        destroy: function () {
        },
        hide: function () {
            this.element.hide();
        },
        show: function () {
            this.element.show();
        }
    });
})(jQuery);
