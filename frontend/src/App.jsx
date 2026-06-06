import { useState, useRef, useEffect } from 'react';
import axios from 'axios';
import { HubConnectionBuilder, LogLevel } from '@microsoft/signalr';
import { motion, AnimatePresence } from 'framer-motion';
import { Sparkles, Loader2, Cpu, FileText, CheckCircle, Send, Copy, Code, AlertTriangle } from 'lucide-react';
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
    tabArticle: 'Bài viết',
    tabAudit: 'Kiểm toán SEO',
    tabSchema: 'Thẻ Meta & Schema',
    seoScore: 'Điểm SEO',
    serpPreview: 'Xem trước Google SERP',
    recommendations: 'Khuyến nghị tối ưu',
    copySchema: 'Sao chép JSON-LD',
    copied: 'Đã sao chép!',
    metaTitle: 'Thẻ Tiêu đề (Meta Title)',
    metaDesc: 'Thẻ Mô tả (Meta Description)',
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
    tabArticle: 'Article',
    tabAudit: 'SEO Audit',
    tabSchema: 'Meta & Schema',
    seoScore: 'SEO Score',
    serpPreview: 'Google SERP Preview',
    recommendations: 'Optimizations Checklist',
    copySchema: 'Copy JSON-LD',
    copied: 'Copied!',
    metaTitle: 'Meta Title',
    metaDesc: 'Meta Description',
  }
};

function App() {
  const [input, setInput] = useState('');
  const [loading, setLoading] = useState(false);
  const [logs, setLogs] = useState([]);
  const [result, setResult] = useState(null);
  const [lang, setLang] = useState(() => localStorage.getItem('seo_agent_lang') || 'vi');
  const [activeTab, setActiveTab] = useState('article');
  const [copied, setCopied] = useState(false);
  const logsEndRef = useRef(null);
  const connectionRef = useRef(null);

  const t = translations[lang];

  const handleLangChange = (newLang) => {
    setLang(newLang);
    localStorage.setItem('seo_agent_lang', newLang);
  };

  const handleCopy = (text) => {
    navigator.clipboard.writeText(text);
    setCopied(true);
    setTimeout(() => setCopied(false), 2000);
  };

  const handleMouseMove = (e) => {
    const rect = e.currentTarget.getBoundingClientRect();
    const x = e.clientX - rect.left;
    const y = e.clientY - rect.top;
    e.currentTarget.style.setProperty('--mouse-x', `${x}px`);
    e.currentTarget.style.setProperty('--mouse-y', `${y}px`);
  };

  const getScoreColorClass = (score) => {
    if (score >= 80) return 'green';
    if (score >= 50) return 'yellow';
    return 'red';
  };

  const getScoreMessage = (score) => {
    if (lang === 'vi') {
      if (score >= 80) return 'Tuyệt vời! Bài viết của bạn đã đáp ứng đầy đủ các tiêu chuẩn SEO cốt lõi.';
      if (score >= 50) return 'Tạm ổn. Bạn nên tối ưu thêm tiêu đề hoặc độ dài để bài viết chuẩn SEO hơn.';
      return 'Cần tối ưu thêm! Bài viết của bạn đang thiếu các từ khóa hoặc cấu trúc cần thiết.';
    } else {
      if (score >= 80) return 'Excellent! Your article meets most core search engine optimization criteria.';
      if (score >= 50) return 'Decent. Consider optimizing headings or length to improve the SEO ranking score.';
      return 'Needs work! Your content lacks crucial keyword density or heading structure.';
    }
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
    setActiveTab('article');

    try {
      const response = await axios.post('http://localhost:5000/api/agent/run', {
        input: input,
        connectionId: connectionRef.current?.connectionId || '',
        language: lang
      });
      
      const { finalArticle, densityResult, postResult, metaAndSchema, seoAudit, googleIndexingResult } = response.data;
      
      let parsedMeta = null;
      let parsedAudit = null;
      try {
        if (metaAndSchema) parsedMeta = JSON.parse(metaAndSchema);
      } catch (e) {
        console.error("Failed to parse metaAndSchema", e);
      }
      try {
        if (seoAudit) parsedAudit = JSON.parse(seoAudit);
      } catch (e) {
        console.error("Failed to parse seoAudit", e);
      }

      setResult({ 
        finalArticle, 
        densityResult, 
        postResult, 
        meta: parsedMeta, 
        audit: parsedAudit,
        googleIndexingResult
      });
      
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

        <form className="input-container" onSubmit={handleRunAgent}>
          <input
            type="text"
            className="flat-input"
            placeholder={t.placeholder}
            value={input}
            onChange={(e) => setInput(e.target.value)}
            disabled={loading}
          />
          <button className="btn-flat btn-flat-primary" type="submit" disabled={loading || !input.trim()}>
            {loading ? <Loader2 className="spinner" size={20} strokeWidth={2.5} /> : <Sparkles size={20} strokeWidth={2.5} />}
            {loading ? t.btnRunning : t.btnSubmit}
          </button>
        </form>

        <div className="main-content">
          <div className="color-block-card card-blue-tint" onMouseMove={handleMouseMove}>
            <h2 className="panel-header cyan">
              <span className="card-icon"><Cpu size={24} strokeWidth={2.5} /></span> {t.panelManager}
            </h2>
            <div className="log-container">
              {logs.length === 0 && !loading && (
                <div style={{ color: 'rgba(148, 163, 184, 0.5)', display: 'flex', flexDirection: 'column', alignItems: 'center', justifyContent: 'center', height: '100%', gap: '1rem' }}>
                  <Send size={32} opacity={0.5} strokeWidth={2} />
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

          <div className="color-block-card card-green-tint result-container" onMouseMove={handleMouseMove}>
            <h2 className="panel-header success">
              <span className="card-icon"><FileText size={24} strokeWidth={2.5} /></span> {t.panelResult}
            </h2>
            
            {!result && !loading && (
              <div style={{ color: 'rgba(148, 163, 184, 0.4)', display: 'flex', flexDirection: 'column', alignItems: 'center', justifyContent: 'center', height: '400px' }}>
                <FileText size={64} style={{ marginBottom: '1.5rem', opacity: 0.5 }} strokeWidth={1.5} />
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
                  <Cpu className="spinner" size={32} strokeWidth={2.5} />
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
                {/* Tabs Selector */}
                <div className="tab-menu">
                  <button 
                    className={`tab-btn ${activeTab === 'article' ? 'active' : ''}`}
                    onClick={() => setActiveTab('article')}
                  >
                    <FileText size={16} strokeWidth={2.5} />
                    <span>{t.tabArticle}</span>
                  </button>
                  <button 
                    className={`tab-btn ${activeTab === 'audit' ? 'active' : ''}`}
                    onClick={() => setActiveTab('audit')}
                  >
                    <CheckCircle size={16} strokeWidth={2.5} />
                    <span>{t.tabAudit}</span>
                  </button>
                  <button 
                    className={`tab-btn ${activeTab === 'schema' ? 'active' : ''}`}
                    onClick={() => setActiveTab('schema')}
                  >
                    <Code size={16} strokeWidth={2.5} />
                    <span>{t.tabSchema}</span>
                  </button>
                </div>

                {/* Tab Content: Article */}
                {activeTab === 'article' && (
                  <div className="tab-pane">
                    <div className="stats-grid">
                      {result.densityResult && (
                        <div className="stat-card">
                          <CheckCircle size={18} strokeWidth={2.5} />
                          <span>{result.densityResult}</span>
                        </div>
                      )}
                      {result.postResult && (
                        <div className="stat-card success">
                          <CheckCircle size={18} strokeWidth={2.5} />
                          <span>{result.postResult}</span>
                        </div>
                      )}
                      {result.googleIndexingResult && (
                        <div className="stat-card indexing">
                          <CheckCircle size={18} strokeWidth={2.5} />
                          <span>{result.googleIndexingResult}</span>
                        </div>
                      )}
                    </div>
                    
                    <div className="article-box">
                      {renderArticle(result.finalArticle)}
                    </div>
                  </div>
                )}

                {/* Tab Content: SEO Audit */}
                {activeTab === 'audit' && (
                  <div className="tab-pane">
                    <div className="score-section">
                      <div className="score-circle-container">
                        <div className={`score-ring ${getScoreColorClass(result.audit?.score || 0)}`}>
                          <span className="score-val">{result.audit?.score || 0}</span>
                          <span className="score-lbl">/100</span>
                        </div>
                        <div className="score-details">
                          <h3>{t.seoScore}</h3>
                          <p>{getScoreMessage(result.audit?.score || 0)}</p>
                        </div>
                      </div>
                    </div>

                    <div className="serp-container">
                      <h3 className="subheading-label">{t.serpPreview}</h3>
                      <div className="serp-preview-card">
                        <span className="serp-url">https://clientwebsite.com/{result.meta?.metaTitle?.toLowerCase().replace(/[^a-z0-9]/g, '-') || 'article-url'}</span>
                        <h4 className="serp-title">{result.meta?.metaTitle || 'Meta Title Placeholder'}</h4>
                        <p className="serp-desc">{result.meta?.metaDescription || 'Meta Description Placeholder'}</p>
                      </div>
                    </div>

                    <div className="recommendations-container">
                      <h3 className="subheading-label">{t.recommendations}</h3>
                      <ul className="recommendations-list">
                        {result.audit?.recommendations?.map((rec, i) => (
                          <li key={i} className={`rec-item ${rec.startsWith('✓') ? 'success' : rec.startsWith('!') ? 'warning' : 'danger'}`}>
                            {rec.startsWith('✓') && <CheckCircle size={16} strokeWidth={2.5} />}
                            {rec.startsWith('!') && <AlertTriangle size={16} strokeWidth={2.5} />}
                            {(!rec.startsWith('✓') && !rec.startsWith('!')) && <AlertTriangle size={16} strokeWidth={2.5} />}
                            <span>{rec.replace(/^[✓!✗]\s*/, '')}</span>
                          </li>
                        ))}
                      </ul>
                    </div>
                  </div>
                )}

                {/* Tab Content: Schema & Meta */}
                {activeTab === 'schema' && (
                  <div className="tab-pane">
                    <div className="meta-info-container">
                      <div className="meta-field">
                        <label>{t.metaTitle}</label>
                        <div className="meta-value-box">{result.meta?.metaTitle || '-'}</div>
                      </div>
                      <div className="meta-field">
                        <label>{t.metaDesc}</label>
                        <div className="meta-value-box">{result.meta?.metaDescription || '-'}</div>
                      </div>
                    </div>

                    <div className="schema-header">
                      <h3 className="subheading-label">JSON-LD Structured Data</h3>
                      <button 
                        className="copy-btn" 
                        onClick={() => handleCopy(JSON.stringify(result.meta?.jsonLd || {}, null, 2))}
                      >
                        <Copy size={16} strokeWidth={2.5} />
                        <span>{copied ? t.copied : t.copySchema}</span>
                      </button>
                    </div>
                    <pre className="schema-box">
                      <code>{JSON.stringify(result.meta?.jsonLd || {}, null, 2)}</code>
                    </pre>
                  </div>
                )}
              </motion.div>
            )}
          </div>
        </div>
      </div>
    </>
  );
}

export default App;
