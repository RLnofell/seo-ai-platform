import { useState, useRef, useEffect } from 'react';
import axios from 'axios';
import { HubConnectionBuilder, LogLevel } from '@microsoft/signalr';
import { motion, AnimatePresence } from 'framer-motion';
import { Search, Loader2, Cpu, FileText, CheckCircle } from 'lucide-react';
import './index.css';

function App() {
  const [input, setInput] = useState('');
  const [loading, setLoading] = useState(false);
  const [logs, setLogs] = useState([]);
  const [result, setResult] = useState(null);
  const logsEndRef = useRef(null);
  const connectionRef = useRef(null);

  // Initialize SignalR connection
  useEffect(() => {
    const newConnection = new HubConnectionBuilder()
      .withUrl('http://localhost:5000/agentHub')
      .withAutomaticReconnect()
      .configureLogging(LogLevel.Information)
      .build();

    newConnection.start()
      .then(() => {
        console.log('Connected to SignalR Hub');
        newConnection.on('ReceiveLog', (message) => {
          setLogs(prev => [...prev, message]);
        });
      })
      .catch(err => console.error('SignalR Connection Error: ', err));

    connectionRef.current = newConnection;

    return () => {
      if (connectionRef.current) {
        connectionRef.current.stop();
      }
    };
  }, []);

  // Auto scroll logs
  useEffect(() => {
    if (logsEndRef.current) {
      logsEndRef.current.scrollIntoView({ behavior: 'smooth' });
    }
  }, [logs]);

  const handleRunAgent = async (e) => {
    e.preventDefault();
    if (!input.trim()) return;

    setLoading(true);
    setLogs([]); // Reset logs for new run
    setResult(null);

    try {
      const response = await axios.post('http://localhost:5000/api/agent/run', {
        input: input,
        connectionId: connectionRef.current?.connectionId || ''
      });
      
      const { finalArticle, densityResult, postResult } = response.data;
      setResult({ finalArticle, densityResult, postResult });
      
    } catch (error) {
      const errorMsg = error.response?.data?.error || error.message;
      setLogs(prev => [...prev, `[LỖI HỆ THỐNG] ${errorMsg}`]);
    } finally {
      setLoading(false);
    }
  };

  const getLogClass = (log) => {
    if (log.includes('[RAG Plugin]') || log.includes('[Seo Plugin]') || log.includes('[Agentic Pipeline]')) return 'highlight';
    if (log.includes('thành công') || log.includes('[v]') || log.includes('[HOÀN TẤT]')) return 'success';
    if (log.includes('[LỖI]')) return 'error';
    return '';
  };

  return (
    <div className="app-container">
      <header>
        <h1>AI SEO Agent <span className="badge">v2.0</span></h1>
        <p className="subtitle">Hệ thống Multi-Agent tối ưu SEO và RAG chuyên sâu</p>
      </header>

      <form className="input-section" onSubmit={handleRunAgent}>
        <input
          type="text"
          className="cyber-input"
          placeholder="Nhập yêu cầu (VD: 'Viết bài SEO về xe cẩu tự hành')"
          value={input}
          onChange={(e) => setInput(e.target.value)}
          disabled={loading}
        />
        <button className="cyber-btn" type="submit" disabled={loading || !input.trim()}>
          {loading ? <Loader2 className="spinner" size={20} /> : <Search size={20} />}
          {loading ? 'Agent đang chạy...' : 'Bắt đầu quy trình'}
        </button>
      </form>

      <div className="main-content">
        <div className="glass-panel">
          <h2 style={{ display: 'flex', alignItems: 'center', gap: '8px', marginBottom: '1rem', fontSize: '1.2rem', color: 'var(--accent-cyan)' }}>
            <Cpu size={20} /> Real-time Agent Workflow
          </h2>
          <div className="log-container">
            {logs.length === 0 && !loading && <span style={{ color: 'var(--text-secondary)' }}>Sẵn sàng nhận lệnh...</span>}
            <AnimatePresence>
              {logs.map((log, index) => (
                <motion.div 
                  key={index}
                  initial={{ opacity: 0, x: -10 }}
                  animate={{ opacity: 1, x: 0 }}
                  className={`log-entry ${getLogClass(log)}`}
                >
                  {log}
                </motion.div>
              ))}
            </AnimatePresence>
            <div ref={logsEndRef} />
          </div>
        </div>

        <div className="glass-panel result-container">
          <h2 style={{ display: 'flex', alignItems: 'center', gap: '8px', fontSize: '1.2rem', color: 'var(--success)' }}>
            <FileText size={20} /> Nội dung SEO hoàn thiện
          </h2>
          
          {!result && !loading && (
            <div style={{ color: 'var(--text-secondary)', textAlign: 'center', marginTop: '4rem', opacity: 0.5 }}>
              <FileText size={48} style={{ marginBottom: '1rem' }} />
              <p>Kết quả sẽ xuất hiện tại đây sau khi Agent xử lý xong</p>
            </div>
          )}

          {loading && (
            <div className="loading-state">
              <Loader2 className="spinner" size={48} />
              <p>Đang tổng hợp dữ liệu RAG và tối ưu bài viết...</p>
            </div>
          )}

          {result && (
            <motion.div 
              initial={{ opacity: 0, scale: 0.95 }}
              animate={{ opacity: 1, scale: 1 }}
              className="result-content"
            >
              <div className="stats-grid">
                {result.densityResult && (
                  <div className="stat-card">
                    <CheckCircle size={16} />
                    <span>{result.densityResult}</span>
                  </div>
                )}
                {result.postResult && (
                  <div className="stat-card success">
                    <CheckCircle size={16} />
                    <span>{result.postResult}</span>
                  </div>
                )}
              </div>
              
              <div className="article-box">
                {result.finalArticle}
              </div>
            </motion.div>
          )}
        </div>
      </div>
    </div>
  );
}

export default App;
