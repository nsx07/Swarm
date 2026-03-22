# Swarm - Distributed ETL Orchestrator

## Phase 1 Implementation - Foundation

This is the foundation phase of the Swarm platform setup. Below is the project structure:

### Project Structure

```
Swarm/
├── Cluster/                 # ASP.NET Core orchestration service
│   ├── Data/               # EF Core DbContext and migrations
│   ├── Models/             # Domain models (Node, TaskDefinition, TaskInstance, ExecutionLog)
│   ├── Services/           # Business logic services
│   ├── Controllers/        # API endpoints
│   ├── Logging/            # Serilog configuration
│   ├── Middleware/         # API key authentication
│   ├── Program.cs          # Application startup
│   ├── appsettings.json    # Configuration
│   ├── Cluster.csproj      # Project file
│   └── Dockerfile          # Container image
│
├── Node/                    # .NET Worker Service (execution agent)
│   ├── Data/               # SQLite DbContext
│   ├── Models/             # Domain models (LocalTask, ScheduledJob)
│   ├── Services/           # Business logic services
│   ├── Logging/            # Serilog configuration
│   ├── NodeWorker.cs       # Main event loop
│   ├── Program.cs          # Application startup
│   ├── appsettings.json    # Configuration
│   ├── Node.csproj         # Project file
│   └── Dockerfile          # Container image
│
├── Frontend/               # React + TypeScript
│   ├── src/
│   │   ├── components/     # React components (NodeDashboard, TaskForm, ExecutionMonitor)
│   │   ├── services/       # API client and SSE client
│   │   ├── store/          # Zustand state management
│   │   ├── App.tsx         # Main app component
│   │   ├── main.tsx        # Entry point
│   │   └── App.css         # Styles
│   ├── package.json        # Dependencies
│   ├── vite.config.ts      # Vite configuration
│   ├── tsconfig.json       # TypeScript configuration
│   ├── index.html          # HTML template
│   └── Dockerfile          # Container image
│
├── docker-compose.yml      # Full stack orchestration
└── .gitignore             # Git ignore rules

```

### Services Overview

**Cluster (Orchestration)**

- PostgreSQL: Persistent storage (nodes, tasks, executions, logs)
- Redis: Caching for node capabilities and task metadata
- RabbitMQ: Reliable job dispatch (cluster → node) and result collection (node → cluster)
- Serilog: Structured application logging (file-based for MVP)

**Node (Execution Agent)**

- SQLite: Local task state and cron schedules
- RabbitMQ: Consume task messages and publish results/logs
- Serilog: Application logging

**Frontend (React)**

- SSE: Real-time execution log streaming
- REST API: Task management and node monitoring

### Running Locally

#### Without Docker (for development):

1. **Cluster**:

```bash
cd Cluster
dotnet restore
dotnet user-secrets init
dotnet user-secrets set "ConnectionStrings:DefaultConnection" "Host=localhost;Port=5432;Database=swarm_cluster;Username=postgres;Password=postgres"
dotnet run
```

2. **Node**:

```bash
cd Node
dotnet restore
dotnet run
```

3. **Frontend**:

```bash
cd Frontend
npm install
npm run dev
```

#### With Docker:

```bash
docker-compose up --build
```

Components will be available at:

- Cluster API: http://localhost:5000
- Frontend: http://localhost:5173
- RabbitMQ Management: http://localhost:15672 (guest/guest)

### Phase 1 Completion Checklist

- ✅ All project structures created
- ✅ Models defined with UUIDs
- ✅ Database contexts configured (PostgreSQL for Cluster, SQLite for Node)
- ✅ Serilog logging configured (file-based)
- ✅ RabbitMQ service stubs
- ✅ SSE infrastructure skeleton
- ✅ Docker Compose with all services
- ⏳ Next: Phase 2 - Node Management Layer

### Next Steps (Phase 2)

- Implement Node registration endpoint
- Implement node heartbeat and status tracking
- Implement capability discovery
- Implement health check endpoints
- Create node dashboard UI
