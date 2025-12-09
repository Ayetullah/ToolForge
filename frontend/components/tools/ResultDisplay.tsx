"use client";

import { Download, AlertCircle, CheckCircle } from "lucide-react";
import { Prism as SyntaxHighlighter } from "react-syntax-highlighter";
import { oneDark } from "react-syntax-highlighter/dist/esm/styles/prism";

interface ResultDisplayProps {
  result: {
    downloadUrl?: string;
    signedDownloadUrl?: string;
    error?: string;
    compressionRatio?: number;
    formattedJson?: string;
    summary?: string;
  } | null;
  loading?: boolean;
}

export default function ResultDisplay({ result, loading }: ResultDisplayProps) {
  if (loading) {
    return (
      <div className="bg-blue-50 border border-blue-200 p-4 rounded-lg">
        <div className="flex items-center gap-2">
          <div className="animate-spin rounded-full h-5 w-5 border-b-2 border-blue-600"></div>
          <p className="text-blue-700">Processing...</p>
        </div>
      </div>
    );
  }

  if (!result) {
    return null;
  }

  if (result.error) {
    return (
      <div className="bg-red-50 border border-red-200 p-4 rounded-lg">
        <div className="flex items-center gap-2 mb-2">
          <AlertCircle className="w-5 h-5 text-red-600" />
          <p className="text-red-700 font-semibold">Error</p>
        </div>
        <p className="text-red-600">{result.error}</p>
      </div>
    );
  }

  const downloadUrl =
    result.downloadUrl || result.signedDownloadUrl;

  if (downloadUrl) {
    return (
      <div className="bg-green-50 border border-green-200 p-4 rounded-lg">
        <div className="flex items-center gap-2 mb-3">
          <CheckCircle className="w-5 h-5 text-green-600" />
          <p className="text-green-700 font-semibold">Success!</p>
        </div>
        {result.compressionRatio && (
          <p className="text-sm text-green-600 mb-3">
            Compression ratio: {(result.compressionRatio * 100).toFixed(1)}%
          </p>
        )}
        <a
          href={downloadUrl}
          download
          className="inline-flex items-center gap-2 bg-green-600 text-white px-4 py-2 rounded-lg hover:bg-green-700"
        >
          <Download className="w-5 h-5" />
          Download Result
        </a>
      </div>
    );
  }

  if (result.formattedJson) {
    return (
      <div className="bg-green-50 border border-green-200 p-4 rounded-lg">
        <div className="flex items-center gap-2 mb-3">
          <CheckCircle className="w-5 h-5 text-green-600" />
          <p className="text-green-700 font-semibold">Formatted JSON</p>
        </div>
        <div className="rounded border overflow-hidden">
          <SyntaxHighlighter
            language="json"
            style={oneDark}
            customStyle={{
              margin: 0,
              padding: "1rem",
              fontSize: "0.875rem",
              lineHeight: "1.5",
              borderRadius: "0.375rem",
            }}
            showLineNumbers={false}
            wrapLines={true}
            wrapLongLines={true}
          >
            {result.formattedJson}
          </SyntaxHighlighter>
        </div>
      </div>
    );
  }

  if (result.summary) {
    return (
      <div className="bg-green-50 border border-green-200 p-4 rounded-lg">
        <div className="flex items-center gap-2 mb-3">
          <CheckCircle className="w-5 h-5 text-green-600" />
          <p className="text-green-700 font-semibold">Summary</p>
        </div>
        <p className="text-gray-700 whitespace-pre-wrap">{result.summary}</p>
      </div>
    );
  }

  return null;
}

