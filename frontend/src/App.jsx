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
        <div className="header-logo">split<span>Desk</span></div>
        <div className="header-sub">Fair splits, every time</div>
      </header>

      <main className="app-main">
        {error && <div className="error-banner">{error}</div>}

        {result
          ? <SplitResult result={result} onReset={() => { setResult(null); setError(null); }} />
          : <BillForm onSubmit={handleSubmit} loading={loading} />
        }
      </main>
    </div>
  );
}
