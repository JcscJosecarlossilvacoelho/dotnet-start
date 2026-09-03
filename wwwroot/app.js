// Progressive enhancement for a statically rendered site. Every feature here was
// once a Blazor interactive component holding a SignalR circuit open; none of it
// needs a server now, which is what lets the whole site ship as flat HTML.
(() => {
  "use strict";

  const FEEDBACK_PREFIX = "dotnet-start:feedback:";

  const store = {
    get(key) {
      try { return localStorage.getItem(key); } catch { return null; }
    },
    set(key, value) {
      try { localStorage.setItem(key, value); } catch { /* private mode */ }
    }
  };

  // --- copy buttons --------------------------------------------------------
  async function copy(text) {
    try {
      await navigator.clipboard.writeText(text);
      return true;
    } catch {
      const textarea = document.createElement("textarea");
      textarea.value = text;
      textarea.setAttribute("readonly", "");
      textarea.style.position = "fixed";
      textarea.style.opacity = "0";
      document.body.appendChild(textarea);
      textarea.select();
      const copied = document.execCommand("copy");
      textarea.remove();
      return copied;
    }
  }

  function initCopy() {
    document.addEventListener("click", async (event) => {
      const button = event.target.closest("[data-copy]");
      if (!button) return;

      if (!(await copy(button.dataset.copy))) return;

      button.classList.add("is-copied");
      const label = button.querySelector("span");
      if (label) label.textContent = "Copied";

      clearTimeout(button._copyTimer);
      button._copyTimer = setTimeout(() => {
        button.classList.remove("is-copied");
        if (label) label.textContent = "Copy";
      }, 1600);
    });
  }

  // --- feedback prompt -----------------------------------------------------
  function showFeedbackState(block, state) {
    block.querySelectorAll("[data-feedback-state]").forEach((panel) => {
      panel.hidden = panel.dataset.feedbackState !== state;
    });
  }

  function initFeedback() {
    document.querySelectorAll("[data-feedback]").forEach((block) => {
      const vote = store.get(FEEDBACK_PREFIX + block.dataset.feedback);
      if (vote === "yes" || vote === "no") showFeedbackState(block, vote);
    });

    document.addEventListener("click", (event) => {
      const button = event.target.closest("[data-feedback-vote]");
      if (!button) return;

      const block = button.closest("[data-feedback]");
      if (!block) return;

      const vote = button.dataset.feedbackVote;
      store.set(FEEDBACK_PREFIX + block.dataset.feedback, vote);
      showFeedbackState(block, vote);
    });
  }

  // --- search palette ------------------------------------------------------
  const search = {
    index: null,
    loading: null,
    results: [],
    active: 0,

    overlay: () => document.querySelector("[data-search-overlay]"),
    input: () => document.querySelector("[data-search-input]"),
    list: () => document.querySelector("[data-search-results]"),

    async load() {
      if (this.index) return this.index;
      if (!this.loading) {
        this.loading = fetch("/search-index.json")
          .then((response) => (response.ok ? response.json() : []))
          .then((rows) => {
            this.index = rows.map((row) => ({
              ...row,
              haystack: `${row.t} ${row.d} ${row.s}`.toLowerCase()
            }));
            return this.index;
          })
          .catch(() => (this.index = []));
      }
      return this.loading;
    },

    open() {
      const overlay = this.overlay();
      if (!overlay || !overlay.hidden) return;

      overlay.hidden = false;
      document.body.style.overflow = "hidden";

      const input = this.input();
      if (input) {
        input.value = "";
        input.focus();
      }

      this.load().then(() => this.query(""));
    },

    close() {
      const overlay = this.overlay();
      if (!overlay || overlay.hidden) return;
      overlay.hidden = true;
      document.body.style.overflow = "";
    },

    toggle() {
      const overlay = this.overlay();
      if (!overlay) return;
      if (overlay.hidden) {
        this.open();
      } else {
        this.close();
      }
    },

    query(text) {
      const rows = this.index || [];
      const words = text.toLowerCase().split(/\s+/).filter(Boolean);

      this.results = words.length
        ? rows.filter((row) => words.every((word) => row.haystack.includes(word))).slice(0, 8)
        : rows.slice(0, 8);

      this.active = 0;
      this.render(text);
    },

    render(text) {
      const list = this.list();
      if (!list) return;

      if (!this.results.length) {
        const empty = document.createElement("p");
        empty.className = "search-empty";
        empty.textContent = `No pages match "${text}".`;
        list.replaceChildren(empty);
        return;
      }

      // Built as nodes rather than innerHTML: titles and descriptions come from
      // contributor-authored Markdown front matter.
      list.replaceChildren(...this.results.map((row, i) => {
        const hit = document.createElement("a");
        hit.className = "search-hit" + (i === this.active ? " active" : "");
        hit.href = row.h;
        hit.setAttribute("role", "option");
        hit.setAttribute("aria-selected", String(i === this.active));

        const title = document.createElement("b");
        title.textContent = row.t;
        const description = document.createElement("span");
        description.textContent = row.d;
        const slug = document.createElement("small");
        slug.textContent = row.s;

        hit.append(title, description, slug);
        return hit;
      }));
    },

    move(delta) {
      if (!this.results.length) return;
      this.active = (this.active + delta + this.results.length) % this.results.length;

      const hits = [...this.list().querySelectorAll(".search-hit")];
      hits.forEach((hit, i) => {
        hit.classList.toggle("active", i === this.active);
        hit.setAttribute("aria-selected", String(i === this.active));
      });
      hits[this.active]?.scrollIntoView({ block: "nearest" });
    }
  };

  function initSearch() {
    document.addEventListener("click", (event) => {
      if (event.target.closest("[data-search-open]")) {
        event.preventDefault();
        search.open();
        return;
      }
      // Backdrop only — a click inside the dialog must not close it.
      const overlay = event.target.closest("[data-search-overlay]");
      if (overlay && !event.target.closest(".search-dialog")) search.close();
    });

    const input = search.input();
    if (input) {
      input.addEventListener("input", () => search.query(input.value));
    }

    window.addEventListener("keydown", (event) => {
      const key = event.key.toLowerCase();

      if ((event.metaKey || event.ctrlKey) && key === "k") {
        event.preventDefault();
        search.toggle();
        return;
      }

      const overlay = search.overlay();
      if (!overlay || overlay.hidden) return;

      if (event.key === "Escape") {
        event.preventDefault();
        search.close();
      } else if (event.key === "ArrowDown") {
        event.preventDefault();
        search.move(1);
      } else if (event.key === "ArrowUp") {
        event.preventDefault();
        search.move(-1);
      } else if (event.key === "Enter") {
        const target = search.results[search.active];
        if (target) {
          event.preventDefault();
          window.location.href = target.h;
        }
      }
    });
  }

  // --- navigation skeleton -------------------------------------------------
  // Until the next document paints, swap the article for a placeholder — after a
  // grace period, so a fast CDN response never flashes one.
  const docsLoading = {
    GRACE_MS: 120,
    _timer: null,

    article() {
      const rows = [
        ["180px", "10px", "0"], ["70%", "38px", "26px"], ["90%", "12px", "22px"],
        ["80%", "12px", "0"], ["45%", "18px", "34px"], ["100%", "11px", "0"],
        ["96%", "11px", "0"], ["60%", "11px", "0"]
      ];
      const wrap = document.createElement("div");
      wrap.className = "doc-skeleton";
      wrap.setAttribute("aria-hidden", "true");
      rows.forEach(([w, h, top]) => {
        const bar = document.createElement("span");
        bar.className = "skeleton";
        bar.style.cssText = `width:${w};height:${h};margin-top:${top}`;
        wrap.appendChild(bar);
      });
      return wrap;
    },

    show() {
      const article = document.querySelector(".doc-article");
      if (article) article.replaceChildren(docsLoading.article());
      const toc = document.querySelector(".docs-toc");
      if (toc) toc.replaceChildren();
    },

    init() {
      document.addEventListener("click", (event) => {
        if (event.defaultPrevented || event.button !== 0) return;
        if (event.metaKey || event.ctrlKey || event.shiftKey || event.altKey) return;

        const link = event.target.closest("a[href]");
        if (!link || link.target === "_blank" || link.hasAttribute("download")) return;

        const url = new URL(link.href, document.baseURI);
        if (url.origin !== location.origin) return;
        if (!url.pathname.startsWith("/docs")) return;
        if (url.pathname === location.pathname) return;

        clearTimeout(this._timer);
        this._timer = setTimeout(docsLoading.show, this.GRACE_MS);
      });

      // Restoring from the back/forward cache must not leave a skeleton behind.
      window.addEventListener("pageshow", () => clearTimeout(this._timer));
    }
  };

  function init() {
    initCopy();
    initFeedback();
    initSearch();
    docsLoading.init();

    // /docs#search opens the palette straight away, so the search is linkable.
    if (location.hash === "#search") search.open();
  }

  if (document.readyState === "loading") {
    document.addEventListener("DOMContentLoaded", init);
  } else {
    init();
  }
})();
