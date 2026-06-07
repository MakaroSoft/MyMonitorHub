;(function ($) {
    $.widget("ui.ajaxdialog", {
        options: {
            width: 800,
            dialogClass: ''
        },
        _create: function () {
            var self = this;

            var title = $(this.element).attr('title');
            $(this.element).click(function () {
                var dialog = $(
                    '<div class="modal fade" tabindex="-1" role="dialog" aria-labelledby="myModalLabel" aria-hidden="true">' +
                        '<div class="modal-dialog">' +
                            '<div class="modal-content">' +
                                '<div class="modal-header">' +
                                    '<h4 class="modal-title" id="myModalLabel">' + title + '</h4>' +
                                    '<button type="button" class="btn-close" data-bs-dismiss="modal" aria-label="Close"></button>' +
                                '</div>' +
                                '<div class="modal-body"></div>' +
                            '</div>' +
                        '</div>' +
                    '</div>'
                ).appendTo('body');

                var modalEl = dialog[0];
                var bsModal = new bootstrap.Modal(modalEl);

                modalEl.addEventListener('hidden.bs.modal', function () {
                    bsModal.dispose();
                    dialog.remove();
                });

                bsModal.show();

                var url = $(this).attr('ajaxdialog');

                $.ajax({
                    url: url,
                    type: "GET",
                    cache: false,
                    success: function (html) {
                        dialog.find(".modal-body").html(html);
                    }
                });

                return false;
            });

        },
        destroy: function () {
        }
    });
})(jQuery);
