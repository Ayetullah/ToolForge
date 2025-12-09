"use client";

import { useState } from "react";
import { FileText, Loader2 } from "lucide-react";
import { toolsApi } from "@/lib/api";
import ToolLayout from "@/components/tools/ToolLayout";
import FileUpload from "@/components/tools/FileUpload";
import ResultDisplay from "@/components/tools/ResultDisplay";

export default function PdfMergePage() {
  const [files, setFiles] = useState<File[]>([]);
  const [uploading, setUploading] = useState(false);
  const [result, setResult] = useState<any>(null);

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    if (files.length < 2) {
      setResult({ error: "Please select at least 2 PDF files" });
      return;
    }

    setUploading(true);
    setResult(null);

    try {
      const response = await toolsApi.mergePdf(files);
      if (response.error) {
        setResult({ error: response.error });
      } else if (response.data) {
        setResult({
          downloadUrl:
            response.data.downloadUrl || response.data.signedDownloadUrl,
        });
      }
    } catch (error) {
      setResult({ error: "Failed to merge PDFs. Please try again." });
    } finally {
      setUploading(false);
    }
  };

  return (
    <ToolLayout
      title="PDF Merge"
      description="Combine multiple PDF files into one. Select 2-20 PDF files to merge."
    >
      <div className="bg-white rounded-lg shadow-lg p-8 max-w-4xl mx-auto">
        <form onSubmit={handleSubmit} className="space-y-6">
          <div>
            <label className="block text-sm font-medium text-gray-700 mb-2">
              Select PDF Files (2-20 files)
            </label>
            <FileUpload
              files={files}
              onFilesChange={setFiles}
              accept=".pdf"
              multiple
              maxFiles={20}
              label="Choose PDF Files"
            />
          </div>

          <button
            type="submit"
            disabled={files.length < 2 || uploading}
            className="w-full bg-blue-600 text-white py-3 rounded-lg font-semibold hover:bg-blue-700 disabled:bg-gray-400 disabled:cursor-not-allowed flex items-center justify-center gap-2"
          >
            {uploading ? (
              <>
                <Loader2 className="w-5 h-5 animate-spin" />
                Merging PDFs...
              </>
            ) : (
              <>
                <FileText className="w-5 h-5" />
                Merge PDFs
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
            <li>Select 2-20 PDF files to merge</li>
            <li>Files are processed in the order you select them</li>
            <li>Download your merged PDF file</li>
          </ol>
        </div>
      </div>
    </ToolLayout>
  );
}
