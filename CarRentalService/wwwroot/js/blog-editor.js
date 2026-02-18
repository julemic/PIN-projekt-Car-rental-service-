document.addEventListener("DOMContentLoaded", function () {

    if (typeof tinymce !== "undefined") {

        tinymce.init({
            selector: 'textarea',
            height: 400,
            plugins: 'lists link image table code',
            toolbar: 'undo redo | styles | bold italic | alignleft aligncenter alignright | bullist numlist | link image | code'
        });

    }

});
