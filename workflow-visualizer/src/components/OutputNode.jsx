import React from 'react';
import { Handle, Position } from 'reactflow';

const OutputNode = ({ data }) => {
  return (
    <div className="node-container node-container--output">
      {/* Multiple handles allow connections from any direction */}
      <Handle type="target" position={Position.Top} className="node-handle" />
      <Handle type="target" position={Position.Bottom} className="node-handle" />
      <Handle type="target" position={Position.Left} className="node-handle" />
      <Handle type="target" position={Position.Right} className="node-handle" />
      
      <div className="node-label text-center">
        {data.label}
      </div>
    </div>
  );
};

export default OutputNode;
