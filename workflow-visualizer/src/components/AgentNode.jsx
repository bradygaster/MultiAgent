import React, { memo, useState } from 'react';
import { Handle, Position } from 'reactflow';

const AgentNode = ({ data }) => {
  const [expanded, setExpanded] = useState(false);

  return (
    <div
      style={{
        background: 'white',
        border: `3px solid ${data.color}`,
        borderRadius: '16px',
        padding: '16px',
        minWidth: '300px',
        maxWidth: '400px',
        boxShadow: '0 10px 15px -3px rgba(0, 0, 0, 0.1)',
        cursor: 'pointer',
        transition: 'all 0.2s ease',
      }}
      onClick={() => setExpanded(!expanded)}
    >
      <Handle type="target" position={Position.Top} style={{ background: data.color }} />
      
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
              marginBottom: '4px'
            }}>
              {data.name}
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
            {data.tools.map((tool, idx) => (
              <div
                key={idx}
                style={{
                  padding: '8px',
                  background: '#f9fafb',
                  borderRadius: '6px',
                  fontSize: '12px',
                  borderLeft: `3px solid ${data.color}`,
                }}
              >
                <div style={{ fontWeight: 'bold', color: '#1f2937', fontFamily: 'monospace' }}>
                  {tool.name}()
                </div>
                <div style={{ color: '#6b7280', marginTop: '2px' }}>
                  {tool.desc}
                </div>
              </div>
            ))}
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
      
      <Handle type="source" position={Position.Bottom} style={{ background: data.color }} />
    </div>
  );
};

export default memo(AgentNode);
