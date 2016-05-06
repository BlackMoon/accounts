var $form, $divContent;
$(function () {
    $divContent = $('#sky-tab1 div.typography');
    $form = $('form');
    

    BootstrapDialog.confirm({
        btnOKLabel: 'Да',
        btnCancelLabel: 'Нет',
        callback: function(result) 
        {
            debugger;
            (result === true) && $form.submit();
        },
        message: 'Would you like to logout of IdentityServer?',
        title: 'Вопрос',
        type: BootstrapDialog.TYPE_INFO
    });
});