"use client";

import { useState, useEffect } from "react";
import { Image as ImageIcon, Loader2 } from "lucide-react";
import { toolsApi } from "@/lib/api";
import ToolLayout from "@/components/tools/ToolLayout";
import FileUpload from "@/components/tools/FileUpload";
import ResultDisplay from "@/components/tools/ResultDisplay";

export default function RemoveBackgroundPage() {
  const [file, setFile] = useState<File | null>(null);
  const [files, setFiles] = useState<File[]>([]);
  const [transparent, setTransparent] = useState(true);
  const [backgroundColor, setBackgroundColor] = useState("");
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
            error: data.errorMessage || "Background removal failed",
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
      setResult({ error: "Please select an image file" });
      return;
    }

    setUploading(true);
    setResult(null);

    try {
      const response = await toolsApi.removeBackground(file, {
        transparent,
        backgroundColor: backgroundColor || undefined,
      });
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
      setResult({ error: "Failed to remove background. Please try again." });
      setUploading(false);
    }
  };

  return (
    <ToolLayout
      title="Remove Background"
      description="Remove backgrounds from images using AI. Premium feature - requires Pro subscription."
    >
      <div className="bg-white rounded-lg shadow-lg p-8 max-w-4xl mx-auto">
        <div className="mb-4">
          <span className="bg-yellow-100 text-yellow-800 text-xs font-semibold px-2 py-1 rounded">
            Premium Feature
          </span>
        </div>

        <form onSubmit={handleSubmit} className="space-y-6">
          <div>
            <label className="block text-sm font-medium text-gray-700 mb-2">
              Select Image File
            </label>
            <FileUpload
              files={files}
              onFilesChange={handleFileChange}
              accept="image/*"
              label="Choose Image"
            />
          </div>

          <div>
            <label className="flex items-center gap-2">
              <input
                type="checkbox"
                checked={transparent}
                onChange={(e) => setTransparent(e.target.checked)}
              />
              <span>Transparent background</span>
            </label>
          </div>

          {!transparent && (
            <div>
              <label className="block text-sm font-medium text-gray-700 mb-2">
                Background Color (hex code)
              </label>
              <input
                type="text"
                value={backgroundColor}
                onChange={(e) => setBackgroundColor(e.target.value)}
                placeholder="#FFFFFF"
                className="w-full border border-gray-300 rounded-lg px-4 py-2 focus:ring-2 focus:ring-blue-500"
              />
            </div>
          )}

          <button
            type="submit"
            disabled={!file || uploading}
            className="w-full bg-blue-600 text-white py-3 rounded-lg font-semibold hover:bg-blue-700 disabled:bg-gray-400 disabled:cursor-not-allowed flex items-center justify-center gap-2"
          >
            {uploading ? (
              <>
                <Loader2 className="w-5 h-5 animate-spin" />
                Processing...
              </>
            ) : (
              <>
                <ImageIcon className="w-5 h-5" />
                Remove Background
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
