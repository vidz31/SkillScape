import React, { useState, useEffect, useRef, useCallback } from 'react';
import { useAuth } from '@/context/AuthContext';
import { chatApi } from '@/services/api';
import { getAuthToken } from '@/services/api';
import * as signalR from '@microsoft/signalr';
import { Search, Send, ImageIcon, Loader2, MessageSquare, Trash2 } from 'lucide-react';
import { Card } from '@/components/ui/card';
import { Input } from '@/components/ui/input';
import { Button } from '@/components/ui/button';
import { ScrollArea } from '@/components/ui/scroll-area';
import { toast } from 'sonner';

const ChatWindow = ({ contact, onClose, onMessagesRead }) => {
    const { user } = useAuth();
    const [messages, setMessages] = useState([]);
    const [newMessage, setNewMessage] = useState("");
    const [connection, setConnection] = useState(null);
    const [uploadingImage, setUploadingImage] = useState(false);
    const [selectedImage, setSelectedImage] = useState(null);
    const [isDeleting, setIsDeleting] = useState(false);
    const messagesEndRef = useRef(null);
    const activeContactIdRef = useRef(contact.otherUserId);
    const isMarkingReadRef = useRef(false);
    const lastMarkReadAtRef = useRef(0);

    useEffect(() => {
        activeContactIdRef.current = contact.otherUserId;
    }, [contact.otherUserId]);

    const markCurrentConversationAsRead = useCallback(async (force = false) => {
        const otherUserId = activeContactIdRef.current;
        if (!otherUserId) return;

        const now = Date.now();
        if (!force && (isMarkingReadRef.current || now - lastMarkReadAtRef.current < 800)) {
            return;
        }

        try {
            isMarkingReadRef.current = true;
            await chatApi.markAsRead(otherUserId);
            lastMarkReadAtRef.current = Date.now();
            if (onMessagesRead) {
                onMessagesRead(otherUserId);
            }
        } catch (err) {
            console.error("Failed to mark as read:", err);
        } finally {
            isMarkingReadRef.current = false;
        }
    }, [onMessagesRead]);

    useEffect(() => {
        const fetchHistory = async () => {
            try {
                const res = await chatApi.getHistory(contact.otherUserId);
                setMessages(res.data?.data || []);
            } catch (err) {
                console.error("Failed to fetch chat history:", err);
            }
        };
        fetchHistory();

        // Mark as read when opening chat
        if (contact.unreadCount > 0) {
            markCurrentConversationAsRead(true);
        }
    }, [contact.otherUserId, contact.unreadCount, markCurrentConversationAsRead]);

    useEffect(() => {
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
    }, []);

    useEffect(() => {
        if (connection) {
            connection.start()
                .then(() => {
                    connection.invoke("JoinChat", contact.otherUserId);

                    connection.off("ReceiveMessage");
                    connection.on("ReceiveMessage", (message) => {
                        setMessages(prev => [...prev, message]);
                        // If we are currently in the chat with this user, mark as read immediately
                        if (message.senderId === activeContactIdRef.current) {
                            markCurrentConversationAsRead();
                        }
                    });
                })
                .catch(e => console.log("Connection failed: ", e));

            return () => {
                if (connection) {
                    connection.invoke("LeaveChat", contact.otherUserId).catch(console.error);
                    connection.off("ReceiveMessage");
                    connection.stop();
                }
            };
        }
    }, [connection, contact.otherUserId, markCurrentConversationAsRead]);

    useEffect(() => {
        messagesEndRef.current?.scrollIntoView({ behavior: "smooth" });
    }, [messages]);

    const handleImageSelect = async (e) => {
        const file = e.target.files?.[0];
        if (!file) return;

        if (!file.type.startsWith('image/')) {
            toast.error("Please select an image file");
            return;
        }

        try {
            setUploadingImage(true);
            const formData = new FormData();
            formData.append('file', file);

            const res = await chatApi.uploadImage(formData);
            const imageUrl = res.data?.data;

            if (connection && imageUrl) {
                await connection.invoke("SendMessage", contact.otherUserId, "Sent an image", imageUrl);
            }
        } catch (err) {
            toast.error("Failed to upload image");
        } finally {
            setUploadingImage(false);
            e.target.value = null;
        }
    };

    const handleSendMessage = async (e) => {
        e.preventDefault();
        if (!newMessage.trim() && !selectedImage) return;

        try {
            if (connection) {
                if (!contact.otherUserId) return;

                if (selectedImage) {
                    // Handled by handleImageSelect
                } else {
                    await connection.invoke("SendMessage", contact.otherUserId, newMessage, null);
                }
                setNewMessage("");
            }
        } catch (err) {
            console.error("Failed to send:", err);
            toast.error("Failed to send message: " + err.message);
        }
    };

    const handleDeleteChat = async () => {
        if (!window.confirm("Are you sure you want to delete all messages in this conversation? This cannot be undone.")) return;

        try {
            setIsDeleting(true);
            await chatApi.clearHistory(contact.otherUserId);
            setMessages([]);
            toast.success("Chat history deleted");
            // If parent passed an onClose or onChatDeleted callback we could call it, otherwise just leave chat open and empty
        } catch (err) {
            console.error("Failed to delete chat:", err);
            toast.error("Failed to delete chat history");
        } finally {
            setIsDeleting(false);
        }
    };

    return (
        <div className="flex flex-col h-full bg-gray-50 rounded-lg overflow-hidden border">
            {/* Header */}
            <div className="p-4 bg-white border-b flex items-center justify-between gap-3">
                <div className="flex items-center gap-3">
                    {contact.otherUserAvatar ? (
                        <img src={`${import.meta.env.VITE_API_URL?.replace('/api', '') || 'http://localhost:5000'}${contact.otherUserAvatar}`} alt={contact.otherUserName} className="w-10 h-10 rounded-full object-cover" />
                    ) : (
                        <div className="w-10 h-10 rounded-full bg-teal-500 text-white flex items-center justify-center font-semibold">
                            {contact.otherUserName.split(" ").map((n) => n[0]).join("").substring(0, 2)}
                        </div>
                    )}
                    <div>
                        <h3 className="font-semibold">{contact.otherUserName}</h3>
                    </div>
                </div>
                <div className="flex items-center gap-2">
                    <Button
                        variant="ghost"
                        size="icon"
                        onClick={handleDeleteChat}
                        disabled={isDeleting || messages.length === 0}
                        className="text-red-500 hover:text-red-700 hover:bg-red-50"
                        title="Delete entire conversation"
                    >
                        {isDeleting ? <Loader2 className="w-5 h-5 animate-spin" /> : <Trash2 className="w-5 h-5" />}
                    </Button>
                    {/* Mobile close button */}
                    <button onClick={onClose} className="md:hidden text-gray-400 hover:text-gray-600 p-2">
                        ✕
                    </button>
                </div>
            </div>

            {/* Messages */}
            <ScrollArea className="flex-1 p-4">
                <div className="space-y-4">
                    {messages.map((msg, idx) => {
                        const isMe = msg.senderId === user?.id;
                        return (
                            <div key={idx} className={`flex ${isMe ? 'justify-end' : 'justify-start'}`}>
                                <div className={`max-w-[75%] rounded-2xl px-4 py-2 ${isMe ? 'bg-teal-500 text-white rounded-br-none' : 'bg-white border rounded-bl-none text-gray-800'}`}>
                                    {msg.imageUrl && (
                                        <img src={`${import.meta.env.VITE_API_URL?.replace('/api', '') || 'http://localhost:5000'}${msg.imageUrl}`} alt="Attachment" className="rounded-lg mb-2 max-w-full h-auto object-cover max-h-48" />
                                    )}
                                    {msg.content !== "Sent an image" && <p className="text-sm">{msg.content}</p>}
                                    <span className={`text-[10px] block mt-1 ${isMe ? 'text-teal-100' : 'text-gray-400'}`}>
                                        {new Date(msg.sentAt + (!msg.sentAt.endsWith('Z') ? 'Z' : '')).toLocaleTimeString([], { hour: '2-digit', minute: '2-digit' })}
                                    </span>
                                </div>
                            </div>
                        );
                    })}
                    <div ref={messagesEndRef} />
                </div>
            </ScrollArea>

            {/* Input */}
            <div className="p-4 bg-white border-t">
                <form onSubmit={handleSendMessage} className="flex gap-2 items-center">
                    <label className="cursor-pointer p-2 hover:bg-gray-100 rounded-full transition text-gray-500">
                        <input
                            type="file"
                            accept="image/*"
                            className="hidden"
                            onChange={handleImageSelect}
                            disabled={uploadingImage}
                        />
                        {uploadingImage ? <Loader2 className="w-5 h-5 animate-spin" /> : <ImageIcon className="w-5 h-5" />}
                    </label>
                    <Input
                        value={newMessage}
                        onChange={(e) => setNewMessage(e.target.value)}
                        placeholder="Type a message..."
                        className="flex-1 rounded-full bg-gray-50 focus-visible:ring-teal-500"
                    />
                    <Button
                        type="submit"
                        size="icon"
                        className="rounded-full bg-teal-500 hover:bg-teal-600 h-10 w-10 shrink-0"
                        disabled={(!newMessage.trim() && !selectedImage) || uploadingImage}
                    >
                        <Send className="w-4 h-4" />
                    </Button>
                </form>
            </div>
        </div>
    );
};

const Messages = () => {
    const [conversations, setConversations] = useState([]);
    const [loading, setLoading] = useState(true);
    const [search, setSearch] = useState('');
    const [selectedContact, setSelectedContact] = useState(null);

    const handleMessagesRead = useCallback((otherUserId) => {
        setConversations(prev => prev.map(c =>
            c.otherUserId === otherUserId ? { ...c, unreadCount: 0 } : c
        ));
    }, []);

    const fetchConversations = async () => {
        try {
            const res = await chatApi.getConversations();
            setConversations(res.data?.data || []);
        } catch (err) {
            console.error("Failed to load conversations:", err);
            toast.error("Failed to load conversations");
        } finally {
            setLoading(false);
        }
    };

    useEffect(() => {
        fetchConversations();
        // Setting up a basic interval to refresh conversations list periodically to catch new unread counts
        const interval = setInterval(fetchConversations, 15000);
        return () => clearInterval(interval);
    }, []);

    const filteredConversations = conversations.filter(c =>
        c.otherUserName.toLowerCase().includes(search.toLowerCase())
    );

    if (loading) {
        return (
            <div className="flex flex-col items-center justify-center min-h-[60vh]">
                <Loader2 className="h-12 w-12 animate-spin text-teal-500 mb-4" />
                <p className="text-gray-500">Loading your messages...</p>
            </div>
        );
    }

    return (
        <div className="space-y-6 h-[calc(100vh-140px)] flex flex-col">
            <div className="flex justify-between items-end">
                <div>
                    <h1 className="text-3xl font-bold text-gray-900 tracking-tight">Messages</h1>
                    <p className="text-gray-500 mt-1">Chat natively with Mentors and Students</p>
                </div>
            </div>

            <div className="flex-1 min-h-0 bg-white rounded-xl shadow-sm border border-gray-100 overflow-hidden flex">
                {/* Left Sidebar: Conversations List */}
                <div className={`w-full md:w-80 border-r flex flex-col ${selectedContact ? 'hidden md:flex' : 'flex'}`}>
                    <div className="p-4 border-b bg-gray-50/50">
                        <div className="relative">
                            <Search className="absolute left-3 top-3 w-4 h-4 text-gray-400" />
                            <input
                                type="text"
                                placeholder="Search messages..."
                                className="w-full pl-9 pr-4 py-2 bg-white border border-gray-200 rounded-lg text-sm focus:outline-none focus:ring-2 focus:ring-teal-500/20 focus:border-teal-500 transition-all"
                                value={search}
                                onChange={(e) => setSearch(e.target.value)}
                            />
                        </div>
                    </div>

                    <ScrollArea className="flex-1">
                        {filteredConversations.length === 0 ? (
                            <div className="p-8 text-center text-gray-500 text-sm">
                                <MessageSquare className="w-8 h-8 mx-auto mb-3 opacity-20" />
                                No conversations found.
                            </div>
                        ) : (
                            <div className="divide-y divide-gray-50">
                                {filteredConversations.map((contact) => (
                                    <button
                                        key={contact.otherUserId}
                                        onClick={() => setSelectedContact(contact)}
                                        className={`w-full text-left p-4 hover:bg-gray-50 transition-colors flex items-center gap-3 ${selectedContact?.otherUserId === contact.otherUserId ? 'bg-teal-50/50 relative before:absolute before:left-0 before:top-0 before:bottom-0 before:w-1 before:bg-teal-500' : ''
                                            }`}
                                    >
                                        <div className="relative shrink-0">
                                            {contact.otherUserAvatar ? (
                                                <img src={`${import.meta.env.VITE_API_URL?.replace('/api', '') || 'http://localhost:5000'}${contact.otherUserAvatar}`} alt={contact.otherUserName} className="w-12 h-12 rounded-full object-cover" />
                                            ) : (
                                                <div className="w-12 h-12 rounded-full bg-teal-100 text-teal-700 flex items-center justify-center font-bold">
                                                    {contact.otherUserName.split(" ").map((n) => n[0]).join("").substring(0, 2)}
                                                </div>
                                            )}
                                        </div>

                                        <div className="flex-1 min-w-0">
                                            <div className="flex justify-between items-baseline mb-0.5">
                                                <h4 className="font-semibold text-gray-900 truncate pr-2">{contact.otherUserName}</h4>
                                                <span className="text-xs text-gray-400 whitespace-nowrap">
                                                    {new Date(contact.lastMessageTime + (!contact.lastMessageTime.endsWith('Z') ? 'Z' : '')).toLocaleDateString([], { month: 'short', day: 'numeric' })}
                                                </span>
                                            </div>
                                            <p className={`text-sm truncate ${contact.unreadCount > 0 ? 'text-gray-900 font-medium' : 'text-gray-500'}`}>
                                                {contact.lastMessage}
                                            </p>
                                        </div>

                                        {contact.unreadCount > 0 && (
                                            <div className="shrink-0 w-5 h-5 bg-teal-500 rounded-full flex items-center justify-center">
                                                <span className="text-[10px] font-bold text-white">{contact.unreadCount}</span>
                                            </div>
                                        )}
                                    </button>
                                ))}
                            </div>
                        )}
                    </ScrollArea>
                </div>

                {/* Right Content: Active Chat */}
                <div className={`flex-1 flex col ${!selectedContact ? 'hidden md:flex items-center justify-center bg-gray-50/50' : 'flex-col min-w-0'}`}>
                    {!selectedContact ? (
                        <div className="text-center text-gray-400">
                            <MessageSquare className="w-16 h-16 mx-auto mb-4 opacity-20" />
                            <p className="text-lg font-medium text-gray-600 mb-1">Your Messages</p>
                            <p className="text-sm">Select a conversation from the sidebar to chat.</p>
                        </div>
                    ) : (
                        <ChatWindow
                            contact={selectedContact}
                            onClose={() => setSelectedContact(null)}
                            onMessagesRead={handleMessagesRead}
                        />
                    )}
                </div>
            </div>
        </div>
    );
};

export default Messages;
