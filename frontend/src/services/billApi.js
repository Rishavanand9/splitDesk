// In Docker: VITE_API_URL="" so BASE="" and all calls are relative (/api/...)
// nginx proxies /api/ to the API container — no CORS needed.
// In local dev without Docker: set VITE_API_URL=http://localhost:5000 in .env.local
const BASE = import.meta.env.VITE_API_URL ?? '';

async function handleResponse(response) {
  if (!response.ok) {
    const text = await response.text();
    let message;
    try {
      const json = JSON.parse(text);
      message = json.error || json.title || text;
    } catch {
      message = text || `HTTP ${response.status}`;
    }
    throw new Error(message);
  }
  return response.json();
}

export async function calculateSplit(bill) {
  const response = await fetch(`${BASE}/api/bills/split`, {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify(bill),
  });
  return handleResponse(response);
}

export async function scanBill(imageFile) {
  const formData = new FormData();
  formData.append('image', imageFile);
  const response = await fetch(`${BASE}/api/scan`, {
    method: 'POST',
    body: formData,
  });
  return handleResponse(response);
}
