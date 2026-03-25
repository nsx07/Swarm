CREATE TABLE IF NOT EXISTS "LocalTasks" (
    "Id" TEXT NOT NULL PRIMARY KEY
        DEFAULT (lower(hex(randomblob(16)))),
    "ClusterTaskId" TEXT NOT NULL,
    "ConfigJson" TEXT NOT NULL,
    "Status" TEXT NOT NULL,
    "CreatedAt" TEXT NOT NULL,
    "CompletedAt" TEXT NULL
);

CREATE INDEX IF NOT EXISTS "IX_LocalTasks_ClusterTaskId"
ON "LocalTasks" ("ClusterTaskId");

CREATE INDEX IF NOT EXISTS "IX_LocalTasks_Status"
ON "LocalTasks" ("Status");

CREATE TABLE IF NOT EXISTS "ScheduledJobs" (
    "Id" TEXT NOT NULL PRIMARY KEY
        DEFAULT (lower(hex(randomblob(16)))),
    "TaskDefinitionId" TEXT NOT NULL,
    "CronExpression" TEXT NOT NULL,
    "NextRunAt" TEXT NULL,
    "CreatedAt" TEXT NOT NULL,
    "IsActive" INTEGER NOT NULL DEFAULT 1
);

CREATE INDEX IF NOT EXISTS "IX_ScheduledJobs_TaskDefinitionId"
ON "ScheduledJobs" ("TaskDefinitionId");

CREATE INDEX IF NOT EXISTS "IX_ScheduledJobs_NextRunAt"
ON "ScheduledJobs" ("NextRunAt");

CREATE TABLE IF NOT EXISTS "Configuration" (
    "Registered" BOOLEAN NOT NULL DEFAULT 0,
    "NodeName" TEXT,
    "NodeId" TEXT
);

CREATE TABLE IF NOT EXISTS "RemoteParameters" (
    "NodeId" TEXT NOT NULL PRIMARY KEY,
    "QueueHost" TEXT,
    "QueuePort" TEXT,
    "QueueUserName" TEXT,
    "QueuePassword" TEXT
);