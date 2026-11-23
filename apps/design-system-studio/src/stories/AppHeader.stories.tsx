import type { Meta, StoryObj } from "@storybook/react";
import { AppHeader } from "@almomiaa/ui";

const meta = {
  title: "ERP/Layout/AppHeader",
  component: AppHeader,
  tags: ["autodocs"],
  parameters: { layout: "fullscreen" },
  args: {
    title: "لوحة التحكم",
    locale: "ar-SA",
    theme: "light",
    onLocaleChange: () => undefined,
    onThemeChange: () => undefined,
  },
} satisfies Meta<typeof AppHeader>;

export default meta;
type Story = StoryObj<typeof meta>;

export const ArabicRTL: Story = {
  args: { title: "لوحة التحكم", locale: "ar-SA" },
  parameters: { direction: "rtl" },
};

export const EnglishLTR: Story = {
  args: { title: "Dashboard", locale: "en-US" },
  parameters: { direction: "ltr" },
};

export const CompactMobileHeader: Story = {
  args: { title: "Dashboard", locale: "en-US", onMenuClick: () => undefined },
  parameters: { direction: "ltr", viewport: { defaultViewport: "mobile" } },
};

export const ArabicMobileHeader: Story = {
  args: { title: "لوحة التحكم", locale: "ar-SA", onMenuClick: () => undefined },
  parameters: { direction: "rtl", viewport: { defaultViewport: "mobile" } },
};

export const DarkTheme: Story = {
  args: { title: "Dashboard", locale: "en-US", theme: "dark" },
  globals: { theme: "dark" },
  parameters: { direction: "ltr" },
};
