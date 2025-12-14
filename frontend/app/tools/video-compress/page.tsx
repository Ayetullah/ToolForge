"use client";

import { useState, useEffect } from "react";
import { Video, Loader2 } from "lucide-react";
import { toolsApi } from "@/lib/api";
import ToolLayout from "@/components/tools/ToolLayout";
import FileUpload from "@/components/tools/FileUpload";
import ResultDisplay from "@/components/tools/ResultDisplay";

export default function VideoCompressPage() {
  const [file, setFile] = useState<File | null>(null);
  const [files, setFiles] = useState<File[]>([]);
  const [crf, setCrf] = useState(23);
  const [preset, setPreset] = useState("medium");
  const [maxWidth, setMaxWidth] = useState("");
  const [maxHeight, setMaxHeight] = useState("");
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
            error: data.errorMessage || "Compression failed",
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
      setResult({ error: "Please select a video file" });
      return;
    }

    setUploading(true);
    setResult(null);

    try {
      const options: {
        crf: number;
        preset: string;
        maxWidth?: number;
        maxHeight?: number;
      } = { crf, preset };
      if (maxWidth) options.maxWidth = parseInt(maxWidth);
      if (maxHeight) options.maxHeight = parseInt(maxHeight);

      const response = await toolsApi.compressVideo(file, options);
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
      setResult({ error: "Failed to compress video. Please try again." });
      setUploading(false);
    }
  };

  return (
    <ToolLayout
      title="Video Compress"
      description="Compress video files to reduce file size. Processing happens in the background."
    >
      <div className="bg-white rounded-lg shadow-lg p-8 max-w-4xl mx-auto">
        <form onSubmit={handleSubmit} className="space-y-6">
          <div>
            <label className="block text-sm font-medium text-gray-700 mb-2">
              Select Video File
            </label>
            <FileUpload
              files={files}
              onFilesChange={handleFileChange}
              accept="video/*"
              label="Choose Video"
            />
          </div>

          <div className="grid md:grid-cols-2 gap-4">
            <div>
              <label className="block text-sm font-medium text-gray-700 mb-2">
                CRF (18-28)
              </label>
              <input
                type="range"
                min="18"
                max="28"
                value={crf}
                onChange={(e) => setCrf(parseInt(e.target.value))}
                className="w-full"
              />
              <p className="text-xs text-gray-700 mt-1 font-medium">{crf}</p>
            </div>

            <div>
              <label className="block text-sm font-medium text-gray-700 mb-2">
                Preset
              </label>
              <select
                value={preset}
                onChange={(e) => setPreset(e.target.value)}
                className="w-full border border-gray-700 rounded-lg px-4 py-2 focus:ring-2 focus:ring-blue-500"
              >
                <option value="ultrafast">Ultrafast</option>
                <option value="superfast">Superfast</option>
                <option value="veryfast">Veryfast</option>
                <option value="faster">Faster</option>
                <option value="fast">Fast</option>
                <option value="medium">Medium</option>
                <option value="slow">Slow</option>
                <option value="slower">Slower</option>
                <option value="veryslow">Veryslow</option>
              </select>
            </div>

            <div>
              <label className="block text-sm font-medium text-gray-700 mb-2">
                Max Width (px, optional)
              </label>
              <input
                type="number"
                value={maxWidth}
                onChange={(e) => setMaxWidth(e.target.value)}
                placeholder="e.g., 1920"
                className="w-full border border-gray-700 rounded-lg px-4 py-2 focus:ring-2 focus:ring-blue-500 placeholder:text-gray-500"
              />
            </div>

            <div>
              <label className="block text-sm font-medium text-gray-700 mb-2">
                Max Height (px, optional)
              </label>
              <input
                type="number"
                value={maxHeight}
                onChange={(e) => setMaxHeight(e.target.value)}
                placeholder="e.g., 1080"
                className="w-full border border-gray-700 rounded-lg px-4 py-2 focus:ring-2 focus:ring-blue-500 placeholder:text-gray-500"
              />
            </div>
          </div>

          <button
            type="submit"
            disabled={!file || uploading}
            className="w-full bg-blue-600 text-white py-3 rounded-lg font-semibold hover:bg-blue-700 disabled:bg-gray-400 disabled:cursor-not-allowed flex items-center justify-center gap-2"
          >
            {uploading ? (
              <>
                <Loader2 className="w-5 h-5 animate-spin" />
                Compressing...
              </>
            ) : (
              <>
                <Video className="w-5 h-5" />
                Compress Video
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
