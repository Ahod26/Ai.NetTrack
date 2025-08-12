import * as SignalR from "@microsoft/signalr";
import { API_BASE_URL, API_ENDPOINTS } from "./config";

class ChatHubService {
  constructor() {
    this.connection = null;
    this.isConnected = false;
    this.messageHandlers = new Set();
    this.chatJoinedHandlers = new Set();
    this.errorHandlers = new Set();
  }

  async initializeConnection() {
    if (this.connection) {
      return;
    }

    this.connection = new SignalR.HubConnectionBuilder()
      .withUrl(`${API_BASE_URL}${API_ENDPOINTS.CHAT.CHATHUB}`, {
        withCredentials: true,
      })
      .withAutomaticReconnect()
      .build();

    // Set up event handlers
    this.connection.on("ReceiveMessage", (message) => {
      console.log("Received message:", message);
      this.messageHandlers.forEach((handler) => handler(message));
    });

    this.connection.on("ChatJoined", (chatId, title, messages) => {
      console.log("Chat joined:", { chatId, title, messages });
      this.chatJoinedHandlers.forEach((handler) =>
        handler(chatId, title, messages)
      );
    });

    this.connection.on("Error", (error) => {
      console.error("Hub error:", error);
      this.errorHandlers.forEach((handler) => handler(error));
    });

    this.connection.onclose(() => {
      this.isConnected = false;
      console.log("SignalR connection closed");
    });

    this.connection.onreconnected(() => {
      this.isConnected = true;
      console.log("SignalR connection reconnected");
    });
  }

  async startConnection() {
    try {
      if (!this.connection) {
        await this.initializeConnection();
      }

      if (this.connection.state === SignalR.HubConnectionState.Disconnected) {
        await this.connection.start();
        this.isConnected = true;
        console.log("SignalR connection established successfully");
      }
    } catch (err) {
      this.isConnected = false;
      console.error("Failed to start SignalR connection:", err);
      throw err;
    }
  }

  async stopConnection() {
    if (
      this.connection &&
      this.connection.state !== SignalR.HubConnectionState.Disconnected
    ) {
      await this.connection.stop();
      this.isConnected = false;
      console.log("SignalR connection stopped");
    }
  }

  async sendMessage(chatId, content) {
    try {
      if (!this.isConnected) {
        throw new Error("Connection not established");
      }
      await this.connection.invoke("SendMessage", chatId, content);
      console.log("Message sent successfully");
    } catch (err) {
      console.error("Failed to send message:", err);
      throw err;
    }
  }

  async joinChat(chatId) {
    try {
      if (!this.isConnected) {
        throw new Error("Connection not established");
      }
      await this.connection.invoke("JoinChat", chatId);
      console.log("Joined chat successfully");
    } catch (err) {
      console.error("Failed to join chat:", err);
      throw err;
    }
  }

  // Event handler management
  onMessageReceived(handler) {
    this.messageHandlers.add(handler);
    return () => this.messageHandlers.delete(handler);
  }

  onChatJoined(handler) {
    this.chatJoinedHandlers.add(handler);
    return () => this.chatJoinedHandlers.delete(handler);
  }

  onError(handler) {
    this.errorHandlers.add(handler);
    return () => this.errorHandlers.delete(handler);
  }

  getConnectionState() {
    return this.connection
      ? this.connection.state
      : SignalR.HubConnectionState.Disconnected;
  }
}

// Create singleton instance
const chatHubService = new ChatHubService();

export default chatHubService;
