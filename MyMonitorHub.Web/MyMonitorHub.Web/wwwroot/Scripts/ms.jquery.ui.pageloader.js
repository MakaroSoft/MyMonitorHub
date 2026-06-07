(function ($) {

    $.widget("ui.pageloader", {
        options: {
            preLoadData: "",
            templateBody: "",
            jsonGetUrl: '',
            sidx: "",
            sord: "",
            page: 1,
            searchCriteria: {},
            pager: null,
            itemsPerPage: 20,
            pageSizes: new Array(10, 20, 30),
            loading: null,
            postDraw: null
        },

        _create: function() {
            var self = this;

            if (self.options.pager) {
                $(self.options.pager).paginator({
                    pageSizes: self.options.pageSizes,
                    itemsPerPage: self.options.itemsPerPage,
                    onPageChanged: function(event, data) {
                        self.options.itemsPerPage = data.itemsPerPage;
                        self.options.page = data.currentPage;
                        $(self.options.pager).paginator("update", data.currentPage, data.itemsPerPage); // could possibly be other pagers. top and bottom for example.
                        self._buttonClick(self);
                    }
                });
            }

            if (self.options.preLoadData == "") {
                self._buttonClick(self);
            } else {
                self._handleData(self, self.options.preLoadData);
            }

        },
        destroy: function() {
            //this.element.next().remove();

        },
        _handleData: function (self, pageLoadData) {

            var totalRecords = pageLoadData.totalRecords != null ? pageLoadData.totalRecords : pageLoadData.TotalRecords;
            var dataObject = pageLoadData.dataObject != null ? pageLoadData.dataObject : pageLoadData.DataObject;

            if (self.options.pager) {
                $(self.options.pager).paginator("update", self.options.page, self.options.itemsPerPage, totalRecords);
            }
            self._displayData(self, dataObject);
            self._hideLoading(self);
        },
        _displayData: function(self, data) {
            if (!Array.isArray(data) || data.length === 0) {
                self.element.html('');
                if (self.options.postDraw != null) {
                    self.options.postDraw();
                }
                return;
            }
            var bodyHtml = $(self.options.templateBody).render(data);
            self.element.html(bodyHtml);
            
            if (self.options.postDraw != null) {
                self.options.postDraw();
            }
        },
        _buttonClick: function(self) {
            self._showLoading(self);

            var combinedData = {
                //__RequestVerificationToken: $("#__requesttoken").val(),
                sidx: self.options.sidx,
                sord: self.options.sord,
                page: self.options.page,
                itemsPerPage: self.options.itemsPerPage
            };

            for (var attrname in self.options.searchCriteria) {
                var val = self.options.searchCriteria[attrname];
                combinedData[attrname] = (typeof val === 'function') ? val() : val;
            }

            $.ajax({
                cache: false,
                type: "POST",
                url: self.options.jsonGetUrl,
                data: combinedData,
                
                error: function(xhr, ajaxOptions, thrownError) {
                    var message = xhr.status + " - " + thrownError;
                    self._hideLoading(self, message);
                },
                success: function(pageLoadData) {
                    self._handleData(self, pageLoadData);
                }
            });
        },
        _showLoading: function (self) {
            if (self.options.loading) {
                $(self.options.loading).showLoading();
            } else {
                self.element.showLoading();
            }
        },
        _hideLoading: function(self, message) {
            if (self.options.loading) {
                $(self.options.loading).hideLoading();
            } else {
                self.element.hideLoading();
            }
        },
        reloadPage: function (currentPage) {
            if (currentPage) {
                this.options.page = parseInt(currentPage);
            }
            this._buttonClick(this);
        }
    });
})(jQuery);