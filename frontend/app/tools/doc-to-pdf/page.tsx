"use client";

import { useState, useEffect } from "react";
import { FileText, Loader2 } from "lucide-react";
import { toolsApi } from "@/lib/api";
import ToolLayout from "@/components/tools/ToolLayout";
import FileUpload from "@/components/tools/FileUpload";
import ResultDisplay from "@/components/tools/ResultDisplay";

// Job status enum matching backend JobStatus enum (int values)
enum JobStatus {
  Pending = 0,
  Processing = 1,
  Completed = 2,
  Failed = 3,
  Cancelled = 4,
  NotFound = -1, // Special case for not found
  Unauthorized = -2, // Special case for unauthorized
}

export default function DocToPdfPage() {
  const [file, setFile] = useState<File | null>(null);
  const [files, setFiles] = useState<File[]>([]);
  const [uploading, setUploading] = useState(false);
  const [jobId, setJobId] = useState<string | null>(null);
  const [jobStatus, setJobStatus] = useState<string | null>(null);
  const [result, setResult] = useState<{
    downloadUrl?: string;
    signedDownloadUrl?: string;
    error?: string;
  } | null>(null);

  const handleFileChange = (newFiles: File[]) => {
    if (newFiles.length > 0) {
      const selectedFile = newFiles[0];
      setFile(selectedFile);
      setFiles([selectedFile]);
    } else {
      setFile(null);
      setFiles([]);
    }
  };

  useEffect(() => {
    if (!jobId) return;

    let pollCount = 0;
    const maxPolls = 300; // 10 minutes max (300 * 2 seconds)
    const pollInterval = 2000; // 2 seconds

    const interval = setInterval(async () => {
      pollCount++;

      // Timeout after max polls
      if (pollCount >= maxPolls) {
        clearInterval(interval);
        setUploading(false);
        setJobStatus(null);
        setResult({
          error: "Conversion timed out. Please try again or contact support.",
        });
        return;
      }

      try {
        const response = await toolsApi.getJobStatus(jobId);
        if (response.error) {
          clearInterval(interval);
          setUploading(false);
          setJobStatus(null);
          setResult({
            error: response.error || "Failed to get job status",
          });
          return;
        }

        if (response.data) {
          const data = response.data as {
            status?: number; // int enum value
            statusName?: string; // Human-readable name
            signedDownloadUrl?: string;
            errorMessage?: string;
          };

          // Update job status for debugging (use statusName for display)
          if (data.statusName) {
            setJobStatus(data.statusName);
          }

          // Get status as int (enum value)
          const statusValue = data.status as JobStatus;

          // Use enum for type-safe comparison
          switch (statusValue) {
            case JobStatus.Completed:
              clearInterval(interval);
              setJobStatus(null);
              setUploading(false);
              setResult({
                downloadUrl: data.signedDownloadUrl,
                signedDownloadUrl: data.signedDownloadUrl,
              });
              break;

            case JobStatus.Failed:
              clearInterval(interval);
              setJobStatus(null);
              setUploading(false);
              setResult({
                error: data.errorMessage || "Conversion failed",
              });
              break;

            case JobStatus.NotFound:
            case JobStatus.Unauthorized:
              clearInterval(interval);
              setJobStatus(null);
              setUploading(false);
              setResult({
                error:
                  statusValue === JobStatus.Unauthorized
                    ? "You do not have access to this job"
                    : "Job not found. It may have been deleted or expired.",
              });
              break;

            case JobStatus.Pending:
            case JobStatus.Processing:
            case JobStatus.Cancelled:
            default:
              // Continue polling for pending, processing, cancelled, etc.
              break;
          }
        }
      } catch (error) {
        // On error, continue polling but log it
        if (pollCount % 10 === 0) {
          // Log every 20 seconds if there's an error
          console.error("Error polling job status:", error);
        }
      }
    }, pollInterval);

    return () => clearInterval(interval);
  }, [jobId]);

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    if (!file) {
      setResult({ error: "Please select a document file" });
      return;
    }

    setUploading(true);
    setResult(null);

    try {
      const response = await toolsApi.convertDocToPdf(file);
      if (response.error) {
        setResult({ error: response.error });
        setUploading(false);
      } else if (response.data) {
        const data = response.data as {
          jobId?: string;
          status?: string;
          errorMessage?: string;
        };

        // Check if there's an error message (e.g., daily limit exceeded)
        if (data.errorMessage) {
          setResult({ error: data.errorMessage });
          setUploading(false);
        } else if (data.jobId) {
          setJobId(data.jobId);
        } else if (data.status === "failed") {
          setResult({
            error: data.errorMessage || "Document conversion failed",
          });
          setUploading(false);
        }
      }
    } catch (error) {
      const errorMessage =
        (
          error as {
            response?: { data?: { errorMessage?: string } };
            message?: string;
          }
        )?.response?.data?.errorMessage ||
        (error as { message?: string })?.message ||
        "Failed to convert document. Please try again.";
      setResult({ error: errorMessage });
      setUploading(false);
    }
  };

  return (
    <ToolLayout
      title="Document to PDF"
      description="Convert Word (.docx, .doc) and Excel (.xlsx, .xls, .csv) documents to PDF format. PowerPoint conversion is not currently supported."
    >
      <div className="bg-white rounded-lg shadow-lg p-8 max-w-4xl mx-auto">
        <form onSubmit={handleSubmit} className="space-y-6">
          <div>
            <label className="block text-sm font-medium text-gray-700 mb-2">
              Select Document File
            </label>
            <FileUpload
              files={files}
              onFilesChange={handleFileChange}
              accept=".doc,.docx,.xls,.xlsx,.csv"
              label="Choose Document"
            />
            <p className="mt-2 text-sm text-gray-700">
              <span className="font-semibold text-gray-900">
                Supported formats:
              </span>{" "}
              Word (.docx, .doc), Excel (.xlsx, .xls, .csv)
            </p>
          </div>

          <button
            type="submit"
            disabled={!file || uploading}
            className="w-full bg-blue-600 text-white py-3 rounded-lg font-semibold hover:bg-blue-700 disabled:bg-gray-400 disabled:cursor-not-allowed flex items-center justify-center gap-2"
          >
            {uploading ? (
              <>
                <Loader2 className="w-5 h-5 animate-spin" />
                Converting...
              </>
            ) : (
              <>
                <FileText className="w-5 h-5" />
                Convert to PDF
              </>
            )}
          </button>
        </form>

        <div className="mt-6">
          <ResultDisplay result={result} loading={uploading} />
          {jobStatus &&
            jobStatus.toLowerCase() !== "completed" &&
            jobStatus.toLowerCase() !== "failed" && (
              <div className="mt-4 text-sm text-gray-800">
                Job Status:{" "}
                <span className="font-semibold capitalize text-gray-900">
                  {jobStatus}
                </span>
              </div>
            )}
        </div>
      </div>
    </ToolLayout>
  );
}
