"use client";

import Link from "next/link";
import {
  FileText,
  Image as ImageIcon,
  FileSpreadsheet,
  LucideIcon,
} from "lucide-react";

interface Tool {
  name: string;
  href: string;
  icon: LucideIcon;
  premium?: boolean;
}

const tools: Tool[] = [
  { name: "PDF Merge", href: "/tools/pdf-merge", icon: FileText },
  { name: "PDF Split", href: "/tools/pdf-split", icon: FileText },
  { name: "Image Compress", href: "/tools/image-compress", icon: ImageIcon },
  {
    name: "Remove Background",
    href: "/tools/remove-background",
    icon: ImageIcon,
    premium: true,
  },
  { name: "Doc to PDF", href: "/tools/doc-to-pdf", icon: FileText },
  { name: "Excel Clean", href: "/tools/excel-clean", icon: FileSpreadsheet },
  { name: "JSON Format", href: "/tools/json-format", icon: FileText },
  { name: "Regex Generate", href: "/tools/regex-generate", icon: FileText },
];

export default function ToolsGrid() {
  return (
    <div className="grid md:grid-cols-2 lg:grid-cols-3 gap-6 mb-8">
      {tools.map((tool, index) => {
        const Icon = tool.icon;
        return (
          <Link
            key={index}
            href={tool.href}
            className="bg-white rounded-lg shadow p-6 hover:shadow-lg transition-shadow border-2 border-transparent hover:border-blue-300"
          >
            <div className="flex items-center gap-4">
              <div className="w-12 h-12 bg-blue-100 rounded-lg flex items-center justify-center">
                <Icon className="w-6 h-6 text-blue-600" />
              </div>
              <div className="flex-1">
                <h3 className="font-semibold text-lg text-gray-900">{tool.name}</h3>
                {tool.premium && (
                  <span className="text-xs bg-yellow-100 text-yellow-800 px-2 py-1 rounded font-medium">
                    Premium
                  </span>
                )}
              </div>
            </div>
          </Link>
        );
      })}
    </div>
  );
}

