"use client";

import { useState } from "react";
import { Image as ImageIcon, Loader2 } from "lucide-react";
import { toolsApi } from "@/lib/api";
import ToolLayout from "@/components/tools/ToolLayout";
import FileUpload from "@/components/tools/FileUpload";
import ResultDisplay from "@/components/tools/ResultDisplay";

export default function ImageCompressPage() {
  const [file, setFile] = useState<File | null>(null);
  const [files, setFiles] = useState<File[]>([]);
  const [quality, setQuality] = useState(80);
  const [targetFormat, setTargetFormat] = useState("jpg");
  const [maxWidth, setMaxWidth] = useState("");
  const [maxHeight, setMaxHeight] = useState("");
  const [uploading, setUploading] = useState(false);
  const [result, setResult] = useState<{
    downloadUrl?: string;
    signedDownloadUrl?: string;
    error?: string;
    compressionRatio?: number;
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

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    if (!file) {
      setResult({ error: "Please select an image file" });
      return;
    }

    setUploading(true);
    setResult(null);

    try {
      const options: {
        quality: number;
        targetFormat: string;
        maxWidth?: number;
        maxHeight?: number;
      } = {
        quality,
        targetFormat,
      };
      if (maxWidth) options.maxWidth = parseInt(maxWidth);
      if (maxHeight) options.maxHeight = parseInt(maxHeight);

      const response = await toolsApi.compressImage(file, options);
      if (response.error) {
        setResult({ error: response.error });
      } else if (response.data) {
        const data = response.data as {
          downloadUrl?: string;
          signedDownloadUrl?: string;
          compressionRatio?: number;
        };
        setResult({
          downloadUrl: data.downloadUrl || data.signedDownloadUrl,
          compressionRatio: data.compressionRatio,
        });
      }
    } catch {
      setResult({ error: "Failed to compress image. Please try again." });
    } finally {
      setUploading(false);
    }
  };

  return (
    <ToolLayout
      title="Image Compress"
      description="Reduce image file size while maintaining quality. Supports JPG, PNG, and WEBP formats."
    >
      <div className="bg-white rounded-lg shadow-lg p-8 max-w-4xl mx-auto">
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

          <div className="grid md:grid-cols-2 gap-4">
            <div>
              <label className="block text-sm font-medium text-gray-700 mb-2">
                Quality (1-100)
              </label>
              <input
                type="range"
                min="1"
                max="100"
                value={quality}
                onChange={(e) => setQuality(parseInt(e.target.value))}
                className="w-full"
              />
              <p className="text-xs text-gray-700 mt-1 font-medium">{quality}%</p>
            </div>

            <div>
              <label className="block text-sm font-medium text-gray-700 mb-2">
                Output Format
              </label>
              <select
                value={targetFormat}
                onChange={(e) => setTargetFormat(e.target.value)}
                className="w-full border border-gray-300 rounded-lg px-4 py-2 focus:ring-2 focus:ring-blue-500"
              >
                <option value="jpg">JPG</option>
                <option value="png">PNG</option>
                <option value="webp">WEBP</option>
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
                className="w-full border border-gray-300 rounded-lg px-4 py-2 focus:ring-2 focus:ring-blue-500"
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
                className="w-full border border-gray-300 rounded-lg px-4 py-2 focus:ring-2 focus:ring-blue-500"
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
                <ImageIcon className="w-5 h-5" />
                Compress Image
              </>
            )}
          </button>
        </form>

        <div className="mt-6">
          <ResultDisplay result={result} loading={uploading} />
        </div>

        <div className="mt-8 p-4 bg-blue-50 rounded-lg">
          <h3 className="font-semibold mb-2">Tips:</h3>
          <ul className="list-disc list-inside space-y-1 text-sm text-gray-800">
            <li>
              Lower quality = smaller file size, but may reduce image quality
            </li>
            <li>WEBP format provides the best compression</li>
            <li>Set max width/height to resize large images</li>
          </ul>
        </div>
      </div>
    </ToolLayout>
  );
}
