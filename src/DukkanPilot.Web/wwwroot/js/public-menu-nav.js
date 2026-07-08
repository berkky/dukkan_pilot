(function () {
    'use strict';

    const root = document.getElementById('public-menu-root');
    if (!root) {
        return;
    }

    const links = Array.from(document.querySelectorAll('[data-category-link]'));
    if (links.length === 0) {
        return;
    }

    const sections = links
        .map(function (link) {
            const id = link.getAttribute('data-category-link');
            const el = id ? document.getElementById(id) : null;
            return el ? { id: id, el: el, link: link } : null;
        })
        .filter(Boolean);

    if (sections.length === 0) {
        return;
    }

    function setActive(id) {
        links.forEach(function (l) {
            const match = l.getAttribute('data-category-link') === id;
            l.classList.toggle('is-active', match);
            if (match) {
                l.setAttribute('aria-current', 'true');
            } else {
                l.removeAttribute('aria-current');
            }
        });
    }

    // Default: first category
    setActive(sections[0].id);

    if (!('IntersectionObserver' in window)) {
        return;
    }

    const observer = new IntersectionObserver(function (entries) {
        // pick the most visible intersecting category
        const visible = entries
            .filter(function (e) { return e.isIntersecting; })
            .sort(function (a, b) { return (b.intersectionRatio || 0) - (a.intersectionRatio || 0); });

        if (visible.length > 0) {
            setActive(visible[0].target.id);
        }
    }, {
        root: null,
        threshold: [0.15, 0.35, 0.6]
    });

    sections.forEach(function (s) { observer.observe(s.el); });
})();

