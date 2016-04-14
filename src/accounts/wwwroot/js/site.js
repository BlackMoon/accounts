var $divBody, $imgLoader;
       
$(function () {
    $divBody = $('div.body');
    $imgLoader = $('#imgLoader');
});

getChangePasswordView = function(url) {
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
    var needToUpdate = 0;
    $(this).find(':input:not([type=hidden])').each(function (ix, el) {
                
        var $el = $(el);
        if ($el.attr('data-encrypt'))
        {
            var val = $el.val(), len = val.length, xor = '';
            // value encryption with xor operator
            for (var i = 0; i < len; ++i) { xor += String.fromCharCode(val.charCodeAt(i) ^ 128); }            
            $el.val(xor);

            needToUpdate = 1;
        }
    });

    needToUpdate && (options.data = $(this).serialize());
}

onFailure = function (xhr) {
    
    $.confirm({
        confirmButton: "Ok",
        cancelButton: null,
        text: xhr.responseJSON ? xhr.responceJSON.message : xhr.statusText
    });
}

onSuccess = function (data) {
            
    debugger;
    
    switch (data.status)
    {
        // LoginStatus decalred in view through [@Html.EnumToJs(typeof(LoginStatus), true)]
        case LoginStatus.Success:
            window.location = data.returnUrl;
            break;

        case LoginStatus.Expired:

            $.confirm({
                confirmButton: "Ok",
                cancelButton: null,
                text: data.message
            });
            getChangePasswordView('/change?returnurl=' + data.returnUrl);

            break;

        case LoginStatus.Expiring:

            $.confirm({
                confirmButton: "Да",
                cancelButton: "Нет",
                text: data.message,
                confirm: function () { getChangePasswordView('/change?returnurl=' + data.returnUrl) },
                cancel: function () { window.location = data.returnUrl; }
            });

            break;

        default:

            $.confirm({
                confirmButton: "Ok",
                cancelButton: null,
                text: data.message
            });

            break;
    }
}