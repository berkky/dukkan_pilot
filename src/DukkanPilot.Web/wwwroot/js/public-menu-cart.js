(function () {
    'use strict';

    const root = document.getElementById('public-menu-root');
    if (!root) {
        return;
    }

    const businessId = root.dataset.businessId;
    const orderUrl = root.dataset.orderUrl;
    const whatsAppNumber = root.dataset.whatsappNumber || '';
    const storageKey = 'cart-' + businessId;

    const cartBar = document.getElementById('cart-bar');
    const cartItemCountEl = document.getElementById('cart-item-count');
    const cartTotalEl = document.getElementById('cart-total');
    const cartItemsContainer = document.getElementById('cart-items');
    const cartEmptyEl = document.getElementById('cart-empty');
    const cartSummaryTotalEl = document.getElementById('cart-summary-total');
    const cartClearBtn = document.getElementById('cart-clear-btn');
    const cartPlaceOrderBtn = document.getElementById('cart-place-order-btn');
    const cartOrderErrorEl = document.getElementById('cart-order-error');
    const cartOffcanvasEl = document.getElementById('cartOffcanvas');
    const customerNameInput = document.getElementById('customer-name');
    const customerPhoneInput = document.getElementById('customer-phone');
    const orderNotesInput = document.getElementById('order-notes');
    const whatsAppWarningEl = document.getElementById('whatsapp-warning');
    const tokenInput = document.querySelector('input[name="__RequestVerificationToken"]');

    const currencyFormatter = new Intl.NumberFormat('tr-TR', {
        minimumFractionDigits: 2,
        maximumFractionDigits: 2
    });

    function formatPrice(amount) {
        return currencyFormatter.format(amount) + ' ₺';
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

    function getCartItemCount(items) {
        return items.reduce(function (sum, item) {
            return sum + item.quantity;
        }, 0);
    }

    function getCartTotal(items) {
        return items.reduce(function (sum, item) {
            return sum + item.quantity * item.price;
        }, 0);
    }

    function findCartItem(items, productId) {
        return items.find(function (item) {
            return item.productId === productId;
        });
    }

    function updateCartUi() {
        const items = loadCart();
        const itemCount = getCartItemCount(items);
        const total = getCartTotal(items);

        if (cartBar) {
            cartBar.hidden = itemCount === 0;
            root.classList.toggle('has-cart-bar', itemCount > 0);
        }

        if (cartItemCountEl) {
            cartItemCountEl.textContent = String(itemCount);
        }

        if (cartTotalEl) {
            cartTotalEl.textContent = formatPrice(total);
        }

        if (cartSummaryTotalEl) {
            cartSummaryTotalEl.textContent = formatPrice(total);
        }

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
            cartPlaceOrderBtn.disabled = itemCount === 0 || !hasWhatsApp;
        }
    }

    function escapeHtml(text) {
        const div = document.createElement('div');
        div.textContent = text;
        return div.innerHTML;
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

        const payload = {
            items: items.map(function (item) {
                return {
                    productId: item.productId,
                    quantity: item.quantity
                };
            }),
            customerName: customerNameInput ? customerNameInput.value.trim() : '',
            customerPhone: customerPhoneInput ? customerPhoneInput.value.trim() : '',
            notes: orderNotesInput ? orderNotesInput.value.trim() : ''
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

            window.location.href = data.whatsAppUrl;
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
