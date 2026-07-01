import { useRef, useState } from 'react';
import { scanBill } from '../services/billApi';

// Props:
//   onScanComplete — (scanResult) => void
//   Called with { items: [{name, price}], taxPercent, tipPercent }
//   so BillForm can pre-fill detected values
export default function ScanUpload({ onScanComplete }) {
  const [preview, setPreview]       = useState(null);
  const [processing, setProcessing] = useState(false);
  const [scanned, setScanned]       = useState(false);
  const [dragOver, setDragOver]     = useState(false);
  const [error, setError]           = useState(null);
  const inputRef = useRef(null);

  async function processFile(file) {
    if (!file || !file.type.startsWith('image/')) return;
    setPreview(URL.createObjectURL(file));
    setProcessing(true);
    setScanned(false);
    setError(null);
    try {
      const result = await scanBill(file);
      setScanned(true);
      onScanComplete(result);
    } catch (err) {
      setError(err.message || 'Could not read bill from image');
    } finally {
      setProcessing(false);
    }
  }

  function handleFileChange(e) { processFile(e.target.files?.[0]); }
  function handleDrop(e) {
    e.preventDefault();
    setDragOver(false);
    processFile(e.dataTransfer.files?.[0]);
  }

  return (
    <div className="card">
      <div className="card-title">Scan bill image</div>

      <div
        className={`scan-area ${dragOver ? 'drag-over' : ''}`}
        onClick={() => inputRef.current?.click()}
        onDragOver={(e) => { e.preventDefault(); setDragOver(true); }}
        onDragLeave={() => setDragOver(false)}
        onDrop={handleDrop}
        role="button"
        tabIndex={0}
        onKeyDown={(e) => e.key === 'Enter' && inputRef.current?.click()}
      >
        <input
          ref={inputRef}
          type="file"
          accept="image/*"
          className="scan-input"
          onChange={handleFileChange}
        />

        {!preview && (
          <>
            <div className="scan-title">Upload or drag a receipt image</div>
            <div className="scan-sub">JPG, PNG or HEIC — items will be detected automatically</div>
          </>
        )}

        {preview && (
          <div className="scan-preview">
            <img src={preview} alt="Bill preview" />
          </div>
        )}

        {processing && <div className="scan-processing">Reading bill...</div>}

        {scanned && !processing && (
          <div className="scan-result-badge">
            Items detected — form pre-filled below
          </div>
        )}

        {error && <div className="hint warning" style={{ marginTop: 8 }}>{error}</div>}
      </div>
    </div>
  );
}
