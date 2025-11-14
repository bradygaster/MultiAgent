# Dynamic Workflow Visualization Implementation Plan

## Overview
Transform the workflow visualizer from a static, hardcoded visualization to a dynamic one that reads agent metadata from markdown instruction files and workflow definitions from the backend.

---

## Current State Analysis

### Static Elements (Hardcoded)
- **Agent Nodes**: 5 agents defined in `App.jsx` with hardcoded:
  - IDs, names, domains, tools, emojis, colors, positions
- **Workflow Structure**: Sequential flow hardcoded in `initialEdges`
- **Tool Definitions**: Tool names and descriptions duplicated from backend

### Dynamic Elements (Already Working)
- **Order Events**: Real-time via SignalR
- **Agent Activity**: Active order counts, tool execution tracking
- **Order Status**: Completion tracking

### Source of Truth (Backend)
1. **Agent Metadata** (`MultiAgent.CLI/Instructions/*.md`)
   - Front matter YAML with: `id`, `name`, `domain`, `tools[]`, `emoji`, `color`
   - Loaded by `InstructionLoader.cs`
   - Stored in `AgentPool` with `AgentMetadata` objects

2. **Workflow Definition** (`OrderWorkflowDefinition.cs`)
   - Sequential workflow via `AgentWorkflowBuilder.BuildSequential()`
   - Agent order: Grill → Fryer → Dessert → Plating
   - Workflow metadata: name, description

3. **Tool Definitions** (MCP Server Tools - `McpServerHost/Tools/*.cs`)
   - `GrillTools`: 5 tools with descriptions
   - `FryerTools`: 6 tools with descriptions
   - `DessertTools`: 3 tools with descriptions
   - `ExpoTools`: 2 tools with descriptions

---

## Implementation Plan

### Phase 1: Backend API Endpoints
**Goal**: Expose workflow and agent metadata via REST API

#### 1.1 Create Workflow Metadata API
**File**: `MultiAgent.StatusHub/Program.cs` (add minimal API endpoint)

**Endpoint**: `GET /api/workflow/definition`
```json
{
  "name": "OrderFulfillment",
  "description": "Processes restaurant orders...",
  "agents": [
    {
      "id": "grill",
      "name": "Grilling Agent",
      "domain": "Grilling meats and produce...",
      "tools": ["cook_patty", "melt_cheese", "add_bacon", "toast_bun", "assemble_burger"],
      "emoji": "🥩",
      "color": "#dc2626",
      "sequenceOrder": 0
    },
    {
      "id": "fryer",
      "name": "Fryer Agent",
      "domain": "Cooking anything in the fryer",
      "tools": ["fry_fries", "fry_waffle_fries", "fry_sweet_potato_fries", "add_salt", "bag_fries_for_order", "fry_onion_rings"],
      "emoji": "🍟",
      "color": "#f59e0b",
      "sequenceOrder": 1
    }
    // ... dessert, plating
  ],
  "workflow": {
    "type": "sequential",
    "connections": [
      { "from": "grill", "to": "fryer" },
      { "from": "fryer", "to": "desserts" },
      { "from": "desserts", "to": "expo" }
    ]
  }
}
```

**Implementation Steps**:
1. Add endpoint to `Program.cs` using minimal API
2. Inject `AgentPool` and `IWorkflowDefinition` via DI
3. Extract metadata from `AgentPool.GetAgentSummaries()`
4. Parse workflow structure from `OrderWorkflowDefinition.BuildWorkflow()`
5. Return combined DTO

**Code Example**:
```csharp
app.MapGet("/api/workflow/definition", (AgentPool agentPool, IWorkflowDefinition workflowDef) =>
{
    var agents = agentPool.GetAgentSummaries()
        .Select((summary, index) => new
        {
            summary.Metadata.Id,
            summary.Metadata.Name,
            summary.Metadata.Domain,
            summary.Metadata.Tools,
            summary.Metadata.Emoji,
            summary.Metadata.Color,
            SequenceOrder = index
        })
        .ToList();
    
    var connections = agents.Zip(agents.Skip(1), (from, to) => new
    {
        From = from.Id,
        To = to.Id
    }).ToList();
    
    return Results.Ok(new
    {
        Name = workflowDef.Name,
        Description = workflowDef.Description,
        Agents = agents,
        Workflow = new
        {
            Type = "sequential",
            Connections = connections
        }
    });
});
```

#### 1.2 Create Tool Details API
**File**: `MultiAgent.StatusHub/Program.cs` (add minimal API endpoint)

**Endpoint**: `GET /api/tools`
```json
{
  "grill": [
    { "name": "cook_patty", "description": "Grill a beef patty." },
    { "name": "melt_cheese", "description": "Melt cheese on a burger patty." }
    // ...
  ],
  "fryer": [
    { "name": "fry_fries", "description": "Fry standard French fries." }
    // ...
  ]
  // ... desserts, expo
}
```

**Implementation Steps**:
1. Create MCP server metadata reflection endpoint (or parse from agent metadata)
2. Parse `McpServerTool` attributes from tool classes (optional - tools are in agent metadata)
3. Group by agent ID
4. Return tool catalog

**Code Example** (simple version using agent metadata):
```csharp
app.MapGet("/api/tools", (AgentPool agentPool) =>
{
    var toolsByAgent = agentPool.GetAgentSummaries()
        .ToDictionary(
            summary => summary.Metadata.Id,
            summary => summary.Metadata.Tools.Select(tool => new
            {
                Name = tool,
                Description = "" // Can be enhanced later with MCP metadata
            }).ToList()
        );
    
    return Results.Ok(toolsByAgent);
});
```

**Alternative**: Extract from instruction markdown files (simpler but less accurate)

---

### Phase 2: Frontend Infrastructure
**Goal**: Fetch and consume backend metadata

#### 2.1 Create Metadata Hook
**File**: `workflow-visualizer/src/hooks/useWorkflowMetadata.js` (new)

```javascript
export const useWorkflowMetadata = () => {
  const [metadata, setMetadata] = useState(null);
  const [tools, setTools] = useState(null);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState(null);

  useEffect(() => {
    const fetchMetadata = async () => {
      try {
        const hubUrl = import.meta.env.VITE_SIGNALR_HUB_URL || 'http://localhost:5274';
        
        // Fetch workflow definition
        const workflowRes = await fetch(`${hubUrl}/api/workflow/definition`);
        const workflowData = await workflowRes.json();
        
        // Fetch tool details
        const toolsRes = await fetch(`${hubUrl}/api/tools`);
        const toolsData = await toolsRes.json();
        
        setMetadata(workflowData);
        setTools(toolsData);
        setLoading(false);
      } catch (err) {
        setError(err);
        setLoading(false);
      }
    };

    fetchMetadata();
  }, []);

  return { metadata, tools, loading, error };
};
```

#### 2.2 Create Node Generator Utility
**File**: `workflow-visualizer/src/utils/workflowLayoutEngine.js` (new)

**Purpose**: Generate ReactFlow nodes/edges from metadata

```javascript
export const generateNodes = (workflowMetadata, tools) => {
  const nodes = [];
  const edges = [];
  
  // Input node
  nodes.push({
    id: 'order',
    type: 'input',
    data: { 
      label: '🛎️ Customer Order',
      description: 'Order received from customer'
    },
    position: calculatePosition(0, true),
    style: getInputNodeStyle()
  });
  
  // Agent nodes
  workflowMetadata.agents.forEach((agent, index) => {
    nodes.push({
      id: agent.id,
      type: 'agentNode',
      data: {
        label: `${agent.emoji} ${agent.name}`,
        agentId: agent.id,
        name: agent.name,
        domain: agent.domain,
        tools: tools[agent.id] || agent.tools.map(name => ({ name, desc: '' })),
        emoji: agent.emoji,
        color: agent.color
      },
      position: calculatePosition(index + 1, false)
    });
  });
  
  // Output node
  nodes.push({
    id: 'output',
    type: 'outputNode',
    data: { 
      label: '🍽️ Order Complete',
      description: 'Hand order to customer or delivery service'
    },
    position: calculatePosition(workflowMetadata.agents.length + 1, true)
  });
  
  // Generate edges from workflow connections
  edges.push({
    id: 'e-order-' + workflowMetadata.agents[0].id,
    source: 'order',
    target: workflowMetadata.agents[0].id,
    animated: false,
    style: { stroke: '#6366f1', strokeWidth: 3 },
    label: 'Process Order'
  });
  
  workflowMetadata.workflow.connections.forEach((conn, index) => {
    const sourceAgent = workflowMetadata.agents.find(a => a.id === conn.from);
    edges.push({
      id: `e-${conn.from}-${conn.to}`,
      source: conn.from,
      target: conn.to,
      animated: false,
      style: { stroke: sourceAgent?.color || '#94a3b8', strokeWidth: 3 }
    });
  });
  
  const lastAgent = workflowMetadata.agents[workflowMetadata.agents.length - 1];
  edges.push({
    id: 'e-' + lastAgent.id + '-output',
    source: lastAgent.id,
    target: 'output',
    animated: false,
    style: { stroke: lastAgent.color, strokeWidth: 3 }
  });
  
  return { nodes, edges };
};

const calculatePosition = (index, isInputOutput) => {
  // Implement auto-layout algorithm
  // For sequential: vertical or horizontal flow
  // Could use dagre or custom algorithm
  
  // Simple implementation:
  if (isInputOutput) {
    return { x: 50, y: index * 150 };
  }
  return { x: 320 + (index % 2) * 260, y: 90 + Math.floor(index / 2) * 200 };
};
```

---

### Phase 3: Refactor App.jsx
**Goal**: Use dynamic metadata instead of hardcoded data

#### 3.1 Update App Component
**File**: `workflow-visualizer/src/App.jsx`

**Changes**:
1. Remove `initialNodes` and `initialEdges` constants
2. Import `useWorkflowMetadata` hook
3. Generate nodes/edges from metadata
4. Show loading state while fetching

```javascript
function App() {
  const { metadata, tools, loading, error } = useWorkflowMetadata();
  const { orderEvents, activeOrders, isConnected, clearCompletedOrders } = useOrderStatus();
  
  const [nodes, setNodes, onNodesChange] = useNodesState([]);
  const [edges, setEdges, onEdgesChange] = useEdgesState([]);
  
  // Initialize nodes/edges when metadata loads
  useEffect(() => {
    if (metadata && tools) {
      const { nodes: generatedNodes, edges: generatedEdges } = generateNodes(metadata, tools);
      setNodes(generatedNodes);
      setEdges(generatedEdges);
    }
  }, [metadata, tools, setNodes, setEdges]);
  
  if (loading) return <LoadingSpinner />;
  if (error) return <ErrorMessage error={error} />;
  
  // ... rest of component
}
```

---

### Phase 4: Enhanced Visualizations
**Goal**: Add metadata-driven UI enhancements

#### 4.1 Agent Metadata Panel
Display agent details from markdown on hover/click:
- Full domain description
- All available tools with descriptions
- Tool usage statistics

#### 4.2 Workflow Type Support
Extend to support different workflow types:
- **Sequential**: Current implementation
- **Parallel**: Agents work concurrently
- **Conditional**: Agent routing based on order contents

#### 4.3 Dynamic Layout Algorithm
Implement auto-layout based on workflow structure:
- Use `dagre` library for automatic graph layout
- Support vertical/horizontal orientations
- Handle complex workflows (not just sequential)

---

## Implementation Order & Priorities

### 🔥 High Priority (MVP)
1. **Backend API** (Phase 1.1): Workflow metadata endpoint
2. **Frontend Hook** (Phase 2.1): Fetch metadata
3. **Node Generator** (Phase 2.2): Basic layout engine
4. **App Refactor** (Phase 3.1): Dynamic nodes/edges

### 🟡 Medium Priority (Enhanced)
5. **Tool Details API** (Phase 1.2): Rich tool information
6. **Enhanced Panels** (Phase 4.1): Detailed agent metadata display
7. **Auto Layout** (Phase 4.3): Dagre integration

### 🟢 Low Priority (Future)
8. **Workflow Types** (Phase 4.2): Support parallel/conditional workflows

---

## Technical Considerations

### Data Flow
```
Markdown Files → InstructionLoader → AgentPool → API Controller → HTTP Response
                                                                          ↓
                                                              Frontend Hook → Layout Engine
                                                                          ↓
                                                              ReactFlow Nodes/Edges
```

### Caching Strategy
- Cache workflow metadata (rarely changes)
- Refresh on app restart or manual refresh button
- Consider SignalR notification when workflow changes

### Error Handling
- Graceful fallback to static config if API fails
- Display error messages for missing metadata
- Validate metadata structure on load

### Performance
- Memoize node/edge generation
- Lazy load tool details (only when agent clicked)
- Debounce layout recalculations

---

## Testing Strategy

### Backend Tests
- Unit tests for metadata extraction
- Integration tests for API endpoints
- Verify JSON schema compliance

### Frontend Tests
- Unit tests for layout engine
- Integration tests for metadata hook
- Visual regression tests for node rendering

### E2E Tests
- Workflow loads correctly
- Nodes match backend configuration
- Real-time updates still work with dynamic layout

---

## Migration Path

### Step 1: Parallel Systems
Keep static config as fallback while building dynamic system

### Step 2: Feature Flag
Add environment variable to toggle dynamic/static mode
```javascript
const USE_DYNAMIC_WORKFLOW = import.meta.env.VITE_USE_DYNAMIC_WORKFLOW === 'true';
```

### Step 3: Gradual Rollout
1. Test with dynamic mode in development
2. Deploy with static mode in production
3. Enable dynamic mode after validation
4. Remove static code after stabilization

---

## Benefits

### ✅ Single Source of Truth
- Agent definitions only in markdown files
- Workflow structure only in C# code
- No duplication between frontend/backend

### ✅ Maintainability
- Add new agent: just create markdown file
- Change workflow: modify C# definition
- No manual frontend updates needed

### ✅ Scalability
- Support multiple workflows (lunch vs dinner)
- Different agent configurations per environment
- Easy A/B testing of workflow changes

### ✅ Accuracy
- Tool descriptions always match backend
- Agent capabilities accurately reflected
- Workflow visualization matches actual execution

---

## Success Metrics
- ✅ Zero hardcoded agent data in frontend
- ✅ Workflow changes deploy without frontend rebuild
- ✅ <500ms metadata load time
- ✅ Tool descriptions match backend 100%
- ✅ Support for 10+ agents without layout issues
