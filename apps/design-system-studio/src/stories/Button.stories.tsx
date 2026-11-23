import type { Meta, StoryObj } from "@storybook/react";
import { Button } from "@almomiaa/ui";

const meta = {
  title: "ERP/Actions/Button",
  component: Button,
  tags: ["autodocs"],
  args: { children: "حفظ التغييرات", variant: "primary" },
} satisfies Meta<typeof Button>;

export default meta;
type Story = StoryObj<typeof meta>;

export const ArabicRTL: Story = {
  args: { children: "حفظ التغييرات" },
  parameters: { direction: "rtl" },
};

export const EnglishLTR: Story = {
  args: { children: "Save changes" },
  parameters: { direction: "ltr" },
};

export const Loading: Story = {
  args: { children: "جاري الحفظ", loading: true },
  parameters: { direction: "rtl" },
};

export const Disabled: Story = {
  args: { children: "غير متاح", disabled: true },
  parameters: { direction: "rtl" },
};

export const Variants: Story = {
  render: () => (
    <div className="flex flex-wrap gap-3">
      <Button>Primary</Button>
      <Button variant="secondary">Secondary</Button>
      <Button variant="ghost">Ghost</Button>
      <Button variant="danger">Danger</Button>
    </div>
  ),
  parameters: { direction: "ltr" },
};

export const DarkThemeVariants: Story = {
  render: () => (
    <div className="flex flex-wrap gap-3">
      <Button>Primary</Button>
      <Button variant="secondary">Secondary</Button>
      <Button variant="ghost">Ghost</Button>
      <Button variant="danger">Danger</Button>
    </div>
  ),
  globals: { theme: "dark" },
  parameters: { direction: "ltr" },
};
