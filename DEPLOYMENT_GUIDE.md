# Cloud Deployment Guide: Render & Vercel

This guide provides simple, step-by-step instructions to deploy your C# backend to **Render** and your Angular frontend to **Vercel**, all linked to your live **Supabase** database.

---

## Part 1: Deploy the Backend API to Render

Render will build and host your C# Web API using the `Dockerfile` we added.

1. **Sign Up**: Go to [Render.com](https://render.com) and click **Sign Up**. Choose **GitHub** to log in.
2. **Create Web Service**: On your Render Dashboard, click the blue **New +** button and select **Web Service**.
3. **Connect Repository**: 
   - Under *Connect a repository*, you should see your `pockemon_game_asignment` repository. Click **Connect**.
   - *(If you don't see it, click "Configure GitHub App" to give Render permissions to access your private repositories).*
4. **Configure Settings**:
   - **Name**: `pokemon-trainer-api` (or any name you prefer)
   - **Region**: Select the region closest to your database (e.g., `Singapore` or any Asian region to match Tokyo).
   - **Branch**: `main`
   - **Root Directory**: `pokemon-backend` *(Very Important: tells Render to look in the backend subfolder)*
   - **Runtime**: Select **Docker** *(Render will automatically find the Dockerfile inside that folder)*
   - **Instance Type**: Select **Free**
5. **Deploy**: Scroll to the bottom and click **Deploy Web Service**.
6. **Copy URL**: Wait 2-3 minutes for the build to finish. Once it says "Live", copy your public API URL at the top left of the Render page (it will look like `https://pokemon-trainer-api.onrender.com`).

---

## Part 2: Deploy the Frontend to Vercel

Vercel will host your Angular user interface.

1. **Sign Up**: Go to [Vercel.com](https://vercel.com) and sign in using your **GitHub** account.
2. **Create Project**: Click **Add New** at the top right and select **Project**.
3. **Import Repository**: Locate your `pockemon_game_asignment` repository and click **Import**.
4. **Configure Settings**:
   - **Project Name**: `pokemon-trainer-portal`
   - **Framework Preset**: Vercel will automatically detect Angular. If not, select **Angular** from the list.
   - **Root Directory**: Click *Edit* and select **`pokemon-frontend`** *(Very Important: tells Vercel where the Angular app lives)*
   - Keep all build commands and outputs as default.
5. **Deploy**: Click the blue **Deploy** button. Vercel will build the Angular app and give you a live preview link in under 2 minutes!

---

## Part 3: Connect Frontend to Backend

Now we need to update the frontend to talk to your live Render backend API instead of `localhost:5088`.

1. Open your code in VS Code.
2. Open the file: [environment.prod.ts](pokemon-frontend/src/environments/environment.prod.ts)
3. Change the `apiUrl` value to your Render backend URL (keep `/api` at the end):
   ```typescript
   export const environment = {
     production: true,
     apiUrl: 'https://your-render-app-name.onrender.com/api' // <-- Paste your Render URL here
   };
   ```
4. Save the file.
5. Commit and push the changes to GitHub:
   ```bash
   git add -A
   git commit -m "Update production API URL with Render deployment link"
   git push origin main
   ```
6. **Vercel will automatically detect the push**, rebuild the frontend, and apply the update in real-time!

---

## Verification
- Open your Vercel URL (e.g. `https://pokemon-trainer-portal.vercel.app`).
- Try registering a new trainer (e.g. `alon_cloud`) and verify you are logged in and can search/build teams immediately!
