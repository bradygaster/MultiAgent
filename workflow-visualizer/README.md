# MultiAgent Workflow Visualizer

Interactive React Flow visualization of the MultiAgent restaurant order processing system.

## Overview

This visualization demonstrates the sequential agent workflow used in the MultiAgent application:

1. **🛎️ Customer Order** - Order received from customer
2. **🥩 Grill Agent** - Processes burgers, cheese, bacon, buns
3. **🍟 Fryer Agent** - Handles fries, onion rings, and sides
4. **🍨 Dessert Agent** - Makes milkshakes and sundaes
5. **🎁 Plating Agent** - Final assembly and packaging
6. **🍽️ Order Complete** - Ready for customer/delivery

## Running with .NET Aspire

This React app is integrated into the .NET Aspire AppHost. Simply run the Aspire application:

```bash
# From the solution root
dotnet run --project MultiAgent.AppHost
```

The workflow visualizer will automatically start and be available in the Aspire dashboard along with the other services.

## Running Standalone

You can also run the visualizer independently:

```bash
npm install
npm run dev
```

The application will be available at http://localhost:3000

## Theme Toggle (Dark / Light Mode)

A global toolbar has been added at the top of the application with a Dark/Light mode toggle.

- Your preference is saved in `localStorage` under the key `theme`.
- The toggle is accessible (uses `aria-pressed` and clear labels).
- Dark mode applies a cohesive palette across panels, nodes, modals, and tool lists.

If you need to reset the theme, open DevTools and run:

```js
localStorage.removeItem('theme'); location.reload();
```

You can further customize colors by editing the CSS variables in `src/styles/App.css` under the `:root` / `.theme-dark` sections.

## Build for Production

```bash
npm run build
```

## Technology Stack

- **React** - UI framework
- **React Flow** - Workflow visualization library
- **Vite** - Build tool and dev server

## Architecture Reference

This visualization is based on the MultiAgent C# application which uses:
- Microsoft.Agents.AI framework
- AgentWorkflowBuilder for sequential workflows
- MCP (Model Context Protocol) tools
- Streaming execution with telemetry

## Agent Details

### Grill Agent
- Domain: Grilling meats and produce
- Tools: cook_patty, melt_cheese, add_bacon, toast_bun, assemble_burger

### Fryer Agent
- Domain: Cooking in the fryer
- Tools: fry_fries, fry_waffle_fries, fry_sweet_potato_fries, fry_onion_rings, add_salt, bag_fries_for_order

### Dessert Agent
- Domain: Making desserts
- Tools: make_shake, make_sundae, add_whipped_cream

### Plating Agent
- Domain: Final assembly and presentation
- Tools: plate_meal, package_for_takeout
