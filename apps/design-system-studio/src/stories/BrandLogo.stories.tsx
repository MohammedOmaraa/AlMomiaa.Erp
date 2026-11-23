import type { Meta, StoryObj } from "@storybook/react";
import { BrandLogo } from "@almomiaa/ui";

const meta = {
  title: "ERP/Brand/BrandLogo",
  component: BrandLogo,
  tags: ["autodocs"],
  args: { label: "المومياء" },
} satisfies Meta<typeof BrandLogo>;

export default meta;
type Story = StoryObj<typeof meta>;

export const ArabicRTL: Story = {
  args: { label: "المومياء" },
  parameters: { direction: "rtl" },
};

export const EnglishLTR: Story = {
  args: { label: "AlMomiaa" },
  parameters: { direction: "ltr" },
};

export const CompactArabicTextFallback: Story = {
  args: { label: "المومياء", compact: true },
  parameters: { direction: "rtl" },
};

export const CompactEnglishTextFallback: Story = {
  args: { label: "AlMomiaa", compact: true },
  parameters: { direction: "ltr" },
};

export const DarkSurface: Story = {
  args: { label: "AlMomiaa" },
  globals: { theme: "dark" },
  parameters: { direction: "ltr" },
};
