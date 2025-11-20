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
      style={{ border: `3px solid ${data.color}`, '--agent-color': data.color }}
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

      <div className="agent-header">
        <div className="flex gap-10 mb-8 agent-header__row">
          <span className="node-emoji">{data.emoji}</span>
          <div className="agent-header__main">
            <div className="node-title">{data.name}</div>
            <div className="agent-header__id">{data.agentId}</div>
          </div>
        </div>
        <div className="node-domain">{data.domain}</div>
        {/* Show current order ID if actively processing */}
        {data.currentOrderId && (
          <div className="node-order node-order--active">
            <span>📋</span>
            Processing: {data.currentOrderId}
          </div>
        )}
      </div>

      {expanded && (
        <div className="tool-section" style={{ borderTop: `2px solid ${data.color}20` }}>
          <div className="tool-header flex gap-6 mb-8" style={{ color: data.color }}>
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
                  <div className={`tool-label ${isRecentlyCalled ? 'tool-label--recent' : ''}`}>
                    <div className="flex gap-6 tool-label__row">
                      {isRecentlyCalled && (
                        <span className="pulse tool-label__pulse">⚡</span>
                      )}
                      {tool.name}()
                    </div>
                    {callCount > 0 && (
                      <span className={`tool-count-badge${isRecentlyCalled ? ' tool-count-badge--recent' : ''}`}>{callCount}</span>
                    )}
                  </div>
                  <div className="tool-desc">{tool.desc}</div>
                  {isRecentlyCalled && (
                    <div className="shimmer tool-item__shimmer" style={{ background: `linear-gradient(90deg, transparent, ${data.color}, transparent)` }} />
                  )}
                </div>
              );
            })}
          </div>
        </div>
      )}

      {!expanded && (
        <div className="text-center agent-tools-collapsed">
          Click to see {data.tools.length} tools
        </div>
      )}
    </div>
  );
};

export default AgentNode;
