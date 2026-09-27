// High School Management System - Client Scripts

document.addEventListener('DOMContentLoaded', function () {
  initLiveClock();
  initSidebarToggle();
});

/**
 * Real-time digital clock matching Vietnamese standard
 * Format: Thứ [X], DD/MM/YYYY and HH:mm:ss
 */
function initLiveClock() {
  const clockTimeEl = document.getElementById('liveClockTime');
  const clockDateEl = document.getElementById('liveClockDate');

  if (!clockTimeEl && !clockDateEl) return;

  const daysOfWeekVi = [
    'Chủ Nhật',
    'Thứ Hai',
    'Thứ Ba',
    'Thứ Tư',
    'Thứ Năm',
    'Thứ Sáu',
    'Thứ Bảy'
  ];

  function pad(n) {
    return n < 10 ? '0' + n : n;
  }

  function update() {
    const now = new Date();
    const dayName = daysOfWeekVi[now.getDay()];
    const dateStr = pad(now.getDate()) + '/' + pad(now.getMonth() + 1) + '/' + now.getFullYear();
    const timeStr = pad(now.getHours()) + ':' + pad(now.getMinutes()) + ':' + pad(now.getSeconds());

    if (clockDateEl) {
      clockDateEl.textContent = dayName + ', ' + dateStr;
    }
    if (clockTimeEl) {
      clockTimeEl.textContent = timeStr;
    }
  }

  update();
  setInterval(update, 1000);
}

/**
 * Mobile responsive sidebar toggle
 */
function initSidebarToggle() {
  const toggleBtn = document.getElementById('sidebarToggleBtn');
  const sidebar = document.querySelector('.app-sidebar');

  if (!toggleBtn || !sidebar) return;

  toggleBtn.addEventListener('click', function (e) {
    e.stopPropagation();
    sidebar.classList.toggle('sidebar-open');
  });

  // Close sidebar when clicking outside on mobile
  document.addEventListener('click', function (e) {
    if (sidebar.classList.contains('sidebar-open') && !sidebar.contains(e.target) && e.target !== toggleBtn) {
      sidebar.classList.remove('sidebar-open');
    }
  });
}
