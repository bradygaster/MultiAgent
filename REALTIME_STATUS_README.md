# Real-Time Order Status Feature

This feature adds real-time order status updates to the MultiAgent workflow visualizer using SignalR.

## What's New

### Backend Components

1. **MultiAgent.StatusHub** - New SignalR hub service
   - Broadcasts order events to connected clients
   - Maintains order history
   - Provides connection management

2. **OrderEventPublisher** - Service in CLI project
   - Publishes workflow events to SignalR hub
   - Auto-reconnects on connection loss
   - Integrated with ConversationLoop

3. **Updated ConversationLoop** - Enhanced event tracking
   - Publishes OrderReceived events
   - Publishes AgentStarted events
   - Publishes ToolCalled events with arguments
   - Publishes OrderCompleted events

### Frontend Components

1. **useOrderStatus Hook** - React hook for SignalR connection
   - Manages SignalR connection lifecycle
   - Tracks active orders
   - Provides connection status

2. **Enhanced Workflow Visualizer**
   - Real-time node highlighting when processing orders
   - Active order count badges on agents
   - Live connection status indicator
   - Recent orders panel with order details
   - Auto-refresh on order completion

## How It Works

```
Order → ConversationLoop → OrderEventPublisher → SignalR Hub → React UI
                                ↓
                         OpenTelemetry (parallel)
```

1. When an order is submitted, ConversationLoop generates an order ID
2. Throughout the workflow, events are published to OrderEventPublisher
3. OrderEventPublisher sends events to the SignalR hub
4. The hub broadcasts to all connected React clients
5. The React UI updates in real-time showing agent progress

## Event Types

- **OrderReceived** - New order submitted to the system
- **AgentStarted** - An agent begins processing the order
- **ToolCalled** - An agent calls an MCP tool (includes tool name and arguments)
- **AgentCompleted** - An agent finishes its work (implicit from next agent starting)
- **OrderCompleted** - Entire workflow finished successfully
- **Error** - Any error during processing

## Visual Features

- **Node Glow Effect** - Active agents have a glowing shadow in their theme color
- **Scale Animation** - Nodes scale up slightly when processing
- **Badge Counter** - Shows number of orders being processed by each agent
- **Status Indicator** - Green dot when connected, red when disconnected
- **Order Timeline** - Shows recent orders with timestamps
- **Clear Completed** - Button to remove completed orders from the panel

## Running the Feature

Simply run the Aspire AppHost:

```bash
dotnet run --project MultiAgent.AppHost
```

The Aspire dashboard will show:
- **mcpserverhost** - MCP tools server
- **statushub** - SignalR hub for real-time events
- **orderSimulator** - Order processing CLI
- **workflow-visualizer** - React Flow UI

Open the workflow-visualizer endpoint in your browser to see live order processing!

## Configuration

The React app automatically gets the SignalR hub URL via Aspire service discovery:

```csharp
.WithEnvironment("VITE_SIGNALR_HUB_URL", statushub.GetEndpoint("http"))
```

For standalone testing, the default URL is `http://localhost:5274`.

## Future Enhancements

- Order replay from history
- Agent performance metrics
- Error notifications and alerts
- Export order logs
- Multi-tenant filtering
- Tool usage analytics
