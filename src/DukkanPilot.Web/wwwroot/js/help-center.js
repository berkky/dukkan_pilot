(function () {
    'use strict';

    function normalize(text) {
        return (text || '').toLocaleLowerCase('tr-TR').trim();
    }

    function initHelpCenter(root) {
        var input = root.querySelector('#helpSearchInput') || root.querySelector('[data-help-search]');
        if (!input) {
            return;
        }

        var cards = Array.prototype.slice.call(root.querySelectorAll('[data-help-card]'));
        var categories = Array.prototype.slice.call(root.querySelectorAll('[data-help-category]'));
        var noResults = root.querySelector('[data-help-no-results]');

        function applyFilter() {
            var query = normalize(input.value);
            var visibleCount = 0;

            cards.forEach(function (card) {
                var keywords = normalize(card.getAttribute('data-keywords'));
                var match = !query || keywords.indexOf(query) !== -1;
                card.classList.toggle('d-none', !match);
                if (match) {
                    visibleCount++;
                }
            });

            categories.forEach(function (section) {
                var sectionCards = section.querySelectorAll('[data-help-card]');
                var anyVisible = Array.prototype.some.call(sectionCards, function (c) {
                    return !c.classList.contains('d-none');
                });
                section.classList.toggle('d-none', !anyVisible && query.length > 0);
            });

            if (noResults) {
                noResults.classList.toggle('d-none', visibleCount > 0 || !query);
            }
        }

        input.addEventListener('input', applyFilter);
        input.addEventListener('search', applyFilter);
    }

    document.addEventListener('DOMContentLoaded', function () {
        var centers = document.querySelectorAll('[data-help-center]');
        centers.forEach(initHelpCenter);
    });
})();
