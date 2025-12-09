# UtilityTools API - Example Requests

## Base URL
- Local: `http://localhost:5000`
- Docker: `http://localhost:8080`

## Authentication Endpoints

### 1. Register User

```bash
curl -X POST http://localhost:5000/api/auth/register \
  -H "Content-Type: application/json" \
  -d '{
    "email": "user@example.com",
    "password": "SecurePass123!",
    "firstName": "John",
    "lastName": "Doe"
  }'
```

**Response:**
```json
{
  "userId": "guid",
  "email": "user@example.com",
  "message": "User registered successfully. Please verify your email."
}
```

### 2. Login

```bash
curl -X POST http://localhost:5000/api/auth/login \
  -H "Content-Type: application/json" \
  -d '{
    "email": "user@example.com",
    "password": "SecurePass123!"
  }'
```

**Response:**
```json
{
  "accessToken": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...",
  "refreshToken": "guid",
  "expiresAt": "2024-01-01T12:00:00Z",
  "userId": "guid",
  "email": "user@example.com",
  "subscriptionTier": "Free"
}
```

### 3. Refresh Token

```bash
curl -X POST http://localhost:5000/api/auth/refresh \
  -H "Content-Type: application/json" \
  -d '{
    "refreshToken": "your-refresh-token-here"
  }'
```

**Response:**
```json
{
  "accessToken": "new-jwt-token",
  "refreshToken": "new-refresh-token",
  "expiresAt": "2024-01-01T12:00:00Z"
}
```

### 4. Logout

```bash
curl -X POST http://localhost:5000/api/auth/logout \
  -H "Authorization: Bearer YOUR_ACCESS_TOKEN" \
  -H "Content-Type: application/json" \
  -d '{
    "refreshToken": "your-refresh-token-here"
  }'
```

**Response:** `204 No Content`

## Tool Endpoints

### 5. Format JSON

```bash
curl -X POST http://localhost:5000/api/tools/json/format \
  -H "Authorization: Bearer YOUR_ACCESS_TOKEN" \
  -H "Content-Type: application/json" \
  -d '{
    "text": "{\"name\":\"John\",\"age\":30}",
    "indent": true,
    "indentSize": 2
  }'
```

**Response:**
```json
{
  "formattedJson": "{\n  \"name\": \"John\",\n  \"age\": 30\n}",
  "isValid": true,
  "errorMessage": null
}
```

**Error Response:**
```json
{
  "formattedJson": "",
  "isValid": false,
  "errorMessage": "Invalid JSON: ..."
}
```

### 6. AI Summarize Text

```bash
curl -X POST http://localhost:5000/api/tools/ai/summarize \
  -H "Authorization: Bearer YOUR_ACCESS_TOKEN" \
  -H "Content-Type: application/json" \
  -d '{
    "text": "Long text to summarize here...",
    "maxLength": 200,
    "tone": "professional"
  }'
```

**Response:**
```json
{
  "summary": "Summarized text here...",
  "tokensUsed": 150,
  "cost": 0.0045,
  "originalLength": 5000,
  "summaryLength": 180
}
```

### 7. AI Summarize URL

```bash
curl -X POST http://localhost:5000/api/tools/ai/summarize \
  -H "Authorization: Bearer YOUR_ACCESS_TOKEN" \
  -H "Content-Type: application/json" \
  -d '{
    "url": "https://example.com/article",
    "maxLength": 300,
    "tone": "casual"
  }'
```

## Health Checks

### 8. Liveness Check

```bash
curl http://localhost:5000/health/live
```

### 9. Readiness Check

```bash
curl http://localhost:5000/health/ready
```

## Using the API with Swagger

1. Start the API: `dotnet run --project src/UtilityTools.Api`
2. Navigate to: `http://localhost:5000/swagger`
3. Click "Authorize" button
4. Enter: `Bearer YOUR_ACCESS_TOKEN`
5. Test endpoints directly from Swagger UI

## Error Responses

All endpoints follow RFC 7807 ProblemDetails format:

```json
{
  "type": "https://tools.ietf.org/html/rfc7231#section-6.5.1",
  "title": "One or more validation errors occurred",
  "status": 400,
  "traceId": "00-...",
  "errors": {
    "Email": ["Email is required."],
    "Password": ["Password must be at least 8 characters."]
  }
}
```

## Rate Limiting

Rate limit headers are included in responses:
- `X-RateLimit-Limit`: Maximum requests allowed
- `X-RateLimit-Remaining`: Remaining requests
- `X-RateLimit-Reset`: Reset time

## Notes

- All tool endpoints require authentication (Bearer token)
- Token expires after 60 minutes (configurable)
- Use refresh token to get new access token
- File upload endpoints will use `multipart/form-data`
- Large files (>20MB) will be processed as background jobs

