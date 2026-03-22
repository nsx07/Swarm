import React from "react";

export const NodeDashboard: React.FC = () => {
  // TODO: Implement node list and monitoring
  return (
    <div>
      <div className="overflow-x-auto">
        <table className="min-w-full divide-y divide-gray-200">
          <thead className="bg-gray-100">
            <tr>
              <th className="px-6 py-3 text-left text-xs font-medium text-gray-900 uppercase tracking-wider">Name</th>
              <th className="px-6 py-3 text-left text-xs font-medium text-gray-900 uppercase tracking-wider">Status</th>
              <th className="px-6 py-3 text-left text-xs font-medium text-gray-900 uppercase tracking-wider">Capabilities</th>
              <th className="px-6 py-3 text-left text-xs font-medium text-gray-900 uppercase tracking-wider">Last Seen</th>
            </tr>
          </thead>
          <tbody className="bg-white divide-y divide-gray-200">
            <tr>
              <td colSpan={4} className="px-6 py-4 text-center text-gray-500">
                No nodes registered yet
              </td>
            </tr>
          </tbody>
        </table>
      </div>
      <p className="text-sm text-gray-600 mt-4">Nodes will appear here once they register with the cluster</p>
    </div>
  );
};
