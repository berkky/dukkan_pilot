(function () {
    'use strict';

    const POLL_INTERVAL_MS = 30000;
    const currencyFormatter = new Intl.NumberFormat('tr-TR', {
        minimumFractionDigits: 2,
        maximumFractionDigits: 2
    });

    function initLiveOrders() {
        const root = document.querySelector('[data-live-orders="true"]');
        if (!root) {
            return;
        }

        const liveUrl = root.dataset.liveUrl || '/Business/Orders/LiveSummary';
        const pageKey = root.dataset.livePageKey || 'business-orders';
        const storageKey = 'dp-live-orders-' + pageKey;

        let baselineLatestOrderId = parseInt(root.dataset.initialLatestOrderId || '0', 10) || null;
        let baselinePendingCount = parseInt(root.dataset.initialPendingCount || '0', 10) || 0;
        let pollTimer = null;
        let stopped = false;
        let soundEnabled = false;

        const newOrderAlert = document.getElementById('live-new-order-alert');
        const liveStatusBadge = document.getElementById('live-status-badge');
        const soundToggleBtn = document.getElementById('live-sound-toggle');

        restoreBaselineFromStorage();

        if (soundToggleBtn) {
            soundToggleBtn.addEventListener('click', function () {
                soundEnabled = !soundEnabled;
                soundToggleBtn.textContent = soundEnabled ? 'Sesli uyarı açık' : 'Sesli uyarıyı etkinleştir';
                soundToggleBtn.classList.toggle('btn-success', soundEnabled);
                soundToggleBtn.classList.toggle('btn-outline-secondary', !soundEnabled);
                if (soundEnabled) {
                    playNotificationBeep();
                }
            });
        }

        const reloadBtn = document.getElementById('live-reload-btn');
        if (reloadBtn) {
            reloadBtn.addEventListener('click', function () {
                window.location.reload();
            });
        }

        const dashboardOrdersLink = document.getElementById('live-dashboard-orders-link');
        if (dashboardOrdersLink) {
            dashboardOrdersLink.addEventListener('click', function () {
                hideNewOrderAlert();
            });
        }

        function restoreBaselineFromStorage() {
            try {
                const raw = sessionStorage.getItem(storageKey);
                if (!raw) {
                    return;
                }

                const saved = JSON.parse(raw);
                if (saved && typeof saved.latestOrderId === 'number') {
                    baselineLatestOrderId = saved.latestOrderId;
                }
                if (saved && typeof saved.pendingCount === 'number') {
                    baselinePendingCount = saved.pendingCount;
                }
            } catch {
                // ignore
            }
        }

        function saveBaseline() {
            try {
                sessionStorage.setItem(storageKey, JSON.stringify({
                    latestOrderId: baselineLatestOrderId,
                    pendingCount: baselinePendingCount
                }));
            } catch {
                // ignore
            }
        }

        function formatRevenue(amount) {
            return currencyFormatter.format(amount) + ' ₺';
        }

        function updateStat(selector, value) {
            const el = document.querySelector(selector);
            if (el) {
                el.textContent = value;
            }
        }

        function showNewOrderAlert() {
            if (newOrderAlert) {
                newOrderAlert.hidden = false;
                newOrderAlert.classList.add('show');
            }

            if (liveStatusBadge) {
                liveStatusBadge.textContent = 'Yeni sipariş var';
                liveStatusBadge.classList.remove('bg-success');
                liveStatusBadge.classList.add('bg-warning', 'text-dark');
            }

            if (soundEnabled) {
                playNotificationBeep();
            }
        }

        function hideNewOrderAlert() {
            if (newOrderAlert) {
                newOrderAlert.hidden = true;
                newOrderAlert.classList.remove('show');
            }

            if (liveStatusBadge) {
                liveStatusBadge.textContent = 'Canlı takip açık';
                liveStatusBadge.classList.remove('bg-warning', 'text-dark');
                liveStatusBadge.classList.add('bg-success');
            }
        }

        function playNotificationBeep() {
            try {
                const AudioContextCtor = window.AudioContext || window.webkitAudioContext;
                if (!AudioContextCtor) {
                    return;
                }

                const audioContext = new AudioContextCtor();
                const oscillator = audioContext.createOscillator();
                const gainNode = audioContext.createGain();

                oscillator.type = 'sine';
                oscillator.frequency.value = 880;
                gainNode.gain.value = 0.05;

                oscillator.connect(gainNode);
                gainNode.connect(audioContext.destination);

                oscillator.start();
                oscillator.stop(audioContext.currentTime + 0.15);
            } catch {
                // ignore sound errors
            }
        }

        function applySummary(data) {
            updateStat('[data-live-pending-count]', data.pendingCount);
            updateStat('[data-live-preparing-count]', data.preparingCount);
            updateStat('[data-live-today-count]', data.todayOrderCount);
            updateStat('[data-live-today-revenue]', formatRevenue(data.todayRevenue));
        }

        function detectNewOrder(data) {
            if (!data.latestOrderId) {
                return false;
            }

            if (baselineLatestOrderId === null) {
                return data.pendingCount > baselinePendingCount;
            }

            if (data.latestOrderId === baselineLatestOrderId) {
                return false;
            }

            return data.pendingCount > baselinePendingCount;
        }

        async function poll() {
            if (stopped || document.hidden) {
                return;
            }

            try {
                const response = await fetch(liveUrl, {
                    method: 'GET',
                    credentials: 'same-origin',
                    headers: {
                        'Accept': 'application/json'
                    },
                    cache: 'no-store'
                });

                if (response.status === 401 || response.status === 403) {
                    stopped = true;
                    if (pollTimer) {
                        clearInterval(pollTimer);
                        pollTimer = null;
                    }
                    console.warn('Canlı sipariş takibi durduruldu: yetkisiz erişim.');
                    return;
                }

                if (!response.ok) {
                    console.warn('Canlı sipariş özeti alınamadı:', response.status);
                    return;
                }

                const contentType = response.headers.get('content-type') || '';
                if (!contentType.includes('application/json')) {
                    stopped = true;
                    if (pollTimer) {
                        clearInterval(pollTimer);
                        pollTimer = null;
                    }
                    console.warn('Canlı sipariş takibi durduruldu: beklenmeyen yanıt.');
                    return;
                }

                const data = await response.json();
                applySummary(data);

                if (detectNewOrder(data)) {
                    showNewOrderAlert();
                    baselineLatestOrderId = data.latestOrderId;
                    baselinePendingCount = data.pendingCount;
                    saveBaseline();
                } else {
                    if (data.latestOrderId) {
                        baselineLatestOrderId = data.latestOrderId;
                    }
                    baselinePendingCount = data.pendingCount;
                    saveBaseline();
                }
            } catch (error) {
                console.warn('Canlı sipariş takibi hatası:', error);
            }
        }

        saveBaseline();
        pollTimer = setInterval(poll, POLL_INTERVAL_MS);

        document.addEventListener('visibilitychange', function () {
            if (!document.hidden && !stopped) {
                poll();
            }
        });
    }

    if (document.readyState === 'loading') {
        document.addEventListener('DOMContentLoaded', initLiveOrders);
    } else {
        initLiveOrders();
    }
})();
