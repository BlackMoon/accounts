$(function () {

    BootstrapDialog.confirm({
        btnOKLabel: 'Да',
        btnCancelLabel: 'Нет',
        callback: function(result) 
        {
            debugger;
            (result === true) && document.forms[0].submit();
        },
        message: 'Would you like to logout of IdentityServer?',
        title: 'Вопрос',
        type: BootstrapDialog.TYPE_INFO
    });
});