import { useState, useEffect, useRef } from 'react';
import { useParams, Link } from 'react-router-dom';
import api from '../api';
import { useAuth } from '../context/AuthContext';

// ── Chat ───────────────────────────────────────────────────────────────────────
export function ChatPage() {
  const { rideId } = useParams();
  const { user } = useAuth();
  const [messages, setMessages] = useState([]);
  const [text, setText] = useState('');
  const bottomRef = useRef(null);
  const lastIdRef = useRef(0);

  useEffect(() => {
    api.get(`/chat/${rideId}`).then(r => {
      setMessages(r.data);
      if (r.data.length > 0) lastIdRef.current = r.data.at(-1).id;
    });
    const interval = setInterval(async () => {
      try {
        const { data } = await api.get(`/chat/${rideId}`, { params: { lastId: lastIdRef.current } });
        if (data.length > 0) { setMessages(prev => [...prev, ...data]); lastIdRef.current = data.at(-1).id; }
      } catch {}
    }, 3000);
    return () => clearInterval(interval);
  }, [rideId]);

  useEffect(() => { bottomRef.current?.scrollIntoView({ behavior: 'smooth' }); }, [messages]);

  const send = async (e) => {
    e.preventDefault();
    if (!text.trim()) return;
    await api.post(`/chat/${rideId}`, { message: text });
    setText('');
    const { data } = await api.get(`/chat/${rideId}`, { params: { lastId: lastIdRef.current } });
    if (data.length > 0) { setMessages(prev => [...prev, ...data]); lastIdRef.current = data.at(-1).id; }
  };

  return (
    <div style={{ background: '#f8f8f8', minHeight: 'calc(100vh - 60px)', display: 'flex', flexDirection: 'column', alignItems: 'center', padding: '2.5rem 2rem' }}>
      <div style={{ width: '100%', maxWidth: 600 }}>
        {/* Header */}
        <div style={{ marginBottom: '1.5rem' }}>
          <div style={{ display: 'flex', alignItems: 'center', gap: 8, fontSize: 11, fontWeight: 600, textTransform: 'uppercase', letterSpacing: '0.18em', color: '#E91E8C', marginBottom: 6 }}>
            <div style={{ width: 14, height: 1.5, background: '#E91E8C' }} /> Chat
          </div>
          <h1 style={{ fontSize: 24, fontWeight: 800, letterSpacing: -0.8 }}>Ride #{rideId}</h1>
        </div>

        {/* Messages */}
        <div style={{ background: 'white', borderRadius: 16, border: '1px solid #efefef', padding: '1rem', minHeight: 300, maxHeight: 420, overflowY: 'auto', display: 'flex', flexDirection: 'column', gap: '0.5rem', marginBottom: '0.75rem' }}>
          {messages.length === 0 && (
            <div style={{ textAlign: 'center', color: '#888', fontSize: 13, padding: '2rem' }}>No messages yet. Start the conversation.</div>
          )}
          {messages.map(m => {
            const isMine = m.senderId === user?.userId;
            return (
              <div key={m.id} style={{ alignSelf: isMine ? 'flex-end' : 'flex-start', maxWidth: '75%' }}>
                {!isMine && <div style={{ fontSize: 11, color: '#aaa', marginBottom: 3, fontWeight: 600 }}>{m.senderName}</div>}
                <div style={{ background: isMine ? '#E91E8C' : '#f0f0f0', color: isMine ? '#fff' : '#0D0D0D', padding: '0.55rem 1rem', borderRadius: isMine ? '18px 18px 4px 18px' : '18px 18px 18px 4px', fontSize: 14, lineHeight: 1.5 }}>
                  {m.message}
                </div>
                <div style={{ fontSize: 10, color: '#ccc', marginTop: 3, textAlign: isMine ? 'right' : 'left' }}>
                  {new Date(m.sentAt).toLocaleTimeString([], { hour: '2-digit', minute: '2-digit' })}
                </div>
              </div>
            );
          })}
          <div ref={bottomRef} />
        </div>

        {/* Input */}
        <form onSubmit={send} style={{ display: 'flex', gap: '0.5rem' }}>
          <input
            value={text} onChange={e => setText(e.target.value)}
            placeholder="Type a message…"
            style={{ flex: 1, padding: '0.75rem 1rem', borderRadius: 999, border: '1.5px solid #efefef', fontSize: 14, fontFamily: 'Inter, sans-serif', outline: 'none' }}
            onFocus={e => e.target.style.borderColor='#E91E8C'}
            onBlur={e => e.target.style.borderColor='#efefef'}
          />
          <button type="submit" style={{ height: 46, padding: '0 22px', background: '#E91E8C', color: '#fff', border: 'none', borderRadius: 999, fontSize: 14, fontWeight: 600, cursor: 'pointer', fontFamily: 'Inter, sans-serif' }}>
            Send
          </button>
        </form>
      </div>
    </div>
  );
}

// ── Receipt ────────────────────────────────────────────────────────────────────
export function ReceiptPage() {
  const { rideId } = useParams();
  const [receipt, setReceipt] = useState(null);
  useEffect(() => { api.get(`/receipt/${rideId}`).then(r => setReceipt(r.data)); }, [rideId]);

  if (!receipt) return <div className="loading">Loading receipt…</div>;

  const rows = [
    ['Passenger', receipt.passengerName],
    ['Driver', receipt.driverName],
    ['Vehicle', receipt.vehicleInfo],
    ['From', receipt.pickupAddress],
    ['To', receipt.dropoffAddress],
    ['Distance', `${receipt.distanceKm?.toFixed(1)} km`],
    ['Duration', `${receipt.durationMinutes} min`],
    ['Base Fare', `${receipt.baseFare} MKD`],
    ['Distance Fare', `${receipt.distanceFare?.toFixed(2)} MKD`],
    ...(receipt.surgeMultiplier > 1 ? [['Surge', `×${receipt.surgeMultiplier}`]] : []),
    ['Payment', receipt.paymentMethod],
    ['Date', new Date(receipt.completedAt).toLocaleString()],
  ];

  return (
    <div style={{ background: '#f8f8f8', minHeight: 'calc(100vh - 60px)', display: 'flex', alignItems: 'flex-start', justifyContent: 'center', padding: '2.5rem 2rem' }}>
      <div style={{ width: '100%', maxWidth: 480 }}>
        {/* Header */}
        <div style={{ background: '#0D0D0D', borderRadius: '16px 16px 0 0', padding: '2rem', textAlign: 'center' }}>
          <div style={{ fontSize: 10, fontWeight: 600, textTransform: 'uppercase', letterSpacing: '0.18em', color: '#E91E8C', marginBottom: 8 }}>Receipt</div>
          <div style={{ fontSize: 32, fontWeight: 800, color: '#fff', letterSpacing: -1, marginBottom: 4 }}>
            {receipt.totalFare?.toFixed(2)} <span style={{ fontSize: 16, fontWeight: 600, color: 'rgba(255,255,255,0.5)' }}>MKD</span>
          </div>
          <div style={{ fontSize: 13, color: 'rgba(255,255,255,0.4)' }}>Ride #{receipt.rideId}</div>
        </div>

        {/* Details */}
        <div style={{ background: 'white', borderRadius: '0 0 16px 16px', border: '1px solid #efefef', borderTop: 'none', padding: '1.5rem' }}>
          {rows.map(([k, v]) => (
            <div key={k} style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'flex-start', padding: '0.5rem 0', borderBottom: '1px solid #f8f8f8' }}>
              <span style={{ fontSize: 13, color: '#888', flexShrink: 0, marginRight: 12 }}>{k}</span>
              <span style={{ fontSize: 13, fontWeight: 500, textAlign: 'right' }}>{v}</span>
            </div>
          ))}

          {/* Total highlight */}
          <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', padding: '1rem 0 0', marginTop: '0.5rem' }}>
            <span style={{ fontSize: 14, fontWeight: 700 }}>Total paid</span>
            <span style={{ fontSize: 20, fontWeight: 800, color: '#E91E8C', letterSpacing: -0.5 }}>{receipt.totalFare?.toFixed(2)} MKD</span>
          </div>

          <button onClick={() => window.print()} style={{ width: '100%', height: 44, background: '#f0f0f0', border: 'none', borderRadius: 999, fontSize: 13, fontWeight: 600, cursor: 'pointer', marginTop: '1rem', fontFamily: 'Inter, sans-serif', color: '#0D0D0D' }}>
            🖨 Print Receipt
          </button>
        </div>
      </div>
    </div>
  );
}
