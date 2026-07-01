export default function PaidBySelect({ people, paidBy, onChange }) {
  if (people.length === 0) return null;

  return (
    <div className="card">
      <div className="card-title">Who paid the bill?</div>

      <div className="people-grid">
        {people.map((person, i) => {
          const selected = paidBy === person;
          return (
            <button
              key={i}
              type="button"
              className={`person-chip payer-chip ${selected ? 'selected' : ''}`}
              onClick={() => onChange(person)}
              aria-pressed={selected}
            >
              <div className="avatar">{person[0].toUpperCase()}</div>
              {person}
            </button>
          );
        })}
      </div>
    </div>
  );
}
