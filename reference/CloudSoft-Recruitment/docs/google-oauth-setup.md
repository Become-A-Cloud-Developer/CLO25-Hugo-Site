# Google OAuth Setup Guide

This guide walks through configuring Google OAuth for the CloudSoft Recruitment Portal using a personal Google account (not Google Workspace).

## Prerequisites

- A personal Google account (any Gmail account works)
- The application running locally with HTTPS (`dotnet run --launch-profile https`)

## Step 1: Create a Google Cloud Project

1. Go to [Google Cloud Console](https://console.cloud.google.com/)
2. Sign in with your Google account
3. Click the project dropdown at the top of the page → **New Project**
4. Enter project name: `CloudSoft Recruitment` (or similar)
5. Click **Create**
6. Make sure the new project is selected in the project dropdown

## Step 2: Configure the OAuth Consent Screen

1. In the left sidebar, go to **APIs & Services** → **OAuth consent screen**
2. Select **External** as the user type (this is the only option for personal accounts)
3. Click **Create**
4. Fill in the required fields:
   - **App name**: `CloudSoft Recruitment Portal`
   - **User support email**: select your email
   - **Developer contact information**: enter your email
5. Click **Save and Continue**
6. On the **Scopes** page, click **Add or Remove Scopes**
   - Select `email` and `profile` (or search for `openid`, `email`, `profile`)
   - Click **Update**
7. Click **Save and Continue**
8. On the **Test users** page, click **Add Users**
   - Add the Google email addresses that should be allowed to log in during testing
   - While the app is in "Testing" publishing status, only these users can authenticate
9. Click **Save and Continue**
10. Review and click **Back to Dashboard**

## Step 3: Create OAuth Client Credentials

1. In the left sidebar, go to **APIs & Services** → **Credentials**
2. Click **Create Credentials** → **OAuth client ID**
3. Select **Web application** as the application type
4. Enter name: `CloudSoft Web App`
5. Under **Authorized JavaScript origins**, add:

   For local development:
   ```
   https://localhost:7296
   ```

   For Azure Container Apps (add later when deployed):
   ```
   https://<your-app>.azurecontainerapps.io
   ```

6. Under **Authorized redirect URIs**, add:

   For local development:
   ```
   https://localhost:7296/signin-google
   ```

   For Azure Container Apps (add later when deployed):
   ```
   https://<your-app>.azurecontainerapps.io/signin-google
   ```

7. Click **Create**
8. A dialog shows your **Client ID** and **Client Secret** — copy both values

Note: `/signin-google` is ASP.NET Core's default callback path for the Google authentication middleware. The middleware handles the OAuth token exchange at this URL, then redirects internally.

## Step 4: Configure the Application

### Option A: Local Development (`dotnet run --launch-profile https`)

Store credentials in .NET User Secrets (never in appsettings.json):

```bash
cd /path/to/project
dotnet user-secrets set "Google:ClientId" "your-client-id.apps.googleusercontent.com" --project src/CloudSoft.Web
dotnet user-secrets set "Google:ClientSecret" "your-client-secret" --project src/CloudSoft.Web
dotnet user-secrets set "FeatureFlags:UseGoogleAuth" "true" --project src/CloudSoft.Web
```

Alternatively, create a `.env` file in the project root (already gitignored) and set the credentials there. The `.env.example` file shows the format.

### Option B: Azure Production

Store credentials in Key Vault:

```bash
az keyvault secret set --vault-name <your-vault> --name "Google--ClientId" --value "your-client-id.apps.googleusercontent.com"
az keyvault secret set --vault-name <your-vault> --name "Google--ClientSecret" --value "your-client-secret"
```

The `UseGoogleAuth` flag is already `true` in `appsettings.Production.json`.

Remember to add the Azure Container Apps URL to both the authorized JavaScript origins and redirect URIs in Google Cloud Console.

## Step 5: Run and Verify

Start the application with HTTPS:

```bash
dotnet run --project src/CloudSoft.Web --launch-profile https
```

Then:

1. Open `https://localhost:7296/Account/Login` in your browser
2. Accept the self-signed certificate warning if prompted
3. The "Sign in with Google" button should be visible
4. Click it and authenticate with one of the test users added in Step 2
5. On first login, a Candidate account is automatically created
6. You should be redirected to the home page, logged in as a Candidate

## Important Notes

### HTTPS Required

Google OAuth requires HTTPS. The application must be run with the `https` launch profile (`dotnet run --launch-profile https`). The HTTP-only profile and Docker Compose do not support Google OAuth — the `UseGoogleAuth` feature flag is set to `false` in those environments.

### Authorized JavaScript Origins

Google Cloud Console requires both **Authorized JavaScript origins** (e.g., `https://localhost:7296`) and **Authorized redirect URIs** (e.g., `https://localhost:7296/signin-google`). Missing either one will result in a `redirect_uri_mismatch` error.

### Testing vs Production Publishing Status

While the consent screen is in **Testing** status:
- Only users added as test users can authenticate
- The consent screen shows a warning ("This app isn't verified")
- Tokens expire after 7 days (users must re-consent)

To allow any Google user to sign in, you would need to publish the app (click **Publish App** on the OAuth consent screen). For a course reference implementation, staying in Testing status is fine — just add the student email addresses as test users.

### The "Sign in with Google" Button

The button only appears on the login page when both conditions are met:
1. `FeatureFlags:UseGoogleAuth` is `true`
2. `Google:ClientId` and `Google:ClientSecret` are configured with non-empty values

If either condition is not met, the button is hidden and the application works with email/password login only.

### What Happens on First Google Login

1. User clicks "Sign in with Google" and authenticates with Google
2. Google redirects back to `/signin-google` with an authorization code
3. The ASP.NET Core middleware exchanges the code for the user's email and name
4. If no account exists with that email, a new account is created with the **Candidate** role
5. The user is signed in via a cookie (same as email/password login from this point on)
6. On subsequent logins, the existing account is reused

### Automated Testing

Google OAuth cannot be tested with Playwright or automated tests because it requires interactive login on Google's page (credentials, possibly 2FA). The automated test suite uses email/password login instead. Google OAuth is verified manually.
