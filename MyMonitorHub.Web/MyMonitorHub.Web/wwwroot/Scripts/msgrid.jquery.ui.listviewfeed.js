(function ($) {

    $.widget("ui.listviewfeed", {
        options: {
            preLoadData: "",
            collectionSize: 50,
            templateHeader: "",
            templateBody: "",
            templateFooter: "",
            jsonGetUrl: '',
            moreText: 'More',
            noMoreText: 'No More'
        },

        _create: function () {
            var self = this;

            var tbl = $("<table style='border: 0px;' border='0' width='100%'></table>");
            tbl.appendTo(this.element);

            // define the body area
            var rwB = $("<tr></tr>");
            var tdB = $("<td style='border: 0px;'></td>");
            rwB.appendTo(tbl);
            tdB.appendTo(rwB);

            // area for more button
            var rwP = $("<tr></tr>");
            var tdP = $("<td style='border: 0px;'></td>");
            rwP.appendTo(tbl);
            tdP.appendTo(rwP);

            self.moreDiv = $("<div class='listviewfeed_moreDivActive'></div>");
            self.moreDiv.appendTo(tdP);
            self.moreDiv.html(self.options.moreText);



            self.listViewBody = tdB;


            // display the header, footer and marker
            var headerHtml = "";
            if (self.options.templateHeader != "") {
                headerHtml = $(self.options.templateHeader).render();
            }
            var footerHtml = "";
            if (self.options.templateFooter != "") {
                footerHtml = $(self.options.templateFooter).render();
            }

            // you can append each one at a time because it needs to be complete html
            self.listViewBody.append(headerHtml + "<!-- marker -->" + footerHtml);
            self.marker = $(self._findMarker(self, self.listViewBody[0]));

            self.moreDiv.click(function () {
                self._buttonClick(self);
            });

            self.lastDate = "";
            if (self.options.preLoadData == "") {
                self._buttonClick(self);
            } else {
                var listViewFeedPackage = self.options.preLoadData;
                self._handleData(self, listViewFeedPackage);
            }
        },
        destroy: function () {
            //this.element.next().remove();

        },
        _handleData: function (self, listViewFeedPackage) {
            var finished = listViewFeedPackage.finished;
            self.lastDate = listViewFeedPackage.last;
            self._displayData(self, listViewFeedPackage.dataObject);
            if (finished) {
                self._hideLoading(self, self.options.noMoreText);
            } else {
                self._hideLoading(self);
            }
        },
        _displayData: function (self, data) {
            var bodyHtml = $(self.options.templateBody).render(data);
            self.marker.before(bodyHtml);
        },
        _buttonClick: function (self) {
            self._showLoading(self);

            self.verificationtoken = {
                __RequestVerificationToken: $("#__requesttoken").val()
            };


            $.ajax({
                cache: false,
                type: "POST",
                url: self.options.jsonGetUrl + '?key=' + self.lastDate + '&take=' + self.options.collectionSize,
                //contentType: 'application/json',
                // dataType: "json",
                data: self.verificationtoken,
                error: function (xhr, ajaxOptions, thrownError) {
                    var message = xhr.status + " - " + thrownError;
                    self._hideLoading(self, message);
                },
                success: function (listViewFeedPackage) {
                    self._handleData(self, listViewFeedPackage);
                }
            });
        },
        _showLoading: function (self) {
            self.moreDiv.unbind('click');
            self.moreDiv.addClass("listviewfeed_moreDivLoading listviewfeed_moreDivNotActive");
            self.moreDiv.removeClass("listviewfeed_moreDivActive");
            self.moreDiv.html("");
        },
        _hideLoading: function (self, message) {
            if (message) {
                self.moreDiv.removeClass("listviewfeed_moreDivLoading");
                self.moreDiv.html(message);
            } else {
                self.moreDiv.addClass("listviewfeed_moreDivActive");
                self.moreDiv.removeClass("listviewfeed_moreDivNotActive listviewfeed_moreDivLoading");
                self.moreDiv.html(self.options.moreText);
                self.moreDiv.bind('click', function () {
                    self._buttonClick(self);
                });
            }
        },
        _findMarker: function (self, obj) {
            var child = obj.firstChild;

            while (child) {
                if (child.nodeName == "#comment" && child.nodeValue.trim() == "marker") {
                    return child;
                }
                if (child.firstChild) {
                    var result = self._findMarker(self, child);
                    if (result != null) {
                        return result;
                    }
                }
                child = child.nextSibling;
            }
            return null;
        }
    });
})(jQuery);