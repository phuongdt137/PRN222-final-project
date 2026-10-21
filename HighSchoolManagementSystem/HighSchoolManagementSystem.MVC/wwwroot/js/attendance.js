(() => {
    const form = document.getElementById('attendance-form');
    if (!form || form.dataset.editable !== 'true') return;
    const selects = [...form.querySelectorAll('.attendance-status')];
    const counter = document.getElementById('attendance-count');
    const update = () => {
        counter.textContent = `Đã chọn ${selects.filter(s => s.value).length}/${selects.length} học sinh`;
    };
    selects.forEach(select => select.addEventListener('change', () => {
        const leave = select.closest('td').querySelector('.attendance-leave');
        leave.value = select.value === 'Excused' ? (leave.dataset.approved || '') : '';
        update();
    }));
    document.getElementById('mark-present').addEventListener('click', () => {
        selects.filter(s => !s.value).forEach(s => { s.value = 'Present'; s.dispatchEvent(new Event('change')); });
    });
    const checkDeadline = () => {
        if (Date.now() < Date.parse(form.dataset.deadline)) return false;
        form.querySelectorAll('input, select, textarea, button').forEach(e => { e.disabled = true; });
        const message = document.getElementById('attendance-deadline');
        message.textContent = 'Đã đến 00:00 ngày kế tiếp (giờ Việt Nam). Bảng điểm danh chỉ được xem.';
        message.className = 'alert alert-info';
        return true;
    };
    form.addEventListener('submit', event => {
        if (checkDeadline()) event.preventDefault();
    });
    update();
    checkDeadline();
    const timer = setInterval(() => { if (checkDeadline()) clearInterval(timer); }, 1000);
})();
