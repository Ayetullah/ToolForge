"use client";

interface UsageStatisticsProps {
  stats: {
    totalOperations?: number;
    totalFileSizeBytes?: number;
    operationsByTool?: Record<string, number>;
  } | null;
}

export default function UsageStatistics({ stats }: UsageStatisticsProps) {
  return (
    <div className="bg-white rounded-lg shadow p-6">
      <h2 className="text-xl font-semibold mb-4">Usage Statistics</h2>
      <div className="space-y-2">
        <p>
          <span className="font-medium">Total Operations:</span>{" "}
          {stats?.totalOperations || 0}
        </p>
        <p>
          <span className="font-medium">Total File Size:</span>{" "}
          {stats?.totalFileSizeBytes
            ? `${(stats.totalFileSizeBytes / 1024 / 1024).toFixed(2)} MB`
            : "0 MB"}
        </p>
        {stats?.operationsByTool &&
          Object.keys(stats.operationsByTool).length > 0 && (
            <div className="mt-4">
              <h3 className="font-medium mb-2">By Tool:</h3>
              <div className="space-y-1 text-sm">
                {Object.entries(stats.operationsByTool).map(
                  ([tool, count]: [string, any]) => (
                    <div key={tool} className="flex justify-between">
                      <span>{tool}:</span>
                      <span className="font-medium">{count}</span>
                    </div>
                  )
                )}
              </div>
            </div>
          )}
      </div>
    </div>
  );
}

