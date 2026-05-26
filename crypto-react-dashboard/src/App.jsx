import { useState, useMemo } from 'react';
import './App.css';

const API_BASE = 'https://localhost:7123/api/prices';

const EXCHANGE_NAMES = [
  'Binance', 'Bybit', 'Kraken', 'Coinbase', 'OKX',
  'Huobi', 'Gate.io', 'MEXC', 'Bitstamp', 'WhiteBIT',
];

const fmtMs = (n) => n == null ? '—' : Math.round(n).toLocaleString();
const fmtPrice = (n) => {
  if (n == null) return '—';
  const decimals = n < 10 ? 4 : 2;
  return n.toLocaleString('en-US', { minimumFractionDigits: decimals, maximumFractionDigits: decimals });
};

const stamp = () => {
  const d = new Date();
  return [d.getHours(), d.getMinutes(), d.getSeconds()]
    .map(v => String(v).padStart(2, '0'))
    .join(':');
};

const mapResult = (r) => ({
  name: r.exchange,
  ok: r.isSuccess,
  elapsed: r.elapsedMs,
  price: r.price,
  errorMessage: r.errorMessage,
});

const IconPlay = () => (
  <svg className="icon" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2" strokeLinecap="round" strokeLinejoin="round">
    <polygon points="6 4 20 12 6 20 6 4" />
  </svg>
);
const IconList = () => (
  <svg className="icon" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2" strokeLinecap="round" strokeLinejoin="round">
    <line x1="8" y1="6" x2="20" y2="6" /><line x1="8" y1="12" x2="20" y2="12" /><line x1="8" y1="18" x2="20" y2="18" />
    <circle cx="4" cy="6" r="1" /><circle cx="4" cy="12" r="1" /><circle cx="4" cy="18" r="1" />
  </svg>
);
const IconCheck = () => (
  <svg viewBox="0 0 24 24" width="10" height="10" fill="none" stroke="currentColor" strokeWidth="3" strokeLinecap="round" strokeLinejoin="round">
    <polyline points="20 6 9 17 4 12" />
  </svg>
);

function InputBar({ pair, setPair, onRun, busy }) {
  const onKey = (e) => { if (e.key === 'Enter' && !busy) onRun('parallel'); };
  return (
    <div className="inputbar">
      <label className="field">
        <span className="field-label">Pair</span>
        <input
          value={pair}
          onChange={(e) => setPair(e.target.value.toUpperCase())}
          onKeyDown={onKey}
          spellCheck="false"
          autoComplete="off"
          placeholder="BTC"
        />
      </label>
      <button className="btn btn-primary" onClick={() => onRun('parallel')} disabled={!!busy}>
        {busy === 'parallel' ? <span className="spinner" /> : <IconPlay />}
        Fetch parallel
      </button>
      <button className="btn btn-secondary" onClick={() => onRun('sequential')} disabled={!!busy}>
        {busy === 'sequential' ? <span className="spinner" /> : <IconList />}
        Fetch sequential
      </button>
    </div>
  );
}

function CompareCard({ mode, run, peer }) {
  const own = run?.elapsed;
  const other = peer?.elapsed;
  const isFastest = own != null && other != null && own < other;
  const isSlowest = own != null && other != null && own > other;
  const tone = isFastest ? 'is-fastest' : isSlowest ? 'is-slowest' : '';

  let widthPct;
  if (own != null && other != null) {
    widthPct = (own / Math.max(own, other)) * 100;
  } else if (own != null) {
    widthPct = 100;
  }

  const slowdown = isSlowest && other > 0 ? (own / other).toFixed(1) + '× slower' : null;

  if (run == null) {
    return (
      <div className="compare-card is-empty">
        <div className="mode-label">{mode === 'parallel' ? 'Parallel mode' : 'Sequential mode'}</div>
        <div className="time">
          <span className="num mono" style={{ color: 'var(--fg-subtle)' }}>—</span>
          <span className="unit">ms</span>
        </div>
        <div className="placeholder-bar" />
        <div className="placeholder">Not run yet</div>
      </div>
    );
  }

  return (
    <div className={`compare-card ${tone}`}>
      {isFastest && <div className="badge badge-fastest">Fastest</div>}
      {isSlowest && <div className="badge badge-slowest">{slowdown}</div>}
      <div className="mode-label">{mode === 'parallel' ? 'Parallel mode' : 'Sequential mode'}</div>
      <div className="time">
        <span key={run.elapsed} className="num mono time-pulse">{fmtMs(run.elapsed)}</span>
        <span className="unit">ms</span>
      </div>
      <div className="bar-track">
        <div className="bar-fill" style={{ width: `${widthPct ?? 0}%` }} />
      </div>
      <div className="meta">
        <span>{run.results.filter(r => r.ok).length} of {run.results.length} succeeded</span>
        <span className="mono">{run.timestamp}</span>
      </div>
    </div>
  );
}

function StatCard({ label, value, sub }) {
  const isEmpty = value == null;
  return (
    <div className="stat-card">
      <div className="label">{label}</div>
      <div className={`value mono${isEmpty ? ' is-empty' : ''}`}>{isEmpty ? '—' : value}</div>
      {sub && <div className="sub">{sub}</div>}
    </div>
  );
}

const SORT_COLS = { exchange: 'exchange', price: 'price', elapsed: 'elapsed' };

function SortIcon({ active, dir }) {
  return (
    <span className={`sort-icon${active ? ' sort-icon--active' : ''}`}>
      {active ? (dir === 'asc' ? ' ↑' : ' ↓') : ' ↕'}
    </span>
  );
}

function ResultsTable({ rows, mode, pair }) {
  const [sortCol, setSortCol] = useState(null);
  const [sortDir, setSortDir] = useState('asc');

  const handleSort = (col) => {
    if (sortCol === col) {
      setSortDir(d => d === 'asc' ? 'desc' : 'asc');
    } else {
      setSortCol(col);
      setSortDir('asc');
    }
  };

  const sorted = useMemo(() => {
    if (!sortCol) return rows;
    return [...rows].sort((a, b) => {
      if (a.pending && b.pending) return 0;
      if (a.pending) return 1;
      if (b.pending) return -1;
      let va, vb;
      if (sortCol === SORT_COLS.exchange) {
        va = a.name?.toLowerCase() ?? '';
        vb = b.name?.toLowerCase() ?? '';
      } else if (sortCol === SORT_COLS.price) {
        va = a.ok && a.price != null ? a.price : -Infinity;
        vb = b.ok && b.price != null ? b.price : -Infinity;
      } else {
        va = a.elapsed ?? 0;
        vb = b.elapsed ?? 0;
      }
      if (va < vb) return sortDir === 'asc' ? -1 : 1;
      if (va > vb) return sortDir === 'asc' ? 1 : -1;
      return 0;
    });
  }, [rows, sortCol, sortDir]);

  return (
    <div className="table-card">
      <div className="table-header">
        <h3>Exchange results</h3>
        {mode && <span className="pill">{mode}</span>}
      </div>
      {(!rows || rows.length === 0) ? (
        <div className="empty-state">No results yet. Hit &ldquo;Fetch parallel&rdquo; to populate the table.</div>
      ) : (
        <table>
          <thead>
            <tr>
              <th
                className="sortable"
                onClick={() => handleSort(SORT_COLS.exchange)}
              >
                Exchange
                <SortIcon active={sortCol === SORT_COLS.exchange} dir={sortDir} />
              </th>
              <th
                className="num sortable"
                onClick={() => handleSort(SORT_COLS.price)}
              >
                Price ({pair})
                <SortIcon active={sortCol === SORT_COLS.price} dir={sortDir} />
              </th>
              <th
                className="num sortable"
                onClick={() => handleSort(SORT_COLS.elapsed)}
              >
                Elapsed (ms)
                <SortIcon active={sortCol === SORT_COLS.elapsed} dir={sortDir} />
              </th>
              <th>Status</th>
            </tr>
          </thead>
          <tbody>
            {sorted.map((r) => {
              if (r.pending) {
                return (
                  <tr key={r.name} className="is-pending">
                    <td className="exchange">{r.name}</td>
                    <td className="num mono">—</td>
                    <td className="num mono">…</td>
                    <td>
                      <span className="status status-pending">
                        <span className="spinner" style={{ width: 11, height: 11, borderWidth: 1.25 }} />
                        Fetching
                      </span>
                    </td>
                  </tr>
                );
              }
              return (
                <tr key={r.name} className={`is-revealed${r.ok ? '' : ' is-failed'}`}>
                  <td className="exchange">{r.name}</td>
                  <td className="num mono price-cell">{r.ok ? fmtPrice(r.price) : '—'}</td>
                  <td className="num mono">{fmtMs(r.elapsed)}</td>
                  <td>
                    {r.ok ? (
                      <span className="status status-ok">
                        <span className="dot"><IconCheck /></span>
                        Success
                      </span>
                    ) : (
                      <span className="status status-err">
                        <span className="dot" style={{ fontWeight: 600 }}>✕</span>
                        {r.errorMessage ? 'Error' : 'Failed'}
                      </span>
                    )}
                  </td>
                </tr>
              );
            })}
          </tbody>
        </table>
      )}
    </div>
  );
}

export default function App() {
  const [pair, setPair] = useState('BTC');
  const [busy, setBusy] = useState(null);
  const [parallelRun, setParallelRun] = useState(null);
  const [sequentialRun, setSequentialRun] = useState(null);
  const [activeMode, setActiveMode] = useState(null);
  const [liveRows, setLiveRows] = useState([]);
  const [error, setError] = useState(null);

  const handleRun = async (mode) => {
    if (busy) return;
    setBusy(mode);
    setActiveMode(mode);
    setError(null);
    setLiveRows(EXCHANGE_NAMES.map(name => ({ name, pending: true })));

    try {
      const url = `${API_BASE}/${mode}/${encodeURIComponent(pair)}`;
      const res = await fetch(url);
      if (!res.ok) throw new Error(`HTTP ${res.status}`);
      const data = await res.json();

      const run = {
        elapsed: data.totalElapsedMs,
        results: data.results.map(mapResult),
        timestamp: stamp(),
        pair,
        speedupFactor: data.speedupFactor,
        totalIndividualElapsedMs: data.totalIndividualElapsedMs,
      };

      if (mode === 'parallel') setParallelRun(run);
      else setSequentialRun(run);
    } catch (err) {
      setError(err.message ?? 'Request failed');
    } finally {
      setBusy(null);
    }
  };

  const tableRows = useMemo(() => {
    if (busy) return liveRows;
    if (activeMode === 'parallel' && parallelRun) return parallelRun.results;
    if (activeMode === 'sequential' && sequentialRun) return sequentialRun.results;
    return [];
  }, [busy, liveRows, activeMode, parallelRun, sequentialRun]);

  const stats = useMemo(() => {
    const successful = tableRows.filter(r => r && r.ok && r.price != null);
    if (successful.length === 0) {
      return { avg: null, min: null, max: null, minExchange: null, maxExchange: null, count: 0, total: tableRows.length };
    }
    const minPrice = Math.min(...successful.map(r => r.price));
    const maxPrice = Math.max(...successful.map(r => r.price));
    return {
      avg: successful.reduce((a, r) => a + r.price, 0) / successful.length,
      min: minPrice,
      max: maxPrice,
      minExchange: successful.find(r => r.price === minPrice)?.name ?? null,
      maxExchange: successful.find(r => r.price === maxPrice)?.name ?? null,
      count: successful.length,
      total: tableRows.length,
    };
  }, [tableRows]);

  const activeRun = activeMode === 'parallel' ? parallelRun : sequentialRun;
  const speedup = activeRun?.speedupFactor;

  return (
    <div className="app">
      <header className="header">
        <div className="eyebrow">Crypto Aggregator</div>
        <h1>Parallel vs. sequential price fetch</h1>
        <p>Query a trading pair across {EXCHANGE_NAMES.length} exchanges and compare execution strategies.</p>
      </header>

      <InputBar pair={pair} setPair={setPair} onRun={handleRun} busy={busy} />

      {error && <div className="error-banner">Failed to fetch: {error}</div>}

      <div className="section">
        <div className="section-head">
          <span className="section-title">Run comparison</span>
        </div>
        <div className="compare-grid">
          <CompareCard mode="parallel"   run={parallelRun}   peer={sequentialRun} />
          <CompareCard mode="sequential" run={sequentialRun} peer={parallelRun}   />
        </div>
      </div>

      <div className="section">
        <div className="section-head">
          <span className="section-title">
            Aggregate stats
            {stats.count > 0 && (
              <span style={{ color: 'var(--fg-subtle)', textTransform: 'none', letterSpacing: 0, fontWeight: 400 }}>
                {' '}· {stats.count} of {stats.total} responses
              </span>
            )}
          </span>
        </div>
        <div className="stats-grid">
          <StatCard label="Average price" value={stats.avg != null ? fmtPrice(stats.avg) : null} />
          <StatCard
            label="Min price"
            value={stats.min != null ? fmtPrice(stats.min) : null}
            sub={stats.minExchange}
          />
          <StatCard
            label="Max price"
            value={stats.max != null ? fmtPrice(stats.max) : null}
            sub={stats.maxExchange}
          />
          <StatCard
            label="Speedup factor"
            value={speedup != null ? `${speedup}×` : null}
            sub={speedup != null ? 'Sum Individual / Wall Clock' : undefined}
          />
          <StatCard
            label="Wall clock"
            value={activeRun != null ? `${fmtMs(activeRun.elapsed)} ms` : null}
          />
          <StatCard
            label="Sum individual"
            value={activeRun != null ? `${fmtMs(activeRun.totalIndividualElapsedMs)} ms` : null}
          />
        </div>
      </div>

      <div className="section">
        <ResultsTable rows={tableRows} mode={activeMode} pair={pair} />
      </div>

      <div className="footer-note">
        Press <span className="kbd">Enter</span> in the pair field to run a parallel fetch.
      </div>
    </div>
  );
}
