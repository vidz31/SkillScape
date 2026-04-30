import React, { useEffect, useState } from 'react';
import { Outlet } from 'react-router-dom';
import { Sidebar } from './Sidebar';
import { Navbar } from './Navbar';
import * as signalR from '@microsoft/signalr';
import { getAuthToken } from '@/services/api';
import { toast } from 'sonner';
import { useAuth } from '@/context/AuthContext';
import { useNavigate } from 'react-router-dom';

export const DashboardLayout = () => {
  const { user } = useAuth();
  const [connection, setConnection] = useState(null);
  const navigate = useNavigate();

  useEffect(() => {
    if (!user) return;

    const token = getAuthToken();
    const chatHubUrl = import.meta.env.VITE_API_URL
      ? import.meta.env.VITE_API_URL.replace('/api', '/hubs/chat')
      : 'http://localhost:5000/hubs/chat';

    const newConnection = new signalR.HubConnectionBuilder()
      .withUrl(chatHubUrl, {
        accessTokenFactory: () => token
      })
      .withAutomaticReconnect()
      .build();

    setConnection(newConnection);
  }, [user]);

  useEffect(() => {
    if (connection) {
      connection.start()
        .then(() => {
          console.log("Global Notification Hub Connected");

          connection.on("ReceiveNotification", (notification) => {
            window.dispatchEvent(new CustomEvent('skillscape:notification-received', {
              detail: notification
            }));

            if (notification.type === 'achievement') {
              toast.success(`${notification.title} - ${notification.message}`, { duration: 5000 });
            } else if (notification.type === 'chat') {
              toast(notification.title, {
                duration: 5000,
                description: (
                  <div className="flex flex-col gap-2">
                    <span className="text-sm text-gray-600">{notification.message !== "Sent an image" ? notification.message : "Sent an image"}</span>
                    {notification.imageUrl && (
                      <img
                        src={`${import.meta.env.VITE_API_URL?.replace('/api', '') || 'http://localhost:5000'}${notification.imageUrl}`}
                        alt="Attachment"
                        className="rounded-lg object-cover max-h-32 mt-1 border border-gray-200"
                      />
                    )}
                  </div>
                ),
                action: {
                  label: "View",
                  onClick: () => navigate('/messages')
                },
              });
            } else {
              toast.info(`${notification.title} - ${notification.message}`, { duration: 5000 });
            }
          });
        })
        .catch(e => console.log("Connection failed: ", e));
    }

    return () => {
      if (connection) {
        connection.off("ReceiveNotification");
        connection.stop();
      }
    };
  }, [connection]);
  return (
    <div className="flex min-h-screen w-full bg-background">
      {/* Sidebar */}
      <Sidebar />

      {/* Main Content Area */}
      <div className="flex-1 ml-20 lg:ml-[280px] transition-all duration-300">
        {/* Top Navbar */}
        <Navbar />

        {/* Page Content */}
        <main className="p-6 lg:p-8">
          <Outlet />
        </main>
      </div>
    </div>
  );
};

export default DashboardLayout;
