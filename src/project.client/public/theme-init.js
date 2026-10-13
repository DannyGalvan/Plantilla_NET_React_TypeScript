// Applies the last theme before React loads to avoid a flash of the wrong colors.
// Kept as a static file (no inline script) so the Content-Security-Policy stays strict.
(function () {
  try {
    var stored = JSON.parse(localStorage.getItem('theme-preferences') || '{}');
    var root = document.documentElement;
    var mode = stored.mode || 'system';
    var dark = mode === 'dark' || (mode === 'system' && window.matchMedia('(prefers-color-scheme: dark)').matches);
    root.classList.toggle('dark', dark);
    root.dataset.theme = stored.preset || 'default';
    root.dataset.density = stored.density || 'comfortable';
    if (stored.radius) root.style.setProperty('--radius', stored.radius);
  } catch (e) {
    // Storage may be blocked: the provider applies the defaults later.
  }
})();
