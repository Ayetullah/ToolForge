"use client";

import { useState, useEffect } from "react";
import { FileText, Loader2 } from "lucide-react";
import { toolsApi } from "@/lib/api";
import ToolLayout from "@/components/tools/ToolLayout";
import FileUpload from "@/components/tools/FileUpload";
import ResultDisplay from "@/components/tools/ResultDisplay";

export default function DocToPdfPage() {
  const [file, setFile] = useState<File | null>(null);
  const [files, setFiles] = useState<File[]>([]);
  const [uploading, setUploading] = useState(false);
  const [jobId, setJobId] = useState<string | null>(null);
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

    const interval = setInterval(async () => {
      const response = await toolsApi.getJobStatus(jobId);
      if (response.data) {
        const data = response.data as {
          status?: string;
          signedDownloadUrl?: string;
          errorMessage?: string;
        };
        if (data.status === "Completed") {
          setResult({
            downloadUrl: data.signedDownloadUrl,
          });
          clearInterval(interval);
          setUploading(false);
        } else if (data.status === "Failed") {
          setResult({
            error: data.errorMessage || "Conversion failed",
          });
          clearInterval(interval);
          setUploading(false);
        }
      }
    }, 2000);

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
        const data = response.data as { jobId?: string };
        if (data.jobId) {
          setJobId(data.jobId);
        }
      }
    } catch {
      setResult({ error: "Failed to convert document. Please try again." });
      setUploading(false);
    }
  };

  return (
    <ToolLayout
      title="Document to PDF"
      description="Convert Word, Excel, PowerPoint, and other documents to PDF format."
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
              accept=".doc,.docx,.xls,.xlsx,.ppt,.pptx,.odt,.ods,.odp"
              label="Choose Document"
            />
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
        </div>
      </div>
    </ToolLayout>
  );
}
