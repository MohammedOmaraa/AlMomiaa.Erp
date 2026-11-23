import type { Meta, StoryObj } from "@storybook/react";
import { LocaleSwitcher } from "@almomiaa/ui";

const meta = {
  title: "ERP/Preferences/LocaleSwitcher",
  component: LocaleSwitcher,
  tags: ["autodocs"],
  args: { value: "ar-SA", onChange: () => undefined },
} satisfies Meta<typeof LocaleSwitcher>;

export default meta;
type Story = StoryObj<typeof meta>;

export const ArabicRTL: Story = {
  args: { value: "ar-SA", label: "اللغة" },
  parameters: { direction: "rtl" },
};

export const EnglishLTR: Story = {
  args: { value: "en-US", label: "Language" },
  parameters: { direction: "ltr" },
};

export const DarkTheme: Story = {
  args: { value: "en-US", label: "Language" },
  globals: { theme: "dark" },
  parameters: { direction: "ltr" },
};
