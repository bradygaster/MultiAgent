import React, { useState, useEffect } from 'react';
import { Handle, Position } from 'reactflow';

const AgentNode = ({ data }) => {
  const [expanded, setExpanded] = useState(true);
  const [recentTools, setRecentTools] = useState([]);
  const isActive = data.isActive || false;
  const globalToolCounts = data.globalToolCounts || {};

  const handleClick = (e) => {
    e.stopPropagation();
    console.log(`🖱️ Clicked ${data.name}, current expanded:`, expanded, 'will toggle to:', !expanded);
    setExpanded(!expanded);
  };

  // Track recently called tools for animation
  useEffect(() => {
    console.log(`🔍 AgentNode ${data.name} - lastToolCalled changed:`, data.lastToolCalled, 'timestamp:', data.lastToolTimestamp);
    
    if (data.lastToolCalled && data.lastToolTimestamp) {
      console.log(`⚡ Tool highlight triggered for ${data.name}:`, data.lastToolCalled);
      
      setRecentTools(prev => {
        // Avoid duplicates based on timestamp
        if (prev.some(t => t.timestamp === data.lastToolTimestamp)) {
          console.log(`   ⏭️  Skipping duplicate timestamp for ${data.name}`);
          return prev;
        }
        const updated = [
          { name: data.lastToolCalled, timestamp: data.lastToolTimestamp },
          ...prev.slice(0, 2)
        ];
        console.log(`   ✅ Updated recentTools for ${data.name}:`, updated);
        return updated;
      });
    }
  }, [data.lastToolCalled, data.lastToolTimestamp, data.name]);

  // Clear old highlights
  useEffect(() => {
    if (recentTools.length === 0) return;
    
    const timer = setInterval(() => {
      const now = Date.now();
      setRecentTools(prev => {
        const filtered = prev.filter(t => {
          const age = now - new Date(t.timestamp).getTime();
          return age < 3000; // Keep for 3 seconds
        });
        return filtered.length !== prev.length ? filtered : prev;
      });
    }, 500);
    
    return () => clearInterval(timer);
  }, [recentTools]);

  return (
    <div
      style={{
        background: 'white',
        border: `3px solid ${data.color}`,
        borderRadius: '16px',
        padding: '16px',
        minWidth: '300px',
        maxWidth: '400px',
        boxShadow: isActive 
          ? `0 0 15px ${data.color}80, 0 0 30px ${data.color}40, 0 10px 15px -3px rgba(0, 0, 0, 0.1)`
          : '0 10px 15px -3px rgba(0, 0, 0, 0.1)',
        cursor: 'pointer',
        transition: 'all 0.3s ease',
        transform: isActive ? 'scale(1.05)' : 'scale(1)',
      }}
      onClick={handleClick}
    >
      {/* Handles on all sides with IDs for custom connections */}
      <Handle type="target" position={Position.Top} id="top" style={{ background: data.color }} />
      <Handle type="target" position={Position.Left} id="left" style={{ background: data.color }} />
      <Handle type="target" position={Position.Bottom} id="bottom" style={{ background: data.color }} />
      <Handle type="target" position={Position.Right} id="right" style={{ background: data.color }} />
      
      <Handle type="source" position={Position.Top} id="top-source" style={{ background: data.color }} />
      <Handle type="source" position={Position.Left} id="left-source" style={{ background: data.color }} />
      <Handle type="source" position={Position.Bottom} id="bottom-source" style={{ background: data.color }} />
      <Handle type="source" position={Position.Right} id="right-source" style={{ background: data.color }} />
      
      <div style={{ marginBottom: '12px' }}>
        <div style={{ 
          display: 'flex', 
          alignItems: 'center', 
          gap: '10px',
          marginBottom: '8px'
        }}>
          <span style={{ fontSize: '32px' }}>{data.emoji}</span>
          <div style={{ flex: 1 }}>
            <div style={{ 
              fontSize: '18px', 
              fontWeight: 'bold', 
              color: '#1f2937',
              marginBottom: '4px',
              display: 'flex',
              alignItems: 'center',
              gap: '8px'
            }}>
              {data.name}
              {data.activeOrders > 0 && (
                <span style={{
                  background: data.color,
                  color: 'white',
                  fontSize: '11px',
                  padding: '2px 8px',
                  borderRadius: '12px',
                  fontWeight: 'bold'
                }}>
                  {data.activeOrders}
                </span>
              )}
            </div>
            <div style={{ 
              fontSize: '12px', 
              color: '#6b7280',
              fontStyle: 'italic'
            }}>
              {data.agentId}
            </div>
          </div>
        </div>
        
        <div style={{
          padding: '8px 12px',
          background: `${data.color}15`,
          borderRadius: '8px',
          fontSize: '13px',
          color: '#374151',
          lineHeight: '1.5',
        }}>
          {data.domain}
        </div>
        
        {/* Show current order ID if actively processing */}
        {data.currentOrderId && (
          <div style={{
            marginTop: '8px',
            padding: '6px 10px',
            background: `${data.color}25`,
            borderRadius: '6px',
            border: `1px solid ${data.color}50`,
            fontSize: '11px',
            fontWeight: 'bold',
            color: data.color,
            display: 'flex',
            alignItems: 'center',
            gap: '6px'
          }}>
            <span>📋</span>
            Processing: {data.currentOrderId}
          </div>
        )}
      </div>

      {expanded && (
        <div style={{
          borderTop: `2px solid ${data.color}20`,
          paddingTop: '12px',
          marginTop: '12px',
        }}>
          <div style={{ 
            fontSize: '14px', 
            fontWeight: 'bold', 
            color: data.color,
            marginBottom: '8px',
            display: 'flex',
            alignItems: 'center',
            gap: '6px'
          }}>
            <span>🛠️</span>
            Available Tools ({data.tools.length})
          </div>
          <div style={{ display: 'flex', flexDirection: 'column', gap: '6px' }}>
            {data.tools.map((tool, idx) => {
              const isRecentlyCalled = recentTools.some(t => t.name === tool.name);
              const callCount = globalToolCounts[`${data.name}:${tool.name}`] || 0;
              
              if (idx === 0 && recentTools.length > 0) {
                console.log(`🔧 ${data.name} - Checking tool "${tool.name}" against recent:`, recentTools.map(t => t.name), 'Match:', isRecentlyCalled);
              }
              
              return (
                <div
                  key={idx}
                  style={{
                    padding: '10px',
                    background: isRecentlyCalled 
                      ? `linear-gradient(135deg, ${data.color}40 0%, ${data.color}20 100%)`
                      : callCount > 0 
                        ? '#f3f4f6'
                        : '#f9fafb',
                    borderRadius: '8px',
                    fontSize: '12px',
                    borderLeft: `4px solid ${isRecentlyCalled ? data.color : callCount > 0 ? '#9ca3af' : '#e5e7eb'}`,
                    transition: 'all 0.2s ease',
                    transform: isRecentlyCalled ? 'scale(1.02)' : 'scale(1)',
                    boxShadow: isRecentlyCalled 
                      ? `0 4px 12px ${data.color}60, 0 0 20px ${data.color}40` 
                      : callCount > 0
                        ? '0 1px 3px rgba(0, 0, 0, 0.1)'
                        : 'none',
                    position: 'relative',
                    border: isRecentlyCalled ? `2px solid ${data.color}` : '1px solid transparent'
                  }}
                >
                  <div style={{ 
                    fontWeight: 'bold', 
                    color: isRecentlyCalled ? data.color : '#1f2937', 
                    fontFamily: 'monospace',
                    display: 'flex',
                    alignItems: 'center',
                    gap: '8px',
                    justifyContent: 'space-between'
                  }}>
                    <div style={{ display: 'flex', alignItems: 'center', gap: '6px' }}>
                      {isRecentlyCalled && (
                        <span style={{ 
                          fontSize: '14px',
                          animation: 'pulse 0.5s ease-in-out infinite'
                        }}>⚡</span>
                      )}
                      {tool.name}()
                    </div>
                    {callCount > 0 && (
                      <span style={{
                        background: isRecentlyCalled ? data.color : '#6b7280',
                        color: 'white',
                        fontSize: '10px',
                        fontWeight: 'bold',
                        padding: '2px 6px',
                        borderRadius: '10px',
                        minWidth: '20px',
                        textAlign: 'center'
                      }}>
                        {callCount}
                      </span>
                    )}
                  </div>
                  <div style={{ color: '#6b7280', marginTop: '4px', fontSize: '11px' }}>
                    {tool.desc}
                  </div>
                  {isRecentlyCalled && (
                    <div style={{
                      position: 'absolute',
                      top: 0,
                      left: 0,
                      right: 0,
                      height: '2px',
                      background: `linear-gradient(90deg, transparent, ${data.color}, transparent)`,
                      animation: 'shimmer 1s ease-in-out infinite'
                    }} />
                  )}
                </div>
              );
            })}
          </div>
        </div>
      )}

      {!expanded && (
        <div style={{
          fontSize: '11px',
          color: '#9ca3af',
          textAlign: 'center',
          marginTop: '8px',
          fontStyle: 'italic'
        }}>
          Click to see {data.tools.length} tools
        </div>
      )}
    </div>
  );
};

export default AgentNode;
