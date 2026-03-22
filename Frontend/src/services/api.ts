import axios, { AxiosInstance } from "axios";

const BASE_URL = (import.meta as any).env.VITE_API_URL || "http://localhost:5000/api";

class ApiClient {
  private client: AxiosInstance;

  constructor() {
    this.client = axios.create({
      baseURL: BASE_URL,
      headers: {
        "Content-Type": "application/json",
      },
    });
  }

  setApiKey(apiKey: string) {
    this.client.defaults.headers.common["X-API-Key"] = apiKey;
  }

  // Nodes
  async getNodes() {
    const response = await this.client.get("/nodes");
    return response.data;
  }

  async getNode(id: string) {
    const response = await this.client.get(`/nodes/${id}`);
    return response.data;
  }

  async registerNode(capabilities: Record<string, unknown>) {
    const response = await this.client.post("/nodes/register", { capabilities });
    return response.data;
  }

  // Tasks
  async getTasks() {
    const response = await this.client.get("/tasks");
    return response.data;
  }

  async getTask(id: string) {
    const response = await this.client.get(`/tasks/${id}`);
    return response.data;
  }

  async createTask(name: string, taskType: string, config: Record<string, unknown>) {
    const response = await this.client.post("/tasks", { name, taskType, configJson: JSON.stringify(config) });
    return response.data;
  }

  async getTaskTypes() {
    const response = await this.client.get("/tasks/types");
    return response.data;
  }

  // Executions/Runs
  async executeTask(taskId: string) {
    const response = await this.client.post(`/tasks/${taskId}/execute`);
    return response.data;
  }

  async getRuns(page: number = 1, pageSize: number = 20) {
    const response = await this.client.get("/runs", { params: { page, pageSize } });
    return response.data;
  }

  async getRun(id: string) {
    const response = await this.client.get(`/runs/${id}`);
    return response.data;
  }

  async getRunLogs(runId: string) {
    const response = await this.client.get(`/runs/${runId}/logs`);
    return response.data;
  }
}

export const apiClient = new ApiClient();
