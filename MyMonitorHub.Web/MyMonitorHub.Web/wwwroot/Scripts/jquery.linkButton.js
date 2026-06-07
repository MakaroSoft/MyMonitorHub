; var linkButton = {
    PostBack: 1,
    Ajax: 2
};

// I chose the name attribute because it is the same way submit buttons pass the value
//
//      $("#myLinkButton").linkButton();
//      <span id='myLinkButton' name='btnSubmit'>Test</span>
//
//       is the same as
//
//      <input id='myLinkButton' name='btnSubmit' type='submit' value='Test'/>
//
// This way you can have both link buttons and regular buttons. all with a name of 'btnSubmit' and then
// your post method could be:
//
// [AcceptVerbs(HttpVerbs.Post)]
// public ActionResult MyPostBack(int id, string btnSubmit)
//
(function ($) {
    // Resolve a Bootstrap 5 Modal instance for the processing element.
    // Falls back gracefully if Bootstrap isn't loaded.
    function getProcessingModal(selector) {
        if (selector == null) return null;
        var el = document.querySelector(selector);
        if (!el || typeof bootstrap === 'undefined' || !bootstrap.Modal) return null;
        return { el: el, modal: bootstrap.Modal.getOrCreateInstance(el) };
    }

    // Show/hide the processing modal, deferring a hide() that arrives mid-transition.
    // Bootstrap 5 silently drops hide() calls while the modal is still fading in,
    // so we wait for `shown.bs.modal` and replay the hide there.
    function ProcessingController(handle) {
        this.handle = handle;
        this.shown = false;
        this.hideRequested = false;
    }
    ProcessingController.prototype.show = function () {
        if (!this.handle) return;
        var self = this;
        var onShown = function () {
            self.handle.el.removeEventListener('shown.bs.modal', onShown);
            self.shown = true;
            if (self.hideRequested) self.handle.modal.hide();
        };
        this.handle.el.addEventListener('shown.bs.modal', onShown);
        this.handle.modal.show();
    };
    ProcessingController.prototype.hide = function () {
        if (!this.handle) return;
        if (this.shown) {
            this.handle.modal.hide();
        } else {
            this.hideRequested = true;
        }
    };

    $.widget("ui.linkButton", {
        options: {
            dataCollector: function () { return null; },
            success: null,
            warning: null,
            failure: null,
            enabled: true,
            url: "",
            type: linkButton.PostBack,
            processing: "#processing"
        },

        _create: function () {
            var self = this;
            if (this.options.enabled) {
                this.enabled(true);
            } else {
                this.enabled(false);
            }

            $(this.element).click(function (event) {
                if (self.options.enabled) {
                    if (self.options.type == linkButton.PostBack) {
                        // postBack code here
                        var form = $(self.element).parents('form');
                        var name = $(self.element).attr('name');
                        var text = $(self.element).text();
                        $('[name=' + name + ']').val(text);
                        form.first().submit();
                    } else {
                        // ajax code here
                        event.preventDefault();

                        var form = $(self.element).parents('form');
                        var name = $(self.element).attr('name');
                        var text = $(self.element).text();
                        $('[name=' + name + ']').val(text);

                        var myData = self.options.dataCollector();
                        if (myData == null) {
                            myData = $(form).serialize();
                        }

                        var processing = new ProcessingController(getProcessingModal(self.options.processing));
                        if (self.options.processing != null) {
                            $(self.options.processing + '-result').hide();
                            processing.show();
                        }

                        $.ajax({
                            cache: false,
                            type: "POST",
                            url: self.options.url,
                            data: myData,
                            success: function (data) {
                                if (self.options.processing != null) {
                                    processing.hide();
                                    if (data.hasOwnProperty('JsonWarning')) {
                                        displayMessage(data.JsonWarning, "fail");
                                    } else {
                                        displayMessage(text + ' completed successfully', "success");
                                    }
                                }
                                if (data.hasOwnProperty('JsonWarning')) {
                                    if (self.options.warning != null) self.options.warning(text, data.JsonWarning);
                                } else {
                                    if (self.options.success != null) self.options.success(text, data);
                                }
                            },
                            error: function (xhr, ajaxOptions, thrownError) {
                                if (self.options.processing != null) {
                                    processing.hide();
                                    $(self.options.processing + '-result')
                                        .text('Oops. Something went wrong. ' + thrownError)
                                        .removeClass()
                                        .addClass('alert alert-danger')
                                        .show();
                                }
                                if (self.options.failure != null) self.options.failure(text, thrownError);
                            }
                        });
                    }
                }
            });
        },
        destroy: function () {
            //this.element.next().remove();

        },
        enabled: function (enabled) {
            if (enabled) {
                this.options.enabled = true;
                $(this.element).removeClass('disabled');
            } else {
                this.options.enabled = false;
                $(this.element).addClass('disabled');
            }
        }
    });
})(jQuery);
