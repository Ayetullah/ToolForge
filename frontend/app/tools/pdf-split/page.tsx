"use client";

import { useState } from "react";
import { FileText, Loader2 } from "lucide-react";
import { toolsApi } from "@/lib/api";
import ToolLayout from "@/components/tools/ToolLayout";
import FileUpload from "@/components/tools/FileUpload";
import ResultDisplay from "@/components/tools/ResultDisplay";

export default function PdfSplitPage() {
  const [file, setFile] = useState<File | null>(null);
  const [files, setFiles] = useState<File[]>([]);
  const [pagesSpec, setPagesSpec] = useState("all");
  const [uploading, setUploading] = useState(false);
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

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    if (!file) {
      setResult({ error: "Please select a PDF file" });
      return;
    }

    setUploading(true);
    setResult(null);

    try {
      const response = await toolsApi.splitPdf(file, pagesSpec);
      if (response.error) {
        setResult({ error: response.error });
      } else if (response.data) {
        const data = response.data as {
          downloadUrl?: string;
          signedDownloadUrl?: string;
        };
        setResult({
          downloadUrl: data.downloadUrl || data.signedDownloadUrl,
        });
      }
    } catch {
      setResult({ error: "Failed to split PDF. Please try again." });
    } finally {
      setUploading(false);
    }
  };

  return (
    <ToolLayout
      title="PDF Split"
      description="Split a PDF into separate pages or page ranges. Use 'all' to split all pages, or specify ranges like '1-5,10,15-20'."
    >
      <div className="bg-white rounded-lg shadow-lg p-8 max-w-4xl mx-auto">
        <form onSubmit={handleSubmit} className="space-y-6">
          <div>
            <label className="block text-sm font-medium text-gray-700 mb-2">
              Select PDF File
            </label>
            <FileUpload
              files={files}
              onFilesChange={handleFileChange}
              accept=".pdf"
              label="Choose PDF File"
            />
          </div>

          <div>
            <label className="block text-sm font-medium text-gray-700 mb-2">
              Pages Specification
            </label>
            <input
              type="text"
              value={pagesSpec}
              onChange={(e) => setPagesSpec(e.target.value)}
              placeholder="all or 1-5,10,15-20"
              className="w-full border border-gray-300 rounded-lg px-4 py-2 focus:ring-2 focus:ring-blue-500 focus:border-transparent"
            />
            <p className="text-xs text-gray-700 mt-1">
              Examples: &quot;all&quot; for all pages, &quot;1-5&quot; for pages
              1 to 5, &quot;1-5,10,15-20&quot; for multiple ranges
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
                Splitting PDF...
              </>
            ) : (
              <>
                <FileText className="w-5 h-5" />
                Split PDF
              </>
            )}
          </button>
        </form>

        <div className="mt-6">
          <ResultDisplay result={result} loading={uploading} />
        </div>

        <div className="mt-8 p-4 bg-blue-50 rounded-lg">
          <h3 className="font-semibold mb-2">How it works:</h3>
          <ol className="list-decimal list-inside space-y-1 text-sm text-gray-800">
            <li>Select a PDF file to split</li>
            <li>
              Specify which pages to extract (use &quot;all&quot; for all pages)
            </li>
            <li>Download your split PDF files as a ZIP archive</li>
          </ol>
        </div>
      </div>
    </ToolLayout>
  );
}
