import React, { useState, useEffect } from 'react';
import { Handle, Position } from 'reactflow';

const AgentNode = ({ data }) => {
  const [expanded, setExpanded] = useState(true);
  const [recentTools, setRecentTools] = useState([]);
  const isActive = data.isActive || false;
  const globalToolCounts = data.globalToolCounts || {};

  const handleClick = (e) => {
    e.stopPropagation();
    setExpanded(!expanded);
  };

  // Track recently called tools for animation
  useEffect(() => {    
    if (data.lastToolCalled && data.lastToolTimestamp) {      
      setRecentTools(prev => {
        // Avoid duplicates based on timestamp
        if (prev.some(t => t.timestamp === data.lastToolTimestamp)) {
          return prev;
        }
        const updated = [
          { name: data.lastToolCalled, timestamp: data.lastToolTimestamp },
          ...prev.slice(0, 2)
        ];
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
      className={`node-container node-container--agent ${isActive ? 'node-container--active' : 'node-container--inactive'}`}
      style={{
        border: `3px solid ${data.color}`,
        '--agent-color': data.color
      }}
      onClick={handleClick}
    >
      {/* Handles on all sides with IDs for custom connections */}
      <Handle type="target" position={Position.Top} id="top" className="node-handle" />
      <Handle type="target" position={Position.Left} id="left" className="node-handle" />
      <Handle type="target" position={Position.Bottom} id="bottom" className="node-handle" />
      <Handle type="target" position={Position.Right} id="right" className="node-handle" />

      <Handle type="source" position={Position.Top} id="top-source" className="node-handle" />
      <Handle type="source" position={Position.Left} id="left-source" className="node-handle" />
      <Handle type="source" position={Position.Bottom} id="bottom-source" className="node-handle" />
      <Handle type="source" position={Position.Right} id="right-source" className="node-handle" />
      
      <div style={{ marginBottom: '12px' }}>
        <div className="flex gap-10 mb-8" style={{ alignItems: 'center' }}>
          <span className="node-emoji">{data.emoji}</span>
          <div style={{ flex: 1 }}>
            <div className="node-title">
              {data.name}
            </div>
            <div style={{ fontSize: '12px', color: '#6b7280', fontStyle: 'italic' }}>
              {data.agentId}
            </div>
          </div>
        </div>
        <div className="node-domain">
          {data.domain}
        </div>
        {/* Show current order ID if actively processing */}
        {data.currentOrderId && (
          <div className="node-order node-order--active">
            <span>📋</span>
            Processing: {data.currentOrderId}
          </div>
        )}
      </div>

      {expanded && (
        <div style={{ borderTop: `2px solid ${data.color}20`, paddingTop: '12px', marginTop: '12px' }}>
          <div className="tool-header flex gap-6 mb-8" style={{ color: data.color, alignItems: 'center' }}>
            <span>🛠️</span>
            Available Tools ({data.tools.length})
          </div>
          <div className="tool-list">
            {data.tools.map((tool, idx) => {
              const isRecentlyCalled = recentTools.some(t => t.name === tool.name);
              const callCount = globalToolCounts[`${data.name}:${tool.name}`] || 0;
              let toolClass = 'tool-item';
              if (isRecentlyCalled) toolClass += ' tool-item--recent';
              else if (callCount > 0) toolClass += ' tool-item--called';
              return (
                <div key={idx} className={toolClass}>
                  <div className="tool-label" style={{ color: isRecentlyCalled ? data.color : undefined, display: 'flex', alignItems: 'center', gap: '8px', justifyContent: 'space-between' }}>
                    <div className="flex gap-6" style={{ alignItems: 'center' }}>
                      {isRecentlyCalled && (
                        <span className="pulse" style={{ fontSize: '14px' }}>⚡</span>
                      )}
                      {tool.name}()
                    </div>
                    {callCount > 0 && (
                      <span className="tool-count-badge" style={{ background: isRecentlyCalled ? data.color : undefined }}>
                        {callCount}
                      </span>
                    )}
                  </div>
                  <div style={{ color: '#6b7280', marginTop: '4px', fontSize: '11px' }}>
                    {tool.desc}
                  </div>
                  {isRecentlyCalled && (
                    <div className="shimmer" style={{ position: 'absolute', top: 0, left: 0, right: 0, height: '2px', background: `linear-gradient(90deg, transparent, ${data.color}, transparent)` }} />
                  )}
                </div>
              );
            })}
          </div>
        </div>
      )}

      {!expanded && (
        <div className="text-center" style={{ fontSize: '11px', color: '#9ca3af', marginTop: '8px', fontStyle: 'italic' }}>
          Click to see {data.tools.length} tools
        </div>
      )}
    </div>
  );
};

export default AgentNode;
