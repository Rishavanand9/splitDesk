export default function SplitResult({ result, onReset }) {
  if (!result) return null;

  return (
    <div>
      <div className="result-header">
        <div className="result-check">
          <div className="result-check-inner" />
        </div>
        <div className="result-bill-title">{result.billTitle}</div>
        <div className="result-total">£{result.totalAmount.toFixed(2)}</div>
      </div>

      <div className="result-body">
        {result.splits.map((split, i) => (
          <div key={i} className="split-row">
            <div className="split-avatar">{split.personName[0].toUpperCase()}</div>
            <div className="split-name">{split.personName}</div>
            <div className="split-amount">£{split.amountOwed.toFixed(2)}</div>
          </div>
        ))}
      </div>

      <div className="sticky-footer">
        <button className="btn-primary" onClick={onReset}>Split Another Bill</button>
      </div>
    </div>
  );
}
