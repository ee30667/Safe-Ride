import { useState, useEffect } from 'react';
import { Link } from 'react-router-dom';
import api from '../api';

const S = {
  eyebrow: { display: 'flex', alignItems: 'center', gap: 8, fontSize: 11, fontWeight: 600, textTransform: 'uppercase', letterSpacing: '0.18em', color: '#E91E8C', marginBottom: 6 },
  eyebrowLine: { width: 14, height: 1.5, background: '#E91E8C' },
};

export default function DriverPage() {
  const [data, setData] = useState(null);
  const [loading, setLoading] = useState(true);
  const [msg, setMsg] = useState('');
  const [profileForm, setProfileForm] = useState({ licenseNumber: '', vehicleMake: '', vehicleModel: '', vehicleColor: '', licensePlate: '', vehicleYear: 2020 });
  const [showProfileForm, setShowProfileForm] = useState(false);

  const load = async () => {
    try {
      const ridesRes = await api.get('/rides/my');
      const rides = ridesRes.data;
      const active  = rides.find(r => r.status === 'Accepted' || r.status === 'InProgress');
      const pending = rides.filter(r => r.status === 'Requested');
      const history = rides.filter(r => r.status === 'Completed');
      let driver = null;
      try { const { data: d } = await api.get('/drivers/me'); driver = d; } catch {}
      setData({ active, pending, history, driver });
    } catch {} finally { setLoading(false); }
  };
  useEffect(() => { load(); }, []);

  const createProfile = async (e) => {
    e.preventDefault(); setMsg('');
    try {
      await api.post('/drivers', { ...profileForm, vehicleYear: parseInt(profileForm.vehicleYear) });
      setMsg('✅ Profile submitted! Waiting for admin approval.');
      setShowProfileForm(false); load();
    } catch (err) { setMsg(err.response?.data?.message || 'Failed'); }
  };

  const toggleAvailability = async () => {
    try {
      const newStatus = !data?.driver?.isAvailable;
      await api.patch('/drivers/availability', newStatus, { headers: { 'Content-Type': 'application/json' } });
      setMsg(newStatus ? '🟢 You are now online.' : '🔴 You are now offline.');
      load();
    } catch (err) { setMsg(err.response?.data?.message || 'Failed'); }
  };

  const rideAction = async (rideId, action) => {
    setMsg('');
    try { await api.patch(`/rides/${rideId}/${action}`); setMsg(`✅ Ride ${action}ed.`); load(); }
    catch (err) { setMsg(err.response?.data?.message || 'Action failed'); }
  };

  if (loading) return <div className="loading">Loading…</div>;

  const isOnline = data?.driver?.isAvailable;

  return (
    <div style={{ background: '#f8f8f8', minHeight: 'calc(100vh - 60px)' }}>
      <div style={{ maxWidth: 900, margin: '0 auto', padding: '2.5rem 2rem' }}>

        <div style={{ marginBottom: '2rem' }}>
          <div style={S.eyebrow}><div style={S.eyebrowLine} /> Driver</div>
          <h1 style={{ fontSize: 28, fontWeight: 800, letterSpacing: -1, color: '#0D0D0D' }}>Your Dashboard</h1>
        </div>

        {msg && (
          <div style={{ background: msg.startsWith('✅') || msg.startsWith('🟢') ? '#f0fdf4' : '#fff5f5', border: `1px solid ${msg.startsWith('✅') || msg.startsWith('🟢') ? '#bbf7d0' : '#fecaca'}`, borderRadius: 10, padding: '0.75rem 1rem', marginBottom: '1.25rem', fontSize: 13, color: msg.startsWith('✅') || msg.startsWith('🟢') ? '#166534' : '#b91c1c' }}>
            {msg}
          </div>
        )}

        {/* ── NO PROFILE ── */}
        {!data?.driver && !showProfileForm && (
          <div style={{ background: 'white', borderRadius: 16, border: '1.5px solid #E91E8C', padding: '1.5rem', marginBottom: '1.25rem' }}>
            <div style={S.eyebrow}><div style={S.eyebrowLine} /> Setup required</div>
            <h2 style={{ fontSize: 18, fontWeight: 800, letterSpacing: -0.5, marginBottom: 8 }}>Complete your driver profile</h2>
            <p style={{ fontSize: 13, color: '#888', marginBottom: '1rem' }}>You need to set up your profile before accepting rides.</p>
            <button onClick={() => setShowProfileForm(true)} style={{ height: 40, padding: '0 20px', background: '#E91E8C', color: '#fff', border: 'none', borderRadius: 999, fontSize: 13, fontWeight: 600, cursor: 'pointer', fontFamily: 'Inter, sans-serif' }}>
              Set Up Profile
            </button>
          </div>
        )}

        {/* ── PENDING APPROVAL ── */}
        {data?.driver && !data.driver.isApproved && (
          <div style={{ background: '#FDF0F4', border: '1px solid #F8BBD9', borderRadius: 16, padding: '1.5rem', marginBottom: '1.25rem' }}>
            <div style={{ fontSize: 18, fontWeight: 800, color: '#C2185B', letterSpacing: -0.5 }}>⏳ Pending approval</div>
            <p style={{ fontSize: 13, color: '#880E4F', marginTop: 6 }}>Your profile is under review. Admin approval is needed before your first ride.</p>
          </div>
        )}

        {/* ── DRIVER STATUS CARD ── */}
        {data?.driver?.isApproved && (
          <div style={{ background: '#0D0D0D', borderRadius: 16, padding: '1.25rem 1.5rem', marginBottom: '1.25rem', display: 'flex', justifyContent: 'space-between', alignItems: 'center' }}>
            <div>
              <div style={{ fontSize: 11, fontWeight: 600, textTransform: 'uppercase', letterSpacing: '0.18em', color: isOnline ? '#22c55e' : 'rgba(255,255,255,0.35)', marginBottom: 4 }}>
                {isOnline ? '● Online' : '○ Offline'}
              </div>
              <div style={{ fontSize: 16, fontWeight: 700, color: '#fff' }}>{data.driver.fullName}</div>
              <div style={{ fontSize: 12, color: 'rgba(255,255,255,0.4)', marginTop: 2 }}>
                ⭐ {data.driver.averageRating?.toFixed(1) || 'No ratings'} · {data.driver.totalRides} rides
              </div>
            </div>
            <button onClick={toggleAvailability} style={{ height: 40, padding: '0 20px', background: isOnline ? 'rgba(239,68,68,0.15)' : '#E91E8C', border: `1px solid ${isOnline ? 'rgba(239,68,68,0.4)' : '#E91E8C'}`, color: isOnline ? '#ef4444' : '#fff', borderRadius: 999, fontSize: 13, fontWeight: 600, cursor: 'pointer', fontFamily: 'Inter, sans-serif' }}>
              {isOnline ? '🔴 Go Offline' : '🟢 Go Online'}
            </button>
          </div>
        )}

        {/* ── PROFILE FORM ── */}
        {showProfileForm && (
          <div style={{ background: 'white', borderRadius: 16, border: '1px solid #efefef', padding: '1.5rem', marginBottom: '1.25rem' }}>
            <h2 style={{ fontSize: 18, fontWeight: 800, letterSpacing: -0.5, marginBottom: '1rem' }}>Driver Profile Setup</h2>
            <form onSubmit={createProfile}>
              <div className="grid-2">
                <div className="form-group"><label>License Number</label><input required value={profileForm.licenseNumber} onChange={e => setProfileForm({...profileForm, licenseNumber: e.target.value})} /></div>
                <div className="form-group"><label>Vehicle Make</label><input required value={profileForm.vehicleMake} onChange={e => setProfileForm({...profileForm, vehicleMake: e.target.value})} placeholder="Toyota" /></div>
                <div className="form-group"><label>Vehicle Model</label><input required value={profileForm.vehicleModel} onChange={e => setProfileForm({...profileForm, vehicleModel: e.target.value})} placeholder="Corolla" /></div>
                <div className="form-group"><label>Vehicle Color</label><input required value={profileForm.vehicleColor} onChange={e => setProfileForm({...profileForm, vehicleColor: e.target.value})} placeholder="White" /></div>
                <div className="form-group"><label>License Plate</label><input required value={profileForm.licensePlate} onChange={e => setProfileForm({...profileForm, licensePlate: e.target.value})} placeholder="SK-1234-AB" /></div>
                <div className="form-group"><label>Vehicle Year</label><input type="number" required value={profileForm.vehicleYear} onChange={e => setProfileForm({...profileForm, vehicleYear: e.target.value})} /></div>
              </div>
              <div style={{ display: 'flex', gap: '0.5rem', marginTop: '0.5rem' }}>
                <button type="submit" className="btn btn-primary">Submit Profile</button>
                <button type="button" className="btn btn-secondary" onClick={() => setShowProfileForm(false)}>Cancel</button>
              </div>
            </form>
          </div>
        )}

        {/* ── ACTIVE RIDE ── */}
        {data?.active && (
          <div style={{ background: '#0D0D0D', borderRadius: 16, padding: '1.5rem', marginBottom: '1.25rem' }}>
            <div style={{ fontSize: 11, fontWeight: 600, textTransform: 'uppercase', letterSpacing: '0.18em', color: '#22c55e', marginBottom: 8 }}>● Current Ride</div>
            <div style={{ fontSize: 18, fontWeight: 800, color: '#fff', letterSpacing: -0.5, marginBottom: '0.75rem' }}>
              {data.active.pickupAddress} → {data.active.dropoffAddress}
            </div>
            <div style={{ display: 'flex', gap: '1.5rem', marginBottom: '1rem' }}>
              {[['Passenger', data.active.passengerName], ['Status', data.active.status], ['Fare', `${data.active.estimatedFare} MKD`]].map(([k,v]) => (
                <div key={k}>
                  <div style={{ fontSize: 10, color: 'rgba(255,255,255,0.35)', textTransform: 'uppercase', letterSpacing: '0.12em', marginBottom: 3 }}>{k}</div>
                  <div style={{ fontSize: 14, fontWeight: 600, color: '#fff' }}>{v}</div>
                </div>
              ))}
            </div>
            <div style={{ display: 'flex', gap: '0.5rem' }}>
              {data.active.status === 'Accepted' && (
                <button onClick={() => rideAction(data.active.id, 'start')} style={{ height: 36, padding: '0 16px', background: '#16a34a', color: '#fff', border: 'none', borderRadius: 999, fontSize: 12, fontWeight: 600, cursor: 'pointer', fontFamily: 'Inter, sans-serif' }}>Start Ride</button>
              )}
              {data.active.status === 'InProgress' && (
                <button onClick={() => rideAction(data.active.id, 'complete')} style={{ height: 36, padding: '0 16px', background: '#E91E8C', color: '#fff', border: 'none', borderRadius: 999, fontSize: 12, fontWeight: 600, cursor: 'pointer', fontFamily: 'Inter, sans-serif' }}>Complete Ride</button>
              )}
              <Link to={`/chat/${data.active.id}`} style={{ height: 36, padding: '0 16px', background: 'rgba(255,255,255,0.08)', border: '1px solid rgba(255,255,255,0.12)', color: '#fff', borderRadius: 999, fontSize: 12, fontWeight: 600, textDecoration: 'none', display: 'inline-flex', alignItems: 'center' }}>
                💬 Chat
              </Link>
            </div>
          </div>
        )}

        {/* ── INCOMING REQUESTS ── */}
        {data?.pending?.length > 0 && (
          <div style={{ background: 'white', borderRadius: 16, border: '1px solid #efefef', padding: '1.5rem', marginBottom: '1.25rem' }}>
            <div style={S.eyebrow}><div style={S.eyebrowLine} /> Incoming</div>
            <h2 style={{ fontSize: 18, fontWeight: 800, letterSpacing: -0.5, marginBottom: '1rem' }}>Ride requests</h2>
            {!isOnline && (
              <div style={{ background: '#fff5f5', border: '1px solid #fecaca', borderRadius: 10, padding: '0.75rem 1rem', marginBottom: '1rem', fontSize: 13, color: '#b91c1c' }}>
                ⚠️ You are offline. Go online above to accept rides.
              </div>
            )}
            <div style={{ display: 'flex', flexDirection: 'column', gap: '0.75rem' }}>
              {data.pending.map(r => (
                <div key={r.id} style={{ border: '1px solid #efefef', borderRadius: 12, padding: '1rem', display: 'flex', justifyContent: 'space-between', alignItems: 'center' }}>
                  <div>
                    <div style={{ fontWeight: 700, fontSize: 14, marginBottom: 3 }}>{r.pickupAddress} → {r.dropoffAddress}</div>
                    <div style={{ fontSize: 12, color: '#888' }}>Passenger: {r.passengerName} · {r.estimatedFare} MKD</div>
                  </div>
                  <button disabled={!isOnline} onClick={() => rideAction(r.id, 'accept')}
                    style={{ height: 36, padding: '0 16px', background: isOnline ? '#16a34a' : '#e2e2e2', color: isOnline ? '#fff' : '#888', border: 'none', borderRadius: 999, fontSize: 12, fontWeight: 600, cursor: isOnline ? 'pointer' : 'not-allowed', fontFamily: 'Inter, sans-serif' }}>
                    {isOnline ? 'Accept' : 'Go Online'}
                  </button>
                </div>
              ))}
            </div>
          </div>
        )}

        {/* ── QUICK LINKS ── */}
        <div style={{ display: 'flex', gap: '0.75rem', marginBottom: '1.25rem' }}>
          <Link to="/driver/earnings" className="btn btn-secondary">💰 Earnings</Link>
          <Link to="/driver/notifications" className="btn btn-secondary">🔔 Notifications</Link>
        </div>

        {/* ── HISTORY ── */}
        {data?.history?.length > 0 && (
          <div style={{ background: 'white', borderRadius: 16, border: '1px solid #efefef', padding: '1.5rem' }}>
            <div style={S.eyebrow}><div style={S.eyebrowLine} /> History</div>
            <h2 style={{ fontSize: 18, fontWeight: 800, letterSpacing: -0.5, marginBottom: '1rem' }}>Completed rides</h2>
            <table>
              <thead><tr><th>From</th><th>To</th><th>Fare</th><th>Date</th><th></th></tr></thead>
              <tbody>
                {data.history.slice(0, 10).map(r => (
                  <tr key={r.id}>
                    <td>{r.pickupAddress}</td>
                    <td>{r.dropoffAddress}</td>
                    <td style={{ fontWeight: 600 }}>{(r.finalFare ?? r.estimatedFare)?.toFixed(2)} MKD</td>
                    <td>{new Date(r.completedAt).toLocaleDateString()}</td>
                    <td><Link to={`/receipt/${r.id}`} className="btn btn-secondary btn-sm">Receipt</Link></td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
        )}
      </div>
    </div>
  );
}
