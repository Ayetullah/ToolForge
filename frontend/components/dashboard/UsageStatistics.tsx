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
      <h2 className="text-xl font-semibold text-gray-900 mb-4">Usage Statistics</h2>
      <div className="space-y-2">
        <p className="text-gray-900">
          <span className="font-semibold text-gray-900">Total Operations:</span>{" "}
          <span className="text-gray-800">{stats?.totalOperations || 0}</span>
        </p>
        <p className="text-gray-900">
          <span className="font-semibold text-gray-900">Total File Size:</span>{" "}
          <span className="text-gray-800">
            {stats?.totalFileSizeBytes
              ? `${(stats.totalFileSizeBytes / 1024 / 1024).toFixed(2)} MB`
              : "0 MB"}
          </span>
        </p>
        {stats?.operationsByTool &&
          Object.keys(stats.operationsByTool).length > 0 && (
            <div className="mt-4">
              <h3 className="font-medium text-gray-900 mb-2">By Tool:</h3>
              <div className="space-y-1 text-sm">
                {Object.entries(stats.operationsByTool).map(
                  ([tool, count]: [string, any]) => (
                    <div key={tool} className="flex justify-between text-gray-800">
                      <span>{tool}:</span>
                      <span className="font-medium text-gray-900">{count}</span>
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

