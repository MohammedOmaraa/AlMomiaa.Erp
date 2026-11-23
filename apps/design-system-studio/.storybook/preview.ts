import React from "react";
import type { Preview } from "@storybook/react";
import "@almomiaa/ui/tokens.css";
import "../src/stories/studio.css";

const preview: Preview = {
  globalTypes: {
    theme: {
      description: "AlMomiaa color theme",
      toolbar: {
        title: "Theme",
        icon: "paintbrush",
        items: [
          { value: "light", title: "Light" },
          { value: "dark", title: "Dark" },
        ],
        dynamicTitle: true,
      },
    },
  },
  initialGlobals: {
    theme: "light",
  },
  parameters: {
    layout: "padded",
    viewport: {
      viewports: {
        mobile: { name: "Mobile 375", styles: { width: "375px", height: "812px" } },
        tablet: { name: "Tablet 768", styles: { width: "768px", height: "1024px" } },
        desktop: { name: "Desktop 1280", styles: { width: "1280px", height: "900px" } },
      },
      defaultViewport: "desktop",
    },
    a11y: { test: "error" },
    controls: { expanded: true },
  },
  decorators: [
    (Story, context) => {
      const direction = context.parameters.direction === "ltr" ? "ltr" : "rtl";
      const locale = direction === "rtl" ? "ar-SA" : "en-US";
      const theme = context.globals.theme === "dark" ? "dark" : "light";
      const isFullscreen = context.parameters.layout === "fullscreen";

      if (typeof document !== "undefined") {
        document.documentElement.dir = direction;
        document.documentElement.lang = locale;
        document.documentElement.dataset.theme = theme;
      }

      return React.createElement(
        "div",
        {
          dir: direction,
          lang: locale,
          "data-theme": theme,
          className: `min-h-screen bg-surface-canvas text-text-primary ${isFullscreen ? "" : "p-4 sm:p-6"}`,
        },
        React.createElement(Story)
      );
    },
  ],
};

export default preview;
