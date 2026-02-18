document.addEventListener("DOMContentLoaded", function () {

    document.querySelectorAll("form[asp-action='DeleteAccidentReport']").forEach(form => {

        form.addEventListener("submit", function (e) {

            if (!confirm("Are you sure you want to delete this accident report?")) {
                e.preventDefault();
            }

        });

    });

});
