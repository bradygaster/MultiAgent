import { useEffect, useState, useCallback } from 'react';
import * as signalR from '@microsoft/signalr';

export const useOrderStatus = () => {
  const [connection, setConnection] = useState(null);
  const [orderEvents, setOrderEvents] = useState([]);
  const [activeOrders, setActiveOrders] = useState({});
  const [isConnected, setIsConnected] = useState(false);

  useEffect(() => {
    const hubUrl = import.meta.env.VITE_SIGNALR_HUB_URL || 'http://localhost:5274';
    
    console.log('Connecting to OrderStatusHub at:', hubUrl);
    
    const newConnection = new signalR.HubConnectionBuilder()
      .withUrl(`${hubUrl}/orderstatus`, {
        skipNegotiation: false,
        withCredentials: false
      })
      .withAutomaticReconnect()
      .configureLogging(signalR.LogLevel.Information)
      .build();

    setConnection(newConnection);
  }, []);

  useEffect(() => {
    if (connection) {
      connection.start()
        .then(() => {
          console.log('✅ Connected to OrderStatusHub');
          setIsConnected(true);

          // Listen for order status updates
          connection.on('WorkflowStatusEvent', (event) => {
            console.log('📡 Raw Order Status Update:', JSON.stringify(event, null, 2));
            console.log('   Properties:', Object.keys(event));
            console.log('   workflowEventType value:', event.workflowEventType, 'type:', typeof event.workflowEventType);
                 
            setOrderEvents(prev => [...prev, event]);
            
            setActiveOrders(prev => {
              const orderData = prev[event.workflowId] || { events: [] };
              
              return {
                ...prev,
                [event.workflowId]: {
                  ...orderData,
                  currentAgent: event.agentId,
                  currentAgentName: event.agentName,
                  lastUpdate: event.timestamp,
                  lastEventType: event.workflowEventType,
                  events: [...orderData.events, event],
                  isComplete: event.workflowEventType === 2 // WorkflowEnded = 2
                }
              };
            });
          });
        })
        .catch(err => {
          console.error('❌ SignalR Connection Error:', err);
          setIsConnected(false);
        });

      connection.onreconnecting(error => {
        console.warn('🔄 SignalR reconnecting...', error);
        setIsConnected(false);
      });

      connection.onreconnected(connectionId => {
        console.log('✅ SignalR reconnected:', connectionId);
        setIsConnected(true);
      });

      connection.onclose(error => {
        console.error('❌ SignalR connection closed:', error);
        setIsConnected(false);
      });
    }

    return () => {
      if (connection) {
        connection.stop();
      }
    };
  }, [connection]);

  const clearCompletedOrders = useCallback(() => {
    setActiveOrders(prev => {
      const filtered = {};
      Object.entries(prev).forEach(([workflowId, evt]) => {
        if (!evt.isComplete) {
          filtered[workflowId] = evt;
        }
      });
      return filtered;
    });
  }, []);

  return { 
    orderEvents, 
    activeOrders, 
    isConnected,
    clearCompletedOrders
  };
};
