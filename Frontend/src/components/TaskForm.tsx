import React, { useState } from "react";

interface TaskFormProps {
  onSubmit: (config: Record<string, unknown>) => void;
}

export const TaskForm: React.FC<TaskFormProps> = ({ onSubmit }) => {
  const [taskType, setTaskType] = useState("etl-generic");
  const [taskName, setTaskName] = useState("");
  const [config] = useState<Record<string, unknown>>({});

  const handleSubmit = (e: React.FormEvent) => {
    e.preventDefault();
    if (!taskName.trim()) {
      alert("Please enter a task name");
      return;
    }
    onSubmit(config);
  };

  return (
    <form onSubmit={handleSubmit} className="space-y-6">
      <div>
        <label className="label">Task Name</label>
        <input type="text" value={taskName} onChange={(e) => setTaskName(e.target.value)} className="input-field" placeholder="e.g., Daily Data Sync" />
      </div>

      <div>
        <label className="label">Task Type</label>
        <select value={taskType} onChange={(e) => setTaskType(e.target.value)} className="input-field">
          <option value="etl-generic">ETL Generic (Extract, Transform, Load)</option>
        </select>
      </div>

      {/* TODO: Implement dynamic form rendering based on task type schema */}
      <div className="bg-blue-50 border border-blue-200 rounded-lg p-4">
        <p className="text-sm text-blue-800">Additional configuration fields will appear based on selected task type</p>
      </div>

      <div className="flex gap-3">
        <button type="submit" className="btn-primary">
          Create Task
        </button>
        <button type="reset" className="btn-secondary">
          Clear Form
        </button>
      </div>
    </form>
  );
};
