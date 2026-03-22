import { create } from "zustand";

interface Node {
  id: string;
  name: string;
  status: string;
  capabilitiesJson: string;
  lastHeartbeatAt: string;
}

interface Task {
  id: string;
  name: string;
  taskType: string;
  schemaJson: string;
  createdAt: string;
}

interface Run {
  id: string;
  taskDefinitionId: string;
  nodeId: string;
  status: string;
  rowsProcessed: number;
  errorCount: number;
  durationMs: number;
  createdAt: string;
  completedAt?: string;
}

interface StoreState {
  nodes: Node[];
  tasks: Task[];
  runs: Run[];
  selectedNode: Node | null;
  selectedTask: Task | null;
  selectedRun: Run | null;

  setNodes: (nodes: Node[]) => void;
  setTasks: (tasks: Task[]) => void;
  setRuns: (runs: Run[]) => void;
  setSelectedNode: (node: Node | null) => void;
  setSelectedTask: (task: Task | null) => void;
  setSelectedRun: (run: Run | null) => void;
}

export const useStore = create<StoreState>((set) => ({
  nodes: [],
  tasks: [],
  runs: [],
  selectedNode: null,
  selectedTask: null,
  selectedRun: null,

  setNodes: (nodes) => set({ nodes }),
  setTasks: (tasks) => set({ tasks }),
  setRuns: (runs) => set({ runs }),
  setSelectedNode: (node) => set({ selectedNode: node }),
  setSelectedTask: (task) => set({ selectedTask: task }),
  setSelectedRun: (run) => set({ selectedRun: run }),
}));
