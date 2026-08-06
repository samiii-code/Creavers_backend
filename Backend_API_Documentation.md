# Creavers Marketplace API — Backend API Documentation

Prepared for frontend / mobile (Flutter) developers.

---

## 1. Project Information

| Item | Value |
|---|---|
| Project Name | Creavers Marketplace API (`Creavers.API`) |
| Framework | ASP.NET Core 8 Web API (`net8.0`), C# |
| Database | PostgreSQL (Npgsql) |
| ORM | Entity Framework Core 8.0.7 |
| Authentication | JWT Bearer (ASP.NET Core Authentication + Identity) |
| Validation | FluentValidation 11.9.2 |
| Mapping | AutoMapper 13.0.1 |
| Logging | Serilog (Console + rolling File in `Logs/` folder) |
| API Documentation | Swagger / OpenAPI (Swashbuckle 6.6.2) |
| Identity | ASP.NET Identity (roles: ADMIN, PROVIDER, CUSTOMER) |
| Swagger availability | Enabled in all environments; UI served at the root route |

**Non-sensitive configuration notes (appsettings.json):**

- CORS allowed origins: `http://localhost:3000` and `https://localhost:3000` (AllowAnyHeader, AllowAnyMethod, AllowCredentials).
- Serilog file sink: `Logs/creavers-api-.log`, rolling daily, 30 files retained.
- Sensitive values (JWT secret, database password) are not reproduced in this document.

---

## 2. Development Base URL

The API runs locally on HTTP port **5000** (see `Properties/launchSettings.json`).

```
http://localhost:5000
```

All endpoints are prefixed with `/api`. Example: `http://localhost:5000/api/health`.

---

## 3. Swagger URL

Swagger UI is served at the root of the application:

```
http://localhost:5000/
```

OpenAPI JSON document:

```
http://localhost:5000/swagger/v1/swagger.json
```

---

## 4. Authentication

### 4.1 Register

Creates a new user with a role (ADMIN, PROVIDER or CUSTOMER). Returns a JWT token on success.

```http
POST /api/auth/register
Content-Type: application/json
```

```json
{
  "fullName": "John Doe",
  "email": "john@example.com",
  "phoneNumber": "+251911123456",
  "password": "Password123!",
  "role": "CUSTOMER"
}
```

**Status codes:** `201 Created`, `400 Bad Request` (validation), `409 Conflict` (duplicate email/phone or role failure).

### 4.2 Login

Authenticates a user by email OR phone number plus password. Returns a JWT token on success.

```http
POST /api/auth/login
Content-Type: application/json
```

```json
{
  "emailOrPhone": "john@example.com",
  "password": "Password123!"
}
```

**Status codes:** `200 OK`, `400 Bad Request` (validation), `401 Unauthorized` (invalid credentials).

### 4.3 Logout

**Not implemented.** The API is stateless (JWT) and does not expose a logout endpoint. Clients should simply discard the token.

### 4.4 OTP endpoints

One-time-password flow used for phone/email verification and password reset. OTPs are 6-digit, valid for **5 minutes**, single-use. The generated code is returned in the response **only in the Development environment**.

#### 4.4.1 Send OTP

```http
POST /api/auth/send-otp
```

```json
{
  "userId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
  "purpose": "PhoneVerification"
}
```

`purpose` ∈ `PhoneVerification | PasswordReset | EmailVerification`

#### 4.4.2 Verify OTP

```http
POST /api/auth/verify-otp
```

```json
{
  "userId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
  "code": "482931",
  "purpose": "PhoneVerification"
}
```

Verifying a `PhoneVerification` or `EmailVerification` OTP marks the user as `IsVerified = true`.

#### 4.4.3 Resend OTP

```http
POST /api/auth/resend-otp
```

```json
{
  "userId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
  "purpose": "PhoneVerification"
}
```

Resend invalidates all previous active OTPs for that user + purpose and issues a new one.

**Status codes (OTP endpoints):** `200 OK`, `400 Bad Request` (validation / invalid or expired code), `404 Not Found` (user does not exist).

### 4.5 Refresh Token

**Not implemented.** No refresh-token endpoint exists. Access tokens expire after the JWT lifetime (see section 5) and the client must re-authenticate via login.

---

## 5. JWT (JSON Web Token)

Protected endpoints require the token in the **Authorization** header:

```
Authorization: Bearer <token>
```

### 5.1 Token configuration

| Property | Value |
|---|---|
| Signing algorithm | HS256 (HMAC-SHA256) symmetric key |
| Issuer | `CreaversAPI` |
| Audience | `CreaversClients` |
| Expiration | 60 minutes (configurable, `JwtSettings:ExpirationInMinutes`) |
| Clock skew | 0 seconds |

### 5.2 Claims contained in the token

| Claim | Value |
|---|---|
| `sub` | User id (GUID) |
| `email` | User email |
| `jti` | Unique token id (GUID) |
| `nameid` | User id (GUID) — ClaimTypes.NameIdentifier |
| `email` (ClaimTypes.Email) | User email |
| `phone_number` (ClaimTypes.MobilePhone) | User phone number |
| `role` | Single role (ADMIN, PROVIDER or CUSTOMER) |
| `FullName` | User full name |

> Note: the controller helper reads the user id from `ClaimTypes.NameIdentifier` first, falling back to the `sub` claim.

---

## 6. User Roles

Three roles are seeded on startup. A user has exactly one role.

| Role | Description |
|---|---|
| ADMIN | Full platform access. Manages categories, approves/rejects provider profiles, views all tasks and all users. |
| PROVIDER | Service provider. Creates and updates their provider profile and waits for admin approval. |
| CUSTOMER | End consumer. Creates, updates, lists and deletes their own tasks. |

---

## 7. API Endpoints

Legend — Auth: **Public** = no token required, **JWT** = valid Bearer token required. Roles: "-" means any authenticated role.

| Method | Endpoint | Auth | Roles | Description |
|---|---|---|---|---|
| GET | `/api/health` | Public | — | Liveness/health check |
| POST | `/api/auth/register` | Public | — | Register a new user |
| POST | `/api/auth/login` | Public | — | Login with email/phone + password |
| POST | `/api/auth/send-otp` | Public | — | Send OTP to a user |
| POST | `/api/auth/verify-otp` | Public | — | Verify an OTP code |
| POST | `/api/auth/resend-otp` | Public | — | Resend OTP |
| GET | `/api/categories` | Public | — | List all categories |
| GET | `/api/categories/{id}` | Public | — | Get category by id |
| POST | `/api/categories` | JWT | ADMIN | Create category |
| PUT | `/api/categories/{id}` | JWT | ADMIN | Update category |
| DELETE | `/api/categories/{id}` | JWT | ADMIN | Soft-delete category |
| POST | `/api/providers/profile` | JWT | PROVIDER | Create own provider profile |
| GET | `/api/providers/profile` | JWT | — | List all provider profiles |
| GET | `/api/providers/profile/{id}` | JWT | — | Get provider profile by id |
| PUT | `/api/providers/profile` | JWT | PROVIDER | Update own provider profile |
| POST | `/api/tasks` | JWT | CUSTOMER | Create a task (multipart/form-data, optional image) |
| GET | `/api/tasks` | JWT | ADMIN | List all tasks |
| GET | `/api/tasks/my` | JWT | CUSTOMER | List authenticated customer's tasks |
| GET | `/api/tasks/{id}` | JWT | CUSTOMER, ADMIN | Get task by id (CUSTOMER only own tasks) |
| PUT | `/api/tasks/{id}` | JWT | CUSTOMER, ADMIN | Update task (CUSTOMER only own tasks) |
| DELETE | `/api/tasks/{id}` | JWT | CUSTOMER, ADMIN | Soft-delete task (CUSTOMER only own tasks) |
| GET | `/api/users` | JWT | ADMIN | List all users |
| GET | `/api/users/{id}` | JWT | — | Get user by id |
| GET | `/api/admin/providers` | JWT | ADMIN | List all provider profiles |
| GET | `/api/admin/providers/pending` | JWT | ADMIN | List pending provider profiles |
| PATCH | `/api/admin/providers/{id}/approve` | JWT | ADMIN | Approve provider profile |
| PATCH | `/api/admin/providers/{id}/reject` | JWT | ADMIN | Reject provider profile |

---

## 8. Provider Registration

A provider registers in two steps: (1) create the user account with role **PROVIDER**, then (2) create the provider profile. The profile starts with status **Pending** and must be approved by an ADMIN before the provider is active.

### Step 1 — Create the account

```http
POST /api/auth/register
```

```json
{
  "fullName": "Abebe Kebede",
  "email": "abebe@example.com",
  "phoneNumber": "+251911000000",
  "password": "Password123!",
  "role": "PROVIDER"
}
```

**Required fields:** fullName, email, phoneNumber, password, role (must be PROVIDER here). Returns a JWT token with role PROVIDER in the response body.

### Step 2 — Create the provider profile

```http
POST /api/providers/profile
Authorization: Bearer <token>
```

```json
{
  "categoryId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
  "experienceYears": 5,
  "bio": "Certified plumber with 5 years of experience.",
  "serviceArea": "Bole, Addis Ababa",
  "availability": "Mon-Fri 8:00-18:00",
  "nationalId": "ET-1234567890",
  "profilePhoto": null,
  "licenseDocument": null
}
```

**Validation:** categoryId required and must exist; experienceYears 0–60; bio required (max 1000); serviceArea required (max 300); availability required (max 300); nationalId required (max 50). One profile per user — a second profile returns `409 Conflict`.

**Sample response (201 Created):**

```json
{
  "success": true,
  "message": "Provider profile created successfully.",
  "data": {
    "id": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
    "applicationUserId": "b5b4a1f2-...",
    "providerFullName": "Abebe Kebede",
    "providerEmail": "abebe@example.com",
    "categoryId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
    "categoryName": "Plumbing",
    "experienceYears": 5,
    "bio": "Certified plumber with 5 years of experience.",
    "serviceArea": "Bole, Addis Ababa",
    "availability": "Mon-Fri 8:00-18:00",
    "nationalId": "ET-1234567890",
    "profilePhoto": null,
    "licenseDocument": null,
    "status": 0,
    "statusName": "Pending",
    "createdAt": "2026-08-06T09:00:00Z",
    "updatedAt": null
  },
  "errors": null
}
```

---

## 9. Provider Profile

Represented by the **ProviderProfile** entity and returned as **ProviderProfileDto**.

| Field | Type | Description |
|---|---|---|
| id | Guid | Unique profile identifier. |
| applicationUserId | Guid | The owning user account (unique — one profile per user). |
| providerFullName | string | Full name of the provider (from the user account). |
| providerEmail | string | Email of the provider (from the user account). |
| categoryId | Guid | The service category this provider belongs to. |
| categoryName | string | Display name of the category. |
| experienceYears | int | Years of professional experience (0–60). |
| bio | string | Short biography / description (max 1000 chars). |
| serviceArea | string | Geographic area where the provider operates (max 300 chars). |
| availability | string | Text description of working hours / availability (max 300 chars). |
| nationalId | string | Government-issued national ID (required, max 50 chars). Sensitive — avoid exposing publicly. |
| profilePhoto | string? | Optional photo path/URL. |
| licenseDocument | string? | Optional license document path/URL. |
| status | enum (int) | ProviderStatus numeric value: 0 Pending, 1 Approved, 2 Rejected. |
| statusName | string | ProviderStatus as text (e.g. "Pending"). |
| createdAt | DateTime | Profile creation timestamp (UTC). |
| updatedAt | DateTime? | Last update timestamp (UTC), null if never updated. |

---

## 10. Categories

Categories are public to read and admin-managed for write operations. Category names are unique. Deletion is a soft delete.

| Method | Endpoint | Roles | Description |
|---|---|---|---|
| GET | `/api/categories` | Public | List all categories |
| GET | `/api/categories/{id}` | Public | Get a category |
| POST | `/api/categories` | ADMIN | Create a category |
| PUT | `/api/categories/{id}` | ADMIN | Update a category |
| DELETE | `/api/categories/{id}` | ADMIN | Soft-delete a category |

**Sample request (create):**

```http
POST /api/categories
Authorization: Bearer <admin-token>
```

```json
{
  "name": "Plumbing",
  "description": "Plumbing and pipe repair services"
}
```

**Sample response (200/201):**

```json
{
  "success": true,
  "message": "Category created successfully.",
  "data": {
    "id": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
    "name": "Plumbing",
    "description": "Plumbing and pipe repair services",
    "createdAt": "2026-08-06T09:00:00Z",
    "updatedAt": null
  },
  "errors": null
}
```

**CategoryDto fields:** id, name, description, createdAt, updatedAt. **Create/Update request fields:** name (required, max 150), description (optional, max 500).

**Status codes:** `200 OK`, `201 Created`, `400 Bad Request` (validation), `404 Not Found` (update/delete of unknown id).

---

## 11. Validation Rules

All request bodies are validated with FluentValidation before reaching the services. Violations return `400 Bad Request` with a list of error messages.

### 11.1 RegisterRequest

| Field | Rules |
|---|---|
| fullName | Required, max 200 characters |
| email | Required, must be a valid email address |
| phoneNumber | Required, must match `^\+?[0-9\s\-]{7,20}$` |
| password | Required, min 8 chars, must contain uppercase, lowercase, digit and special character |
| role | Required, must be ADMIN, PROVIDER or CUSTOMER |

### 11.2 LoginRequest

| Field | Rules |
|---|---|
| emailOrPhone | Required (email or phone number) |
| password | Required |

### 11.3 OTP requests (SendOtp / VerifyOtp / ResendOtp)

| Field | Rules |
|---|---|
| userId | Required (Guid) |
| purpose | Must be a valid OtpPurpose (PhoneVerification, PasswordReset, EmailVerification) |
| code (verify only) | Required, exactly 6 digits (`^\d{6}$`) |

### 11.4 CreateTaskRequest

| Field | Rules |
|---|---|
| title | Required, max 200 |
| description | Required, max 2000 |
| categoryId | Required |
| address | Required, max 500 |
| subCity | Required, max 100 |
| woreda | Required, max 100 |
| landmark | Optional, max 300 |
| budget | Must be greater than 0 |
| preferredDate | Must be in the future (UTC) |
| latitude | Optional, between -90 and 90 |
| longitude | Optional, between -180 and 180 |

### 11.5 UpdateTaskRequest

Same max-length and range rules as create; budget must be greater than 0 when provided; status must be a valid CustomerTaskStatus. Applied only for non-null fields (partial update).

### 11.6 CreateProviderProfileRequest

| Field | Rules |
|---|---|
| categoryId | Required |
| experienceYears | Between 0 and 60 |
| bio | Required, max 1000 |
| serviceArea | Required, max 300 |
| availability | Required, max 300 |
| nationalId | Required, max 50 |

### 11.7 UpdateProviderProfileRequest

categoryId required; experienceYears 0–60; bio max 1000; serviceArea max 300; availability max 300. **nationalId is not updatable** (ignored by the mapper).

### 11.8 CreateCategoryRequest / UpdateCategoryRequest

| Field | Rules |
|---|---|
| name | Required, max 150 |
| description | Optional, max 500 |

---

## 12. Error Responses

All errors use the **ApiResponse** envelope: `{ "success": false, "message": "...", "data": null, "errors": [...] }`.

| Code | Name | Description |
|---|---|---|
| 400 | Bad Request | Validation failed. Body contains a list of error messages. Also used for invalid/expired OTP. |
| 401 | Unauthorized | Missing, invalid or expired JWT token; invalid login credentials. |
| 403 | Forbidden | Authenticated but not allowed to access the resource (wrong role, or not the owner of the task). |
| 404 | Not Found | Resource does not exist (user, category, task, provider profile). |
| 500 | Internal Server Error | Unhandled exception. Handled by GlobalExceptionMiddleware, which also returns the exception message in the errors array. |

**Validation error example (400):**

```json
{
  "success": false,
  "message": "Validation failed.",
  "data": null,
  "errors": [
    "Title is required.",
    "Budget must be greater than 0."
  ]
}
```

---

## 13. Database Entities

Base abstract entity shared by all domain entities: **Id** (Guid, generated), **CreatedAt** (DateTime, UTC), **UpdatedAt** (DateTime?), **IsDeleted** (bool, soft-delete flag). Soft-deleted rows are excluded by global query filters.

### 13.1 ApplicationUser (extends IdentityUser<Guid>)

| Property | Type | Notes |
|---|---|---|
| Id | Guid | Primary key (Identity user id). |
| UserName | string | Set equal to the email on registration. |
| Email | string | Unique email. |
| PhoneNumber | string | Phone number. |
| PasswordHash | string | Hashed password (Identity). |
| FullName | string | Full name (required, max 200). |
| IsVerified | bool | True after phone/email OTP verification. |
| CreatedAt | DateTime | Account creation time (UTC). |
| UpdatedAt | DateTime? | Last update time. |
| SecurityStamp | string | Identity security stamp. |

**Relationships:** one-to-one with ProviderProfile; one-to-many with OtpCodes and CustomerTasks.

### 13.2 Category (BaseEntity)

| Property | Type | Notes |
|---|---|---|
| Id | Guid | Primary key. |
| Name | string | Required, max 150, unique index. |
| Description | string | Max 500. |

**Relationships:** one-to-many with ProviderProfiles (Restrict) and CustomerTasks (Restrict).

### 13.3 ProviderProfile (BaseEntity)

| Property | Type | Notes |
|---|---|---|
| ApplicationUserId | Guid | FK to ApplicationUser, unique (one profile per user), Cascade. |
| CategoryId | Guid | FK to Category, Restrict. |
| ExperienceYears | int | 0–60 (validated). |
| Bio | string | Max 1000. |
| ServiceArea | string | Max 300. |
| Availability | string | Max 300. |
| NationalId | string | Required, max 50. |
| ProfilePhoto | string? | Optional path. |
| LicenseDocument | string? | Optional path. |
| Status | ProviderStatus | Default Pending (stored as string). |

**Relationships:** one-to-one with ApplicationUser; many-to-one with Category.

### 13.4 OtpCode (BaseEntity)

| Property | Type | Notes |
|---|---|---|
| UserId | Guid | FK to ApplicationUser, Cascade. |
| Code | string | Required, max 6. |
| Purpose | OtpPurpose | PhoneVerification / PasswordReset / EmailVerification (stored as string). |
| ExpiresAt | DateTime | Expiry time (UTC). |
| IsUsed | bool | Default false; true after verification or invalidation. |

**Relationships:** many-to-one with ApplicationUser. Index on (UserId, Purpose).

### 13.5 CustomerTask (BaseEntity)

| Property | Type | Notes |
|---|---|---|
| CustomerId | Guid | FK to ApplicationUser (customer), Cascade. |
| CategoryId | Guid | FK to Category, Restrict. |
| Title | string | Required, max 200. |
| Description | string | Required, max 2000. |
| Address | string | Required, max 500. |
| SubCity | string | Required, max 100 (e.g. Bole, Yeka). |
| Woreda | string | Required, max 100. |
| Landmark | string? | Optional, max 300. |
| Latitude | double? | Optional. |
| Longitude | double? | Optional. |
| Budget | decimal | decimal(18,2). |
| PreferredDate | DateTime | Desired service date. |
| Status | CustomerTaskStatus | Default Pending (stored as string). |
| ImagePath | string? | Optional, max 500. |

**Relationships:** many-to-one with ApplicationUser (customer) and Category. Indexes on CustomerId and CategoryId.

### 13.6 Enums

| Enum | Values |
|---|---|
| ProviderStatus | Pending = 0, Approved = 1, Rejected = 2 |
| OtpPurpose | PhoneVerification = 0, PasswordReset = 1, EmailVerification = 2 |
| CustomerTaskStatus | Pending = 0, Matched = 1, Accepted = 2, Completed = 3, Cancelled = 4 |

---

## 14. Request Examples

### 14.1 Register (POST /api/auth/register)

```json
{
  "fullName": "John Doe",
  "email": "john@example.com",
  "phoneNumber": "+251911123456",
  "password": "Password123!",
  "role": "CUSTOMER"
}
```

### 14.2 Login (POST /api/auth/login)

```json
{
  "emailOrPhone": "john@example.com",
  "password": "Password123!"
}
```

### 14.3 Send OTP (POST /api/auth/send-otp)

```json
{
  "userId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
  "purpose": "PhoneVerification"
}
```

### 14.4 Verify OTP (POST /api/auth/verify-otp)

```json
{
  "userId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
  "code": "482931",
  "purpose": "PhoneVerification"
}
```

### 14.5 Create category (POST /api/categories)

```json
{
  "name": "Plumbing",
  "description": "Plumbing and pipe repair services"
}
```

### 14.6 Create provider profile (POST /api/providers/profile)

```json
{
  "categoryId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
  "experienceYears": 5,
  "bio": "Certified plumber with 5 years of experience.",
  "serviceArea": "Bole, Addis Ababa",
  "availability": "Mon-Fri 8:00-18:00",
  "nationalId": "ET-1234567890",
  "profilePhoto": null,
  "licenseDocument": null
}
```

### 14.7 Create task (POST /api/tasks) — multipart/form-data

The task endpoint consumes **multipart/form-data**. Send each field below as a form field and optionally an **image** file field:

```
title        = "Fix kitchen sink"
description  = "The kitchen sink is leaking and needs a plumber urgently."
categoryId   = "3fa85f64-5717-4562-b3fc-2c963f66afa6"
address      = "Bole Road, near Atlas Hotel"
subCity      = "Bole"
woreda       = "03"
landmark     = "Near Atlas Hotel"
latitude     = 8.9956
longitude    = 38.7636
budget       = 500.00
preferredDate = "2026-08-10T09:00:00Z"
image        = <file, optional>
```

### 14.8 Update task (PUT /api/tasks/{id})

```json
{
  "title": "Fix kitchen sink (updated)",
  "budget": 650.00,
  "status": "Pending"
}
```

### 14.9 Update provider profile (PUT /api/providers/profile)

```json
{
  "categoryId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
  "experienceYears": 6,
  "bio": "Updated bio.",
  "serviceArea": "Bole and Yeka, Addis Ababa",
  "availability": "Mon-Sat 8:00-19:00",
  "profilePhoto": null,
  "licenseDocument": null
}
```

---

## 15. Response Examples

Every response wraps the payload in the **ApiResponse** envelope: `{success, message, data, errors}`.

### 15.1 Login / Register success

```json
{
  "success": true,
  "message": "Login successful.",
  "data": {
    "token": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...",
    "userId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
    "fullName": "John Doe",
    "email": "john@example.com",
    "phoneNumber": "+251911123456",
    "role": "CUSTOMER",
    "expiresAt": "2026-08-06T11:00:00Z"
  },
  "errors": null
}
```

### 15.2 OTP send (Development)

```json
{
  "success": true,
  "message": "OTP sent successfully. It expires in 5 minutes.",
  "data": {
    "message": "OTP sent successfully. It expires in 5 minutes.",
    "otpCode": "482931"
  },
  "errors": null
}
```

### 15.3 Category list

```json
{
  "success": true,
  "message": "Request successful.",
  "data": [
    {
      "id": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
      "name": "Plumbing",
      "description": "Plumbing and pipe repair services",
      "createdAt": "2026-08-06T09:00:00Z",
      "updatedAt": null
    }
  ],
  "errors": null
}
```

### 15.4 Task response (TaskResponse)

```json
{
  "success": true,
  "message": "Task created successfully.",
  "data": {
    "id": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
    "customerId": "b5b4a1f2-0000-0000-0000-000000000000",
    "customerName": "John Doe",
    "categoryId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
    "categoryName": "Plumbing",
    "title": "Fix kitchen sink",
    "description": "The kitchen sink is leaking and needs a plumber urgently.",
    "address": "Bole Road, near Atlas Hotel",
    "subCity": "Bole",
    "woreda": "03",
    "landmark": "Near Atlas Hotel",
    "latitude": 8.9956,
    "longitude": 38.7636,
    "budget": 500.00,
    "preferredDate": "2026-08-10T09:00:00Z",
    "status": 0,
    "imagePath": "uploads/tasks/1e8a2f3b-....jpg",
    "createdAt": "2026-08-06T09:00:00Z",
    "updatedAt": null
  },
  "errors": null
}
```

### 15.5 User list (UserDto)

```json
{
  "success": true,
  "message": "Request successful.",
  "data": [
    {
      "id": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
      "fullName": "John Doe",
      "email": "john@example.com",
      "phoneNumber": "+251911123456",
      "role": "CUSTOMER",
      "isVerified": true,
      "createdAt": "2026-08-06T09:00:00Z"
    }
  ],
  "errors": null
}
```

---

## 16. Integration Guide (Flutter)

### 16.1 Store the base URL

```dart
const String baseUrl = 'http://localhost:5000/api';
```

Use the machine's LAN IP instead of `localhost` when testing on a physical device, and ensure the CORS origins list includes the frontend origin.

### 16.2 Authenticate (login)

```dart
Future<String> login() async {
  final response = await http.post(
    Uri.parse('$baseUrl/auth/login'),
    headers: {'Content-Type': 'application/json'},
    body: jsonEncode({
      'emailOrPhone': 'john@example.com',
      'password': 'Password123!',
    }),
  );

  if (response.statusCode == 200) {
    final body = jsonDecode(response.body) as Map<String, dynamic>;
    final data = body['data'] as Map<String, dynamic>;
    return data['token'] as String; // store securely (e.g. flutter_secure_storage)
  }
  throw Exception('Login failed: ${response.body}');
}
```

### 16.3 Register

```http
POST /api/auth/register
```

Body: `{ "fullName": "...", "email": "...", "phoneNumber": "...", "password": "...", "role": "CUSTOMER" }` (or `PROVIDER`). Response `201` → token at `body["data"]["token"]`.

### 16.4 OTP verification

1. Call **POST /api/auth/send-otp** with `{userId, purpose}`.
2. Call **POST /api/auth/verify-otp** with `{userId, code, purpose}` — success marks the user verified.
3. Optionally call **POST /api/auth/resend-otp** to issue a new code (invalidates previous ones).

The OTP code is only returned in the Development environment.

### 16.5 Send the Bearer token on protected calls

```dart
Future<http.Response> protectedGet(String path, String token) async {
  return http.get(
    Uri.parse('$baseUrl$path'),
    headers: {
      'Content-Type': 'application/json',
      'Authorization': 'Bearer $token', // <-- required for protected endpoints
    },
  );
}
```

### 16.6 Which endpoints require authentication

| Access | Endpoints |
|---|---|
| Public (no token) | `POST /api/auth/register`, `/login`, `/send-otp`, `/verify-otp`, `/resend-otp`; `GET /api/categories`, `/api/categories/{id}`; `GET /api/health` |
| Any authenticated role | `GET /api/providers/profile`, `GET /api/providers/profile/{id}`, `GET /api/users/{id}` |
| CUSTOMER only | `POST /api/tasks`, `GET /api/tasks/my` (and `/api/tasks/{id}`, `PUT/DELETE /api/tasks/{id}` for own tasks) |
| PROVIDER only | `POST /api/providers/profile`, `PUT /api/providers/profile` |
| ADMIN only | `POST/PUT/DELETE /api/categories`, `GET /api/tasks`, `GET /api/users`, `GET /api/admin/providers`, `GET /api/admin/providers/pending`, `PATCH /api/admin/providers/{id}/approve`, `PATCH /api/admin/providers/{id}/reject` |
| CUSTOMER or ADMIN | `GET/PUT/DELETE /api/tasks/{id}` |

### 16.7 Important notes

- The token expires after 60 minutes. On a `401` response, re-authenticate via login.
- Task creation is **multipart/form-data**; send fields as form fields and attach the image as a file part.
- Response envelope: check `success` first, then read `data`. On failure read `errors`.
- All timestamps are UTC (ISO 8601, e.g. `2026-08-06T09:00:00Z`).
- Enum values are returned as numbers (0, 1, 2, ...) with a `StatusName` text companion where applicable.
