import type { Meta, StoryObj } from "@storybook/react";
import { ThemeToggle } from "@almomiaa/ui";

const meta = {
  title: "ERP/Preferences/ThemeToggle",
  component: ThemeToggle,
  tags: ["autodocs"],
  args: { value: "light", onChange: () => undefined },
} satisfies Meta<typeof ThemeToggle>;

export default meta;
type Story = StoryObj<typeof meta>;

export const ArabicRTL: Story = {
  args: {
    value: "light",
    labels: {
      light: "التبديل إلى الوضع الداكن",
      dark: "التبديل إلى الوضع الفاتح",
    },
  },
  parameters: { direction: "rtl" },
};

export const EnglishLTR: Story = {
  args: { value: "dark" },
  globals: { theme: "dark" },
  parameters: { direction: "ltr" },
};
