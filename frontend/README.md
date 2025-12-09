# UtilityTools Frontend

Modern, SEO-optimized frontend for UtilityTools built with Next.js, React, and Tailwind CSS.

## Features

-   🎨 Modern, responsive UI design
-   🔍 SEO-optimized pages
-   🚀 Fast performance with Next.js
-   📱 Mobile-friendly
-   🔐 Authentication pages
-   🛠️ Tool pages for all utilities
-   💳 Pricing page
-   📊 Dashboard (coming soon)

## Getting Started

### Prerequisites

-   Node.js 18+ and npm

### Installation

1. Install dependencies:

```bash
npm install
```

2. Copy environment file:

```bash
cp .env.local.example .env.local
```

3. Update `.env.local` with your API URL:

```
NEXT_PUBLIC_API_URL=http://localhost:5000
```

4. Run the development server:

```bash
npm run dev
```

5. Open [http://localhost:3000](http://localhost:3000) in your browser.

## Project Structure

```
frontend/
├── app/                    # Next.js app directory
│   ├── page.tsx           # Home/Landing page
│   ├── login/              # Login page
│   ├── register/           # Registration page
│   ├── tools/              # Tools listing and individual tool pages
│   ├── pricing/            # Pricing page
│   └── dashboard/          # User dashboard (coming soon)
├── lib/                    # Utilities
│   └── api.ts             # API client
└── components/             # Reusable components (coming soon)
```

## Available Scripts

-   `npm run dev` - Start development server
-   `npm run build` - Build for production
-   `npm run start` - Start production server
-   `npm run lint` - Run ESLint

## Pages

### Public Pages

-   `/` - Landing page
-   `/tools` - All tools listing
-   `/tools/[tool-name]` - Individual tool pages
-   `/pricing` - Pricing plans
-   `/login` - User login
-   `/register` - User registration
-   `/forgot-password` - Password reset request
-   `/reset-password` - Password reset

### Protected Pages (coming soon)

-   `/dashboard` - User dashboard
-   `/settings` - User settings
-   `/subscription` - Subscription management

## API Integration

The frontend uses the API client in `lib/api.ts` to communicate with the backend API. All API calls are centralized and include:

-   Authentication (login, register, logout)
-   Tools (all utility tools)
-   Payments (subscription management)
-   Job status tracking

## Styling

The project uses Tailwind CSS for styling. All components are styled with utility classes for fast development and consistent design.

## SEO

All pages include proper meta tags, Open Graph tags, and semantic HTML for optimal SEO performance.

## Deployment

The frontend can be deployed to:

-   Vercel (recommended for Next.js)
-   Netlify
-   Any static hosting service
-   Docker container

For production deployment:

```bash
npm run build
npm run start
```
