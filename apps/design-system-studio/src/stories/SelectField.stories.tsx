import type { Meta, StoryObj } from "@storybook/react";
import { SelectField } from "@almomiaa/ui";

const meta = {
  title: "ERP/Forms/SelectField",
  component: SelectField,
  tags: ["autodocs"],
  args: {
    label: "نوع المستودع",
    required: true,
    options: [
      { value: "MainWarehouse", label: "مستودع رئيسي (MainWarehouse)" },
      { value: "BranchWarehouse", label: "مستودع فرعي (BranchWarehouse)" },
      { value: "MobileVan", label: "سيارة توزيع (MobileVan)" },
    ],
  },
} satisfies Meta<typeof SelectField>;

export default meta;
type Story = StoryObj<typeof meta>;

export const ArabicRTL: Story = {
  parameters: { direction: "rtl" },
};

export const WithErrorLTR: Story = {
  args: {
    label: "Customer Category",
    placeholderOption: "Select category...",
    error: "Category must be one of: Market, Store, Restaurant, Supermarket, Wholesale.",
  },
  parameters: { direction: "ltr" },
};

export const DarkTheme: Story = {
  globals: { theme: "dark" },
  parameters: { direction: "ltr" },
};
