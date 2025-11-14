import React from 'react';
import { Handle, Position } from 'reactflow';

const OutputNode = ({ data }) => {
  return (
    <div
      style={{
        background: '#22c55e',
        color: 'white',
        border: '2px solid #16a34a',
        borderRadius: '12px',
        padding: '20px',
        fontSize: '16px',
        fontWeight: 'bold',
        minWidth: '200px',
      }}
    >
      <Handle type="target" position={Position.Bottom} style={{ background: '#16a34a' }} />
      
      <div style={{ textAlign: 'center' }}>
        {data.label}
      </div>
    </div>
  );
};

export default OutputNode;
