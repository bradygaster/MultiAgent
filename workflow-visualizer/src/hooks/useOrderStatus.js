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
          
          // Subscribe to order updates
          connection.invoke('SubscribeToOrders')
            .catch(err => console.error('Error subscribing to orders:', err));

          // Listen for order status updates
          connection.on('OrderStatusUpdate', (event) => {
            console.log('📡 Order Status Update:', event);
            
            // Log tool calls specifically (eventType 2 = ToolCalled)
            if (event.eventType === 2) {
              console.log('🔧 Tool Called:', event.toolCall?.name, 'by', event.agentName);
            }
            
            setOrderEvents(prev => [...prev, event]);
            
            setActiveOrders(prev => {
              const orderData = prev[event.orderId] || { events: [] };
              
              return {
                ...prev,
                [event.orderId]: {
                  ...orderData,
                  currentAgent: event.agentId,
                  currentAgentName: event.agentName,
                  lastUpdate: event.timestamp,
                  lastEventType: event.eventType,
                  events: [...orderData.events, event],
                  isComplete: event.eventType === 4 // OrderCompleted = 4
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
        connection.invoke('SubscribeToOrders')
          .catch(err => console.error('Error re-subscribing to orders:', err));
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
      Object.entries(prev).forEach(([orderId, order]) => {
        if (!order.isComplete) {
          filtered[orderId] = order;
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
