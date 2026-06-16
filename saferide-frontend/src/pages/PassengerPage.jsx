import { useState, useEffect } from 'react';
import { Link } from 'react-router-dom';
import api from '../api';

const TETOVO_LOCATIONS = [
  { label: 'City Center (Qendra)', lat: 41.9981, lng: 20.9716 },
  { label: 'Tetovo Bazaar', lat: 41.9973, lng: 20.9712 },
  { label: 'Tetovo Bus Station', lat: 41.9990, lng: 20.9650 },
  { label: 'Tetovo Hospital', lat: 42.0010, lng: 20.9680 },
  { label: 'Kale Fortress', lat: 42.0089, lng: 20.9706 },
  { label: 'Tetovo University (SEEU)', lat: 42.0021, lng: 20.9631 },
  { label: 'Gostivar Road', lat: 41.9850, lng: 20.9600 },
  { label: 'Kamenjane', lat: 41.9780, lng: 20.9550 },
  { label: 'Lavce', lat: 42.0120, lng: 20.9800 },
  { label: 'Negotino (Tetovo)', lat: 41.9920, lng: 20.9900 },
  { label: 'Popova Shapka (turnoff)', lat: 42.0300, lng: 20.9400 },
  { label: 'Tetovo Railway Station', lat: 41.9995, lng: 20.9670 },
];

const S = { // shared inline styles
  eyebrow: { display: 'flex', alignItems: 'center', gap: 8, fontSize: 11, fontWeight: 600, textTransform: 'uppercase', letterSpacing: '0.18em', color: '#E91E8C', marginBottom: 6 },
  eyebrowLine: { width: 14, height: 1.5, background: '#E91E8C' },
  sectionTitle: { fontSize: 22, fontWeight: 800, letterSpacing: -0.8, color: '#0D0D0D', marginBottom: 20 },
};

export default function PassengerPage() {
  const [data, setData] = useState(null);
  const [loading, setLoading] = useState(true);
  const [msg, setMsg] = useState('');
  const [bookForm, setBookForm] = useState({ pickupAddress: '', pickupLat: 0, pickupLng: 0, dropoffAddress: '', dropoffLat: 0, dropoffLng: 0, paymentMethod: 'Cash' });
  const [step, setStep] = useState('form');
  const [drivers, setDrivers] = useState([]);
  const [fareInfo, setFareInfo] = useState(null);
  const [rateModal, setRateModal] = useState(null);
  const [sosLoading, setSosLoading] = useState(false);

  const load = async () => {
    try {
      const rides = (await api.get('/rides/my')).data;
      const active = rides.find(r => ['Requested','Accepted','InProgress'].includes(r.status));
      const history = rides.filter(r => ['Completed','Cancelled'].includes(r.status));
      setData({ active, history });
    } catch { } finally { setLoading(false); }
  };
  useEffect(() => { load(); }, []);

  const handlePickupChange = (e) => {
    const loc = TETOVO_LOCATIONS[e.target.value];
    if (loc) setBookForm(f => ({ ...f, pickupAddress: loc.label, pickupLat: loc.lat, pickupLng: loc.lng }));
  };
  const handleDropoffChange = (e) => {
    const loc = TETOVO_LOCATIONS[e.target.value];
    if (loc) setBookForm(f => ({ ...f, dropoffAddress: loc.label, dropoffLat: loc.lat, dropoffLng: loc.lng }));
  };

  const handleFindDrivers = async (e) => {
    e.preventDefault(); setMsg('');
    if (!bookForm.pickupLat || !bookForm.dropoffLat) { setMsg('Please select both locations.'); return; }
    if (bookForm.pickupAddress === bookForm.dropoffAddress) { setMsg('Pickup and dropoff cannot be the same.'); return; }
    try {
      const { data: res } = await api.get('/passenger/find-drivers', { params: { pickupLat: bookForm.pickupLat, pickupLng: bookForm.pickupLng, dropoffLat: bookForm.dropoffLat, dropoffLng: bookForm.dropoffLng } });
      setDrivers(res.drivers); setFareInfo(res); setStep('drivers');
    } catch (err) { setMsg(err.response?.data?.message || 'Failed to find drivers.'); }
  };

  const bookDriver = async (driverProfileId) => {
    setMsg('');
    try {
      await api.post('/passenger/book', { driverProfileId, ...bookForm });
      setStep('form'); setMsg('✅ Ride booked! Your driver is on the way.'); load();
    } catch (err) { setMsg(err.response?.data?.message || 'Booking failed.'); }
  };

  const cancelRide = async (rideId) => {
    if (!confirm('Cancel this ride?')) return;
    try { await api.patch(`/rides/${rideId}/cancel`); setMsg('Ride cancelled.'); load(); }
    catch (err) { setMsg(err.response?.data?.message || 'Cancel failed.'); }
  };

  const triggerSOS = async (rideId) => {
    if (!confirm('🚨 Send SOS? This will alert all admins immediately.')) return;
    setSosLoading(true);
    try {
      let lat = 41.9981, lng = 20.9716;
      try { const pos = await new Promise((res,rej) => navigator.geolocation.getCurrentPosition(res,rej,{timeout:5000})); lat = pos.coords.latitude; lng = pos.coords.longitude; } catch {}
      await api.post('/sos/trigger', { rideId, latitude: lat, longitude: lng });
      setMsg('🚨 SOS sent! Admins have been notified.');
    } catch (err) { setMsg('SOS failed. Please call 112 directly.'); }
    finally { setSosLoading(false); }
  };

  const submitRating = async (rideId, score, comment) => {
    try { await api.post('/ratings', { rideId, score: parseInt(score), comment }); setRateModal(null); load(); }
    catch (err) { setMsg(err.response?.data?.message || 'Rating failed.'); }
  };

  if (loading) return <div className="loading">Loading…</div>;

  return (
    <div style={{ background: '#f8f8f8', minHeight: 'calc(100vh - 60px)' }}>
      <div style={{ maxWidth: 900, margin: '0 auto', padding: '2.5rem 2rem' }}>

        {/* Page header */}
        <div style={{ marginBottom: '2rem' }}>
          <div style={S.eyebrow}><div style={S.eyebrowLine} /> Passenger</div>
          <h1 style={{ fontSize: 28, fontWeight: 800, letterSpacing: -1, color: '#0D0D0D' }}>Your Dashboard</h1>
        </div>

        {msg && (
          <div style={{ background: msg.startsWith('✅') || msg.startsWith('🚨') ? '#f0fdf4' : '#fff5f5', border: `1px solid ${msg.startsWith('✅') || msg.startsWith('🚨') ? '#bbf7d0' : '#fecaca'}`, borderRadius: 10, padding: '0.75rem 1rem', marginBottom: '1.25rem', fontSize: 13, color: msg.startsWith('✅') || msg.startsWith('🚨') ? '#166534' : '#b91c1c' }}>
            {msg}
          </div>
        )}

        {/* ── ACTIVE RIDE ── */}
        {data?.active && (
          <div style={{ background: '#0D0D0D', borderRadius: 16, padding: '1.5rem', marginBottom: '1.25rem' }}>
            <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'flex-start', marginBottom: '1rem' }}>
              <div>
                <div style={{ fontSize: 11, fontWeight: 600, textTransform: 'uppercase', letterSpacing: '0.18em', color: '#E91E8C', marginBottom: 4 }}>Active Ride</div>
                <div style={{ fontSize: 18, fontWeight: 800, color: '#fff', letterSpacing: -0.5 }}>
                  {data.active.pickupAddress} → {data.active.dropoffAddress}
                </div>
              </div>
              <button
                onClick={() => triggerSOS(data.active.id)} disabled={sosLoading}
                style={{ height: 40, padding: '0 18px', background: '#e53e3e', color: '#fff', border: 'none', borderRadius: 999, fontSize: 13, fontWeight: 700, cursor: 'pointer', fontFamily: 'Inter, sans-serif', flexShrink: 0 }}
              >
                🚨 {sosLoading ? 'Sending…' : 'SOS'}
              </button>
            </div>

            <div style={{ display: 'flex', gap: '1.5rem', marginBottom: '1rem' }}>
              {[['Driver', data.active.driverName || 'Matching…'], ['Status', data.active.status], ['Fare', `${data.active.estimatedFare?.toFixed(2)} MKD`]].map(([k, v]) => (
                <div key={k}>
                  <div style={{ fontSize: 10, fontWeight: 600, textTransform: 'uppercase', letterSpacing: '0.12em', color: 'rgba(255,255,255,0.35)', marginBottom: 3 }}>{k}</div>
                  <div style={{ fontSize: 14, fontWeight: 600, color: '#fff' }}>{v}</div>
                </div>
              ))}
            </div>

            {/* Map */}
            <div style={{ borderRadius: 10, overflow: 'hidden', marginBottom: '1rem' }}>
              <iframe title="map" width="100%" height="200" frameBorder="0" scrolling="no"
                src="https://www.openstreetmap.org/export/embed.html?bbox=20.90%2C41.95%2C21.10%2C42.05&layer=mapnik"
                style={{ display: 'block' }} />
            </div>

            <div style={{ display: 'flex', gap: '0.5rem' }}>
              <button onClick={() => cancelRide(data.active.id)} style={{ height: 36, padding: '0 16px', background: 'rgba(255,255,255,0.08)', border: '1px solid rgba(255,255,255,0.12)', color: '#fff', borderRadius: 999, fontSize: 13, fontWeight: 600, cursor: 'pointer', fontFamily: 'Inter, sans-serif' }}>
                Cancel Ride
              </button>
              {data.active.driverProfileId && (
                <Link to={`/chat/${data.active.id}`} style={{ height: 36, padding: '0 16px', background: '#E91E8C', color: '#fff', borderRadius: 999, fontSize: 13, fontWeight: 600, textDecoration: 'none', display: 'inline-flex', alignItems: 'center' }}>
                  💬 Chat
                </Link>
              )}
            </div>
          </div>
        )}

        {/* ── BOOK A RIDE ── */}
        {!data?.active && step === 'form' && (
          <div style={{ background: 'white', borderRadius: 16, border: '1px solid #efefef', padding: '1.5rem', marginBottom: '1.25rem' }}>
            <div style={S.eyebrow}><div style={S.eyebrowLine} /> Book a ride</div>
            <h2 style={{ fontSize: 18, fontWeight: 800, letterSpacing: -0.5, marginBottom: 4 }}>Where are you going?</h2>
            <p style={{ fontSize: 13, color: '#888', marginBottom: '1.25rem' }}>SafeRide operates within Tetovo. Pick your locations below.</p>

            <form onSubmit={handleFindDrivers}>
              <div className="grid-2" style={{ marginBottom: '1rem' }}>
                <div className="form-group">
                  <label>Pickup location</label>
                  <select required onChange={handlePickupChange} defaultValue="">
                    <option value="" disabled>Select pickup…</option>
                    {TETOVO_LOCATIONS.map((loc, i) => <option key={i} value={i}>{loc.label}</option>)}
                  </select>
                </div>
                <div className="form-group">
                  <label>Dropoff location</label>
                  <select required onChange={handleDropoffChange} defaultValue="">
                    <option value="" disabled>Select dropoff…</option>
                    {TETOVO_LOCATIONS.map((loc, i) => <option key={i} value={i}>{loc.label}</option>)}
                  </select>
                </div>
              </div>

              {bookForm.pickupAddress && bookForm.dropoffAddress && (
                <div style={{ background: '#FCE4EC', border: '1px solid #F8BBD9', borderRadius: 10, padding: '0.75rem 1rem', marginBottom: '1rem', fontSize: 13, color: '#C2185B' }}>
                  📍 <strong>{bookForm.pickupAddress}</strong> → <strong>{bookForm.dropoffAddress}</strong>
                </div>
              )}

              <div style={{ display: 'flex', gap: '0.75rem', alignItems: 'flex-end' }}>
                <div className="form-group" style={{ margin: 0, flex: '0 0 180px' }}>
                  <label>Payment</label>
                  <select value={bookForm.paymentMethod} onChange={e => setBookForm({ ...bookForm, paymentMethod: e.target.value })}>
                    <option value="Cash">Cash</option>
                    <option value="Card">Card</option>
                  </select>
                </div>
                <button type="submit" disabled={!bookForm.pickupLat || !bookForm.dropoffLat}
                  style={{ height: 46, padding: '0 24px', background: '#E91E8C', color: '#fff', border: 'none', borderRadius: 999, fontSize: 14, fontWeight: 600, cursor: 'pointer', fontFamily: 'Inter, sans-serif', opacity: !bookForm.pickupLat ? 0.5 : 1 }}>
                  Find Drivers →
                </button>
              </div>
            </form>
          </div>
        )}

        {/* ── DRIVER SELECTION ── */}
        {step === 'drivers' && (
          <div style={{ background: 'white', borderRadius: 16, border: '1px solid #efefef', padding: '1.5rem', marginBottom: '1.25rem' }}>
            <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', marginBottom: '1rem' }}>
              <div>
                <div style={S.eyebrow}><div style={S.eyebrowLine} /> Choose a driver</div>
                <h2 style={{ fontSize: 18, fontWeight: 800, letterSpacing: -0.5 }}>Available near you</h2>
              </div>
              <button onClick={() => setStep('form')} style={{ height: 34, padding: '0 14px', background: '#f0f0f0', border: 'none', borderRadius: 999, fontSize: 12, fontWeight: 600, cursor: 'pointer', fontFamily: 'Inter, sans-serif' }}>← Back</button>
            </div>

            {fareInfo && (
              <div style={{ background: '#FCE4EC', border: '1px solid #F8BBD9', borderRadius: 10, padding: '0.75rem 1rem', marginBottom: '1rem', fontSize: 13, color: '#C2185B' }}>
                {bookForm.pickupAddress} → {bookForm.dropoffAddress} &nbsp;·&nbsp;
                <strong>{fareInfo.estimatedFare?.toFixed(2)} MKD</strong> &nbsp;·&nbsp; {fareInfo.distanceKm} km
                {fareInfo.surgeMultiplier > 1 && <span> &nbsp;·&nbsp; surge ×{fareInfo.surgeMultiplier}</span>}
              </div>
            )}

            {drivers.length === 0 ? (
              <div style={{ textAlign: 'center', padding: '3rem', color: '#888' }}>
                <div style={{ fontSize: 32, marginBottom: 8 }}>🚫</div>
                <p>No drivers available right now.</p>
                <button onClick={() => setStep('form')} className="btn btn-secondary" style={{ marginTop: '1rem' }}>← Go Back</button>
              </div>
            ) : (
              <div style={{ display: 'flex', flexDirection: 'column', gap: '0.75rem' }}>
                {drivers.map(d => (
                  <div key={d.id} style={{ border: '1px solid #efefef', borderRadius: 12, padding: '1rem', display: 'flex', justifyContent: 'space-between', alignItems: 'center', gap: '1rem', transition: 'border-color 0.15s' }}
                    onMouseEnter={e => e.currentTarget.style.borderColor='#E91E8C'}
                    onMouseLeave={e => e.currentTarget.style.borderColor='#efefef'}>
                    <div style={{ flex: 1 }}>
                      <div style={{ fontWeight: 700, fontSize: 14, marginBottom: 3 }}>{d.fullName}</div>
                      <div style={{ fontSize: 12, color: '#888' }}>{d.vehicleColor} {d.vehicleYear} {d.vehicleMake} {d.vehicleModel} — {d.licensePlate}</div>
                      <div style={{ fontSize: 12, color: '#888', marginTop: 2 }}>📍 {d.distanceToPickup} km away · ETA ~{d.etaMinutes} min</div>
                    </div>
                    <div style={{ textAlign: 'right', flexShrink: 0 }}>
                      <div style={{ fontSize: 14, fontWeight: 700, color: '#E91E8C', marginBottom: 2 }}>⭐ {d.averageRating > 0 ? d.averageRating.toFixed(1) : 'New'}</div>
                      <div style={{ fontSize: 11, color: '#888', marginBottom: 8 }}>{d.totalRides} rides</div>
                      <button onClick={() => bookDriver(d.id)} style={{ height: 34, padding: '0 16px', background: '#E91E8C', color: '#fff', border: 'none', borderRadius: 999, fontSize: 12, fontWeight: 600, cursor: 'pointer', fontFamily: 'Inter, sans-serif' }}>
                        Book
                      </button>
                    </div>
                  </div>
                ))}
              </div>
            )}
          </div>
        )}

        {/* ── RIDE HISTORY ── */}
        {data?.history?.length > 0 && (
          <div style={{ background: 'white', borderRadius: 16, border: '1px solid #efefef', padding: '1.5rem' }}>
            <div style={S.eyebrow}><div style={S.eyebrowLine} /> History</div>
            <h2 style={{ fontSize: 18, fontWeight: 800, letterSpacing: -0.5, marginBottom: '1rem' }}>Past rides</h2>
            <table>
              <thead>
                <tr><th>From</th><th>To</th><th>Driver</th><th>Fare</th><th>Status</th><th></th></tr>
              </thead>
              <tbody>
                {data.history.map(r => (
                  <tr key={r.id}>
                    <td>{r.pickupAddress}</td>
                    <td>{r.dropoffAddress}</td>
                    <td>{r.driverName || '—'}</td>
                    <td style={{ fontWeight: 600 }}>{(r.finalFare ?? r.estimatedFare)?.toFixed(2)} MKD</td>
                    <td><span className={`status-${r.status}`}>{r.status}</span></td>
                    <td>
                      <div style={{ display: 'flex', gap: 6 }}>
                        <Link to={`/receipt/${r.id}`} className="btn btn-secondary btn-sm">Receipt</Link>
                        {r.status === 'Completed' && (
                          <button className="btn btn-primary btn-sm" onClick={() => setRateModal(r.id)}>⭐ Rate</button>
                        )}
                      </div>
                    </td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
        )}

        {rateModal && <RatingModal rideId={rateModal} onSubmit={submitRating} onClose={() => setRateModal(null)} />}
      </div>
    </div>
  );
}

function RatingModal({ rideId, onSubmit, onClose }) {
  const [score, setScore] = useState(5);
  const [comment, setComment] = useState('');
  return (
    <div style={{ position: 'fixed', inset: 0, background: 'rgba(0,0,0,0.6)', display: 'flex', alignItems: 'center', justifyContent: 'center', zIndex: 999 }}>
      <div style={{ background: 'white', borderRadius: 16, padding: '1.5rem', width: 380, border: '1px solid #efefef' }}>
        <h2 style={{ fontSize: 18, fontWeight: 800, letterSpacing: -0.5, marginBottom: '1rem' }}>Rate your ride</h2>
        <div className="form-group">
          <label>Score</label>
          <select value={score} onChange={e => setScore(e.target.value)}>
            {[5,4,3,2,1].map(n => <option key={n} value={n}>{n} ⭐</option>)}
          </select>
        </div>
        <div className="form-group">
          <label>Comment (optional)</label>
          <textarea rows={3} value={comment} onChange={e => setComment(e.target.value)} style={{ width: '100%', padding: '0.65rem 1rem', borderRadius: 10, border: '1.5px solid #efefef', fontFamily: 'Inter, sans-serif', resize: 'vertical' }} />
        </div>
        <div style={{ display: 'flex', gap: '0.5rem' }}>
          <button onClick={() => onSubmit(rideId, score, comment)} style={{ height: 40, padding: '0 20px', background: '#E91E8C', color: '#fff', border: 'none', borderRadius: 999, fontSize: 13, fontWeight: 600, cursor: 'pointer', fontFamily: 'Inter, sans-serif' }}>Submit</button>
          <button onClick={onClose} style={{ height: 40, padding: '0 20px', background: '#f0f0f0', border: 'none', borderRadius: 999, fontSize: 13, fontWeight: 600, cursor: 'pointer', fontFamily: 'Inter, sans-serif' }}>Cancel</button>
        </div>
      </div>
    </div>
  );
}
