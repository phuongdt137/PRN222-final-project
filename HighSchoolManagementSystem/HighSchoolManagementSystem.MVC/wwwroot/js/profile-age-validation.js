(function ($) {
    "use strict";

    function parseDate(value) {
        if (!/^\d{4}-\d{2}-\d{2}$/.test(value || "")) return null;
        var parts = value.split("-").map(Number);
        var date = new Date(0);
        date.setUTCHours(0, 0, 0, 0);
        date.setUTCFullYear(parts[0], parts[1] - 1, parts[2]);
        return date.getUTCFullYear() === parts[0] && date.getUTCMonth() === parts[1] - 1
            && date.getUTCDate() === parts[2] ? date : null;
    }

    function cutoff(element, parameters) {
        var reference = parameters.reference && element.form.elements.namedItem(parameters.reference);
        var date = parseDate(reference && reference.value ? reference.value : parameters.today);
        var years = Number(parameters.years);
        if (!date || date.getUTCFullYear() <= years) return null;
        // Match DateTime.AddYears: clamp February 29 to February 28 when necessary.
        var day = date.getUTCDate();
        date.setUTCDate(1);
        date.setUTCFullYear(date.getUTCFullYear() - years);
        var lastDay = new Date(date.getTime());
        lastDay.setUTCMonth(lastDay.getUTCMonth() + 1, 0);
        date.setUTCDate(Math.min(day, lastDay.getUTCDate()));
        return date;
    }

    $.validator.addMethod("minimumage", function (value, element, parameters) {
        if (element.validity && element.validity.badInput) return false;
        if (this.optional(element)) return true;
        var birth = parseDate(value);
        var latest = cutoff(element, parameters);
        return birth !== null && latest !== null && birth <= latest;
    });

    $.validator.unobtrusive.adapters.add("minimumage", ["years", "reference", "today"], function (options) {
        options.rules.minimumage = options.params;
        options.messages.minimumage = options.message;
    });

    $(function () {
        $("input[data-val-minimumage]").each(function () {
            var birth = this;
            var parameters = {
                years: birth.getAttribute("data-val-minimumage-years"),
                reference: birth.getAttribute("data-val-minimumage-reference"),
                today: birth.getAttribute("data-val-minimumage-today")
            };
            var reference = parameters.reference && birth.form.elements.namedItem(parameters.reference);
            function updateLimit() {
                var latest = cutoff(birth, parameters);
                if (latest) birth.max = latest.toISOString().slice(0, 10);
                else birth.removeAttribute("max");
            }
            updateLimit();
            if (reference) $(reference).on("change input", function () {
                updateLimit();
                if (birth.value) $(birth).valid();
            });
        });
    });
})(jQuery);
