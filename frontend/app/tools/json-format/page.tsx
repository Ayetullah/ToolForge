"use client";

import { useState } from "react";
import { FileText, Copy, Check } from "lucide-react";
import { toolsApi } from "@/lib/api";
import ToolLayout from "@/components/tools/ToolLayout";
import ResultDisplay from "@/components/tools/ResultDisplay";

export default function JsonFormatPage() {
  const [jsonInput, setJsonInput] = useState("");
  const [indent, setIndent] = useState(2);
  const [formatting, setFormatting] = useState(false);
  const [result, setResult] = useState<any>(null);
  const [copied, setCopied] = useState(false);

  const handleFormat = async (e: React.FormEvent) => {
    e.preventDefault();
    if (!jsonInput.trim()) {
      setResult({ error: "Please enter JSON to format" });
      return;
    }

    setFormatting(true);
    setResult(null);

    try {
      const response = await toolsApi.formatJson(jsonInput, indent);
      if (response.error) {
        setResult({ error: response.error });
      } else if (response.data) {
        setResult({ formattedJson: response.data.formattedJson });
      }
    } catch (error) {
      setResult({ error: "Failed to format JSON. Please check your input." });
    } finally {
      setFormatting(false);
    }
  };

  const handleCopy = () => {
    if (result?.formattedJson) {
      navigator.clipboard.writeText(result.formattedJson);
      setCopied(true);
      setTimeout(() => setCopied(false), 2000);
    }
  };

  return (
    <ToolLayout
      title="JSON Formatter"
      description="Format and validate your JSON code. Make it readable and properly indented."
    >
      <div className="bg-white rounded-lg shadow-lg p-8 max-w-6xl mx-auto">
        <form onSubmit={handleFormat} className="space-y-6">
          <div>
            <label className="block text-sm font-medium text-gray-700 mb-2">
              JSON Input
            </label>
            <textarea
              value={jsonInput}
              onChange={(e) => setJsonInput(e.target.value)}
              placeholder='{"key": "value"}'
              className="w-full border border-gray-700 rounded-lg px-4 py-3 font-mono text-sm h-64 focus:ring-2 focus:ring-blue-500 placeholder:text-gray-500"
            />
          </div>

          <div>
            <label className="block text-sm font-medium text-gray-700 mb-2">
              Indentation (spaces)
            </label>
            <input
              type="number"
              min="1"
              max="8"
              value={indent}
              onChange={(e) => setIndent(parseInt(e.target.value))}
              className="w-32 border border-gray-700 rounded-lg px-4 py-2 focus:ring-2 focus:ring-blue-500"
            />
          </div>

          <button
            type="submit"
            disabled={!jsonInput.trim() || formatting}
            className="bg-blue-600 text-white px-6 py-3 rounded-lg font-semibold hover:bg-blue-700 disabled:bg-gray-400 disabled:cursor-not-allowed flex items-center gap-2"
          >
            <FileText className="w-5 h-5" />
            Format JSON
          </button>
        </form>

        {result && result.formattedJson && (
          <div className="mt-6">
            <div className="flex justify-between items-center mb-2">
              <h3 className="font-semibold text-gray-900">Formatted JSON:</h3>
              <button
                onClick={handleCopy}
                className="flex items-center gap-2 text-blue-600 hover:text-blue-700"
              >
                {copied ? (
                  <>
                    <Check className="w-5 h-5" />
                    Copied!
                  </>
                ) : (
                  <>
                    <Copy className="w-5 h-5" />
                    Copy
                  </>
                )}
              </button>
            </div>
          </div>
        )}

        <div className="mt-6">
          <ResultDisplay result={result} loading={formatting} />
        </div>
      </div>
    </ToolLayout>
  );
}
