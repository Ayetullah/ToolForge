import Link from "next/link";
import { Check } from "lucide-react";
import Navigation from "@/components/common/Navigation";

export default function PricingPage() {
  const plans = [
    {
      name: "Free",
      price: "$0",
      period: "forever",
      features: [
        "10 operations per day",
        "PDF Merge & Split",
        "Image Compression",
        "JSON Formatter",
        "Basic support",
      ],
      cta: "Get Started",
      popular: false,
    },
    {
      name: "Basic",
      price: "$9",
      period: "month",
      features: [
        "100 operations per day",
        "All Free features",
        "Doc to PDF",
        "Excel Cleaning",
        "Email support",
      ],
      cta: "Subscribe",
      popular: true,
    },
    {
      name: "Pro",
      price: "$29",
      period: "month",
      features: [
        "1,000 operations per day",
        "All Basic features",
        "Background Removal",
        "Priority support",
        "API access",
      ],
      cta: "Subscribe",
      popular: false,
    },
    {
      name: "Enterprise",
      price: "Custom",
      period: "",
      features: [
        "Unlimited operations",
        "All Pro features",
        "Custom integrations",
        "Dedicated support",
        "SLA guarantee",
        "On-premise option",
      ],
      cta: "Contact Sales",
      popular: false,
    },
  ];

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

      <main className="container mx-auto px-4 py-16">
        <div className="text-center mb-12">
          <h1 className="text-4xl font-bold text-gray-900 mb-4">
            Simple, Transparent Pricing
          </h1>
          <p className="text-xl text-gray-700">
            Choose the plan that's right for you
          </p>
        </div>

        <div className="grid md:grid-cols-2 lg:grid-cols-4 gap-8">
          {plans.map((plan, index) => (
            <div
              key={index}
              className={`bg-white rounded-lg shadow-lg p-8 ${
                plan.popular
                  ? "border-2 border-blue-600 transform scale-105"
                  : "border border-gray-200"
              }`}
            >
              {plan.popular && (
                <div className="bg-blue-600 text-white text-sm font-semibold px-3 py-1 rounded-full inline-block mb-4">
                  Most Popular
                </div>
              )}
              <h3 className="text-2xl font-bold text-gray-900 mb-2">{plan.name}</h3>
              <div className="mb-4">
                <span className="text-4xl font-bold text-gray-900">{plan.price}</span>
                {plan.period && (
                  <span className="text-gray-700">/{plan.period}</span>
                )}
              </div>
              <ul className="space-y-3 mb-8">
                {plan.features.map((feature, featureIndex) => (
                  <li key={featureIndex} className="flex items-start">
                    <Check className="w-5 h-5 text-green-500 mr-2 flex-shrink-0 mt-0.5" />
                    <span className="text-gray-800">{feature}</span>
                  </li>
                ))}
              </ul>
              <Link
                href={plan.name === "Enterprise" ? "/contact" : "/register"}
                className={`block text-center py-3 px-6 rounded-lg font-semibold ${
                  plan.popular
                    ? "bg-blue-600 text-white hover:bg-blue-700"
                    : "bg-gray-100 text-gray-900 hover:bg-gray-200"
                }`}
              >
                {plan.cta}
              </Link>
            </div>
          ))}
        </div>
      </main>
    </div>
  );
}
