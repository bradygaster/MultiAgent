# Implementation Plan: Real-Time Order Status to UI

## Overview
Stream order processing events from the MultiAgent workflow to the React Flow UI in real-time, showing visual progress as orders move through each agent station.

---

## Architecture Components

### 1. **SignalR Hub for Real-Time Communication**
   - Create a new `OrderStatusHub` in a new Web API project
   - Hub will broadcast order events to connected React clients
   - Aspire will orchestrate the hub alongside other services

### 2. **Order Event Model**
   ```csharp
   public class OrderStatusEvent
   {
       public string OrderId { get; set; }
       public string AgentId { get; set; }
       public string AgentName { get; set; }
       public OrderEventType EventType { get; set; }
       public string Message { get; set; }
       public DateTime Timestamp { get; set; }
       public Dictionary<string, object>? ToolCall { get; set; }
   }

   public enum OrderEventType
   {
       OrderReceived,
       AgentStarted,
       ToolCalled,
       AgentCompleted,
       OrderCompleted,
       Error
   }
   ```

### 3. **Event Publisher Service**
   - Injectable service in the CLI project
   - Wraps SignalR hub client connection
   - Called from `ConversationLoop` to publish events

---

## Implementation Steps

### Phase 1: Create SignalR Hub Service

#### 1.1 Create new Web API project
```bash
Location: MultiAgent.StatusHub/
Files:
  - Program.cs
  - Hubs/OrderStatusHub.cs
  - Models/OrderStatusEvent.cs
  - MultiAgent.StatusHub.csproj
```

**OrderStatusHub.cs**
```csharp
using Microsoft.AspNetCore.SignalR;

public class OrderStatusHub : Hub
{
    private static readonly Dictionary<string, List<OrderStatusEvent>> _orderHistory = new();

    public async Task SubscribeToOrders()
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, "OrderUpdates");
    }

    public async Task PublishOrderEvent(OrderStatusEvent evt)
    {
        if (!_orderHistory.ContainsKey(evt.OrderId))
            _orderHistory[evt.OrderId] = new();
        
        _orderHistory[evt.OrderId].Add(evt);
        
        await Clients.Group("OrderUpdates").SendAsync("OrderStatusUpdate", evt);
    }

    public async Task<List<OrderStatusEvent>> GetOrderHistory(string orderId)
    {
        return _orderHistory.GetValueOrDefault(orderId) ?? new();
    }
}
```

**Program.cs**
```csharp
var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();

builder.Services.AddSignalR();
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyHeader()
              .AllowAnyMethod();
    });
});

var app = builder.Build();

app.MapDefaultEndpoints();
app.UseCors();
app.MapHub<OrderStatusHub>("/orderstatus");

app.Run();
```

#### 1.2 Update Aspire AppHost
```csharp
var statusHub = builder.AddProject<Projects.MultiAgent_StatusHub>("statushub");

var workflow_visualizer = builder.AddViteApp("workflow-visualizer", "../workflow-visualizer")
    .WithReference(statusHub)
    .WithEnvironment("VITE_SIGNALR_HUB_URL", statusHub.GetEndpoint("http"))
    .PublishAsDockerFile();
```

---

### Phase 2: Modify CLI to Publish Events

#### 2.1 Create Event Publisher Service
**Location: MultiAgent.CLI/Services/OrderEventPublisher.cs**

```csharp
using Microsoft.AspNetCore.SignalR.Client;

public class OrderEventPublisher : IAsyncDisposable
{
    private readonly HubConnection _hubConnection;
    private readonly ILogger<OrderEventPublisher> _logger;

    public OrderEventPublisher(IConfiguration config, ILogger<OrderEventPublisher> logger)
    {
        _logger = logger;
        var hubUrl = config["services:statushub:http:0"] ?? "http://localhost:5000";
        
        _hubConnection = new HubConnectionBuilder()
            .WithUrl($"{hubUrl}/orderstatus")
            .WithAutomaticReconnect()
            .Build();
    }

    public async Task StartAsync()
    {
        await _hubConnection.StartAsync();
        _logger.LogInformation("Connected to OrderStatusHub");
    }

    public async Task PublishEventAsync(OrderStatusEvent evt)
    {
        if (_hubConnection.State == HubConnectionState.Connected)
        {
            await _hubConnection.InvokeAsync("PublishOrderEvent", evt);
        }
    }

    public async ValueTask DisposeAsync()
    {
        await _hubConnection.DisposeAsync();
    }
}
```

#### 2.2 Update ConversationLoop to Publish Events
```csharp
public class ConversationLoop(
    AgentPool agentPool, 
    ILogger<ConversationLoop> logger,
    OrderEventPublisher eventPublisher)  // <-- Add dependency
{
    public async Task<List<ChatMessage>> SubmitOrder(string order)
    {
        var orderId = Guid.NewGuid().ToString("N")[..8];
        
        // Publish order received event
        await eventPublisher.PublishEventAsync(new OrderStatusEvent
        {
            OrderId = orderId,
            AgentId = "system",
            AgentName = "System",
            EventType = OrderEventType.OrderReceived,
            Message = order,
            Timestamp = DateTime.UtcNow
        });

        // ... existing code ...

        await foreach (WorkflowEvent evt in run.WatchStreamAsync())
        {
            if (evt is AgentRunUpdateEvent e)
            {
                if (e.ExecutorId != lastExecutorId)
                {
                    lastExecutorId = e.ExecutorId;
                    
                    // Publish agent started event
                    await eventPublisher.PublishEventAsync(new OrderStatusEvent
                    {
                        OrderId = orderId,
                        AgentId = e.ExecutorId,
                        AgentName = e.Update.AuthorName,
                        EventType = OrderEventType.AgentStarted,
                        Message = $"{e.Update.AuthorName} starting",
                        Timestamp = DateTime.UtcNow
                    });
                }

                if (e.Update.Contents.OfType<FunctionCallContent>().FirstOrDefault() is FunctionCallContent call)
                {
                    // Publish tool call event
                    await eventPublisher.PublishEventAsync(new OrderStatusEvent
                    {
                        OrderId = orderId,
                        AgentId = e.ExecutorId,
                        AgentName = e.Update.AuthorName,
                        EventType = OrderEventType.ToolCalled,
                        Message = $"Calling {call.Name}",
                        Timestamp = DateTime.UtcNow,
                        ToolCall = new Dictionary<string, object>
                        {
                            ["name"] = call.Name,
                            ["arguments"] = call.Arguments
                        }
                    });
                }
            }
            else if (evt is WorkflowOutputEvent output)
            {
                // Publish order completed event
                await eventPublisher.PublishEventAsync(new OrderStatusEvent
                {
                    OrderId = orderId,
                    AgentId = "system",
                    AgentName = "System",
                    EventType = OrderEventType.OrderCompleted,
                    Message = "Order completed successfully",
                    Timestamp = DateTime.UtcNow
                });
                
                return output.As<List<ChatMessage>>() ?? new List<ChatMessage>();
            }
        }
        
        return new List<ChatMessage>();
    }
}
```

#### 2.3 Register Service in Program.cs
```csharp
builder.Services.AddSingleton<OrderEventPublisher>();

// Start the connection on app start
var app = builder.Build();

var eventPublisher = app.Services.GetRequiredService<OrderEventPublisher>();
await eventPublisher.StartAsync();

app.Run();
```

---

### Phase 3: Update React UI to Consume Events

#### 3.1 Install SignalR Client
```bash
cd workflow-visualizer
npm install @microsoft/signalr
```

#### 3.2 Create SignalR Hook
**Location: workflow-visualizer/src/hooks/useOrderStatus.js**

```javascript
import { useEffect, useState } from 'react';
import * as signalR from '@microsoft/signalr';

export const useOrderStatus = () => {
  const [connection, setConnection] = useState(null);
  const [orderEvents, setOrderEvents] = useState([]);
  const [activeOrders, setActiveOrders] = useState({});

  useEffect(() => {
    const hubUrl = import.meta.env.VITE_SIGNALR_HUB_URL || 'http://localhost:5000';
    
    const newConnection = new signalR.HubConnectionBuilder()
      .withUrl(`${hubUrl}/orderstatus`)
      .withAutomaticReconnect()
      .build();

    setConnection(newConnection);
  }, []);

  useEffect(() => {
    if (connection) {
      connection.start()
        .then(() => {
          console.log('Connected to OrderStatusHub');
          connection.invoke('SubscribeToOrders');

          connection.on('OrderStatusUpdate', (event) => {
            setOrderEvents(prev => [...prev, event]);
            
            setActiveOrders(prev => ({
              ...prev,
              [event.orderId]: {
                ...(prev[event.orderId] || {}),
                currentAgent: event.agentId,
                lastUpdate: event.timestamp,
                events: [...(prev[event.orderId]?.events || []), event]
              }
            }));
          });
        })
        .catch(err => console.error('SignalR Connection Error:', err));
    }

    return () => {
      if (connection) {
        connection.stop();
      }
    };
  }, [connection]);

  return { orderEvents, activeOrders, isConnected: connection?.state === 'Connected' };
};
```

#### 3.3 Update App.jsx to Show Live Status
```javascript
import { useOrderStatus } from './hooks/useOrderStatus';

function App() {
  const { orderEvents, activeOrders, isConnected } = useOrderStatus();
  const [nodes, setNodes, onNodesChange] = useNodesState(initialNodes);
  const [edges, setEdges, onEdgesChange] = useEdgesState(initialEdges);

  // Update node styling based on active orders
  useEffect(() => {
    setNodes(currentNodes => 
      currentNodes.map(node => {
        const activeOrdersInNode = Object.values(activeOrders).filter(
          order => order.currentAgent === node.data.agentId
        );

        return {
          ...node,
          style: {
            ...node.style,
            boxShadow: activeOrdersInNode.length > 0 
              ? `0 0 20px ${node.data.color}` 
              : node.style.boxShadow,
            transform: activeOrdersInNode.length > 0 
              ? 'scale(1.05)' 
              : 'scale(1)'
          },
          data: {
            ...node.data,
            activeOrders: activeOrdersInNode.length
          }
        };
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
        fontSize: '14px'
      }}>
        {isConnected ? '🟢 Live' : '🔴 Disconnected'}
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
        maxHeight: '300px',
        overflow: 'auto'
      }}>
        <h3 style={{ margin: '0 0 12px 0', fontSize: '16px' }}>
          Active Orders ({Object.keys(activeOrders).length})
        </h3>
        {Object.entries(activeOrders).map(([orderId, order]) => (
          <div key={orderId} style={{
            padding: '8px',
            marginBottom: '8px',
            background: '#f3f4f6',
            borderRadius: '6px',
            fontSize: '12px'
          }}>
            <strong>Order #{orderId}</strong>
            <div>Current: {order.currentAgent}</div>
            <div style={{ color: '#6b7280' }}>
              {new Date(order.lastUpdate).toLocaleTimeString()}
            </div>
          </div>
        ))}
      </div>

      {/* Existing ReactFlow component */}
      <ReactFlow ... />
    </div>
  );
}
```

---

## Benefits of This Approach

1. **Real-Time Updates**: SignalR provides instant bi-directional communication
2. **Scalable**: Hub can handle multiple concurrent orders and clients
3. **Aspire Integration**: All services orchestrated through Aspire dashboard
4. **Visual Feedback**: React Flow nodes highlight when processing orders
5. **Order History**: Hub maintains event history for debugging/replay
6. **Decoupled**: CLI doesn't need to know about UI clients
7. **Telemetry Friendly**: Works alongside existing OpenTelemetry tracing

---

## File Structure Summary

```
MultiAgent/
├── MultiAgent.StatusHub/          [NEW]
│   ├── Program.cs
│   ├── Hubs/
│   │   └── OrderStatusHub.cs
│   └── Models/
│       └── OrderStatusEvent.cs
├── MultiAgent.CLI/
│   └── Services/
│       ├── ConversationLoop.cs    [MODIFIED]
│       └── OrderEventPublisher.cs [NEW]
├── MultiAgent.AppHost/
│   └── AppHost.cs                 [MODIFIED]
└── workflow-visualizer/
    ├── package.json               [ADD @microsoft/signalr]
    └── src/
        ├── App.jsx                [MODIFIED]
        └── hooks/
            └── useOrderStatus.js  [NEW]
```

---

## Testing Strategy

1. **Unit Tests**: Mock SignalR hub for ConversationLoop tests
2. **Integration Tests**: Verify events flow from CLI → Hub → React
3. **Load Tests**: Simulate multiple concurrent orders
4. **Visual Tests**: Confirm UI updates match actual agent progress

---

## Future Enhancements

- **Replay**: UI can replay past orders from history
- **Metrics**: Show throughput, avg time per agent
- **Alerts**: Notify on errors or long-running orders
- **Multi-Tenant**: Filter orders by user/session
- **Agent Analytics**: Track tool usage patterns
