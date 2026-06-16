import { useState, useEffect } from 'react';
import { Link } from 'react-router-dom';
import api from '../api';

const E = { // eyebrow helper
  wrap: { display: 'flex', alignItems: 'center', gap: 8, fontSize: 11, fontWeight: 600, textTransform: 'uppercase', letterSpacing: '0.18em', color: '#E91E8C', marginBottom: 6 },
  line: { width: 14, height: 1.5, background: '#E91E8C' },
};

const PageShell = ({ eyebrow, title, children, maxWidth = 800 }) => (
  <div style={{ background: '#f8f8f8', minHeight: 'calc(100vh - 60px)' }}>
    <div style={{ maxWidth, margin: '0 auto', padding: '2.5rem 2rem' }}>
      <div style={{ marginBottom: '2rem' }}>
        <div style={E.wrap}><div style={E.line} />{eyebrow}</div>
        <h1 style={{ fontSize: 28, fontWeight: 800, letterSpacing: -1, color: '#0D0D0D' }}>{title}</h1>
      </div>
      {children}
    </div>
  </div>
);

const Card = ({ children, style = {} }) => (
  <div style={{ background: 'white', borderRadius: 16, border: '1px solid #efefef', padding: '1.5rem', marginBottom: '1.25rem', ...style }}>
    {children}
  </div>
);

// ── Notifications ──────────────────────────────────────────────────────────────
export function NotificationsPage() {
  const [notifs, setNotifs] = useState([]);
  useEffect(() => { api.get('/notifications').then(r => setNotifs(r.data)); }, []);
  const markRead = async (id) => {
    await api.patch(`/notifications/${id}/read`);
    setNotifs(n => n.map(x => x.id === id ? { ...x, isRead: true } : x));
  };

  return (
    <PageShell eyebrow="Notifications" title="Your notifications" maxWidth={680}>
      {notifs.length === 0 && <p style={{ color: '#888', fontSize: 14 }}>No notifications yet.</p>}
      {notifs.map(n => (
        <div key={n.id} style={{ background: 'white', borderRadius: 12, border: `1px solid ${n.isRead ? '#efefef' : '#F8BBD9'}`, borderLeft: `3px solid ${n.isRead ? '#efefef' : '#E91E8C'}`, padding: '1rem 1.25rem', marginBottom: '0.75rem', display: 'flex', justifyContent: 'space-between', alignItems: 'flex-start' }}>
          <div>
            <div style={{ fontWeight: 700, fontSize: 14, marginBottom: 4 }}>{n.title}</div>
            <p style={{ fontSize: 13, color: '#555' }}>{n.message}</p>
            <p style={{ fontSize: 11, color: '#aaa', marginTop: 4 }}>{new Date(n.createdAt).toLocaleString()}</p>
          </div>
          {!n.isRead && (
            <button onClick={() => markRead(n.id)} style={{ height: 30, padding: '0 12px', background: '#f0f0f0', border: 'none', borderRadius: 999, fontSize: 11, fontWeight: 600, cursor: 'pointer', flexShrink: 0, fontFamily: 'Inter, sans-serif' }}>
              Mark read
            </button>
          )}
        </div>
      ))}
    </PageShell>
  );
}

// ── Earnings ───────────────────────────────────────────────────────────────────
export function EarningsPage() {
  const [data, setData] = useState(null);
  useEffect(() => { api.get('/earnings').then(r => setData(r.data)); }, []);
  if (!data) return <div className="loading">Loading earnings…</div>;

  return (
    <PageShell eyebrow="Driver" title="My Earnings">
      <div style={{ display: 'grid', gridTemplateColumns: 'repeat(4,1fr)', gap: '1rem', marginBottom: '1.5rem' }}>
        {[['Today', data.todayEarnings], ['This Week', data.weekEarnings], ['This Month', data.monthEarnings], ['All Time', data.totalEarnings]].map(([label, num]) => (
          <div key={label} style={{ background: 'white', borderRadius: 12, padding: '1.25rem', border: '1px solid #efefef' }}>
            <div style={{ fontSize: 28, fontWeight: 800, color: '#E91E8C', letterSpacing: -1, lineHeight: 1 }}>{num?.toFixed(0)}</div>
            <div style={{ fontSize: 10, fontWeight: 600, textTransform: 'uppercase', letterSpacing: '0.1em', color: '#888', marginTop: 6 }}>{label} · MKD</div>
          </div>
        ))}
      </div>

      <Card>
        <div style={E.wrap}><div style={E.line} /> Ride history</div>
        <h2 style={{ fontSize: 18, fontWeight: 800, letterSpacing: -0.5, marginBottom: '1rem' }}>Completed rides</h2>
        <table>
          <thead><tr><th>From</th><th>To</th><th>Fare (MKD)</th><th>Date</th></tr></thead>
          <tbody>
            {data.rides?.map(r => (
              <tr key={r.id}>
                <td>{r.pickupAddress}</td>
                <td>{r.dropoffAddress}</td>
                <td style={{ fontWeight: 600 }}>{r.finalFare?.toFixed(2)}</td>
                <td>{r.completedAt ? new Date(r.completedAt).toLocaleDateString() : '—'}</td>
              </tr>
            ))}
          </tbody>
        </table>
      </Card>
    </PageShell>
  );
}

// ── Analytics ──────────────────────────────────────────────────────────────────
export function AnalyticsPage() {
  const [data, setData] = useState(null);
  useEffect(() => { api.get('/analytics').then(r => setData(r.data)); }, []);
  if (!data) return <div className="loading">Loading analytics…</div>;

  return (
    <PageShell eyebrow="Admin" title="Analytics" maxWidth={1000}>
      <div style={{ display: 'grid', gridTemplateColumns: 'repeat(4,1fr)', gap: '1rem', marginBottom: '1.5rem' }}>
        {[['Total Rides', data.totalRides], ['Completion', `${data.completionRate}%`], ['Active Drivers', data.activeDrivers], ['Revenue', `${data.totalRevenue?.toFixed(0)} MKD`]].map(([label, num]) => (
          <div key={label} style={{ background: 'white', borderRadius: 12, padding: '1.25rem', border: '1px solid #efefef' }}>
            <div style={{ fontSize: 28, fontWeight: 800, color: '#E91E8C', letterSpacing: -1, lineHeight: 1 }}>{num}</div>
            <div style={{ fontSize: 10, fontWeight: 600, textTransform: 'uppercase', letterSpacing: '0.1em', color: '#888', marginTop: 6 }}>{label}</div>
          </div>
        ))}
      </div>

      <div style={{ display: 'grid', gridTemplateColumns: '1fr 1fr', gap: '1rem' }}>
        <Card>
          <div style={E.wrap}><div style={E.line} /> Rides per day</div>
          <h2 style={{ fontSize: 16, fontWeight: 800, letterSpacing: -0.3, marginBottom: '0.75rem' }}>Last 7 days</h2>
          {data.ridesPerDay?.map(d => (
            <div key={d.date} style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', padding: '0.4rem 0', borderBottom: '1px solid #f8f8f8' }}>
              <span style={{ fontSize: 13, color: '#888' }}>{d.date}</span>
              <span style={{ fontWeight: 700, fontSize: 13 }}>{d.count}</span>
            </div>
          ))}
        </Card>

        <Card>
          <div style={E.wrap}><div style={E.line} /> Revenue per day</div>
          <h2 style={{ fontSize: 16, fontWeight: 800, letterSpacing: -0.3, marginBottom: '0.75rem' }}>Last 7 days</h2>
          {data.revenuePerDay?.map(d => (
            <div key={d.date} style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', padding: '0.4rem 0', borderBottom: '1px solid #f8f8f8' }}>
              <span style={{ fontSize: 13, color: '#888' }}>{d.date}</span>
              <span style={{ fontWeight: 700, fontSize: 13 }}>{d.amount?.toFixed(0)} MKD</span>
            </div>
          ))}
        </Card>

        <Card>
          <div style={E.wrap}><div style={E.line} /> Status breakdown</div>
          <h2 style={{ fontSize: 16, fontWeight: 800, letterSpacing: -0.3, marginBottom: '0.75rem' }}>By status</h2>
          {data.statusBreakdown?.map(s => (
            <div key={s.status} style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', padding: '0.4rem 0', borderBottom: '1px solid #f8f8f8' }}>
              <span className={`status-${s.status}`}>{s.status}</span>
              <span style={{ fontWeight: 700, fontSize: 13 }}>{s.count}</span>
            </div>
          ))}
        </Card>

        <Card>
          <div style={E.wrap}><div style={E.line} /> Peak hours</div>
          <h2 style={{ fontSize: 16, fontWeight: 800, letterSpacing: -0.3, marginBottom: '0.75rem' }}>By hour</h2>
          <div style={{ maxHeight: 220, overflowY: 'auto' }}>
            {data.peakHours?.filter(h => h.count > 0).map(h => (
              <div key={h.hour} style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', padding: '0.3rem 0', borderBottom: '1px solid #f8f8f8' }}>
                <span style={{ fontSize: 13, color: '#888' }}>{h.hour}</span>
                <span style={{ fontWeight: 700, fontSize: 13 }}>{h.count}</span>
              </div>
            ))}
          </div>
        </Card>
      </div>
    </PageShell>
  );
}

// ── SOS Alerts ─────────────────────────────────────────────────────────────────
export function SosAlertsPage() {
  const [alerts, setAlerts] = useState([]);
  const [msg, setMsg] = useState('');
  useEffect(() => { api.get('/sos/alerts').then(r => setAlerts(r.data)); }, []);

  const resolve = async (id) => {
    await api.patch(`/sos/alerts/${id}/resolve`);
    setMsg('Alert resolved.');
    setAlerts(a => a.map(x => x.id === id ? { ...x, isResolved: true } : x));
  };

  return (
    <PageShell eyebrow="Admin · Safety" title="SOS Alerts" maxWidth={760}>
      {msg && <div style={{ background: '#f0fdf4', border: '1px solid #bbf7d0', borderRadius: 10, padding: '0.75rem 1rem', marginBottom: '1rem', fontSize: 13, color: '#166534' }}>{msg}</div>}
      {alerts.length === 0 && <p style={{ color: '#888', fontSize: 14 }}>No SOS alerts. All clear.</p>}
      {alerts.map(a => (
        <div key={a.id} style={{ background: 'white', borderRadius: 12, border: `1px solid ${a.isResolved ? '#efefef' : '#fecaca'}`, borderLeft: `3px solid ${a.isResolved ? '#efefef' : '#e53e3e'}`, padding: '1rem 1.25rem', marginBottom: '0.75rem', display: 'flex', justifyContent: 'space-between', alignItems: 'flex-start' }}>
          <div>
            <div style={{ fontWeight: 700, fontSize: 14, marginBottom: 4 }}>{a.userName} <span style={{ color: '#888', fontWeight: 400 }}>·</span> User #{a.userId}</div>
            <p style={{ fontSize: 13, color: '#555' }}>Location: {a.latitude?.toFixed(4)}, {a.longitude?.toFixed(4)}</p>
            {a.rideId && <p style={{ fontSize: 13, color: '#555' }}>Ride #{a.rideId}</p>}
            <p style={{ fontSize: 11, color: '#aaa', marginTop: 4 }}>{new Date(a.createdAt).toLocaleString()}</p>
          </div>
          {a.isResolved
            ? <span className="badge badge-green">Resolved</span>
            : <button onClick={() => resolve(a.id)} className="btn btn-success btn-sm">Resolve</button>}
        </div>
      ))}
    </PageShell>
  );
}

// ── Home / Landing ─────────────────────────────────────────────────────────────
const TICKER_ITEMS = [
  { bold: '100%', text: 'verified drivers' }, { bold: '4.9★', text: 'average rating' },
  { bold: 'Under 5 min', text: 'pickup in Tetovo' }, { bold: '24/7', text: 'around the clock' },
  { bold: 'SOS', text: 'emergency on every ride' }, { bold: '50 MKD', text: 'starting fare' },
];

const FEATURES = [
  { icon: 'ti-coin',           title: 'Honest pricing',     desc: 'See your fare before you book. No surprises, no hidden fees. Starting from 50 MKD.' },
  { icon: 'ti-calendar',      title: 'Book in advance',    desc: 'Schedule rides ahead of time — perfect for early mornings or planned appointments.' },
  { icon: 'ti-credit-card',   title: 'Card or cash',       desc: 'Stripe-secured card payments or cash on arrival. The choice is always yours.' },
  { icon: 'ti-user-check',    title: 'Choose your driver', desc: 'See ratings, vehicle info, and ETA for every available driver before you confirm.' },
  { icon: 'ti-map-pin',       title: 'Live map tracking',  desc: 'Watch your driver approach in real time. No more guessing, no more waiting blind.' },
  { icon: 'ti-message-circle',title: 'In-ride chat',       desc: 'Message your driver directly inside the app. No personal numbers ever shared.' },
];

const SAFETY_ITEMS = [
  { icon: 'ti-shield-check',    title: 'Verified drivers only',  desc: 'Every driver is approved by our admin team before their very first ride.' },
  { icon: 'ti-alert-triangle',  title: 'SOS emergency button',   desc: 'One tap sends your GPS location and an alert to our safety team instantly.' },
  { icon: 'ti-star',            title: 'Two-way ratings',        desc: 'Rate your driver after every ride. Together we maintain the highest standards.' },
  { icon: 'ti-lock',            title: 'Private by design',      desc: 'No personal numbers shared. Chat stays inside the app, always.' },
];

const STEPS = [
  { num: '01', title: 'Create your account',  desc: 'Sign up in under a minute. Passengers verified instantly. Drivers need a quick admin approval.' },
  { num: '02', title: 'Choose your driver',   desc: 'Browse drivers near you — rating, vehicle, plate number, and live ETA. Pick who feels right.' },
  { num: '03', title: 'Ride and pay',         desc: 'Track your ride. Chat if needed. Pay by card or cash. Rate your experience — that is it.' },
];

export function HomePage() {
  const doubled = [...TICKER_ITEMS, ...TICKER_ITEMS];

  return (
    <div style={{ fontFamily: "'Inter', -apple-system, sans-serif", color: '#0D0D0D', background: '#fff', overflowX: 'hidden' }}>
      <style>{`
        @keyframes ticker{0%{transform:translateX(0)}100%{transform:translateX(-50%)}}
        @keyframes blink{0%,100%{opacity:1}50%{opacity:0.3}}
        @keyframes carMove{0%{left:10%}100%{left:85%}}
      `}</style>

      {/* ── NAV ── */}
      <nav style={{ height: 60, display: 'flex', alignItems: 'center', padding: '0 32px', background: '#0D0D0D', borderBottom: '1px solid rgba(255,255,255,0.06)', position: 'sticky', top: 0, zIndex: 100 }}>
        <Link to="/" style={{ display: 'flex', alignItems: 'center', gap: 10, textDecoration: 'none' }}>
          <div style={{ width: 22, height: 22, background: '#E91E8C', borderRadius: '50%' }} />
          <span style={{ fontSize: 17, fontWeight: 700, color: '#fff', letterSpacing: '-0.3px' }}>SafeRide</span>
        </Link>
        <div style={{ display: 'flex', alignItems: 'center', gap: 4, marginLeft: 'auto' }}>
          {['How it works', 'Safety', 'Drive'].map(l => (
            <a key={l} href={`#${l.toLowerCase().replace(/ /g,'-')}`} style={{ color: 'rgba(255,255,255,0.55)', textDecoration: 'none', fontSize: 13, padding: '6px 12px' }}>{l}</a>
          ))}
          <Link to="/login" style={{ height: 34, padding: '0 16px', borderRadius: 17, fontSize: 12, fontWeight: 600, textDecoration: 'none', display: 'flex', alignItems: 'center', background: 'transparent', border: '1px solid rgba(255,255,255,0.18)', color: '#fff' }}>Log in</Link>
          <Link to="/register" style={{ height: 34, padding: '0 16px', borderRadius: 17, fontSize: 12, fontWeight: 600, textDecoration: 'none', display: 'flex', alignItems: 'center', background: '#E91E8C', color: '#fff', marginLeft: 6 }}>Sign up</Link>
        </div>
      </nav>

      {/* ── HERO ── */}
      <section style={{ background: '#0D0D0D', display: 'grid', gridTemplateColumns: '1fr 1fr', alignItems: 'center', padding: '60px 32px 56px', gap: 40, borderBottom: '1px solid rgba(255,255,255,0.06)' }}>
        <div>
          <div style={{ display: 'inline-flex', alignItems: 'center', gap: 8, fontSize: 11, fontWeight: 600, textTransform: 'uppercase', letterSpacing: '0.18em', color: '#E91E8C', marginBottom: 24 }}>
            <div style={{ width: 5, height: 5, background: '#E91E8C', borderRadius: '50%', animation: 'blink 2s infinite' }} />
            Now serving Tetovo
          </div>
          <h1 style={{ fontSize: 'clamp(44px,6vw,72px)', fontWeight: 800, color: '#fff', lineHeight: 1.0, letterSpacing: -2, marginBottom: 20 }}>
            Your ride,<br /><span style={{ color: '#E91E8C' }}>safe</span><br />
            <em style={{ fontStyle: 'italic', fontWeight: 300, color: 'rgba(255,255,255,0.45)' }}>&amp; sweet.</em>
          </h1>
          <p style={{ fontSize: 15, color: 'rgba(255,255,255,0.5)', lineHeight: 1.7, maxWidth: 380, marginBottom: 32, fontWeight: 300 }}>
            Book a trusted verified driver in seconds. Transparent pricing, real-time chat, and an SOS button on every ride.
          </p>
          <div style={{ display: 'flex', gap: 10, flexWrap: 'wrap' }}>
            <Link to="/register" style={{ height: 48, padding: '0 28px', background: '#E91E8C', color: '#fff', borderRadius: 24, fontSize: 14, fontWeight: 600, textDecoration: 'none', display: 'inline-flex', alignItems: 'center', gap: 7 }}>
              Get started →
            </Link>
            <a href="#how-it-works" style={{ height: 48, padding: '0 28px', background: 'transparent', border: '1.5px solid rgba(255,255,255,0.2)', color: '#fff', borderRadius: 24, fontSize: 14, fontWeight: 500, textDecoration: 'none', display: 'inline-flex', alignItems: 'center' }}>
              See how it works
            </a>
          </div>
        </div>

        {/* Ride card */}
        <div style={{ display: 'flex', justifyContent: 'flex-end' }}>
          <div style={{ width: '100%', maxWidth: 340, background: 'rgba(255,255,255,0.04)', border: '1px solid rgba(255,255,255,0.08)', borderRadius: 20, overflow: 'hidden' }}>
            <div style={{ height: 180, background: '#1a0d12', position: 'relative' }}>
              <svg width="100%" height="180" viewBox="0 0 340 180" preserveAspectRatio="xMidYMid slice">
                {[45,90,135].map(y => <line key={y} x1="0" y1={y} x2="340" y2={y} stroke="rgba(255,255,255,0.05)" strokeWidth="1"/>)}
                {[68,136,204,272].map(x => <line key={x} x1={x} y1="0" x2={x} y2="180" stroke="rgba(255,255,255,0.05)" strokeWidth="1"/>)}
                <path d="M60 140 Q60 60 180 60 L270 60" fill="none" stroke="#E91E8C" strokeWidth="2" strokeDasharray="5 3" opacity="0.7"/>
                <circle cx="60" cy="140" r="5" fill="#E91E8C"/>
                <circle cx="60" cy="140" r="10" fill="none" stroke="#E91E8C" strokeWidth="1" opacity="0.3"/>
                <circle cx="270" cy="60" r="5" fill="#fff"/>
                <circle r="4" fill="#E91E8C">
                  <animateMotion dur="4s" repeatCount="indefinite"><mpath href="#rp"/></animateMotion>
                </circle>
                <path id="rp" d="M60 140 Q60 60 180 60 L270 60" fill="none"/>
                <text x="50" y="158" fontFamily="Inter" fontSize="9" fill="rgba(255,255,255,0.35)">Pickup</text>
                <text x="258" y="52" fontFamily="Inter" fontSize="9" fill="rgba(255,255,255,0.35)">Drop</text>
              </svg>
            </div>
            <div style={{ padding: 20 }}>
              <div style={{ display: 'flex', alignItems: 'center', gap: 12, marginBottom: 16 }}>
                <div style={{ width: 40, height: 40, borderRadius: '50%', background: '#F8BBD9', display: 'flex', alignItems: 'center', justifyContent: 'center', fontSize: 16, fontWeight: 700, color: '#C2185B', flexShrink: 0 }}>A</div>
                <div>
                  <div style={{ fontSize: 15, fontWeight: 600, color: '#fff' }}>Ana M.</div>
                  <div style={{ fontSize: 12, color: 'rgba(255,255,255,0.4)' }}>Toyota Corolla · SK-1234-AB</div>
                </div>
                <div style={{ marginLeft: 'auto', fontSize: 13, fontWeight: 600, color: '#E91E8C' }}>⭐ 4.9</div>
              </div>
              <div style={{ display: 'flex', justifyContent: 'space-between', paddingTop: 14, borderTop: '1px solid rgba(255,255,255,0.07)' }}>
                {[['ETA','4 min'],['Fare','80 MKD'],['Payment','Cash']].map(([k,v]) => (
                  <div key={k}>
                    <div style={{ fontSize: 10, fontWeight: 500, textTransform: 'uppercase', letterSpacing: '0.12em', color: 'rgba(255,255,255,0.3)', marginBottom: 3 }}>{k}</div>
                    <div style={{ fontSize: 14, fontWeight: 600, color: '#fff', display: 'flex', alignItems: 'center', gap: 5 }}>
                      {k === 'ETA' && <div style={{ width: 5, height: 5, borderRadius: '50%', background: '#22c55e' }} />}{v}
                    </div>
                  </div>
                ))}
              </div>
            </div>
          </div>
        </div>
      </section>

      {/* ── TICKER ── */}
      <div style={{ borderTop: '1px solid rgba(233,30,140,0.12)', borderBottom: '1px solid rgba(233,30,140,0.12)', padding: '12px 0', overflow: 'hidden', background: '#FDF0F4' }}>
        <div style={{ display: 'flex', gap: 48, animation: 'ticker 20s linear infinite', whiteSpace: 'nowrap' }}>
          {doubled.map((item, i) => (
            <span key={i} style={{ fontSize: 12, color: '#7B3B54', display: 'inline-flex', alignItems: 'center', gap: 8 }}>
              <strong style={{ fontWeight: 600, color: '#C2185B' }}>{item.bold}</strong> {item.text}
              <span style={{ width: 3, height: 3, borderRadius: '50%', background: '#F8BBD9', display: 'inline-block' }} />
            </span>
          ))}
        </div>
      </div>

      {/* ── STATS ── */}
      <div style={{ display: 'grid', gridTemplateColumns: 'repeat(4,1fr)', borderBottom: '1px solid #f0f0f0' }}>
        {[['100%','Drivers verified before first ride'],['50 MKD','Starting fare in Tetovo'],['<5 min','Average pickup time'],['0','Safety incidents since launch']].map(([num, desc], i) => (
          <div key={num} style={{ padding: '32px 28px', borderRight: i < 3 ? '1px solid #f0f0f0' : 'none' }}>
            <div style={{ fontSize: 36, fontWeight: 800, color: '#E91E8C', letterSpacing: -1.5, lineHeight: 1 }}>{num}</div>
            <div style={{ fontSize: 13, color: '#888', marginTop: 6, lineHeight: 1.4 }}>{desc}</div>
          </div>
        ))}
      </div>

      {/* ── FEATURES ── */}
      <section style={{ padding: '64px 32px', borderBottom: '1px solid #f0f0f0' }}>
        <div style={{ display: 'flex', alignItems: 'center', gap: 8, fontSize: 11, fontWeight: 600, textTransform: 'uppercase', letterSpacing: '0.18em', color: '#E91E8C', marginBottom: 10 }}>
          <div style={{ width: 18, height: 1.5, background: '#E91E8C' }} /> What we offer
        </div>
        <h2 style={{ fontSize: 'clamp(28px,3.5vw,42px)', fontWeight: 800, letterSpacing: -1.5, lineHeight: 1.05, marginBottom: 40 }}>
          Every ride,<br />exactly as it should be.
        </h2>
        <div style={{ display: 'grid', gridTemplateColumns: 'repeat(3,1fr)', border: '1px solid #efefef' }}>
          {FEATURES.map((f, i) => (
            <div key={f.title}
              style={{ padding: '28px 24px', borderRight: i%3!==2?'1px solid #efefef':'none', borderBottom: i<3?'1px solid #efefef':'none', transition: 'background 0.2s' }}
              onMouseEnter={e => e.currentTarget.style.background='#fdf4f8'}
              onMouseLeave={e => e.currentTarget.style.background='#fff'}>
              <div style={{ width: 38, height: 38, borderRadius: '50%', background: '#FCE4EC', display: 'flex', alignItems: 'center', justifyContent: 'center', marginBottom: 16, fontSize: 17, color: '#E91E8C' }}>
                <i className={`ti ${f.icon}`} aria-hidden="true" />
              </div>
              <div style={{ fontSize: 14, fontWeight: 700, marginBottom: 6 }}>{f.title}</div>
              <div style={{ fontSize: 13, color: '#888', lineHeight: 1.6, fontWeight: 300 }}>{f.desc}</div>
            </div>
          ))}
        </div>
      </section>

      {/* ── SAFETY ── */}
      <section style={{ background: '#0D0D0D', padding: '64px 32px' }} id="safety">
        <div style={{ display: 'grid', gridTemplateColumns: '1fr 1fr', gap: 64, alignItems: 'center' }}>
          <div>
            <div style={{ display: 'flex', alignItems: 'center', gap: 8, fontSize: 11, fontWeight: 600, textTransform: 'uppercase', letterSpacing: '0.18em', color: '#E91E8C', marginBottom: 10 }}>
              <div style={{ width: 18, height: 1.5, background: '#E91E8C' }} /> Safety
            </div>
            <h2 style={{ fontSize: 'clamp(28px,3.5vw,42px)', fontWeight: 800, letterSpacing: -1.5, lineHeight: 1.05, color: '#fff', marginBottom: 0 }}>
              Safe isn't a feature.<br />It's the foundation.
            </h2>
            <div style={{ display: 'flex', flexDirection: 'column', marginTop: 32 }}>
              {SAFETY_ITEMS.map((s, i) => (
                <div key={s.title} style={{ display: 'flex', gap: 16, padding: '18px 0', borderBottom: i<SAFETY_ITEMS.length-1?'1px solid rgba(255,255,255,0.06)':'none' }}>
                  <div style={{ width: 34, height: 34, borderRadius: '50%', background: 'rgba(233,30,140,0.12)', display: 'flex', alignItems: 'center', justifyContent: 'center', flexShrink: 0 }}>
                    <i className={`ti ${s.icon}`} style={{ color: '#E91E8C', fontSize: 15 }} aria-hidden="true" />
                  </div>
                  <div>
                    <div style={{ fontSize: 14, fontWeight: 600, color: '#fff', marginBottom: 4 }}>{s.title}</div>
                    <div style={{ fontSize: 13, color: 'rgba(255,255,255,0.4)', lineHeight: 1.6, fontWeight: 300 }}>{s.desc}</div>
                  </div>
                </div>
              ))}
            </div>
          </div>
          <div style={{ background: 'rgba(255,255,255,0.03)', border: '1px solid rgba(255,255,255,0.07)', borderRadius: 20, padding: '36px 32px' }}>
            <div style={{ fontSize: 10, fontWeight: 600, textTransform: 'uppercase', letterSpacing: '0.15em', color: '#E91E8C', marginBottom: 4 }}>Safety record</div>
            <div style={{ fontSize: 80, fontWeight: 800, color: '#F8BBD9', lineHeight: 1, letterSpacing: -4 }}>0</div>
            <div style={{ fontSize: 10, textTransform: 'uppercase', letterSpacing: '0.18em', color: 'rgba(255,255,255,0.28)', marginTop: 4 }}>safety incidents</div>
            <div style={{ height: 1, background: 'rgba(255,255,255,0.06)', margin: '28px 0' }} />
            <div style={{ fontSize: 10, fontWeight: 600, textTransform: 'uppercase', letterSpacing: '0.15em', color: '#E91E8C', marginBottom: 4 }}>Driver verification</div>
            <div style={{ fontSize: 80, fontWeight: 800, color: '#fff', lineHeight: 1, letterSpacing: -4 }}>100%</div>
            <div style={{ fontSize: 10, textTransform: 'uppercase', letterSpacing: '0.18em', color: 'rgba(255,255,255,0.28)', marginTop: 4 }}>drivers verified</div>
            <div style={{ height: 1, background: 'rgba(255,255,255,0.06)', margin: '28px 0' }} />
            <div style={{ fontSize: 12, color: 'rgba(255,255,255,0.28)', fontWeight: 300, lineHeight: 1.65 }}>Every driver goes through identity verification, vehicle inspection, and admin approval before their first ride.</div>
          </div>
        </div>
      </section>

      {/* ── HOW IT WORKS ── */}
      <section style={{ padding: '64px 32px', borderBottom: '1px solid #f0f0f0' }} id="how-it-works">
        <div style={{ display: 'flex', alignItems: 'center', gap: 8, fontSize: 11, fontWeight: 600, textTransform: 'uppercase', letterSpacing: '0.18em', color: '#E91E8C', marginBottom: 10 }}>
          <div style={{ width: 18, height: 1.5, background: '#E91E8C' }} /> How it works
        </div>
        <h2 style={{ fontSize: 'clamp(28px,3.5vw,42px)', fontWeight: 800, letterSpacing: -1.5, marginBottom: 40 }}>Ready in minutes.</h2>
        <div style={{ display: 'grid', gridTemplateColumns: 'repeat(3,1fr)', borderLeft: '1px solid #efefef', borderTop: '1px solid #efefef' }}>
          {STEPS.map(s => (
            <div key={s.num} style={{ padding: '28px 24px', borderRight: '1px solid #efefef', borderBottom: '1px solid #efefef' }}>
              <div style={{ fontSize: 11, fontWeight: 700, textTransform: 'uppercase', letterSpacing: '0.18em', color: '#E91E8C', marginBottom: 16 }}>Step {s.num}</div>
              <div style={{ fontSize: 15, fontWeight: 700, letterSpacing: -0.3, marginBottom: 8 }}>{s.title}</div>
              <div style={{ fontSize: 13, color: '#888', lineHeight: 1.6, fontWeight: 300 }}>{s.desc}</div>
            </div>
          ))}
        </div>
      </section>

      {/* ── DRIVER BANNER ── */}
      <section style={{ display: 'grid', gridTemplateColumns: '1fr 1fr' }} id="drive">
        <div style={{ padding: '64px 32px', borderRight: '1px solid #f0f0f0' }}>
          <div style={{ display: 'flex', alignItems: 'center', gap: 8, fontSize: 11, fontWeight: 600, textTransform: 'uppercase', letterSpacing: '0.18em', color: '#E91E8C', marginBottom: 10 }}>
            <div style={{ width: 18, height: 1.5, background: '#E91E8C' }} /> For drivers
          </div>
          <div style={{ fontSize: 'clamp(28px,3.5vw,40px)', fontWeight: 800, letterSpacing: -1.5, lineHeight: 1.1, marginBottom: 12 }}>
            Drive on your schedule.<br />Earn on your terms.
          </div>
          <div style={{ fontSize: 14, color: '#888', fontWeight: 300, lineHeight: 1.65, maxWidth: 320 }}>
            Join verified SafeRide drivers in Tetovo. Flexible hours, transparent earnings, and a platform built around you.
          </div>
          <Link to="/register" style={{ height: 46, padding: '0 26px', background: '#E91E8C', color: '#fff', borderRadius: 23, fontSize: 13, fontWeight: 600, textDecoration: 'none', display: 'inline-flex', alignItems: 'center', gap: 6, marginTop: 28 }}>
            Sign up to drive →
          </Link>
        </div>
        <div style={{ padding: '64px 32px', background: '#E91E8C', display: 'flex', flexDirection: 'column', justifyContent: 'space-between' }}>
          <div>
            <div style={{ fontSize: 10, fontWeight: 600, textTransform: 'uppercase', letterSpacing: '0.18em', color: 'rgba(255,255,255,0.65)', marginBottom: 6 }}>Average daily earnings</div>
            <div style={{ fontSize: 'clamp(40px,5vw,68px)', fontWeight: 800, color: '#fff', letterSpacing: -3, lineHeight: 1 }}>
              2,400 <span style={{ fontSize: '0.4em', fontWeight: 600 }}>MKD</span>
            </div>
            <div style={{ fontSize: 13, color: 'rgba(255,255,255,0.65)', fontWeight: 300, marginTop: 6 }}>flexible hours · cash or card</div>
          </div>
          <Link to="/register" style={{ height: 46, padding: '0 26px', background: '#0D0D0D', color: '#fff', borderRadius: 23, fontSize: 13, fontWeight: 600, textDecoration: 'none', display: 'inline-flex', alignItems: 'center', gap: 6, marginTop: 28, alignSelf: 'flex-start' }}>
            Apply as a driver →
          </Link>
        </div>
      </section>

      {/* ── FOOTER ── */}
      <footer style={{ background: '#0D0D0D', padding: '32px', display: 'flex', alignItems: 'center', justifyContent: 'space-between', flexWrap: 'wrap', gap: 16, borderTop: '1px solid rgba(255,255,255,0.06)' }}>
        <Link to="/" style={{ display: 'flex', alignItems: 'center', gap: 8, textDecoration: 'none' }}>
          <div style={{ width: 18, height: 18, background: '#E91E8C', borderRadius: '50%' }} />
          <span style={{ fontSize: 16, fontWeight: 700, color: '#fff' }}>SafeRide</span>
        </Link>
        <div style={{ display: 'flex', gap: 20 }}>
          {['Privacy','Terms','Safety','Help'].map(l => (
            <a key={l} href="#" style={{ color: 'rgba(255,255,255,0.35)', textDecoration: 'none', fontSize: 12 }}>{l}</a>
          ))}
        </div>
        <div style={{ fontSize: 12, color: 'rgba(255,255,255,0.25)' }}>© 2026 SafeRide · Tetovo, North Macedonia</div>
      </footer>
    </div>
  );
}
