import { useState } from 'react';
import BillForm from './components/BillForm';
import SplitResult from './components/SplitResult';
import { calculateSplit } from './services/billApi';
import './index.css';

export default function App() {
  const [result, setResult]   = useState(null);
  const [loading, setLoading] = useState(false);
  const [error, setError]     = useState(null);

  async function handleSubmit(bill) {
    setLoading(true);
    setError(null);
    setResult(null);
    try {
      const data = await calculateSplit(bill);
      setResult(data);
    } catch (err) {
      setError(err.message);
    } finally {
      setLoading(false);
    }
  }

  return (
    <div className="app">
      <header className="app-header">
        <div className="logo-mark" aria-hidden="true">
          <svg viewBox="0 0 32 32" width="32" height="32" fill="none">
            <rect x="1" y="1" width="30" height="30" rx="9" fill="rgba(255,255,255,0.16)" stroke="rgba(255,255,255,0.5)" strokeWidth="1.2" />
            <path d="M16 6V26" stroke="#fff" strokeWidth="1.4" strokeDasharray="2.5 2.5" strokeLinecap="round" />
            <circle cx="10.5" cy="16" r="4.5" fill="#fff" fillOpacity="0.92" />
            <circle cx="21.5" cy="16" r="4.5" fill="#fff" fillOpacity="0.55" />
          </svg>
        </div>
        <div className="header-text">
          <div className="header-logo">split<span>Desk</span></div>
          <div className="header-sub">Fair splits, every time</div>
        </div>
      </header>

      <main className="app-main">
        {error && <div className="error-banner">{error}</div>}

        {result
          ? <SplitResult result={result} onReset={() => { setResult(null); setError(null); }} />
          : <BillForm onSubmit={handleSubmit} loading={loading} />
        }

        <footer className="app-footer">
          <div className="footer-copyright">&copy; {new Date().getFullYear()} SplitDesk. All rights reserved.</div>
          <div className="footer-tagline">For casual bill splitting only &mdash; not a financial service.</div>
        </footer>
      </main>
    </div>
  );
}
