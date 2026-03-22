import { NodeDashboard } from "./components/NodeDashboard";
import { TaskForm } from "./components/TaskForm";

function App() {
  return (
    <div className="min-h-screen bg-gray-50 flex flex-col">
      <header className="bg-gradient-to-r from-blue-600 to-blue-800 text-white shadow-lg">
        <div className="max-w-7xl mx-auto px-6 py-6">
          <h1 className="text-3xl font-bold">Swarm ETL Orchestrator</h1>
          <p className="text-blue-100 mt-1">Distributed data orchestration platform</p>
        </div>
      </header>

      <main className="flex-1 max-w-7xl w-full mx-auto px-6 py-8">
        <div className="grid grid-cols-1 lg:grid-cols-3 gap-6">
          <aside className="lg:col-span-1">
            <nav className="card">
              <h2 className="text-lg font-semibold mb-4 text-gray-900">Navigation</h2>
              <ul className="space-y-2">
                <li>
                  <a href="#" className="text-blue-600 hover:text-blue-800">
                    Nodes
                  </a>
                </li>
                <li>
                  <a href="#" className="text-blue-600 hover:text-blue-800">
                    Tasks
                  </a>
                </li>
                <li>
                  <a href="#" className="text-blue-600 hover:text-blue-800">
                    Executions
                  </a>
                </li>
              </ul>
            </nav>
          </aside>

          <section className="lg:col-span-2 space-y-6">
            <div className="card">
              <h2 className="text-2xl font-bold text-gray-900 mb-4">Node Monitoring</h2>
              <NodeDashboard />
            </div>

            <div className="card">
              <h2 className="text-2xl font-bold text-gray-900 mb-4">Create Task</h2>
              <TaskForm onSubmit={(config) => console.log("Task config:", config)} />
            </div>
          </section>
        </div>
      </main>
    </div>
  );
}

export default App;
