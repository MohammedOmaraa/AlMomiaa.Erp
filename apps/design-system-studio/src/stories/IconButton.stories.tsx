import type { Meta, StoryObj } from "@storybook/react";
import { IconButton } from "@almomiaa/ui";

const meta = {
  title: "ERP/Actions/IconButton",
  component: IconButton,
  tags: ["autodocs"],
  args: { label: "إشعارات", children: <span aria-hidden="true">{"\u2605"}</span> },
} satisfies Meta<typeof IconButton>;

export default meta;
type Story = StoryObj<typeof meta>;

export const ArabicRTL: Story = {
  args: { label: "إشعارات" },
  parameters: { direction: "rtl" },
};

export const EnglishLTR: Story = {
  args: { label: "Notifications" },
  parameters: { direction: "ltr" },
};

export const SolidVariant: Story = {
  args: { label: "Quick action", variant: "solid" },
  parameters: { direction: "ltr" },
};

export const Disabled: Story = {
  args: { label: "Notifications", disabled: true },
  parameters: { direction: "ltr" },
};

export const DarkTheme: Story = {
  args: { label: "Notifications", variant: "subtle" },
  globals: { theme: "dark" },
  parameters: { direction: "ltr" },
};
