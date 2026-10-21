(() => {
    const form = document.getElementById('create-attendance-form');
    if (!form) return;
    const submit = form.querySelector('button[type="submit"]');
    const update = () => {
        submit.disabled = !form.querySelector('input[name="EntryIds"]:checked');
    };
    form.addEventListener('change', update);
    update();
})();
