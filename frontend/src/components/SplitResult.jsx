export default function SplitResult({ result, onReset }) {
  if (!result) return null;

  const settlements = result.settlements ?? [];
  const breakdown = result.breakdown;
  const halfTaxPercent = breakdown ? (breakdown.taxPercent / 2).toFixed(1) : null;

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

      {breakdown && (
        <>
          <div className="settlements-title">How this was calculated</div>
          <div className="result-body">
            <div className="breakdown-row">
              <span>Subtotal</span>
              <span>₹{breakdown.subtotal.toFixed(2)}</span>
            </div>
            <div className="breakdown-row">
              <span>CGST ({halfTaxPercent}%)</span>
              <span>₹{breakdown.cgstAmount.toFixed(2)}</span>
            </div>
            <div className="breakdown-row">
              <span>SGST ({halfTaxPercent}%)</span>
              <span>₹{breakdown.sgstAmount.toFixed(2)}</span>
            </div>
            {breakdown.tipAmount > 0 && (
              <div className="breakdown-row">
                <span>Tip ({breakdown.tipPercent}%)</span>
                <span>₹{breakdown.tipAmount.toFixed(2)}</span>
              </div>
            )}
            <div className="breakdown-row breakdown-total">
              <span>Grand Total</span>
              <span>₹{breakdown.grandTotal.toFixed(2)}</span>
            </div>
          </div>
        </>
      )}

      <div className="settlements-title">Per person</div>
      <div className="result-body">
        {result.splits.map((split, i) => (
          <div key={i} className="split-row">
            <div className="split-avatar">{split.personName[0].toUpperCase()}</div>
            <div className="split-info">
              <div className="split-name">{split.personName}</div>
              <div className="split-calc">
                ₹{split.subtotal.toFixed(2)} + ₹{split.taxShare.toFixed(2)} GST
                {split.tipShare > 0 && ` + ₹${split.tipShare.toFixed(2)} tip`}
              </div>
            </div>
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
