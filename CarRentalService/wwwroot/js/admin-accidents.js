document.addEventListener("DOMContentLoaded", function () {

    document.querySelectorAll("form.delete-accident-form").forEach(form => {

        form.addEventListener("submit", function (e) {

            if (!confirm("Are you sure you want to delete this accident report?")) {
                e.preventDefault();
            }

        });

    });

});
