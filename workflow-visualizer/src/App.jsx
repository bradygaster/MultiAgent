import React, { useCallback, useEffect } from 'react';
import ReactFlow, {
  MiniMap,
  Controls,
  Background,
  useNodesState,
  useEdgesState,
  addEdge,
  BackgroundVariant,
} from 'reactflow';
import 'reactflow/dist/style.css';
import AgentNode from './components/AgentNode';
import { useOrderStatus } from './hooks/useOrderStatus';
import './App.css';

const nodeTypes = {
  agentNode: AgentNode,
};

// Define the workflow nodes based on the MultiAgent application
const initialNodes = [
  {
    id: 'order',
    type: 'input',
    data: { 
      label: '🛎️ Customer Order',
      description: 'Order received from customer',
      examples: [
        '1 cheeseburger with fries and a chocolate milkshake',
        '2 cheeseburgers, 2 orders of fries, and 2 chocolate milkshakes'
      ]
    },
    position: { x: 250, y: 50 },
    style: {
      background: '#6366f1',
      color: 'white',
      border: '2px solid #4f46e5',
      borderRadius: '12px',
      padding: '20px',
      fontSize: '16px',
      fontWeight: 'bold',
      width: 300,
    },
  },
  {
    id: 'grill-agent',
    type: 'agentNode',
    data: {
      label: '🥩 Grill Agent',
      agentId: 'grill',
      name: 'Grilling Expert',
      domain: 'Grilling meats and produce on the grill or griddle',
      tools: [
        { name: 'cook_patty', desc: 'Grill burger patties' },
        { name: 'melt_cheese', desc: 'Add and melt cheese' },
        { name: 'add_bacon', desc: 'Add bacon to burger' },
        { name: 'toast_bun', desc: 'Toast burger buns' },
        { name: 'assemble_burger', desc: 'Assemble complete burger' },
      ],
      emoji: '🥩',
      color: '#dc2626',
    },
    position: { x: 250, y: 200 },
  },
  {
    id: 'fryer-agent',
    type: 'agentNode',
    data: {
      label: '🍟 Fryer Agent',
      agentId: 'fryer',
      name: 'Fryer Expert',
      domain: 'Cooking anything in the fryer',
      tools: [
        { name: 'fry_fries', desc: 'Fry regular fries' },
        { name: 'fry_waffle_fries', desc: 'Fry waffle fries' },
        { name: 'fry_sweet_potato_fries', desc: 'Fry sweet potato fries' },
        { name: 'fry_onion_rings', desc: 'Fry onion rings' },
        { name: 'add_salt', desc: 'Add salt to fries' },
        { name: 'bag_fries_for_order', desc: 'Bag completed fries' },
      ],
      emoji: '🍟',
      color: '#f59e0b',
    },
    position: { x: 250, y: 400 },
  },
  {
    id: 'dessert-agent',
    type: 'agentNode',
    data: {
      label: '🍨 Dessert Agent',
      agentId: 'desserts',
      name: 'Dessert Expert',
      domain: 'Making and baking desserts',
      tools: [
        { name: 'make_shake', desc: 'Make milkshakes' },
        { name: 'make_sundae', desc: 'Make ice cream sundaes' },
        { name: 'add_whipped_cream', desc: 'Add whipped cream topping' },
      ],
      emoji: '🍨',
      color: '#ec4899',
    },
    position: { x: 250, y: 600 },
  },
  {
    id: 'plating-agent',
    type: 'agentNode',
    data: {
      label: '🎁 Plating Agent',
      agentId: 'expo',
      name: 'Plating Expert',
      domain: 'Final meal assembly and presentation prep',
      tools: [
        { name: 'plate_meal', desc: 'Plate complete order for dine-in' },
        { name: 'package_for_takeout', desc: 'Package order for takeout' },
      ],
      emoji: '🎁',
      color: '#10b981',
    },
    position: { x: 250, y: 800 },
  },
  {
    id: 'output',
    type: 'output',
    data: { 
      label: '🍽️ Order Complete',
      description: 'Hand order to customer or delivery service'
    },
    position: { x: 250, y: 1000 },
    style: {
      background: '#22c55e',
      color: 'white',
      border: '2px solid #16a34a',
      borderRadius: '12px',
      padding: '20px',
      fontSize: '16px',
      fontWeight: 'bold',
      width: 300,
    },
  },
];

// Define the edges (connections between agents)
const initialEdges = [
  {
    id: 'e-order-grill',
    source: 'order',
    target: 'grill-agent',
    animated: true,
    style: { stroke: '#6366f1', strokeWidth: 3 },
    label: 'Process Order',
    labelStyle: { fill: '#6366f1', fontWeight: 700 },
  },
  {
    id: 'e-grill-fryer',
    source: 'grill-agent',
    target: 'fryer-agent',
    animated: true,
    style: { stroke: '#dc2626', strokeWidth: 3 },
    label: 'Burgers Ready',
    labelStyle: { fill: '#dc2626', fontWeight: 700 },
  },
  {
    id: 'e-fryer-dessert',
    source: 'fryer-agent',
    target: 'dessert-agent',
    animated: true,
    style: { stroke: '#f59e0b', strokeWidth: 3 },
    label: 'Fries Ready',
    labelStyle: { fill: '#f59e0b', fontWeight: 700 },
  },
  {
    id: 'e-dessert-plating',
    source: 'dessert-agent',
    target: 'plating-agent',
    animated: true,
    style: { stroke: '#ec4899', strokeWidth: 3 },
    label: 'Desserts Ready',
    labelStyle: { fill: '#ec4899', fontWeight: 700 },
  },
  {
    id: 'e-plating-output',
    source: 'plating-agent',
    target: 'output',
    animated: true,
    style: { stroke: '#10b981', strokeWidth: 3 },
    label: 'Plated/Packaged',
    labelStyle: { fill: '#10b981', fontWeight: 700 },
  },
];

function App() {
  const [nodes, setNodes, onNodesChange] = useNodesState(initialNodes);
  const [edges, setEdges, onEdgesChange] = useEdgesState(initialEdges);
  const { orderEvents, activeOrders, isConnected, clearCompletedOrders } = useOrderStatus();

  const onConnect = useCallback(
    (params) => setEdges((eds) => addEdge(params, eds)),
    [setEdges]
  );

  // Update node styling based on active orders
  useEffect(() => {
    setNodes(currentNodes => 
      currentNodes.map(node => {
        if (node.type === 'agentNode') {
          const activeOrdersInNode = Object.values(activeOrders).filter(
            order => order.currentAgent === node.data.agentId && !order.isComplete
          );

          return {
            ...node,
            style: {
              ...node.style,
              boxShadow: activeOrdersInNode.length > 0 
                ? `0 0 30px ${node.data.color}, 0 0 60px ${node.data.color}` 
                : undefined,
              transform: activeOrdersInNode.length > 0 
                ? 'scale(1.05)' 
                : 'scale(1)',
              transition: 'all 0.3s ease'
            },
            data: {
              ...node.data,
              activeOrders: activeOrdersInNode.length
            }
          };
        }
        return node;
      })
    );
  }, [activeOrders, setNodes]);

  return (
    <div style={{ width: '100vw', height: '100vh' }}>
      {/* Connection Status Indicator */}
      <div style={{
        position: 'absolute',
        top: 10,
        right: 10,
        zIndex: 1001,
        padding: '8px 16px',
        background: isConnected ? '#10b981' : '#ef4444',
        color: 'white',
        borderRadius: '8px',
        fontSize: '14px',
        fontWeight: 'bold',
        boxShadow: '0 4px 6px rgba(0, 0, 0, 0.1)',
        display: 'flex',
        alignItems: 'center',
        gap: '8px'
      }}>
        <span style={{ 
          width: '8px', 
          height: '8px', 
          borderRadius: '50%', 
          background: 'white',
          animation: isConnected ? 'pulse 2s infinite' : 'none'
        }} />
        {isConnected ? 'Live' : 'Disconnected'}
      </div>

      {/* Info Panel */}
      <div style={{
        position: 'absolute',
        top: 20,
        left: 20,
        zIndex: 1000,
        background: 'white',
        padding: '20px',
        borderRadius: '12px',
        boxShadow: '0 4px 6px rgba(0, 0, 0, 0.1)',
        maxWidth: '350px'
      }}>
        <h1 style={{ margin: 0, fontSize: '24px', color: '#1f2937', marginBottom: '8px' }}>
          🏪 MultiAgent Restaurant Workflow
        </h1>
        <p style={{ margin: 0, fontSize: '14px', color: '#6b7280', lineHeight: '1.5' }}>
          This diagram visualizes the sequential agent workflow for processing restaurant orders in real-time.
          Watch as orders flow through each station!
        </p>
        <div style={{ marginTop: '12px', padding: '10px', background: '#f3f4f6', borderRadius: '8px' }}>
          <strong style={{ fontSize: '12px', color: '#374151' }}>Live Stats:</strong>
          <div style={{ margin: '8px 0 0 0', fontSize: '12px', color: '#6b7280' }}>
            <div>🔴 Active Orders: <strong>{Object.values(activeOrders).filter(o => !o.isComplete).length}</strong></div>
            <div>✅ Completed: <strong>{Object.values(activeOrders).filter(o => o.isComplete).length}</strong></div>
            <div>📊 Total Events: <strong>{orderEvents.length}</strong></div>
          </div>
        </div>
      </div>

      {/* Active Orders Panel */}
      <div style={{
        position: 'absolute',
        bottom: 20,
        right: 20,
        zIndex: 1000,
        background: 'white',
        padding: '16px',
        borderRadius: '12px',
        boxShadow: '0 4px 6px rgba(0, 0, 0, 0.1)',
        maxWidth: '400px',
        maxHeight: '400px',
        overflow: 'auto'
      }}>
        <div style={{ 
          display: 'flex', 
          justifyContent: 'space-between', 
          alignItems: 'center',
          marginBottom: '12px' 
        }}>
          <h3 style={{ margin: 0, fontSize: '16px', fontWeight: 'bold' }}>
            Recent Orders ({Object.keys(activeOrders).length})
          </h3>
          {Object.values(activeOrders).some(o => o.isComplete) && (
            <button
              onClick={clearCompletedOrders}
              style={{
                padding: '4px 8px',
                fontSize: '11px',
                background: '#ef4444',
                color: 'white',
                border: 'none',
                borderRadius: '4px',
                cursor: 'pointer'
              }}
            >
              Clear Completed
            </button>
          )}
        </div>
        {Object.keys(activeOrders).length === 0 && (
          <div style={{ 
            textAlign: 'center', 
            color: '#9ca3af', 
            padding: '20px',
            fontSize: '14px'
          }}>
            Waiting for orders...
          </div>
        )}
        {Object.entries(activeOrders)
          .sort((a, b) => new Date(b[1].lastUpdate) - new Date(a[1].lastUpdate))
          .map(([orderId, order]) => (
          <div key={orderId} style={{
            padding: '12px',
            marginBottom: '8px',
            background: order.isComplete ? '#f0fdf4' : '#fef3c7',
            border: `2px solid ${order.isComplete ? '#10b981' : '#f59e0b'}`,
            borderRadius: '8px',
            fontSize: '12px'
          }}>
            <div style={{ 
              display: 'flex', 
              justifyContent: 'space-between',
              alignItems: 'center',
              marginBottom: '6px'
            }}>
              <strong style={{ fontSize: '13px' }}>Order #{orderId}</strong>
              {order.isComplete && <span style={{ color: '#10b981', fontSize: '16px' }}>✓</span>}
            </div>
            <div style={{ color: '#374151', marginBottom: '4px' }}>
              {order.isComplete ? '🍽️ Complete' : `🔄 ${order.currentAgentName}`}
            </div>
            <div style={{ color: '#6b7280', fontSize: '11px' }}>
              {new Date(order.lastUpdate).toLocaleTimeString()}
            </div>
            <div style={{ 
              marginTop: '6px', 
              fontSize: '11px',
              color: '#6b7280'
            }}>
              {order.events.length} events
            </div>
          </div>
        ))}
      </div>
      
      <ReactFlow
        nodes={nodes}
        edges={edges}
        onNodesChange={onNodesChange}
        onEdgesChange={onEdgesChange}
        onConnect={onConnect}
        nodeTypes={nodeTypes}
        fitView
        attributionPosition="bottom-left"
      >
        <Controls />
        <MiniMap 
          nodeColor={(node) => {
            if (node.type === 'input') return '#6366f1';
            if (node.type === 'output') return '#22c55e';
            return node.data.color || '#94a3b8';
          }}
          maskColor="rgba(0, 0, 0, 0.1)"
        />
        <Background variant={BackgroundVariant.Dots} gap={12} size={1} />
      </ReactFlow>
    </div>
  );
}

export default App;
