"use client";

import { useState, useEffect } from "react";
import Link from "next/link";
import { ArrowRight, Check, Zap, Shield, Globe } from "lucide-react";
import Navigation from "@/components/common/Navigation";
import { apiClient } from "@/lib/api";

export default function HomePage() {
  const [isAuthenticated, setIsAuthenticated] = useState(false);

  useEffect(() => {
    // Check if user is authenticated
    const token =
      typeof window !== "undefined"
        ? localStorage.getItem("token") || sessionStorage.getItem("token")
        : null;
    setIsAuthenticated(!!token);
  }, []);
  const tools = [
    {
      name: "PDF Merge",
      description: "Combine multiple PDF files into one",
      href: "/tools/pdf-merge",
      icon: "📄",
    },
    {
      name: "PDF Split",
      description: "Split PDF into separate pages",
      href: "/tools/pdf-split",
      icon: "✂️",
    },
    {
      name: "Image Compress",
      description: "Reduce image file size",
      href: "/tools/image-compress",
      icon: "🖼️",
    },
    {
      name: "JSON Formatter",
      description: "Format and validate JSON",
      href: "/tools/json-format",
      icon: "📋",
    },
    {
      name: "Excel Cleaner",
      description: "Clean and optimize Excel files",
      href: "/tools/excel-clean",
      icon: "📊",
    },
    {
      name: "Regex Generator",
      description: "Generate regex patterns",
      href: "/tools/regex-generate",
      icon: "🔍",
    },
    // Premium features temporarily disabled
    // {
    //   name: "Doc to PDF",
    //   description:
    //     "Convert Word (.docx, .doc) and Excel (.xlsx, .xls, .csv) to PDF",
    //   href: "/tools/doc-to-pdf",
    //   icon: "📑",
    // },
    // {
    //   name: "Remove Background",
    //   description: "Remove image backgrounds",
    //   href: "/tools/remove-background",
    //   icon: "🎨",
    // },
  ];

  const features = [
    {
      icon: Zap,
      title: "Lightning Fast",
      description: "Process files in seconds",
    },
    {
      icon: Shield,
      title: "Secure & Private",
      description: "Your files are processed securely",
    },
    { icon: Globe, title: "Cloud-Based", description: "Access from anywhere" },
  ];

  return (
    <div className="min-h-screen bg-gradient-to-b from-gray-50 to-white">
      {/* Header */}
      <header className="border-b bg-white/80 backdrop-blur-sm sticky top-0 z-50">
        <div className="container mx-auto px-4 py-4 flex justify-between items-center">
          <Link href="/" className="text-2xl font-bold text-blue-600">
            UtilityTools
          </Link>
          <Navigation />
        </div>
      </header>

      {/* Hero Section */}
      <section className="container mx-auto px-4 py-20 text-center">
        <h1 className="text-5xl md:text-6xl font-bold text-gray-900 mb-6">
          All Your Utility Tools
          <span className="text-blue-600"> In One Place</span>
        </h1>
        <p className="text-xl text-gray-700 mb-8 max-w-2xl mx-auto">
          Powerful, fast, and secure tools for PDF, images, documents, and more.
          No installation required.
        </p>
        <div className="flex gap-4 justify-center">
          {isAuthenticated ? (
            <>
              <Link
                href="/dashboard"
                className="bg-blue-600 text-white px-8 py-3 rounded-lg text-lg font-semibold hover:bg-blue-700 flex items-center gap-2"
              >
                Go to Dashboard
                <ArrowRight className="w-5 h-5" />
              </Link>
              <Link
                href="/tools"
                className="border border-gray-700 text-gray-700 px-8 py-3 rounded-lg text-lg font-semibold hover:bg-gray-50"
              >
                Browse Tools
              </Link>
            </>
          ) : (
            <>
              <Link
                href="/register"
                className="bg-blue-600 text-white px-8 py-3 rounded-lg text-lg font-semibold hover:bg-blue-700 flex items-center gap-2"
              >
                Get Started Free
                <ArrowRight className="w-5 h-5" />
              </Link>
              <Link
                href="/tools"
                className="border border-gray-700 text-gray-700 px-8 py-3 rounded-lg text-lg font-semibold hover:bg-gray-50"
              >
                Browse Tools
              </Link>
            </>
          )}
        </div>
      </section>

      {/* Features */}
      <section className="container mx-auto px-4 py-16">
        <div className="grid md:grid-cols-3 gap-8">
          {features.map((feature, index) => (
            <div key={index} className="text-center p-6">
              <div className="w-16 h-16 bg-blue-100 rounded-full flex items-center justify-center mx-auto mb-4">
                <feature.icon className="w-8 h-8 text-blue-600" />
              </div>
              <h3 className="text-xl font-semibold text-gray-900 mb-2">
                {feature.title}
              </h3>
              <p className="text-gray-700">{feature.description}</p>
            </div>
          ))}
        </div>
      </section>

      {/* Tools Grid */}
      <section className="container mx-auto px-4 py-16">
        <h2 className="text-3xl font-bold text-gray-900 text-center mb-12">
          All Tools
        </h2>
        <div className="grid md:grid-cols-2 lg:grid-cols-3 gap-6">
          {tools
            .filter((tool) => tool.href) // Filter out commented premium features
            .map((tool, index) => (
              <Link
                key={index}
                href={tool.href}
                className="border border-gray-200 rounded-lg p-6 hover:shadow-lg transition-shadow hover:border-blue-300"
              >
                <div className="text-4xl mb-4">{tool.icon}</div>
                <h3 className="text-xl font-semibold text-gray-900 mb-2">
                  {tool.name}
                </h3>
                <p className="text-gray-700">{tool.description}</p>
              </Link>
            ))}
        </div>
      </section>

      {/* Pricing CTA - Temporarily disabled */}
      {/* <section className="bg-blue-600 text-white py-16">
        <div className="container mx-auto px-4 text-center">
          <h2 className="text-3xl font-bold mb-4">Ready to Get Started?</h2>
          <p className="text-xl mb-8 text-blue-100">
            Choose a plan that works for you. Free tier available.
          </p>
          <Link
            href="/pricing"
            className="bg-white text-blue-600 px-8 py-3 rounded-lg text-lg font-semibold hover:bg-gray-100 inline-block"
          >
            View Pricing
          </Link>
        </div>
      </section> */}

      {/* Footer */}
      <footer className="border-t bg-gray-50 py-12">
        <div className="container mx-auto px-4">
          <div className="grid md:grid-cols-4 gap-8">
            <div>
              <h3 className="text-xl font-bold text-blue-600 mb-4">
                UtilityTools
              </h3>
              <p className="text-gray-700">
                Powerful utility tools for your everyday needs.
              </p>
            </div>
            <div>
              <h4 className="font-semibold text-gray-900 mb-4">Tools</h4>
              <ul className="space-y-2 text-gray-700">
                <li>
                  <Link href="/tools/pdf-merge" className="hover:text-blue-600">
                    PDF Merge
                  </Link>
                </li>
                <li>
                  <Link
                    href="/tools/image-compress"
                    className="hover:text-blue-600"
                  >
                    Image Compress
                  </Link>
                </li>
                <li>
                  <Link href="/tools" className="hover:text-blue-600">
                    All Tools
                  </Link>
                </li>
              </ul>
            </div>
            <div>
              <h4 className="font-semibold text-gray-900 mb-4">Company</h4>
              <ul className="space-y-2 text-gray-700">
                {/* Pricing temporarily disabled */}
                {/* <li>
                  <Link href="/pricing" className="hover:text-blue-600">
                    Pricing
                  </Link>
                </li> */}
                <li>
                  <Link href="/about" className="hover:text-blue-600">
                    About
                  </Link>
                </li>
                <li>
                  <Link href="/contact" className="hover:text-blue-600">
                    Contact
                  </Link>
                </li>
              </ul>
            </div>
            <div>
              <h4 className="font-semibold text-gray-900 mb-4">Legal</h4>
              <ul className="space-y-2 text-gray-700">
                <li>
                  <Link href="/privacy" className="hover:text-blue-600">
                    Privacy
                  </Link>
                </li>
                <li>
                  <Link href="/terms" className="hover:text-blue-600">
                    Terms
                  </Link>
                </li>
              </ul>
            </div>
          </div>
          <div className="border-t mt-8 pt-8 text-center text-gray-700">
            <p>&copy; 2024 UtilityTools. All rights reserved.</p>
          </div>
        </div>
      </footer>
    </div>
  );
}
