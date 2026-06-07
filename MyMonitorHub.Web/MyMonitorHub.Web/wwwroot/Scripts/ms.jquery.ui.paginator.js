(function ($) {

    $.widget("ui.paginator", {
        options: {
            itemsPerPage: 10,
            pageSizes: new Array(10,20,30),
            onPageChanged: function() {
            }
        },

        _create: function() {
            var self = this;

            $(this).on("onPageChanged", self.options.onPageChanged);

            var pagerLayout =
                $('<div class="ms-paginator">' +
                    '<span class="centered">' +
                    '<a class="ui-icon ui-icon-seek-first imageButton" href="#"></a>' +
                    '<a class="ui-icon ui-icon-triangle-1-w imageButton" href="#"></a>' +
                    '<span>Page <input name="page" type="text" value=""/> of <span style="padding-right: 10px;" name="total-pages">0</span></span>' +
                    '<a class="ui-icon ui-icon-triangle-1-e imageButton" href="#"></a>' +
                    '<a class="ui-icon ui-icon-seek-end imageButton" href="#"></a>' +
                    '<select>' +
                    '</select>' +
                    '</span>' +
                    '<span name="items">No records to view</span>' +
                    '</div>');

            pagerLayout.appendTo(this.element);

            self.textbox = this.element.find("input");

            self.rewind = this.element.find("a.ui-icon-seek-first");
            self.back = this.element.find("a.ui-icon-triangle-1-w");
            self.forward = this.element.find("a.ui-icon-triangle-1-e");
            self.ff = this.element.find("a.ui-icon-seek-end");

            self.pageSize = this.element.find("select");
            
            for (var i = 0; i < self.options.pageSizes.length; i++) {
                var option = $('<option value="' + self.options.pageSizes[i] + '">' + self.options.pageSizes[i] + '</option>');
                option.appendTo(self.pageSize);
            }
            

            self.totalPages = this.element.find("[name=total-pages]");
            self.totalItems = this.element.find("[name=items]");

            self.pageSize.val(self.options.itemsPerPage);
            self.textbox.val('1');

            self.lastPage = 1;


            self.rewind.click(function (event) {
                event.stopImmediatePropagation();
                var currentPage = self.textbox.val();
                if (currentPage != 1) {
                    currentPage = 1;
                    self.textbox.val("1");
                    self.lastPage = 1;
                    $(self).triggerHandler("onPageChanged", { currentPage: currentPage, itemsPerPage: self.options.itemsPerPage });
                }
                return false;
            });
            self.back.click(function(event) {
                event.stopImmediatePropagation();
                var currentPage = self.textbox.val();
                if (currentPage != 1) {
                    currentPage -= 1;
                    self.textbox.val(currentPage);
                    self.lastPage = currentPage;
                    $(self).triggerHandler("onPageChanged", { currentPage: currentPage, itemsPerPage: self.options.itemsPerPage });
                }
                return false;
            });
            self.forward.click(function(event) {
                event.stopImmediatePropagation();
                var currentPage = self.textbox.val();
                var totalPages = self.totalPages.text();
                if (currentPage < totalPages) {
                    currentPage++;
                    self.textbox.val(currentPage);
                    $(self).triggerHandler("onPageChanged", { currentPage: currentPage, itemsPerPage: self.options.itemsPerPage });
                }
                return false;
            });
            self.ff.click(function(event) {
                event.stopImmediatePropagation();
                var currentPage = self.textbox.val();
                var totalPages = self.totalPages.text();
                if (currentPage < totalPages) {
                    currentPage = totalPages;
                    self.textbox.val(currentPage);
                    self.lastPage = currentPage;
                    $(self).triggerHandler("onPageChanged", { currentPage: currentPage, itemsPerPage: self.options.itemsPerPage });
                }
                return false;
            });
            self.pageSize.change(function () {
                self.textbox.val('1');
                var itemsPerPage = parseInt(self.pageSize.val());
                self.options.itemsPerPage = itemsPerPage;
                $(self).triggerHandler("onPageChanged", { currentPage: 1, itemsPerPage: itemsPerPage });
            });

            self.textbox.change(function () {
                var value = self.textbox.val();
                if (!$.isNumeric(value)) {
                    self.textbox.val(self.lastPage);
                    return;
                }
                value = Math.floor(value);
                if (value == self.lastPage) {
                    self.textbox.val(self.lastPage);
                    return;
                }
                var totalPages = self.totalPages.text();
                if (value < 1 || value > totalPages) {
                    self.textbox.val(self.lastPage);
                    return;
                }
                self.lastPage = value;
                self.textbox.val(self.lastPage);
                
                $(self).triggerHandler("onPageChanged", { currentPage: value, itemsPerPage: self.options.itemsPerPage });
            });


        },
        destroy: function() {
            //this.element.next().remove();

        },
        update: function(currentPage, itemsPerPage, totalItems) {
            this.options.itemsPerPage = itemsPerPage;
            this.pageSize.val(itemsPerPage);
            
            this.textbox.val(currentPage);
            this.lastPage = currentPage;
            
            if (totalItems) {
                if (totalItems == 0) {
                    this.totalPages.text("0");
                    this.totalItems.text("No records to view");
                }

                var totalPages = Math.floor(totalItems / itemsPerPage + 1);
                this.totalPages.text(totalPages);


                var viewStart = ((currentPage - 1) * itemsPerPage) + 1;
                var viewEnd = viewStart + itemsPerPage - 1;
                if (viewEnd > totalItems) {
                    viewEnd = totalItems;
                }
                this.totalItems.text("view " + viewStart + " - " + viewEnd + " of " + totalItems);
            }
        }
    });
})(jQuery);