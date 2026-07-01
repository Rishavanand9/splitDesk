import { useState } from 'react';
import PeopleInput from './PeopleInput';
import ItemInput from './ItemInput';
import ScanUpload from './ScanUpload';
import PaidBySelect from './PaidBySelect';

// Indian restaurant bills always carry 5% GST (2.5% CGST + 2.5% SGST) —
// default to it so it's never accidentally left out, while still letting
// the user override it (e.g. a bill that's already tax-inclusive).
const DEFAULT_GST_PERCENT = '5';

export default function BillForm({ onSubmit, loading }) {
  const [title, setTitle]         = useState('');
  const [taxPercent, setTaxPercent] = useState(DEFAULT_GST_PERCENT);
  const [tipPercent, setTipPercent] = useState('');
  const [people, setPeople]       = useState([]);
  const [items, setItems]         = useState([]);
  const [paidBy, setPaidBy]       = useState('');
  const [showErrors, setShowErrors] = useState(false);

  function addPerson(name) { setPeople((prev) => [...prev, name]); }

  function removePerson(index) {
    const removed = people[index];
    setPeople((prev) => prev.filter((_, i) => i !== index));
    setItems((prev) => prev.map((item) => ({
      ...item,
      consumers: (item.consumers ?? []).filter((c) => c !== removed),
    })));
    setPaidBy((prev) => (prev === removed ? '' : prev));
  }

  function addItem(item) { setItems((prev) => [...prev, item]); }

  function toggleItemConsumer(index, person) {
    setItems((prev) => prev.map((item, i) => {
      if (i !== index) return item;
      const consumers = item.consumers ?? [];
      return {
        ...item,
        consumers: consumers.includes(person)
          ? consumers.filter((c) => c !== person)
          : [...consumers, person],
      };
    }));
  }

  // Called by ScanUpload when OCR finishes — pre-fills detected values.
  // Scanned items only have {name, price} — OCR can't know who consumed what —
  // so default consumers to everyone already added; the user can uncheck as needed.
  function handleScanComplete(scanResult) {
    if (scanResult.items?.length) {
      setItems(scanResult.items.map((item) => ({ ...item, consumers: [...people] })));
    }
    if (scanResult.taxPercent != null) setTaxPercent(String(scanResult.taxPercent));
    if (scanResult.tipPercent != null) setTipPercent(String(scanResult.tipPercent));
  }

  // Derived fresh from current state on every render — so once an invalid
  // submit attempt reveals errors, they clear themselves the instant the
  // user actually fixes the form, instead of lingering as a stale message.
  const errors = [];
  if (!title.trim()) errors.push('Enter a bill title.');
  if (people.length === 0) errors.push('Add at least one person.');
  if (items.length === 0) errors.push('Add at least one item.');
  if (items.some((item) => (item.consumers ?? []).length === 0)) {
    errors.push('Every item needs at least one person assigned to it.');
  }
  if (!paidBy) errors.push('Select who paid the bill.');

  function handleCalculate() {
    if (errors.length > 0) {
      setShowErrors(true);
      return;
    }
    setShowErrors(false);
    onSubmit({
      title,
      taxPercent: parseFloat(taxPercent) || 0,
      tipPercent: parseFloat(tipPercent) || 0,
      people,
      items,
      paidBy,
    });
  }

  return (
    <div>
      {/* Bill details */}
      <div className="card">
        <div className="card-title">Bill details</div>

        <div className="field">
          <input
            type="text"
            placeholder=" "
            value={title}
            onChange={(e) => setTitle(e.target.value)}
          />
          <label>Bill title (e.g. Dinner at Nando's)</label>
        </div>

        <div className="field-row">
          <div className="field">
            <input
              type="number"
              placeholder=" "
              value={taxPercent}
              min="0"
              max="100"
              step="0.1"
              onChange={(e) => setTaxPercent(e.target.value)}
              className={taxPercent !== '' ? 'has-value' : ''}
            />
            <label>GST % (CGST+SGST)</label>
          </div>
          <div className="field">
            <input
              type="number"
              placeholder=" "
              value={tipPercent}
              min="0"
              max="100"
              step="0.1"
              onChange={(e) => setTipPercent(e.target.value)}
              className={tipPercent !== '' ? 'has-value' : ''}
            />
            <label>Tip %</label>
          </div>
        </div>
        <p className="hint">Indian restaurant bills carry 5% GST by default &mdash; adjust if yours differs.</p>
      </div>

      {/* Bill scan */}
      <ScanUpload onScanComplete={handleScanComplete} />

      {/* People */}
      <PeopleInput people={people} onAdd={addPerson} onRemove={removePerson} />

      {/* Who paid */}
      <PaidBySelect people={people} paidBy={paidBy} onChange={setPaidBy} />

      {/* Items */}
      <ItemInput people={people} onAdd={addItem} />

      {/* Items summary */}
      {items.length > 0 && (
        <div className="card">
          <div className="card-title">{items.length} item{items.length > 1 ? 's' : ''}</div>
          <ul className="item-list">
            {items.map((item, i) => {
              const consumers = item.consumers ?? [];
              return (
                <li key={i} className="item-row">
                  <div className="item-icon">{item.name.slice(0, 3).toUpperCase()}</div>
                  <div className="item-info">
                    <div className="item-name">{item.name}</div>
                    {people.length > 0 ? (
                      <div className="consumers-grid">
                        {people.map((person) => {
                          const checked = consumers.includes(person);
                          return (
                            <label key={person} className={`consumer-toggle ${checked ? 'checked' : ''}`}>
                              <input
                                type="checkbox"
                                checked={checked}
                                onChange={() => toggleItemConsumer(i, person)}
                              />
                              <span className="consumer-dot" />
                              {person}
                            </label>
                          );
                        })}
                      </div>
                    ) : (
                      <p className="hint warning">Add people above to assign who had this.</p>
                    )}
                  </div>
                  <div className="item-price">₹{item.price.toFixed(2)}</div>
                </li>
              );
            })}
          </ul>
        </div>
      )}

      {showErrors && errors.length > 0 && (
        <div className="error-banner">
          <ul className="error-list">
            {errors.map((message) => <li key={message}>{message}</li>)}
          </ul>
        </div>
      )}

      <div className="sticky-footer">
        <button className="btn-primary" onClick={handleCalculate} disabled={loading}>
          {loading ? 'Calculating...' : 'Calculate Split'}
        </button>
      </div>
    </div>
  );
}
