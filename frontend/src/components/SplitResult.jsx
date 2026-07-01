export default function SplitResult({ result, onReset }) {
  if (!result) return null;

  const settlements = result.settlements ?? [];

  return (
    <div>
      <div className="result-header">
        <div className="result-check">
          <div className="result-check-inner" />
        </div>
        <div className="result-bill-title">{result.billTitle}</div>
        <div className="result-total">₹{result.totalAmount.toFixed(2)}</div>
        {result.paidBy && (
          <div className="result-paid-by">Paid by <span className="name">{result.paidBy}</span></div>
        )}
      </div>

      <div className="result-body">
        {result.splits.map((split, i) => (
          <div key={i} className="split-row">
            <div className="split-avatar">{split.personName[0].toUpperCase()}</div>
            <div className="split-name">{split.personName}</div>
            <div className="split-amount">₹{split.amountOwed.toFixed(2)}</div>
          </div>
        ))}
      </div>

      <div className="settlements-title">Who owes whom</div>
      <div className="result-body">
        {settlements.length === 0 && (
          <div className="settlement-empty">Everyone's settled up — no payments needed.</div>
        )}
        {settlements.map((s, i) => (
          <div key={i} className="settlement-row">
            <div className="settlement-text">
              <span className="name">{s.fromPerson}</span>
              <span className="settlement-arrow"> owes </span>
              <span className="name">{s.toPerson}</span>
            </div>
            <div className="settlement-amount">₹{s.amount.toFixed(2)}</div>
          </div>
        ))}
      </div>

      <div className="sticky-footer">
        <button className="btn-primary" onClick={onReset}>Split Another Bill</button>
      </div>
    </div>
  );
}
