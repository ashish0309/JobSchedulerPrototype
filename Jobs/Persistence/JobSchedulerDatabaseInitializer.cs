using Microsoft.EntityFrameworkCore;
using System.Data;

namespace JobSchedulerPrototype.Jobs;

public static class JobSchedulerDatabaseInitializer
{
    public static void EnsureCreated(JobSchedulerDbContext db)
    {
        db.Database.EnsureCreated();
        EnsureRunAtColumn(db);
        BackfillPendingRunAt(db);
    }

    private static void EnsureRunAtColumn(JobSchedulerDbContext db)
    {
        if (!ColumnExists(db, tableName: "Jobs", columnName: nameof(JobRecord.RunAt)))
        {
            db.Database.ExecuteSqlRaw("""ALTER TABLE "Jobs" ADD COLUMN "RunAt" INTEGER NULL;""");
        }

        db.Database.ExecuteSqlRaw(
            """CREATE INDEX IF NOT EXISTS "IX_Jobs_Status_RunAt" ON "Jobs" ("Status", "RunAt");""");
    }

    private static bool ColumnExists(
        JobSchedulerDbContext db,
        string tableName,
        string columnName)
    {
        var connection = db.Database.GetDbConnection();
        var closeConnection = connection.State == ConnectionState.Closed;

        if (closeConnection)
        {
            connection.Open();
        }

        try
        {
            using var command = connection.CreateCommand();
            command.CommandText = $"""PRAGMA table_info("{tableName}");""";

            using var reader = command.ExecuteReader();
            while (reader.Read())
            {
                if (string.Equals(
                    reader.GetString(1),
                    columnName,
                    StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
            }

            return false;
        }
        finally
        {
            if (closeConnection)
            {
                connection.Close();
            }
        }
    }

    private static void BackfillPendingRunAt(JobSchedulerDbContext db)
    {
        var pendingJobs = db.Jobs
            .AsNoTracking()
            .Include(job => job.History)
            .Where(job => job.RunAt == null
                && (job.Status == JobStatus.Queued || job.Status == JobStatus.Scheduled))
            .AsEnumerable()
            .Select(job => job.WithOrderedHistory())
            .ToArray();

        foreach (var job in pendingJobs)
        {
            var runAt = job.ScheduledAt ?? job.EnqueuedAt;
            db.Database.ExecuteSqlInterpolated(
                $"""UPDATE "Jobs" SET "RunAt" = {runAt.UtcDateTime.Ticks} WHERE "Id" = {job.Id};""");
        }
    }
}
