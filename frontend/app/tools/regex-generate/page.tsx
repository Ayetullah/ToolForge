"use client";

import { useState } from "react";
import { Search, Loader2 } from "lucide-react";
import { toolsApi } from "@/lib/api";
import ToolLayout from "@/components/tools/ToolLayout";
import ResultDisplay from "@/components/tools/ResultDisplay";

export default function RegexGeneratePage() {
  const [description, setDescription] = useState("");
  const [examples, setExamples] = useState("");
  const [generating, setGenerating] = useState(false);
  const [result, setResult] = useState<{
    pattern?: string;
    explanation?: string;
    error?: string;
  } | null>(null);

  const handleGenerate = async (e: React.FormEvent) => {
    e.preventDefault();
    if (!description.trim()) {
      setResult({ error: "Please enter a description" });
      return;
    }

    setGenerating(true);
    setResult(null);

    try {
      const exampleArray = examples
        .split(",")
        .map((e) => e.trim())
        .filter((e) => e);
      const response = await toolsApi.generateRegex(
        description,
        exampleArray.length > 0 ? exampleArray : undefined
      );
      if (response.error) {
        setResult({ error: response.error });
      } else if (response.data) {
        const data = response.data as {
          pattern?: string;
          explanation?: string;
        };
        setResult({
          pattern: data.pattern,
          explanation: data.explanation,
        });
      }
    } catch {
      setResult({ error: "Failed to generate regex. Please try again." });
    } finally {
      setGenerating(false);
    }
  };

  return (
    <ToolLayout
      title="Regex Generator"
      description="Generate regex patterns from natural language descriptions using AI."
    >
      <div className="bg-white rounded-lg shadow-lg p-8 max-w-4xl mx-auto">
        <form onSubmit={handleGenerate} className="space-y-6">
          <div>
            <label className="block text-sm font-medium text-gray-700 mb-2">
              Description
            </label>
            <textarea
              value={description}
              onChange={(e) => setDescription(e.target.value)}
              placeholder="e.g., Match email addresses"
              className="w-full border border-gray-300 rounded-lg px-4 py-3 h-32 focus:ring-2 focus:ring-blue-500"
            />
          </div>

          <div>
            <label className="block text-sm font-medium text-gray-700 mb-2">
              Examples (comma-separated, optional)
            </label>
            <input
              type="text"
              value={examples}
              onChange={(e) => setExamples(e.target.value)}
              placeholder="example1@email.com, example2@email.com"
              className="w-full border border-gray-300 rounded-lg px-4 py-3 focus:ring-2 focus:ring-blue-500"
            />
          </div>

          <button
            type="submit"
            disabled={!description.trim() || generating}
            className="w-full bg-blue-600 text-white py-3 rounded-lg font-semibold hover:bg-blue-700 disabled:bg-gray-400 disabled:cursor-not-allowed flex items-center justify-center gap-2"
          >
            {generating ? (
              <>
                <Loader2 className="w-5 h-5 animate-spin" />
                Generating...
              </>
            ) : (
              <>
                <Search className="w-5 h-5" />
                Generate Regex
              </>
            )}
          </button>
        </form>

        <div className="mt-6">
          {result && result.pattern && (
            <div className="mb-6 space-y-4">
              <div>
                <h3 className="font-semibold mb-2">Pattern:</h3>
                <pre className="bg-gray-50 border border-gray-200 rounded-lg p-4 overflow-x-auto">
                  <code>{result.pattern}</code>
                </pre>
              </div>
              {result.explanation && (
                <div>
                  <h3 className="font-semibold mb-2">Explanation:</h3>
                  <p className="text-gray-700">{result.explanation}</p>
                </div>
              )}
            </div>
          )}
          <ResultDisplay result={result} loading={generating} />
        </div>
      </div>
    </ToolLayout>
  );
}
