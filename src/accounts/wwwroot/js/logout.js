$(function () {
    BootstrapDialog.confirm({
        btnOKLabel: "Да",
        btnCancelLabel: "Нет",
        callback: function(result) 
        {
            debugger;
            if (result === true) 
                document.forms[0].submit();
            else
                // referer declared in Index.cshtml view through [@Html.ValueToJs("referrer", @Model.Referrer, true)]
                window.referer && (window.location = window.referer);
        },
        message: "Выход из системы?",
        title: "Вопрос",
        type: BootstrapDialog.TYPE_INFO
    });
});