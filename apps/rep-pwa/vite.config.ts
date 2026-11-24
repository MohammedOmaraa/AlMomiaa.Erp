import { defineConfig } from "vite";
import react from "@vitejs/plugin-react";
import { VitePWA } from "vite-plugin-pwa";

const backendOrigin = process.env.VITE_BACKEND_API_ORIGIN ?? "http://localhost:5000";

export default defineConfig({
  plugins: [
    react(),
    VitePWA({
      registerType: "autoUpdate",
      includeAssets: ["icons/pwa-192x192.png", "icons/pwa-512x512.png"],
      manifest: {
        name: "المومياء للمبيعات | AlMomiaa Rep",
        short_name: "AlMomiaa Rep",
        description: "تطبيق مناديب المبيعات لشركة المومياء للتجارة — إرسال طلبات البيع ومتابعة الاعتماد الفوري",
        start_url: "/",
        scope: "/",
        display: "standalone",
        orientation: "portrait",
        dir: "rtl",
        lang: "ar",
        theme_color: "#1C1B1A",
        background_color: "#F8F7F4",
        icons: [
          {
            src: "/icons/pwa-192x192.png",
            sizes: "192x192",
            type: "image/png",
            purpose: "any"
          },
          {
            src: "/icons/pwa-512x512.png",
            sizes: "512x512",
            type: "image/png",
            purpose: "any maskable"
          }
        ]
      },
      workbox: {
        navigateFallback: "/index.html",
        navigateFallbackDenylist: [/^\/api\//, /^\/hubs\//, /^\/health$/],
        runtimeCaching: [
          {
            urlPattern: ({ url }) =>
              url.pathname.startsWith("/api/") ||
              url.pathname.startsWith("/hubs/") ||
              url.pathname === "/health",
            handler: "NetworkOnly"
          }
        ]
      },
      devOptions: {
        enabled: true
      }
    })
  ],
  server: {
    port: 5173,
    strictPort: true,
    proxy: {
      "/api": {
        target: backendOrigin,
        changeOrigin: false
      },
      "/health": {
        target: backendOrigin,
        changeOrigin: false
      },
      "/hubs": {
        target: backendOrigin,
        changeOrigin: false,
        ws: true
      }
    }
  }
});
