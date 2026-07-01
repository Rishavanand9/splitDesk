import { useState } from 'react';

export default function ItemInput({ people, onAdd }) {
  const [itemName, setItemName] = useState('');
  const [price, setPrice]       = useState('');
  const [consumers, setConsumers] = useState([]);

  function toggleConsumer(person) {
    setConsumers((prev) =>
      prev.includes(person) ? prev.filter((p) => p !== person) : [...prev, person]
    );
  }

  function handleAdd() {
    const trimmedName = itemName.trim();
    const parsedPrice = parseFloat(price);
    if (!trimmedName || isNaN(parsedPrice) || parsedPrice <= 0 || consumers.length === 0) return;
    onAdd({ name: trimmedName, price: parsedPrice, consumers });
    setItemName('');
    setPrice('');
    setConsumers([]);
  }

  return (
    <div className="card">
      <div className="card-title">Add items</div>

      <div className="input-add-row">
        <div className="field" style={{ flex: 2 }}>
          <input
            type="text"
            placeholder=" "
            value={itemName}
            onChange={(e) => setItemName(e.target.value)}
          />
          <label>Item name</label>
        </div>
        <div className="field has-prefix" style={{ flex: 1 }}>
          <span className="field-prefix">₹</span>
          <input
            type="number"
            placeholder=" "
            value={price}
            min="0"
            step="0.01"
            onChange={(e) => setPrice(e.target.value)}
          />
          <label>Price</label>
        </div>
        <button
          className="btn-icon"
          onClick={handleAdd}
          disabled={people.length === 0}
          aria-label="Add item"
        >+</button>
      </div>

      {people.length > 0 && (
        <>
          <div className="consumers-label">Who had this?</div>
          <div className="consumers-grid">
            {people.map((person) => {
              const checked = consumers.includes(person);
              return (
                <label key={person} className={`consumer-toggle ${checked ? 'checked' : ''}`}>
                  <input type="checkbox" checked={checked} onChange={() => toggleConsumer(person)} />
                  <span className="consumer-dot" />
                  {person}
                </label>
              );
            })}
          </div>
        </>
      )}

      {people.length === 0 && (
        <p className="hint warning">Add people above before adding items.</p>
      )}
    </div>
  );
}
