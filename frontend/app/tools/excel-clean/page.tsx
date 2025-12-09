"use client";

import { useState } from "react";
import { FileSpreadsheet, Loader2 } from "lucide-react";
import { toolsApi } from "@/lib/api";
import ToolLayout from "@/components/tools/ToolLayout";
import FileUpload from "@/components/tools/FileUpload";
import ResultDisplay from "@/components/tools/ResultDisplay";

export default function ExcelCleanPage() {
  const [file, setFile] = useState<File | null>(null);
  const [files, setFiles] = useState<File[]>([]);
  const [options, setOptions] = useState({
    removeEmptyRows: true,
    removeEmptyColumns: true,
    trimWhitespace: true,
    removeDuplicates: false,
    standardizeFormats: true,
    outputFormat: "xlsx",
  });
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
      setResult({ error: "Please select an Excel file" });
      return;
    }

    setUploading(true);
    setResult(null);

    try {
      const response = await toolsApi.cleanExcel(file, options);
      if (response.error) {
        setResult({ error: response.error });
      } else if (response.data) {
        setResult({
          downloadUrl:
            (
              response.data as {
                downloadUrl?: string;
                signedDownloadUrl?: string;
              }
            ).downloadUrl ||
            (
              response.data as {
                downloadUrl?: string;
                signedDownloadUrl?: string;
              }
            ).signedDownloadUrl,
        });
      }
    } catch (error) {
      setResult({
        error:
          error instanceof Error
            ? error.message
            : "Failed to clean Excel file. Please try again.",
      });
    } finally {
      setUploading(false);
    }
  };

  return (
    <ToolLayout
      title="Excel Cleaner"
      description="Clean and optimize your Excel files. Remove empty rows, trim whitespace, and more."
    >
      <div className="bg-white rounded-lg shadow-lg p-8 max-w-4xl mx-auto">
        <form onSubmit={handleSubmit} className="space-y-6">
          <div>
            <label className="block text-sm font-semibold text-gray-900 mb-2">
              Select Excel File
            </label>
            <FileUpload
              files={files}
              onFilesChange={handleFileChange}
              accept=".xlsx,.xls,.csv"
              label="Choose Excel File"
            />
          </div>

          <div className="space-y-3">
            <label className="flex items-center gap-2 text-gray-900 font-medium">
              <input
                type="checkbox"
                checked={options.removeEmptyRows}
                onChange={(e) =>
                  setOptions({ ...options, removeEmptyRows: e.target.checked })
                }
                className="w-4 h-4 text-blue-600 border-gray-300 rounded focus:ring-blue-500"
              />
              <span>Remove empty rows</span>
            </label>
            <label className="flex items-center gap-2 text-gray-900 font-medium">
              <input
                type="checkbox"
                checked={options.removeEmptyColumns}
                onChange={(e) =>
                  setOptions({
                    ...options,
                    removeEmptyColumns: e.target.checked,
                  })
                }
                className="w-4 h-4 text-blue-600 border-gray-300 rounded focus:ring-blue-500"
              />
              <span>Remove empty columns</span>
            </label>
            <label className="flex items-center gap-2 text-gray-900 font-medium">
              <input
                type="checkbox"
                checked={options.trimWhitespace}
                onChange={(e) =>
                  setOptions({ ...options, trimWhitespace: e.target.checked })
                }
                className="w-4 h-4 text-blue-600 border-gray-300 rounded focus:ring-blue-500"
              />
              <span>Trim whitespace</span>
            </label>
            <label className="flex items-center gap-2 text-gray-900 font-medium">
              <input
                type="checkbox"
                checked={options.removeDuplicates}
                onChange={(e) =>
                  setOptions({
                    ...options,
                    removeDuplicates: e.target.checked,
                  })
                }
                className="w-4 h-4 text-blue-600 border-gray-300 rounded focus:ring-blue-500"
              />
              <span>Remove duplicates</span>
            </label>
            <label className="flex items-center gap-2 text-gray-900 font-medium">
              <input
                type="checkbox"
                checked={options.standardizeFormats}
                onChange={(e) =>
                  setOptions({
                    ...options,
                    standardizeFormats: e.target.checked,
                  })
                }
                className="w-4 h-4 text-blue-600 border-gray-300 rounded focus:ring-blue-500"
              />
              <span>Standardize formats</span>
            </label>
          </div>

          <div>
            <label className="block text-sm font-semibold text-gray-900 mb-2">
              Output Format
            </label>
            <select
              value={options.outputFormat}
              onChange={(e) =>
                setOptions({ ...options, outputFormat: e.target.value })
              }
              className="w-full border border-gray-300 rounded-lg px-4 py-2 text-gray-900 bg-white focus:ring-2 focus:ring-blue-500 focus:border-blue-500"
            >
              <option value="xlsx">XLSX</option>
              <option value="xls">XLS</option>
              <option value="csv">CSV</option>
            </select>
          </div>

          <button
            type="submit"
            disabled={!file || uploading}
            className="w-full bg-blue-600 text-white py-3 rounded-lg font-semibold hover:bg-blue-700 disabled:bg-gray-400 disabled:cursor-not-allowed flex items-center justify-center gap-2"
          >
            {uploading ? (
              <>
                <Loader2 className="w-5 h-5 animate-spin" />
                Cleaning...
              </>
            ) : (
              <>
                <FileSpreadsheet className="w-5 h-5" />
                Clean Excel
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
