import { useState } from 'react';

export default function PeopleInput({ people, onAdd, onRemove }) {
  const [name, setName] = useState('');

  function handleAdd() {
    const trimmed = name.trim();
    if (!trimmed || people.includes(trimmed)) return;
    onAdd(trimmed);
    setName('');
  }

  return (
    <div className="card">
      <div className="card-title">Who is splitting?</div>

      <div className="input-add-row">
        <div className="field">
          <input
            type="text"
            placeholder=" "
            value={name}
            onChange={(e) => setName(e.target.value)}
            onKeyDown={(e) => e.key === 'Enter' && handleAdd()}
          />
          <label>Person name</label>
        </div>
        <button className="btn-icon" onClick={handleAdd} aria-label="Add person">+</button>
      </div>

      {people.length === 0 && (
        <div className="empty-state">Add at least one person to continue</div>
      )}

      <div className="people-grid">
        {people.map((person, i) => (
          <div key={i} className="person-chip">
            <div className="avatar">{person[0].toUpperCase()}</div>
            {person}
            <button className="chip-remove" onClick={() => onRemove(i)} aria-label={`Remove ${person}`}>
              &times;
            </button>
          </div>
        ))}
      </div>
    </div>
  );
}
