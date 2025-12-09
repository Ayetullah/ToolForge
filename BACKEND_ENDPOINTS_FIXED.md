# Backend Endpoints Düzeltmeleri

## Yapılan Düzeltmeler

### 1. JSON Serialization (CamelCase)
- ✅ JSON response'ları camelCase formatında döndürülüyor
- ✅ `AddControllers().AddJsonOptions()` ile camelCase policy eklendi

### 2. Video Compress Endpoint
- ✅ Frontend'den gelen `crf` parametresi destekleniyor
- ✅ Hem `crf` hem `quality` parametreleri kabul ediliyor
- ✅ Backend'de `Quality` property'si kullanılıyor

### 3. Payment Subscribe Endpoint
- ✅ Frontend'den gelen `tier` (string) enum'a çevriliyor
- ✅ `PaymentMethodId` optional yapıldı (validator güncellendi)
- ✅ String tier değeri `SubscriptionTier` enum'ına parse ediliyor

### 4. Format JSON Endpoint
- ✅ Frontend'den gelen `json` parametresi destekleniyor
- ✅ Hem `json` hem `text` parametreleri kabul ediliyor
- ✅ `indent` parametresi `IndentSize`'a map ediliyor
- ✅ Command ve Handler güncellendi

## Endpoint Listesi

### Auth Endpoints (8)
1. `POST /api/auth/register` ✅
2. `POST /api/auth/login` ✅
3. `POST /api/auth/refresh` ✅
4. `POST /api/auth/logout` ✅
5. `POST /api/auth/forgot-password` ✅
6. `POST /api/auth/reset-password` ✅
7. `POST /api/auth/verify-email` ✅
8. `POST /api/auth/resend-verification` ✅

### Tool Endpoints (10)
1. `POST /api/tools/pdf/merge` ✅
2. `POST /api/tools/pdf/split` ✅
3. `POST /api/tools/image/compress` ✅
4. `POST /api/tools/image/remove-background` ✅
5. `POST /api/tools/convert/doc-to-pdf` ✅
6. `POST /api/tools/excel/clean` ✅
7. `POST /api/tools/json/format` ✅ (Düzeltildi)
8. `POST /api/tools/ai/summarize` ✅
9. `POST /api/tools/regex/generate` ✅
10. `POST /api/tools/video/compress` ✅ (Düzeltildi)

### Job Status Endpoints (2)
1. `GET /api/jobs/{jobId}/status` ✅
2. `GET /api/tools/pdf/status/{jobId}` ✅ (Legacy, redirect to above)

### Payment Endpoints (3)
1. `POST /api/payments/subscribe` ✅ (Düzeltildi)
2. `POST /api/payments/cancel` ✅
3. `POST /api/payments/webhook` ✅

### Health & File Endpoints (3)
1. `GET /health/live` ✅
2. `GET /health/ready` ✅
3. `GET /api/files/download/{fileKey}` ✅

**Toplam: 26 Endpoint**

## Frontend-Backend Uyumluluğu

### ✅ Uyumlu Endpoint'ler
- PDF Merge: `files` → `form.Files` ✅
- PDF Split: `file`, `pagesSpec` ✅
- Image Compress: `file`, `quality`, `targetFormat`, `maxWidth`, `maxHeight` ✅
- Excel Clean: `file`, `removeEmptyRows`, etc. ✅
- AI Summarize: `text`, `url`, `maxLength` ✅
- Regex Generate: `description`, `examples` ✅
- Video Compress: `file`, `crf`, `preset`, `maxWidth`, `maxHeight` ✅ (Düzeltildi)
- Doc to PDF: `file` ✅
- Remove Background: `file`, `transparent`, `backgroundColor` ✅
- JSON Format: `json`, `indent` ✅ (Düzeltildi)
- Payment Subscribe: `tier` ✅ (Düzeltildi)

### Response Formatları
- Tüm response'lar camelCase formatında ✅
- `DownloadUrl` → `downloadUrl` ✅
- `JobId` → `jobId` ✅
- `SignedDownloadUrl` → `signedDownloadUrl` (Job status'ta) ✅

## Test Edilmesi Gerekenler

1. ✅ Video compress endpoint'i `crf` parametresini kabul ediyor
2. ✅ JSON format endpoint'i `json` ve `indent` parametrelerini kabul ediyor
3. ✅ Payment subscribe endpoint'i `tier` string'ini enum'a çeviriyor
4. ✅ JSON serialization camelCase çalışıyor

## Notlar

- Tüm endpoint'ler JWT authentication gerektiriyor (auth endpoint'leri hariç)
- File upload endpoint'leri `multipart/form-data` kullanıyor
- Background job'lar için job status polling yapılmalı
- Payment webhook endpoint'i Stripe signature validation yapıyor

