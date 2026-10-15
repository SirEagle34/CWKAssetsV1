(() => {
  "use strict";
  const search = document.querySelector("[data-search-input]");
  const count = document.querySelector("[data-result-count]");
  const empty = document.querySelector("[data-empty-state]");
  const catalogue = document.querySelector("[data-catalogue]");
  if (!search || !count || !empty || !catalogue) return;

  // Catalogue entries will be populated from edition-specific API data in a later step.
  // Until then, keep the empty state honest rather than displaying invented game data.
  search.addEventListener("input", () => {
    const hasQuery = search.value.trim().length > 0;
    count.textContent = hasQuery ? "No connected entries to search" : "Catalogue preview";
    empty.hidden = false;
    catalogue.replaceChildren();
  });
})();