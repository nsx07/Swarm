import React, { useEffect, useState } from "react";
import { sseClient, LogMessage } from "../services/sse";

interface ExecutionMonitorProps {
  runId: string;
}

const LOG_LEVEL_COLORS: Record<string, string> = {
  INFO: "bg-blue-50 border-l-4 border-blue-500 text-blue-900",
  WARNING: "bg-yellow-50 border-l-4 border-yellow-500 text-yellow-900",
  ERROR: "bg-red-50 border-l-4 border-red-500 text-red-900",
  CRITICAL: "bg-red-100 border-l-4 border-red-700 text-red-900",
};

const LOG_LEVEL_BADGES: Record<string, string> = {
  INFO: "bg-blue-100 text-blue-800",
  WARNING: "bg-yellow-100 text-yellow-800",
  ERROR: "bg-red-100 text-red-800",
  CRITICAL: "bg-red-200 text-red-900",
};

export const ExecutionMonitor: React.FC<ExecutionMonitorProps> = ({ runId }) => {
  const [logs, setLogs] = useState<LogMessage[]>([]);
  const [connected, setConnected] = useState(false);

  useEffect(() => {
    sseClient.connect(
      runId,
      (log) => {
        setLogs((prev) => [...prev, log]);
      },
      () => {
        setConnected(false);
      },
    );

    setConnected(sseClient.isConnected());

    return () => {
      sseClient.disconnect();
    };
  }, [runId]);

  return (
    <div className="space-y-4">
      <div className="flex items-center gap-4">
        <h2 className="text-xl font-bold text-gray-900">Execution Logs</h2>
        <div className={`flex items-center gap-2 px-3 py-1 rounded-full text-sm font-medium ${connected ? "bg-green-100 text-green-800" : "bg-gray-200 text-gray-800"}`}>
          <span className={`w-2 h-2 rounded-full ${connected ? "bg-green-600 animate-pulse" : "bg-gray-600"}`}></span>
          {connected ? "Connected" : "Disconnected"}
        </div>
      </div>

      <div className="bg-white border border-gray-200 rounded-lg overflow-hidden">
        <div className="h-96 overflow-y-auto bg-gray-50 font-mono text-sm">
          {logs.length === 0 ? (
            <div className="p-4 text-gray-500 text-center">Waiting for execution logs...</div>
          ) : (
            logs.map((log) => (
              <div key={log.id} className={`p-4 border-b border-gray-200 ${LOG_LEVEL_COLORS[log.level] || "bg-white"}`}>
                <div className="flex items-start gap-3">
                  <time className="text-xs text-gray-600 flex-shrink-0">{new Date(log.timestamp).toLocaleTimeString()}</time>
                  <span className={`px-2 py-1 text-xs font-bold rounded ${LOG_LEVEL_BADGES[log.level] || "bg-gray-100 text-gray-800"}`}>{log.level}</span>
                  {log.phase && <span className="text-xs bg-purple-100 text-purple-800 px-2 py-1 rounded">{log.phase}</span>}
                  <div className="flex flex-col flex-1">
                    <p className="break-words">{log.message}</p>
                  </div>
                </div>
              </div>
            ))
          )}
        </div>
      </div>

      {logs.length > 0 && <p className="text-xs text-gray-600">Showing {logs.length} log entries</p>}
    </div>
  );
};
