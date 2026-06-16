import { Routes, Route, Navigate } from 'react-router-dom';
import Navbar from './components/Navbar';
import ProtectedRoute from './components/ProtectedRoute';
import LoginPage from './pages/LoginPage';
import RegisterPage from './pages/RegisterPage';
import PassengerPage from './pages/PassengerPage';
import DriverPage from './pages/DriverPage';
import AdminPage from './pages/AdminPage';
import { ChatPage, ReceiptPage } from './pages/ChatAndReceiptPages';
import { NotificationsPage, EarningsPage, AnalyticsPage, SosAlertsPage, HomePage } from './pages/OtherPages';

export default function App() {
  return (
    <>
      <Navbar />
      <Routes>
        <Route path="/" element={<HomePage />} />
        <Route path="/login" element={<LoginPage />} />
        <Route path="/register" element={<RegisterPage />} />

        <Route path="/passenger" element={<ProtectedRoute role="Passenger"><PassengerPage /></ProtectedRoute>} />
        <Route path="/passenger/notifications" element={<ProtectedRoute role="Passenger"><NotificationsPage /></ProtectedRoute>} />

        <Route path="/driver" element={<ProtectedRoute role="Driver"><DriverPage /></ProtectedRoute>} />
        <Route path="/driver/earnings" element={<ProtectedRoute role="Driver"><EarningsPage /></ProtectedRoute>} />
        <Route path="/driver/notifications" element={<ProtectedRoute role="Driver"><NotificationsPage /></ProtectedRoute>} />

        <Route path="/admin" element={<ProtectedRoute role="Admin"><AdminPage /></ProtectedRoute>} />
        <Route path="/admin/analytics" element={<ProtectedRoute role="Admin"><AnalyticsPage /></ProtectedRoute>} />
        <Route path="/admin/sos" element={<ProtectedRoute role="Admin"><SosAlertsPage /></ProtectedRoute>} />

        <Route path="/chat/:rideId" element={<ProtectedRoute><ChatPage /></ProtectedRoute>} />
        <Route path="/receipt/:rideId" element={<ProtectedRoute><ReceiptPage /></ProtectedRoute>} />

        <Route path="*" element={<Navigate to="/" replace />} />
      </Routes>
    </>
  );
}
