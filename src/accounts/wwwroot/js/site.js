var $divBody, $imgLoader;
       
$(function () {
    $divBody = $('div.body');
    $imgLoader = $('#imgLoader');
});

getView = function(url) {
    $.ajax({
        url: url,
        beforeSend: function() { $imgLoader.show(); },
        complete: function (event, xhr) {
            $imgLoader.hide();

            if (xhr === 'success')
            {
                $divBody.html(event.responseText);
                $.validator.unobtrusive.parse($divBody);
            }
        }
    });
}

onBegin = function (xhr, options) {
    debugger;

    var needToUpdate = 0;
    $(this).find(':input:not([type=hidden])').each(function(ix, el) {

        var $el = $(el);
        if ($el.attr('data-encrypt')) {
            var val = $el.val(), len = val.length, xor = '';
            // value encryption with xor operator
            for (var i = 0; i < len; ++i) {
                xor += String.fromCharCode(val.charCodeAt(i) ^ 128);
            }
            $el.val(xor);

            needToUpdate = 1;
        }
    });

    needToUpdate && (options.data = $(this).serialize());
    this.submit.disabled = true;
}

onComplete = function (xhr, status) {
            
    debugger;
    this.submit.disabled = false;

    var data = xhr.responseJSON;
    if (status === 'success') {
        var that = this;
        switch (data.status)
        {
            // LoginStatus decalred in Login.cshtml view through [@Html.EnumToJs(typeof(LoginStatus), true)]
            case LoginStatus.Success:
                window.location = data.returnUrl;
                break;

            case LoginStatus.Expired:

                BootstrapDialog.warning(data.message);
                getView('/ui/change?id=' + $(that.SignInId).val());

                break;

            case LoginStatus.Expiring:

                BootstrapDialog.confirm({
                    btnOKLabel: 'Да',
                    btnCancelLabel: 'Нет',
                    callback: function(result) {
                        (result === true) ? getView('/ui/change?id=' + $(that.SignInId).val()) : window.location = data.returnUrl;
                    },
                    message: data.message,
                    title: 'Вопрос',
                    type: BootstrapDialog.TYPE_INFO
                });

                break;

            default:
                BootstrapDialog.danger(data.message);
                break;
        }
    }
    else
        BootstrapDialog.danger(data.message);
}