document.addEventListener("DOMContentLoaded", function () {

    if (typeof tinymce !== "undefined") {

        tinymce.init({
            selector: 'textarea.rich-editor',
            height: 400,
            plugins: 'lists link image table code',
            toolbar: 'undo redo | styles | bold italic | alignleft aligncenter alignright | bullist numlist | link image | code'
        }).catch(function (err) {
            console.error('TinyMCE init failed:', err);
        });

    }

});
