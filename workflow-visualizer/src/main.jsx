import React from 'react';
import ReactDOM from 'react-dom/client';
import App from './App';
import './index.css';

// Apply stored theme before React renders to avoid flash
const storedTheme = (typeof window !== 'undefined' && localStorage.getItem('theme')) || 'light';
document.documentElement.classList.add(storedTheme === 'dark' ? 'theme-dark' : 'theme-light');

ReactDOM.createRoot(document.getElementById('root')).render(
  <React.StrictMode>
    <App />
  </React.StrictMode>
);
