window.dotnetStart = {
  copy: async (text) => {
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
  },

  bindSearchKeys(dotNet) {
    window.dotnetStart.unbindSearchKeys();
    const onKey = (event) => {
      const key = event.key.toLowerCase();
      if ((event.metaKey || event.ctrlKey) && key === "k") {
        event.preventDefault();
        dotNet.invokeMethodAsync("ToggleFromKeyboard");
        return;
      }
      if (event.key === "Escape") {
        dotNet.invokeMethodAsync("CloseFromKeyboard");
      }
    };
    window.dotnetStart._searchKeyHandler = onKey;
    window.addEventListener("keydown", onKey);
  },

  unbindSearchKeys() {
    if (window.dotnetStart._searchKeyHandler) {
      window.removeEventListener("keydown", window.dotnetStart._searchKeyHandler);
      window.dotnetStart._searchKeyHandler = null;
    }
  },

  // ---------------------------------------------------------------------------
  // Navigation skeletons.
  //
  // Doc pages are static-SSR + enhanced navigation, so a click is a fetch-and-patch.
  // That is fast, but not free: until the response lands the old article is still on
  // screen and nothing says the click registered. Swap in a skeleton instead — after
  // a short grace period, so a cached, instant navigation never flashes one.
  // ---------------------------------------------------------------------------
  docsLoading: {
    GRACE_MS: 90,
    _timer: null,

    article() {
      return (
        '<div class="doc-skeleton" aria-hidden="true">' +
        '<span class="skeleton" style="width:180px;height:10px"></span>' +
        '<span class="skeleton" style="width:70%;height:38px;margin-top:26px"></span>' +
        '<span class="skeleton" style="width:90%;height:12px;margin-top:22px"></span>' +
        '<span class="skeleton" style="width:80%;height:12px"></span>' +
        '<div class="doc-skeleton-block">' +
        '<span class="skeleton" style="width:45%;height:18px"></span>' +
        '<span class="skeleton" style="width:100%;height:11px"></span>' +
        '<span class="skeleton" style="width:96%;height:11px"></span>' +
        '<span class="skeleton" style="width:60%;height:11px"></span>' +
        '</div>' +
        '<span class="skeleton" style="width:100%;height:120px;border-radius:10px"></span>' +
        '<div class="doc-skeleton-block">' +
        '<span class="skeleton" style="width:38%;height:18px"></span>' +
        '<span class="skeleton" style="width:100%;height:11px"></span>' +
        '<span class="skeleton" style="width:88%;height:11px"></span>' +
        '<span class="skeleton" style="width:52%;height:11px"></span>' +
        '</div>' +
        '</div>'
      );
    },

    toc() {
      const rows = [82, 64, 92, 71, 58];
      return (
        '<div class="toc-skeleton" aria-hidden="true">' +
        rows
          .map((w) => '<span class="skeleton" style="width:' + w + '%;height:9px"></span>')
          .join('') +
        '</div>'
      );
    },

    show() {
      const article = document.querySelector('.doc-article');
      if (!article) return;
      article.innerHTML = window.dotnetStart.docsLoading.article();
      const toc = document.querySelector('.docs-toc');
      if (toc) toc.innerHTML = window.dotnetStart.docsLoading.toc();
      document.documentElement.classList.add('docs-navigating');
    },

    clear() {
      const state = window.dotnetStart.docsLoading;
      if (state._timer) {
        clearTimeout(state._timer);
        state._timer = null;
      }
      document.documentElement.classList.remove('docs-navigating');
    },

    arm() {
      const state = window.dotnetStart.docsLoading;

      document.addEventListener(
        'click',
        (event) => {
          if (event.defaultPrevented || event.button !== 0) return;
          if (event.metaKey || event.ctrlKey || event.shiftKey || event.altKey) return;

          const link = event.target.closest && event.target.closest('a[href]');
          if (!link || link.target === '_blank' || link.hasAttribute('download')) return;

          const url = new URL(link.href, document.baseURI);
          if (url.origin !== location.origin) return;
          if (!url.pathname.startsWith('/docs')) return;
          if (url.pathname === location.pathname) return;

          state.clear();
          state._timer = setTimeout(state.show, state.GRACE_MS);
        },
        true
      );

      window.addEventListener('popstate', state.clear);
      state._bindEnhancedLoad(0);
    },

    // blazor.web.js may not have defined window.Blazor yet when the page is parsed.
    _bindEnhancedLoad(attempt) {
      const state = window.dotnetStart.docsLoading;
      if (window.Blazor && window.Blazor.addEventListener) {
        window.Blazor.addEventListener('enhancedload', state.clear);
        return;
      }
      if (attempt > 40) return;
      setTimeout(() => state._bindEnhancedLoad(attempt + 1), 50);
    }
  },

  feedback: {
    get(slug) {
      return localStorage.getItem("dotnet-start:feedback:" + slug);
    },
    set(slug, value) {
      localStorage.setItem("dotnet-start:feedback:" + slug, value);
    }
  }
};

document.addEventListener("DOMContentLoaded", () => window.dotnetStart.docsLoading.arm());
