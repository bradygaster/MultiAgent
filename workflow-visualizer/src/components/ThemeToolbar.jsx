import React, { useEffect, useState } from 'react';

const THEME_KEY = 'theme';

function ThemeToolbar() {
  const [theme, setTheme] = useState(() => {
    if (typeof window !== 'undefined') {
      return localStorage.getItem(THEME_KEY) || 'light';
    }
    return 'light';
  });

  useEffect(() => {
    const isDark = theme === 'dark';
    const root = document.documentElement;
    root.classList.toggle('theme-dark', isDark);
    root.classList.toggle('theme-light', !isDark);
    try {
      localStorage.setItem(THEME_KEY, theme);
    } catch {}
  }, [theme]);

  const toggleTheme = () => {
    setTheme(t => (t === 'dark' ? 'light' : 'dark'));
  };

  return (
    <header className="app-toolbar" role="banner">
      <div className="app-toolbar__inner">
        <div className="app-toolbar__brand" aria-label="Application">
          <span className="app-toolbar__logo" role="img" aria-label="MultiAgent">🤖</span>
          <span className="app-toolbar__title">MultiAgent Workflow Visualizer</span>
        </div>
        <nav className="app-toolbar__nav" aria-label="Toolbar actions">
          <button
            type="button"
            className="theme-toggle-btn"
            onClick={toggleTheme}
            aria-pressed={theme === 'dark'}
            aria-label={theme === 'dark' ? 'Switch to light mode' : 'Switch to dark mode'}
          >
            {theme === 'dark' ? '🌞 Light' : '🌙 Dark'}
          </button>
        </nav>
      </div>
    </header>
  );
}

export default ThemeToolbar;