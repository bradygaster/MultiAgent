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
import OutputNode from './components/OutputNode';
import { useOrderStatus } from './hooks/useOrderStatus';
import './styles/index.css';

const nodeTypes = {
  agentNode: AgentNode,
  outputNode: OutputNode,
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
    position: { x: 50, y: 150 },
    style: {
      background: '#6366f1',
      color: 'white',
      border: '2px solid #4f46e5',
      borderRadius: '12px',
      padding: '20px',
      fontSize: '16px',
      fontWeight: 'bold',
      width: 180,
    },
  },
  {
    id: 'grill-agent',
    type: 'agentNode',
    data: {
      label: '🥩 Grill Agent',
      agentId: 'grill',
      name: 'Grilling Agent',
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
    position: { x: 320, y: 120 },
  },
  {
    id: 'fryer-agent',
    type: 'agentNode',
    data: {
      label: '🍟 Fryer Agent',
      agentId: 'fryer',
      name: 'Fryer Agent',
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
    position: { x: 580, y: 90 },
  },
  {
    id: 'dessert-agent',
    type: 'agentNode',
    data: {
      label: '🍨 Dessert Agent',
      agentId: 'desserts',
      name: 'Dessert Agent',
      domain: 'Making and baking desserts',
      tools: [
        { name: 'make_shake', desc: 'Make milkshakes' },
        { name: 'make_sundae', desc: 'Make ice cream sundaes' },
        { name: 'add_whipped_cream', desc: 'Add whipped cream topping' },
      ],
      emoji: '🍨',
      color: '#ec4899',
    },
    position: { x: 540, y: 485 },
  },
  {
    id: 'plating-agent',
    type: 'agentNode',
    data: {
      label: '🎁 Plating Agent',
      agentId: 'expo',
      name: 'Plating Agent',
      domain: 'Final meal assembly and presentation prep',
      tools: [
        { name: 'plate_meal', desc: 'Plate complete order for dine-in' },
        { name: 'package_for_takeout', desc: 'Package order for takeout' },
      ],
      emoji: '🎁',
      color: '#10b981',
    },
    position: { x: 140, y: 490 },
  },
  {
    id: 'output',
    type: 'outputNode',
    data: { 
      label: '🍽️ Order Complete',
      description: 'Hand order to customer or delivery service'
    },
    position: { x: 50, y: 600 },
  },
];

// Define the edges (connections between agents)
// No sourceHandle/targetHandle specified - ReactFlow will automatically route based on node positions
const initialEdges = [
  {
    id: 'e-order-grill',
    source: 'order',
    target: 'grill-agent',
    animated: false,
    style: { stroke: '#6366f1', strokeWidth: 3 },
    label: 'Process Order',
    labelStyle: { fill: '#6366f1', fontWeight: 700 },
  },
  {
    id: 'e-grill-fryer',
    source: 'grill-agent',
    target: 'fryer-agent',
    animated: false,
    style: { stroke: '#dc2626', strokeWidth: 3 },
    label: 'Burgers Ready',
    labelStyle: { fill: '#dc2626', fontWeight: 700 },
  },
  {
    id: 'e-fryer-dessert',
    source: 'fryer-agent',
    target: 'dessert-agent',
    animated: false,
    style: { stroke: '#f59e0b', strokeWidth: 3 },
    label: 'Fries Ready',
    labelStyle: { fill: '#f59e0b', fontWeight: 700 },
  },
  {
    id: 'e-dessert-plating',
    source: 'dessert-agent',
    target: 'plating-agent',
    animated: false,
    style: { stroke: '#ec4899', strokeWidth: 3 },
    label: 'Desserts Ready',
    labelStyle: { fill: '#ec4899', fontWeight: 700 },
  },
  {
    id: 'e-plating-output',
    source: 'plating-agent',
    target: 'output',
    animated: false,
    style: { stroke: '#10b981', strokeWidth: 3 },
    label: 'Plated/Packaged',
    labelStyle: { fill: '#10b981', fontWeight: 700 },
  },
];

function App() {
  const [nodes, setNodes, onNodesChange] = useNodesState(initialNodes);
  const [edges, setEdges, onEdgesChange] = useEdgesState(initialEdges);
  const { orderEvents, activeOrders, isConnected, clearCompletedOrders } = useOrderStatus();
  const [globalToolCounts, setGlobalToolCounts] = React.useState({});

  // Ensure edges are never animated
  React.useEffect(() => {
    setEdges(edges => edges.map(edge => ({ ...edge, animated: false })));
  }, [setEdges]);

  const onConnect = useCallback(
    (params) => setEdges((eds) => addEdge(params, eds)),
    [setEdges]
  );

  // Update global tool counts whenever a new tool is called
  useEffect(() => {
    const toolEvents = orderEvents.filter(e => e.workflowEventType === 5); // ToolCalled = 5
    const newCounts = {};
    
    toolEvents.forEach(event => {
      const toolName = event.toolCall?.name;
      const agentName = event.agentName;
      if (toolName && agentName) {
        const key = `${agentName}:${toolName}`;
        newCounts[key] = (newCounts[key] || 0) + 1;
      }
    });
    
    setGlobalToolCounts(newCounts);
  }, [orderEvents]);

  // Update node data based on active orders (without affecting position)
  useEffect(() => {
    const completedOrdersCount = Object.values(activeOrders).filter(order => order.isComplete).length;
    
    setNodes(currentNodes => 
      currentNodes.map(node => {
        if (node.type === 'agentNode') {
          // Find orders currently being processed by this agent
          // Match by currentAgentName since currentAgent is a long hash ID
          const activeOrdersForAgent = Object.entries(activeOrders)
            .filter(([orderId, order]) => {
              const nameMatch = order.currentAgentName?.toLowerCase().includes(node.data.name.toLowerCase());
              return nameMatch && !order.isComplete;
            })
            .map(([orderId, order]) => ({ orderId, ...order }));
          
          // Get the most recent order ID being processed by this agent
          const currentOrder = activeOrdersForAgent.length > 0 
            ? activeOrdersForAgent.sort((a, b) => new Date(b.lastUpdate) - new Date(a.lastUpdate))[0]
            : null;
          
          // Match by agentName - workflowEventType is a number (ToolCalled = 5)
          const recentToolEvents = orderEvents
            .filter(e => {
              if (e.workflowEventType !== 5) return false; // ToolCalled = 5
              
              const nameMatch = e.agentName?.toLowerCase().includes(node.data.name.toLowerCase());
              return nameMatch;
            })
            .sort((a, b) => new Date(b.timestamp) - new Date(a.timestamp));
          
          const lastTool = recentToolEvents[0]?.toolCall?.name;

          return {
            ...node,
            data: {
              ...node.data,
              activeOrders: activeOrdersForAgent.length,
              isActive: activeOrdersForAgent.length > 0,
              currentOrderId: currentOrder?.orderId || null,
              lastToolCalled: lastTool,
              lastToolTimestamp: recentToolEvents[0]?.timestamp,
              globalToolCounts: globalToolCounts
            }
          };
        } else if (node.id === 'output') {
          // Update the output node with completed count
          return {
            ...node,
            data: {
              ...node.data,
              label: `🍽️ Order Complete (${completedOrdersCount})`
            }
          };
        }
        return node;
      })
    );
  }, [activeOrders, orderEvents, setNodes, globalToolCounts]);

  return (
    <div className="app-container">
      {/* Connection Status Indicator */}
      <div className={`panel panel--status flex gap-8`} style={{
        position: 'absolute',
        top: 10,
        right: 10,
        zIndex: 1001,
        background: isConnected ? '#10b981' : '#ef4444',
        color: 'white',
        fontWeight: 'bold',
        boxShadow: '0 4px 6px rgba(0, 0, 0, 0.1)'
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
      <div className="panel panel--info" style={{
        position: 'absolute',
        top: 20,
        right: 20,
        zIndex: 1000
      }}>
        <h1 className="heading-lg">🏪 MultiAgent Restaurant Workflow</h1>
        <p className="text-muted" style={{ fontSize: '14px', lineHeight: '1.5', marginBottom: '12px' }}>
          This diagram visualizes the sequential agent workflow for processing restaurant orders in real-time.
          Watch as orders flow through each station!
        </p>
        <div style={{ marginTop: '12px', padding: '10px', background: '#f3f4f6', borderRadius: '8px' }}>
          <strong style={{ fontSize: '12px', color: '#374151' }}>Live Stats:</strong>
          <div className="text-muted" style={{ margin: '8px 0 0 0', fontSize: '12px' }}>
            <div>🔴 Active Orders: <strong>{Object.values(activeOrders).filter(o => !o.isComplete).length}</strong></div>
            <div>✅ Completed: <strong>{Object.values(activeOrders).filter(o => o.isComplete).length}</strong></div>
            <div>📊 Total Events: <strong>{orderEvents.length}</strong></div>
          </div>
        </div>
        {/* Recent Orders Section */}
        <div style={{ marginTop: '16px', borderTop: '1px solid #e5e7eb', paddingTop: '16px' }}>
          <div className="flex" style={{ justifyContent: 'space-between', alignItems: 'center', marginBottom: '12px' }}>
            <h3 style={{ margin: 0, fontSize: '16px', fontWeight: 'bold' }}>
              Recent Orders ({Object.keys(activeOrders).length})
            </h3>
            {Object.values(activeOrders).some(o => o.isComplete) && (
              <button
                onClick={clearCompletedOrders}
                className="btn btn-danger btn-small"
              >
                Clear
              </button>
            )}
          </div>
          <div className="overflow-auto max-h-400">
            {Object.keys(activeOrders).length === 0 && (
              <div className="text-center" style={{ color: '#9ca3af', padding: '20px', fontSize: '13px' }}>
                Waiting for orders...
              </div>
            )}
            {Object.entries(activeOrders)
              .sort((a, b) => new Date(b[1].lastUpdate) - new Date(a[1].lastUpdate))
              .map(([orderId, order]) => {
                const firstEvent = order.events.find(e => e.workflowEventType === 1); // WorkflowStarted = 1
                const orderMessage = firstEvent?.message || 'Order in progress';
                const cardClass = order.isComplete ? 'order-card order-card--complete' : 'order-card order-card--active';
                return (
                  <div key={orderId} className={cardClass}>
                    <div className="order-card__header">
                      <div style={{ flex: 1 }}>
                        <div style={{ fontSize: '12px', fontWeight: 'bold', color: '#1f2937', marginBottom: '3px', fontFamily: 'monospace' }}>
                          #{orderId}
                        </div>
                        <div className="order-card__body">
                          {orderMessage.length > 50 
                            ? orderMessage.substring(0, 50) + '...' 
                            : orderMessage}
                        </div>
                      </div>
                      {order.isComplete && (
                        <span style={{ color: '#10b981', fontSize: '16px', marginLeft: '6px', flexShrink: 0 }}>✓</span>
                      )}
                    </div>
                    <div className="flex gap-6" style={{ padding: '4px 8px', background: order.isComplete ? '#dcfce7' : '#fef9c3', borderRadius: '4px', marginBottom: '6px', alignItems: 'center' }}>
                      <span style={{ fontSize: '12px' }}>
                        {order.isComplete ? '🍽️' : '🔄'}
                      </span>
                      <span style={{ fontSize: '11px', fontWeight: '600', color: '#374151' }}>
                        {order.isComplete ? 'Complete' : order.currentAgentName}
                      </span>
                    </div>
                    <div className="order-card__footer">
                      <span>{new Date(order.lastUpdate).toLocaleTimeString()}</span>
                      <span>{order.events.length} event{order.events.length !== 1 ? 's' : ''}</span>
                    </div>
                  </div>
                );
              })}
          </div>
        </div>
      </div>

      {/* Active Orders Panel - REMOVED, now in left panel */}
      
      <ReactFlow
        nodes={nodes}
        edges={edges}
        onNodesChange={onNodesChange}
        onEdgesChange={onEdgesChange}
        onConnect={onConnect}
        nodeTypes={nodeTypes}
        defaultViewport={{ x: 0, y: 0, zoom: 1 }}
        attributionPosition="bottom-left"
        connectionMode="loose"
        snapToGrid={true}
        snapGrid={[15, 15]}
        defaultEdgeOptions={{
          type: 'smoothstep',
          animated: false,
        }}
        fitView
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
