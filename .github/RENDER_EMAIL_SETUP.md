# Email Configuration for Render

## Environment Variables Required

Add these to **Render Dashboard → Environment**:

### Production (Render)

```
SMTP_HOST = smtp.gmail.com
SMTP_PORT = 587
SMTP_USERNAME = caohuutritl1234@gmail.com
SMTP_PASSWORD = thsd bqcd eaua zife
SMTP_ENABLE_SSL = true
EMAIL_FROM = caohuutritl1234@gmail.com
```

## Setup Instructions

### 1. Go to Render Dashboard
- Open your service: `swd-project-api`
- Click **Environment** tab

### 2. Add Environment Variables
Click **Add Environment Variable** and add:

| Key | Value |
|-----|-------|
| `SMTP_HOST` | `smtp.gmail.com` |
| `SMTP_PORT` | `587` |
| `SMTP_USERNAME` | `caohuutritl1234@gmail.com` |
| `SMTP_PASSWORD` | `thsd bqcd eaua zife` |
| `SMTP_ENABLE_SSL` | `true` |
| `EMAIL_FROM` | `caohuutritl1234@gmail.com` |

### 3. Redeploy
- Click **Manual Deploy** → **Deploy latest commit**
- Or push code to trigger auto-deploy

## How It Works

- **Local:** Uses hardcoded values (fallback)
- **Render:** Uses environment variables (secure)

## Security

✅ Email credentials are NOT in git history  
✅ Can rotate password without code changes  
✅ Different credentials per environment

## Testing

After adding environment variables and deploying, test:

```http
POST https://swd-project-api.onrender.com/api/users
Authorization: Bearer {admin_token}
{
  "email": "test@example.com",
  "fullName": "Test User",
  "roleId": 3,
  "orgId": 1
}
```

Check email at `test@example.com` for the password!

## Troubleshooting

If email still doesn't send:
- Verify environment variables are set correctly
- Check Render logs for SMTP errors
- Port 587 may still be blocked (use password from response as fallback)
