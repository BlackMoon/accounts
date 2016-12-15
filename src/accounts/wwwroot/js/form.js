var $divBody, $imgLoader;
       
$(function () {
    $divBody = $("div.body");
    $imgLoader = $("#imgLoader");
});

getView = function(url) {
    $.ajax({
        url: url,
        beforeSend: function() { $imgLoader.show(); },
        complete: function (event, xhr) {
            $imgLoader.hide();

            if (xhr === "success")
            {
                $divBody.html(event.responseText);
                $.validator.unobtrusive.parse($divBody);
            }
        }
    });
}

onBegin = function (xhr, options) {
    debugger;

    var encrypted = {}, needToUpdate = 0;
    $(this)
        .find(":input:not([type=hidden])")
        .each(function(ix, el) {

            var $el = $(el);
            if ($el.attr("data-encrypt")) {
                var val = $el.val(), len = val.length, xor = "";
                // value encryption with xor operator
                for (var i = 0; i < len; ++i) {
                    xor += String.fromCharCode(val.charCodeAt(i) ^ 128);
                }
                encrypted[$el.attr("name")] = xor;

                needToUpdate = 1;
            }
        });

    if (needToUpdate) {
        
        for (var k in encrypted) {
            if (encrypted.hasOwnProperty(k)) {
                options.data = options.data.replace(new RegExp(k + "=[^&]+"), k + "=" + encrypted[k]);
            }
        }
    }

    this.submit.disabled = true;
}

onComplete = function (xhr, status) {
            
    debugger;
    this.submit.disabled = false;

    var data = xhr.responseJSON;
    if (!data) {
        try {
            data = $.parseJSON(xhr.responseText);
        } catch (e) {
            data = {};
        }
    }

    if (status === "success") {
        var returnUrl = $(this.ReturnUrl).val();
        switch (data.status) {
            // LoginStatus declared in Login.cshtml view through [@Html.EnumToJs(typeof(LoginStatus), true)]
            case LoginStatus.Success:
                window.location = returnUrl;
                break;

            case LoginStatus.Expired:

                BootstrapDialog.warning(data.message);
                getView("/change?returnUrl=" + encodeURIComponent(returnUrl));

                break;

            case LoginStatus.Expiring:

                BootstrapDialog.confirm({
                    btnOKLabel: "Да",
                    btnCancelLabel: "Нет",
                    callback: function (result) {
                        (result === true) ? getView("/change?returnUrl=" + encodeURIComponent(returnUrl)) : window.location = returnUrl;
                    },
                    message: data.message,
                    title: "Вопрос",
                    type: BootstrapDialog.TYPE_INFO
                });

                break;

            default:
                BootstrapDialog.danger(data.message);
                break;
        }
    }
    else
        BootstrapDialog.danger(data.message || xhr.statusText);
}