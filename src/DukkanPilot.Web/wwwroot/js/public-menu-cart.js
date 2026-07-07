(function () {
    'use strict';

    const root = document.getElementById('public-menu-root');
    if (!root) {
        return;
    }

    const businessId = root.dataset.businessId;
    const orderUrl = root.dataset.orderUrl;
    const previewUrl = root.dataset.previewUrl || '';
    const whatsAppNumber = root.dataset.whatsappNumber || '';
    const currency = (root.dataset.currency || 'TRY').toUpperCase();
    const storageKey = 'cart-' + businessId;
    const rewardStorageKey = 'reward-request-' + businessId;

    const cartBar = document.getElementById('cart-bar');
    const cartItemCountEl = document.getElementById('cart-item-count');
    const cartTotalEl = document.getElementById('cart-total');
    const cartItemsContainer = document.getElementById('cart-items');
    const cartEmptyEl = document.getElementById('cart-empty');
    const cartSummaryTotalEl = document.getElementById('cart-summary-total');
    const cartPricingSummaryEl = document.getElementById('cart-pricing-summary');
    const cartSubtotalEl = document.getElementById('cart-subtotal');
    const cartDiscountRowEl = document.getElementById('cart-discount-row');
    const cartDiscountEl = document.getElementById('cart-discount');
    const cartCampaignMessageEl = document.getElementById('cart-campaign-message');
    const cartLoyaltyPreviewEl = document.getElementById('cart-loyalty-preview');
    const cartLoyaltyTextEl = document.getElementById('cart-loyalty-text');
    const cartClearBtn = document.getElementById('cart-clear-btn');
    const cartPlaceOrderBtn = document.getElementById('cart-place-order-btn');
    const cartOrderErrorEl = document.getElementById('cart-order-error');
    const cartOffcanvasEl = document.getElementById('cartOffcanvas');
    const customerNameInput = document.getElementById('customer-name');
    const customerPhoneInput = document.getElementById('customer-phone');
    const orderNotesInput = document.getElementById('order-notes');
    const whatsAppWarningEl = document.getElementById('whatsapp-warning');
    const selectedRewardBannerEl = document.getElementById('selected-reward-banner');
    const selectedRewardNameEl = document.getElementById('selected-reward-name');
    const clearRewardBtn = document.getElementById('clear-reward-btn');
    const tokenInput = document.querySelector('input[name="__RequestVerificationToken"]');

    let previewTimer = null;
    let latestPricing = null;

    const currencyFormatter = new Intl.NumberFormat('tr-TR', {
        minimumFractionDigits: 2,
        maximumFractionDigits: 2
    });

    function formatPrice(amount) {
        const formatted = currencyFormatter.format(amount || 0);
        if (currency === 'TRY') {
            return formatted + ' ₺';
        }
        return formatted + ' ' + currency;
    }

    function loadCart() {
        try {
            const raw = sessionStorage.getItem(storageKey);
            if (!raw) {
                return [];
            }
            const parsed = JSON.parse(raw);
            return Array.isArray(parsed) ? parsed : [];
        } catch {
            return [];
        }
    }

    function saveCart(items) {
        sessionStorage.setItem(storageKey, JSON.stringify(items));
    }

    function loadRewardRequest() {
        try {
            return sessionStorage.getItem(rewardStorageKey) || '';
        } catch {
            return '';
        }
    }

    function saveRewardRequest(name) {
        if (name) {
            sessionStorage.setItem(rewardStorageKey, name);
        } else {
            sessionStorage.removeItem(rewardStorageKey);
        }
    }

    function getCartItemCount(items) {
        return items.reduce(function (sum, item) {
            return sum + item.quantity;
        }, 0);
    }

    function getCartSubtotal(items) {
        return items.reduce(function (sum, item) {
            return sum + item.quantity * item.price;
        }, 0);
    }

    function findCartItem(items, productId) {
        return items.find(function (item) {
            return item.productId === productId;
        });
    }

    function updateRewardBanner() {
        const rewardName = loadRewardRequest();
        if (!selectedRewardBannerEl || !selectedRewardNameEl) {
            return;
        }

        if (rewardName) {
            selectedRewardNameEl.textContent = rewardName;
            selectedRewardBannerEl.hidden = false;
        } else {
            selectedRewardBannerEl.hidden = true;
            selectedRewardNameEl.textContent = '';
        }
    }

    function applyPricingPreview(pricing) {
        latestPricing = pricing;
        const items = loadCart();
        const clientSubtotal = getCartSubtotal(items);
        const subtotal = pricing && typeof pricing.subtotal === 'number' ? pricing.subtotal : clientSubtotal;
        const discount = pricing && typeof pricing.discountAmount === 'number' ? pricing.discountAmount : 0;
        const total = pricing && typeof pricing.total === 'number' ? pricing.total : clientSubtotal;

        if (cartBar) {
            const itemCount = getCartItemCount(items);
            cartBar.hidden = itemCount === 0;
            root.classList.toggle('has-cart-bar', itemCount > 0);
        }

        if (cartItemCountEl) {
            cartItemCountEl.textContent = String(getCartItemCount(items));
        }

        if (cartTotalEl) {
            cartTotalEl.textContent = formatPrice(total);
        }

        if (cartSummaryTotalEl) {
            cartSummaryTotalEl.textContent = formatPrice(total);
        }

        if (cartPricingSummaryEl) {
            cartPricingSummaryEl.hidden = items.length === 0;
        }

        if (cartSubtotalEl) {
            cartSubtotalEl.textContent = formatPrice(subtotal);
        }

        if (cartDiscountRowEl && cartDiscountEl) {
            if (discount > 0) {
                cartDiscountRowEl.hidden = false;
                cartDiscountEl.textContent = '-' + formatPrice(discount);
            } else {
                cartDiscountRowEl.hidden = true;
                cartDiscountEl.textContent = formatPrice(0);
            }
        }

        if (cartCampaignMessageEl) {
            if (pricing && pricing.campaignMessage) {
                cartCampaignMessageEl.textContent = pricing.campaignMessage;
                cartCampaignMessageEl.hidden = false;
            } else {
                cartCampaignMessageEl.textContent = '';
                cartCampaignMessageEl.hidden = true;
            }
        }

        if (cartLoyaltyPreviewEl && cartLoyaltyTextEl) {
            if (pricing && pricing.loyaltyPreviewMessage) {
                cartLoyaltyTextEl.textContent = pricing.loyaltyPreviewMessage;
                cartLoyaltyPreviewEl.hidden = false;
            } else {
                cartLoyaltyTextEl.textContent = '';
                cartLoyaltyPreviewEl.hidden = true;
            }
        }
    }

    function updateCartUi() {
        const items = loadCart();

        if (cartEmptyEl) {
            cartEmptyEl.hidden = items.length > 0;
        }

        if (cartItemsContainer) {
            cartItemsContainer.innerHTML = '';

            items.forEach(function (item) {
                const row = document.createElement('div');
                row.className = 'cart-item';
                row.dataset.productId = String(item.productId);

                row.innerHTML =
                    '<div class="cart-item-info">' +
                    '<div class="cart-item-name">' + escapeHtml(item.name) + '</div>' +
                    '<div class="cart-item-unit-price">' + formatPrice(item.price) + '</div>' +
                    '</div>' +
                    '<div class="cart-item-actions">' +
                    '<div class="cart-qty-control">' +
                    '<button type="button" class="btn btn-sm btn-outline-secondary cart-qty-btn" data-action="decrease" aria-label="Azalt">−</button>' +
                    '<span class="cart-qty-value">' + item.quantity + '</span>' +
                    '<button type="button" class="btn btn-sm btn-outline-secondary cart-qty-btn" data-action="increase" aria-label="Artır">+</button>' +
                    '</div>' +
                    '<button type="button" class="btn btn-sm btn-link text-danger cart-remove-btn" data-action="remove">Sil</button>' +
                    '</div>' +
                    '<div class="cart-item-line-total">' + formatPrice(item.quantity * item.price) + '</div>';

                cartItemsContainer.appendChild(row);
            });
        }

        const hasWhatsApp = whatsAppNumber.trim().length > 0;
        if (whatsAppWarningEl) {
            whatsAppWarningEl.hidden = hasWhatsApp;
        }

        if (cartPlaceOrderBtn) {
            cartPlaceOrderBtn.disabled = items.length === 0 || !hasWhatsApp;
        }

        updateRewardBanner();
        schedulePricingPreview();
    }

    function escapeHtml(text) {
        const div = document.createElement('div');
        div.textContent = text;
        return div.innerHTML;
    }

    function schedulePricingPreview() {
        if (!previewUrl || !tokenInput || !tokenInput.value) {
            applyPricingPreview(null);
            return;
        }

        if (previewTimer) {
            clearTimeout(previewTimer);
        }

        previewTimer = setTimeout(requestPricingPreview, 350);
    }

    async function requestPricingPreview() {
        const items = loadCart();
        if (items.length === 0) {
            applyPricingPreview(null);
            return;
        }

        if (!previewUrl || !tokenInput || !tokenInput.value) {
            applyPricingPreview(null);
            return;
        }

        const payload = {
            items: items.map(function (item) {
                return {
                    productId: item.productId,
                    quantity: item.quantity
                };
            }),
            rewardRequestName: loadRewardRequest() || null
        };

        try {
            const response = await fetch(previewUrl, {
                method: 'POST',
                headers: {
                    'Content-Type': 'application/json',
                    'RequestVerificationToken': tokenInput.value
                },
                body: JSON.stringify(payload)
            });

            if (!response.ok) {
                applyPricingPreview(null);
                return;
            }

            const data = await response.json();
            applyPricingPreview(data);
        } catch {
            applyPricingPreview(null);
        }
    }

    function addToCart(product) {
        const items = loadCart();
        const existing = findCartItem(items, product.productId);

        if (existing) {
            existing.quantity += 1;
        } else {
            items.push({
                productId: product.productId,
                name: product.name,
                price: product.price,
                quantity: 1
            });
        }

        saveCart(items);
        updateCartUi();
    }

    function changeQuantity(productId, delta) {
        const items = loadCart();
        const item = findCartItem(items, productId);

        if (!item) {
            return;
        }

        item.quantity += delta;

        if (item.quantity <= 0) {
            const filtered = items.filter(function (i) {
                return i.productId !== productId;
            });
            saveCart(filtered);
        } else {
            saveCart(items);
        }

        updateCartUi();
    }

    function removeFromCart(productId) {
        const items = loadCart().filter(function (item) {
            return item.productId !== productId;
        });
        saveCart(items);
        updateCartUi();
    }

    function clearCart() {
        sessionStorage.removeItem(storageKey);
        updateCartUi();
    }

    function showOrderError(message) {
        if (!cartOrderErrorEl) {
            return;
        }

        if (message) {
            cartOrderErrorEl.textContent = message;
            cartOrderErrorEl.hidden = false;
        } else {
            cartOrderErrorEl.textContent = '';
            cartOrderErrorEl.hidden = true;
        }
    }

    async function placeOrder() {
        const items = loadCart();
        if (items.length === 0) {
            return;
        }

        if (!tokenInput || !tokenInput.value) {
            showOrderError('Güvenlik doğrulaması başarısız. Sayfayı yenileyip tekrar deneyin.');
            return;
        }

        showOrderError('');
        cartPlaceOrderBtn.disabled = true;
        cartPlaceOrderBtn.textContent = 'Gönderiliyor...';

        const rewardRequestName = loadRewardRequest();
        const payload = {
            items: items.map(function (item) {
                return {
                    productId: item.productId,
                    quantity: item.quantity
                };
            }),
            customerName: customerNameInput ? customerNameInput.value.trim() : '',
            customerPhone: customerPhoneInput ? customerPhoneInput.value.trim() : '',
            notes: orderNotesInput ? orderNotesInput.value.trim() : '',
            rewardRequestName: rewardRequestName || null
        };

        try {
            const response = await fetch(orderUrl, {
                method: 'POST',
                headers: {
                    'Content-Type': 'application/json',
                    'RequestVerificationToken': tokenInput.value
                },
                body: JSON.stringify(payload)
            });

            const data = await response.json();

            if (!response.ok) {
                showOrderError(data.error || 'Sipariş kaydedilemedi. Lütfen tekrar deneyin.');
                return;
            }

            clearCart();
            saveRewardRequest('');

            if (customerNameInput) {
                customerNameInput.value = '';
            }
            if (customerPhoneInput) {
                customerPhoneInput.value = '';
            }
            if (orderNotesInput) {
                orderNotesInput.value = '';
            }

            if (cartOffcanvasEl) {
                const offcanvas = bootstrap.Offcanvas.getInstance(cartOffcanvasEl);
                if (offcanvas) {
                    offcanvas.hide();
                }
            }

            var redirectUrl = data.confirmationUrl || data.whatsAppUrl;
            if (redirectUrl) {
                window.location.href = redirectUrl;
            } else {
                showOrderError('Sipariş kaydedildi ancak yönlendirme bağlantısı alınamadı.');
            }
        } catch {
            showOrderError('Bağlantı hatası. Lütfen tekrar deneyin.');
        } finally {
            cartPlaceOrderBtn.textContent = 'WhatsApp ile Sipariş Ver';
            updateCartUi();
        }
    }

    document.querySelectorAll('.btn-add-to-cart').forEach(function (button) {
        button.addEventListener('click', function () {
            addToCart({
                productId: parseInt(button.dataset.productId, 10),
                name: button.dataset.productName,
                price: parseFloat(button.dataset.productPrice)
            });
        });
    });

    document.querySelectorAll('.public-reward-select-btn').forEach(function (button) {
        button.addEventListener('click', function () {
            const rewardName = button.dataset.rewardName || '';
            if (!rewardName) {
                return;
            }

            saveRewardRequest(rewardName);
            updateRewardBanner();
            schedulePricingPreview();

            if (cartOffcanvasEl) {
                const offcanvas = bootstrap.Offcanvas.getOrCreateInstance(cartOffcanvasEl);
                offcanvas.show();
            }
        });
    });

    if (clearRewardBtn) {
        clearRewardBtn.addEventListener('click', function () {
            saveRewardRequest('');
            updateRewardBanner();
            schedulePricingPreview();
        });
    }

    if (cartItemsContainer) {
        cartItemsContainer.addEventListener('click', function (event) {
            const target = event.target;
            if (!(target instanceof HTMLElement)) {
                return;
            }

            const row = target.closest('.cart-item');
            if (!row) {
                return;
            }

            const productId = parseInt(row.dataset.productId, 10);
            const action = target.dataset.action;

            if (action === 'increase') {
                changeQuantity(productId, 1);
            } else if (action === 'decrease') {
                changeQuantity(productId, -1);
            } else if (action === 'remove') {
                removeFromCart(productId);
            }
        });
    }

    if (cartClearBtn) {
        cartClearBtn.addEventListener('click', function () {
            clearCart();
            showOrderError('');
        });
    }

    if (cartPlaceOrderBtn) {
        cartPlaceOrderBtn.addEventListener('click', placeOrder);
    }

    updateCartUi();
})();
