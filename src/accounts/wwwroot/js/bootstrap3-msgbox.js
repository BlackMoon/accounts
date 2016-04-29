(function($) {
    
    'use strict';

    function msgBox(message, title, type) {
        return new BootstrapDialog({
            buttons: [{
                action: function (dlg) { dlg.close(); },
                label: 'Ok'
            }],
            message: message,
            title: title,
            type: type
        }).open();
    }

    $.extend(BootstrapDialog, {
        // change default buttons' order
        confirm: function () {
            var confirmOptions = {};
            var defaultConfirmOptions = {
                type: BootstrapDialog.TYPE_PRIMARY,
                title: null,
                message: null,
                closable: false,
                draggable: false,
                btnCancelLabel: BootstrapDialog.DEFAULT_TEXTS.CANCEL,
                btnCancelClass: null,
                btnOKLabel: BootstrapDialog.DEFAULT_TEXTS.OK,
                btnOKClass: null,
                callback: null
            };
            if (typeof arguments[0] === 'object' && arguments[0].constructor === {}.constructor) {
                confirmOptions = $.extend(true, defaultConfirmOptions, arguments[0]);
            } else {
                confirmOptions = $.extend(true, defaultConfirmOptions, {
                    message: arguments[0],
                    callback: typeof arguments[1] !== 'undefined' ? arguments[1] : null
                });
            }
            if (confirmOptions.btnOKClass === null) {
                confirmOptions.btnOKClass = ['btn', confirmOptions.type.split('-')[1]].join('-');
            }

            var dialog = new BootstrapDialog(confirmOptions);
            dialog.setData('callback', confirmOptions.callback);

            dialog.addButton({
                label: confirmOptions.btnOKLabel,
                cssClass: confirmOptions.btnOKClass,
                action: function (dialog) {
                    if (typeof dialog.getData('callback') === 'function' && dialog.getData('callback').call(this, true) === false) {
                        return false;
                    }

                    return dialog.close();
                }
            });

            dialog.addButton({
                label: confirmOptions.btnCancelLabel,
                cssClass: confirmOptions.btnCancelClass,
                action: function (dialog) {
                    if (typeof dialog.getData('callback') === 'function' && dialog.getData('callback').call(this, false) === false) {
                        return false;
                    }

                    return dialog.close();
                }
            });

            return dialog.open();
        },

        danger: function (message) { msgBox(message, 'Ошибка', BootstrapDialog.TYPE_DANGER); },
        info: function (message) { msgBox(message, 'Информация', BootstrapDialog.TYPE_INFO); },
        warning: function(message) { msgBox(message, 'Внимание', BootstrapDialog.TYPE_WARNING); }

    });


})(jQuery);