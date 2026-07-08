(function () {
    'use strict';

    const POLL_INTERVAL_MS = 18000;
    const SLOW_POLL_INTERVAL_MS = 60000;

    const root = document.getElementById('public-order-status-root');
    if (!root) {
        return;
    }

    const summaryUrl = root.dataset.summaryUrl || '';
    const currency = (root.dataset.currency || 'TRY').toUpperCase();
    const statusBadge = document.getElementById('order-status-badge');
    const statusMessageEl = document.getElementById('order-status-message');
    const timelineEl = document.getElementById('order-status-timeline');
    const pollingErrorEl = document.getElementById('order-status-polling-error');
    const copyTrackingBtn = document.getElementById('copy-tracking-link-btn');
    const trackingUrl = window.publicOrderStatusConfig && window.publicOrderStatusConfig.trackingUrl
        ? window.publicOrderStatusConfig.trackingUrl
        : window.location.pathname.replace(/\/order-confirmation\//, '/order-status/');

    const currencyFormatter = new Intl.NumberFormat('tr-TR', {
        minimumFractionDigits: 2,
        maximumFractionDigits: 2
    });

    let pollTimer = null;
    let stopped = false;
    let currentStatus = root.dataset.status || '';

    function formatPrice(amount) {
        const formatted = currencyFormatter.format(amount);
        if (currency === 'TRY') {
            return formatted + ' ₺';
        }
        return formatted + ' ' + currency;
    }

    function showPollingError(message) {
        if (!pollingErrorEl) {
            return;
        }

        if (message) {
            pollingErrorEl.textContent = message;
            pollingErrorEl.hidden = false;
        } else {
            pollingErrorEl.textContent = '';
            pollingErrorEl.hidden = true;
        }
    }

    function isTerminalStatus(status) {
        return status === 'Completed' || status === 'Cancelled';
    }

    function getPollInterval(status) {
        return isTerminalStatus(status) ? SLOW_POLL_INTERVAL_MS : POLL_INTERVAL_MS;
    }

    function buildTimelineStepClass(step) {
        var classes = ['order-timeline-step'];
        if (step.isActive) {
            classes.push('active');
        }
        if (step.isCurrent) {
            classes.push('current');
        }
        if (step.isCancelled) {
            classes.push('cancelled');
        }
        return classes.join(' ');
    }

    function updateTimeline(steps) {
        if (!timelineEl || !Array.isArray(steps)) {
            return;
        }

        timelineEl.innerHTML = steps.map(function (step) {
            return (
                '<div class="' + buildTimelineStepClass(step) + '" data-step-key="' + escapeHtml(step.key || '') + '">' +
                '<div class="order-timeline-marker" aria-hidden="true"></div>' +
                '<div class="order-timeline-label">' + escapeHtml(step.label || '') + '</div>' +
                '</div>'
            );
        }).join('');
    }

    function escapeHtml(text) {
        var div = document.createElement('div');
        div.textContent = text;
        return div.innerHTML;
    }

    function applyStatusUpdate(data) {
        if (statusBadge && data.statusText) {
            statusBadge.textContent = data.statusText;
            statusBadge.className = 'badge order-status-badge ' + (data.statusBadgeClass || 'bg-secondary');
        }

        if (statusMessageEl && data.statusMessage) {
            statusMessageEl.textContent = data.statusMessage;
        }

        if (data.timelineSteps) {
            updateTimeline(data.timelineSteps);
        }

        const totalEl = document.getElementById('order-status-total');
        if (totalEl && typeof data.totalAmount === 'number') {
            totalEl.textContent = formatPrice(data.totalAmount);
        }
    }

    async function pollStatus() {
        if (stopped || document.hidden || !summaryUrl) {
            return;
        }

        try {
            const response = await fetch(summaryUrl, {
                method: 'GET',
                headers: {
                    Accept: 'application/json'
                },
                cache: 'no-store'
            });

            if (response.status === 404) {
                showPollingError('Sipariş takip bağlantısı artık geçerli değil.');
                stopPolling();
                return;
            }

            if (!response.ok) {
                showPollingError('Durum güncellenemedi, birazdan tekrar denenecek.');
                return;
            }

            const data = await response.json();
            showPollingError('');
            applyStatusUpdate(data);

            if (data.status && data.status !== currentStatus) {
                currentStatus = data.status;
                root.dataset.status = currentStatus;
            }

            scheduleNextPoll();
        } catch {
            showPollingError('Durum güncellenemedi, birazdan tekrar denenecek.');
        }
    }

    function scheduleNextPoll() {
        if (pollTimer) {
            clearTimeout(pollTimer);
        }

        if (stopped) {
            return;
        }

        pollTimer = setTimeout(function () {
            pollStatus();
        }, getPollInterval(currentStatus));
    }

    function stopPolling() {
        stopped = true;
        if (pollTimer) {
            clearTimeout(pollTimer);
            pollTimer = null;
        }
    }

    function startPolling() {
        if (!summaryUrl) {
            return;
        }

        stopped = false;
        pollStatus();
    }

    async function copyTrackingLink() {
        if (!trackingUrl) {
            return;
        }

        const absoluteUrl = new URL(trackingUrl, window.location.origin).toString();

        try {
            if (navigator.clipboard && navigator.clipboard.writeText) {
                await navigator.clipboard.writeText(absoluteUrl);
            } else {
                const input = document.createElement('textarea');
                input.value = absoluteUrl;
                input.setAttribute('readonly', '');
                input.style.position = 'absolute';
                input.style.left = '-9999px';
                document.body.appendChild(input);
                input.select();
                document.execCommand('copy');
                document.body.removeChild(input);
            }

            if (copyTrackingBtn) {
                const originalText = copyTrackingBtn.textContent;
                copyTrackingBtn.textContent = 'Kopyalandı!';
                setTimeout(function () {
                    copyTrackingBtn.textContent = originalText;
                }, 2000);
            }
        } catch {
            if (copyTrackingBtn) {
                copyTrackingBtn.textContent = 'Kopyalanamadı';
            }
        }
    }

    document.addEventListener('visibilitychange', function () {
        if (!document.hidden && !stopped) {
            pollStatus();
        }
    });

    if (copyTrackingBtn) {
        copyTrackingBtn.addEventListener('click', copyTrackingLink);
    }

    startPolling();
})();
