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
                // referrer decalred in Index.cshtml view through [@Html.ValueToJs("referrer", @Model.Referrer, true)]
                referer && (window.location = referer);
        },
        message: "Выход из системы?",
        title: "Вопрос",
        type: BootstrapDialog.TYPE_INFO
    });
});