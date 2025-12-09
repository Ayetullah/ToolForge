import Link from 'next/link';
import Navigation from "@/components/common/Navigation";

export default function ToolsPage() {
  const tools = [
    { name: 'PDF Merge', description: 'Combine multiple PDF files into one', href: '/tools/pdf-merge', icon: '📄', category: 'PDF' },
    { name: 'PDF Split', description: 'Split PDF into separate pages', href: '/tools/pdf-split', icon: '✂️', category: 'PDF' },
    { name: 'Image Compress', description: 'Reduce image file size', href: '/tools/image-compress', icon: '🖼️', category: 'Image' },
    { name: 'Remove Background', description: 'Remove image backgrounds', href: '/tools/remove-background', icon: '🎨', category: 'Image', premium: true },
    { name: 'JSON Formatter', description: 'Format and validate JSON', href: '/tools/json-format', icon: '📋', category: 'Developer' },
    { name: 'Excel Cleaner', description: 'Clean and optimize Excel files', href: '/tools/excel-clean', icon: '📊', category: 'Document' },
    { name: 'Regex Generator', description: 'Generate regex patterns', href: '/tools/regex-generate', icon: '🔍', category: 'Developer' },
    { name: 'Video Compress', description: 'Compress video files', href: '/tools/video-compress', icon: '🎬', category: 'Media' },
    { name: 'Doc to PDF', description: 'Convert documents to PDF', href: '/tools/doc-to-pdf', icon: '📑', category: 'Document' },
  ];

  const categories = ['All', 'PDF', 'Image', 'Document', 'Developer', 'AI', 'Media'];

  return (
    <div className="min-h-screen bg-gray-50">
      <header className="border-b bg-white">
        <div className="container mx-auto px-4 py-4 flex justify-between items-center">
          <Link href="/" className="text-2xl font-bold text-blue-600">
            UtilityTools
          </Link>
          <Navigation />
        </div>
      </header>

      <main className="container mx-auto px-4 py-12">
        <h1 className="text-4xl font-bold mb-4">All Tools</h1>
        <p className="text-gray-600 mb-8">Choose a tool to get started</p>

        <div className="grid md:grid-cols-2 lg:grid-cols-3 gap-6">
          {tools.map((tool, index) => (
            <Link
              key={index}
              href={tool.href}
              className="bg-white border border-gray-200 rounded-lg p-6 hover:shadow-lg transition-shadow hover:border-blue-300 relative"
            >
              {tool.premium && (
                <span className="absolute top-4 right-4 bg-yellow-400 text-yellow-900 text-xs font-semibold px-2 py-1 rounded">
                  Premium
                </span>
              )}
              <div className="text-4xl mb-4">{tool.icon}</div>
              <div className="text-sm text-blue-600 mb-2">{tool.category}</div>
              <h3 className="text-xl font-semibold mb-2">{tool.name}</h3>
              <p className="text-gray-600">{tool.description}</p>
            </Link>
          ))}
        </div>
      </main>
    </div>
  );
}

