"use client";

import { Upload, X } from "lucide-react";

interface FileUploadProps {
  files: File[];
  onFilesChange: (files: File[]) => void;
  accept?: string;
  multiple?: boolean;
  maxFiles?: number;
  label?: string;
}

export default function FileUpload({
  files,
  onFilesChange,
  accept,
  multiple = false,
  maxFiles,
  label = "Select files",
}: FileUploadProps) {
  const handleFileChange = (e: React.ChangeEvent<HTMLInputElement>) => {
    if (e.target.files) {
      const newFiles = Array.from(e.target.files);
      const updatedFiles = multiple
        ? [...files, ...newFiles].slice(0, maxFiles)
        : newFiles;
      onFilesChange(updatedFiles);
    }
  };

  const removeFile = (index: number) => {
    const updatedFiles = files.filter((_, i) => i !== index);
    onFilesChange(updatedFiles);
  };

  return (
    <div className="space-y-4">
      <div className="border-2 border-dashed border-gray-700 rounded-lg p-8 text-center hover:border-blue-400 transition-colors">
        <input
          type="file"
          id="file-upload"
          className="hidden"
          accept={accept}
          multiple={multiple}
          onChange={handleFileChange}
        />
        <label
          htmlFor="file-upload"
          className="cursor-pointer flex flex-col items-center gap-2"
        >
          <Upload className="w-12 h-12 text-gray-700" />
          <span className="text-gray-800 font-medium">{label}</span>
          <span className="text-sm text-gray-700">
            {multiple && maxFiles
              ? `Up to ${maxFiles} files`
              : "Click to select"}
          </span>
        </label>
      </div>

      {files.length > 0 && (
        <div className="space-y-2">
          {files.map((file, index) => (
            <div
              key={index}
              className="flex items-center justify-between bg-white p-3 rounded-lg border"
            >
              <div className="flex-1">
                <p className="font-semibold text-sm text-gray-900">{file.name}</p>
                <p className="text-xs text-gray-800 font-medium">
                  {(file.size / 1024 / 1024).toFixed(2)} MB
                </p>
              </div>
              <button
                type="button"
                onClick={() => removeFile(index)}
                className="text-red-500 hover:text-red-700"
              >
                <X className="w-5 h-5" />
              </button>
            </div>
          ))}
        </div>
      )}
    </div>
  );
}
