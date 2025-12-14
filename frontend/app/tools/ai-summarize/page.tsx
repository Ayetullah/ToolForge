"use client";

import { useState } from "react";
import { Sparkles, Loader2 } from "lucide-react";
import { toolsApi } from "@/lib/api";
import ToolLayout from "@/components/tools/ToolLayout";
import ResultDisplay from "@/components/tools/ResultDisplay";

export default function AiSummarizePage() {
  const [text, setText] = useState("");
  const [url, setUrl] = useState("");
  const [maxLength, setMaxLength] = useState(200);
  const [summarizing, setSummarizing] = useState(false);
  const [result, setResult] = useState<any>(null);

  const handleSummarize = async (e: React.FormEvent) => {
    e.preventDefault();
    if (!text.trim() && !url.trim()) {
      setResult({ error: "Please enter text or URL to summarize" });
      return;
    }

    setSummarizing(true);
    setResult(null);

    try {
      const response = await toolsApi.summarize({
        text: text || undefined,
        url: url || undefined,
        maxLength,
      });
      if (response.error) {
        setResult({ error: response.error });
      } else if (response.data) {
        setResult({ summary: response.data.summary });
      }
    } catch (error) {
      setResult({ error: "Failed to summarize. Please try again." });
    } finally {
      setSummarizing(false);
    }
  };

  return (
    <ToolLayout
      title="AI Text Summarizer"
      description="Summarize long texts or web pages using AI. Get concise summaries in seconds."
    >
      <div className="bg-white rounded-lg shadow-lg p-8 max-w-4xl mx-auto">
        <form onSubmit={handleSummarize} className="space-y-6">
          <div>
            <label className="block text-sm font-medium text-gray-700 mb-2">
              Text to Summarize
            </label>
            <textarea
              value={text}
              onChange={(e) => setText(e.target.value)}
              placeholder="Enter text to summarize..."
              className="w-full border border-gray-700 rounded-lg px-4 py-3 h-48 focus:ring-2 focus:ring-blue-500 placeholder:text-gray-500"
            />
          </div>

          <div className="text-center text-gray-800 font-medium">OR</div>

          <div>
            <label className="block text-sm font-medium text-gray-700 mb-2">
              URL to Summarize
            </label>
            <input
              type="url"
              value={url}
              onChange={(e) => setUrl(e.target.value)}
              placeholder="https://example.com/article"
              className="w-full border border-gray-700 rounded-lg px-4 py-3 focus:ring-2 focus:ring-blue-500 placeholder:text-gray-500"
            />
          </div>

          <div>
            <label className="block text-sm font-medium text-gray-700 mb-2">
              Max Summary Length (words)
            </label>
            <input
              type="number"
              min="50"
              max="1000"
              value={maxLength}
              onChange={(e) => setMaxLength(parseInt(e.target.value))}
              className="w-32 border border-gray-700 rounded-lg px-4 py-2 focus:ring-2 focus:ring-blue-500"
            />
          </div>

          <button
            type="submit"
            disabled={(!text.trim() && !url.trim()) || summarizing}
            className="w-full bg-blue-600 text-white py-3 rounded-lg font-semibold hover:bg-blue-700 disabled:bg-gray-400 disabled:cursor-not-allowed flex items-center justify-center gap-2"
          >
            {summarizing ? (
              <>
                <Loader2 className="w-5 h-5 animate-spin" />
                Summarizing...
              </>
            ) : (
              <>
                <Sparkles className="w-5 h-5" />
                Summarize
              </>
            )}
          </button>
        </form>

        <div className="mt-6">
          <ResultDisplay result={result} loading={summarizing} />
        </div>
      </div>
    </ToolLayout>
  );
}
