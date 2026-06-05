import { useState, useRef, useEffect } from 'react';
import axios from 'axios';
import { HubConnectionBuilder, LogLevel } from '@microsoft/signalr';
import { motion, AnimatePresence } from 'framer-motion';
import { Sparkles, Loader2, Cpu, FileText, CheckCircle, Send } from 'lucide-react';
import './index.css';

const translations = {
  vi: {
    title: 'AI SEO Agent',
    badge: 'PRO',
    subtitle: 'Hệ thống Multi-Agent tối ưu SEO và RAG chuyên sâu',
    placeholder: "Nhập yêu cầu (VD: 'Viết bài chuẩn SEO về dịch vụ Thắng Hiền...')",
    btnSubmit: 'Khởi chạy Agent',
    btnRunning: 'Đang phân tích...',
    panelManager: 'Trình quản lý Agent (Real-time)',
    waiting: 'Hệ thống đang chờ lệnh từ bạn...',
    panelResult: 'Kết quả xuất bản',
    resultPlaceholder: 'Nội dung bài viết sẽ xuất hiện tại đây',
    loadingState: 'Đang tổng hợp dữ liệu RAG & tối ưu nội dung...',
  },
  en: {
    title: 'AI SEO Agent',
    badge: 'PRO',
    subtitle: 'Autonomous Multi-Agent & RAG System for Advanced SEO',
    placeholder: "Enter request (e.g. 'Write a search-optimized article about Dat Phat...')",
    btnSubmit: 'Run Agent',
    btnRunning: 'Analyzing...',
    panelManager: 'Agent Manager (Real-time)',
    waiting: 'System is waiting for your command...',
    panelResult: 'Publishing Results',
    resultPlaceholder: 'Generated article will appear here',
    loadingState: 'Synthesizing RAG data & optimizing content...',
  }
};

function App() {
  const [input, setInput] = useState('');
  const [loading, setLoading] = useState(false);
  const [logs, setLogs] = useState([]);
  const [result, setResult] = useState(null);
  const [lang, setLang] = useState(() => localStorage.getItem('seo_agent_lang') || 'vi');
  const logsEndRef = useRef(null);
  const connectionRef = useRef(null);

  const t = translations[lang];

  const handleLangChange = (newLang) => {
    setLang(newLang);
    localStorage.setItem('seo_agent_lang', newLang);
  };

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
        connectionId: connectionRef.current?.connectionId || '',
        language: lang
      });
      
      const { finalArticle, densityResult, postResult } = response.data;
      setResult({ finalArticle, densityResult, postResult });
      
    } catch (error) {
      const errorMsg = error.response?.data?.error || error.message;
      setLogs(prev => [...prev, `[SYSTEM ERROR] ${errorMsg}`]);
    } finally {
      setLoading(false);
    }
  };

  const getLogClass = (log) => {
    if (log.includes('[RAG Plugin]') || log.includes('[Seo Plugin]') || log.includes('[Agentic Pipeline]')) return 'highlight';
    if (log.includes('thành công') || log.includes('successfully') || log.includes('[v]') || log.includes('[HOÀN TẤT]') || log.includes('[COMPLETED]')) return 'success';
    if (log.includes('[LỖI]') || log.includes('[SYSTEM ERROR]') || log.includes('[ERROR]')) return 'error';
    return '';
  };

  // Simple Markdown parser to make headers bold and style them in the article box
  const renderArticle = (text) => {
    if (!text) return null;
    return text.split('\n').map((line, i) => {
      if (line.startsWith('### ')) return <h3 key={i}>{line.replace('### ', '')}</h3>;
      if (line.startsWith('## ')) return <h2 key={i}>{line.replace('## ', '')}</h2>;
      if (line.startsWith('# ')) return <h1 key={i}>{line.replace('# ', '')}</h1>;
      if (line.startsWith('- ') || line.startsWith('* ')) return <li key={i} style={{marginLeft: '20px', marginBottom: '8px'}}>{line.substring(2)}</li>;
      if (line.trim() === '') return <br key={i} />;
      return <p key={i} style={{marginBottom: '10px'}}>{line}</p>;
    });
  };

  return (
    <>
      {/* Animated Background */}
      <div className="bg-orbs">
        <div className="orb orb-1"></div>
        <div className="orb orb-2"></div>
        <div className="orb orb-3"></div>
      </div>

      <div className="app-container">
        <header>
          <div className="lang-switcher">
            <button 
              className={`lang-btn ${lang === 'vi' ? 'active' : ''}`} 
              onClick={() => handleLangChange('vi')}
            >
              VI
            </button>
            <button 
              className={`lang-btn ${lang === 'en' ? 'active' : ''}`} 
              onClick={() => handleLangChange('en')}
            >
              EN
            </button>
          </div>
          <h1>{t.title} <span className="badge">{t.badge}</span></h1>
          <p className="subtitle">{t.subtitle}</p>
        </header>

        <form className="input-section" onSubmit={handleRunAgent}>
          <input
            type="text"
            className="cyber-input"
            placeholder={t.placeholder}
            value={input}
            onChange={(e) => setInput(e.target.value)}
            disabled={loading}
          />
          <button className="cyber-btn" type="submit" disabled={loading || !input.trim()}>
            {loading ? <Loader2 className="spinner" size={20} /> : <Sparkles size={20} />}
            {loading ? t.btnRunning : t.btnSubmit}
          </button>
        </form>

        <div className="main-content">
          <div className="glass-panel">
            <h2 className="panel-header cyan">
              <Cpu size={24} /> {t.panelManager}
            </h2>
            <div className="log-container">
              {logs.length === 0 && !loading && (
                <div style={{ color: 'rgba(148, 163, 184, 0.5)', display: 'flex', flexDirection: 'column', alignItems: 'center', justifyContent: 'center', height: '100%', gap: '1rem' }}>
                  <Send size={32} opacity={0.5} />
                  <span>{t.waiting}</span>
                </div>
              )}
              <AnimatePresence>
                {logs.map((log, index) => (
                  <motion.div 
                    key={index}
                    initial={{ opacity: 0, x: -20 }}
                    animate={{ opacity: 1, x: 0 }}
                    transition={{ duration: 0.3 }}
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
            <h2 className="panel-header success">
              <FileText size={24} /> {t.panelResult}
            </h2>
            
            {!result && !loading && (
              <div style={{ color: 'rgba(148, 163, 184, 0.4)', display: 'flex', flexDirection: 'column', alignItems: 'center', justifyContent: 'center', height: '400px' }}>
                <FileText size={64} style={{ marginBottom: '1.5rem', opacity: 0.5 }} />
                <p style={{ fontSize: '1.1rem' }}>{t.resultPlaceholder}</p>
              </div>
            )}

            {loading && (
              <motion.div 
                initial={{ opacity: 0 }}
                animate={{ opacity: 1 }}
                className="loading-state"
              >
                <div className="pulse-ring">
                  <Cpu className="spinner" size={32} />
                </div>
                <p style={{ fontSize: '1.1rem', letterSpacing: '0.5px' }}>{t.loadingState}</p>
              </motion.div>
            )}

            {result && (
              <motion.div 
                initial={{ opacity: 0, y: 20 }}
                animate={{ opacity: 1, y: 0 }}
                transition={{ duration: 0.5 }}
                style={{ display: 'flex', flexDirection: 'column', height: '100%' }}
              >
                <div className="stats-grid">
                  {result.densityResult && (
                    <div className="stat-card">
                      <CheckCircle size={18} />
                      <span>{result.densityResult}</span>
                    </div>
                  )}
                  {result.postResult && (
                    <div className="stat-card success">
                      <CheckCircle size={18} />
                      <span>{result.postResult}</span>
                    </div>
                  )}
                </div>
                
                <div className="article-box">
                  {renderArticle(result.finalArticle)}
                </div>
              </motion.div>
            )}
          </div>
        </div>
      </div>
    </>
  );
}

export default App;
