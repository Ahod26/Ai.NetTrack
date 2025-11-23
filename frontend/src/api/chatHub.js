import * as SignalR from "@microsoft/signalr";
import { API_BASE_URL, API_ENDPOINTS } from "./config";

class ChatHubService {
  constructor() {
    this.connection = null;
    this.isConnected = false;
    this.fullMessageHandlers = new Set();
    this.chunkMessageHandlers = new Set();
    this.chatJoinedHandlers = new Set();
    this.errorHandlers = new Set();
  }

  async initializeConnection() {
    if (this.connection) {
      return;
    }

    this.connection = new SignalR.HubConnectionBuilder()
      .withUrl(`${API_BASE_URL}${API_ENDPOINTS.CHAT.CHAT_HUB}`, {
        withCredentials: true,
      })
      .withAutomaticReconnect()
      .build();

    this.connection.on("ReceiveMessage", (message) => {
      if (message.isChunkMessage) {
        this.chunkMessageHandlers.forEach((handler) => handler(message));
      } else {
        this.fullMessageHandlers.forEach((handler) => handler(message));
      }
    });

    this.connection.on("ChatJoined", (chatId, title, messages) => {
      this.chatJoinedHandlers.forEach((handler) =>
        handler(chatId, title, messages)
      );
    });

    this.connection.on("Error", (error) => {
      this.errorHandlers.forEach((handler) => handler(error));
    });

    this.connection.onclose(() => {
      this.isConnected = false;
    });

    this.connection.onreconnected(() => {
      this.isConnected = true;
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
      }
    } catch (err) {
      this.isConnected = false;
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
    }
  }

  async sendMessage(chatId, content) {
    try {
      if (!this.isConnected) {
        throw new Error("Connection not established");
      }
      await this.connection.invoke("SendMessage", chatId, content);
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
    } catch (err) {
      console.error("Failed to join chat:", err);
      throw err;
    }
  }

  async stopGeneration(chatId) {
    try {
      if (!this.isConnected) {
        throw new Error("Connection not established");
      }
      await this.connection.invoke("StopGeneration", chatId);
    } catch (err) {
      console.error("Failed to stop generation:", err);
      throw err;
    }
  }

  // Event handlers
  onFullMessageReceived(handler) {
    this.fullMessageHandlers.add(handler);
    return () => this.fullMessageHandlers.delete(handler);
  }

  onChunkMessageReceived(handler) {
    this.chunkMessageHandlers.add(handler);
    return () => this.chunkMessageHandlers.delete(handler);
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

const chatHubService = new ChatHubService();
export default chatHubService;
