import type { Meta, StoryObj } from "@storybook/react";
import { JsonResponseViewer } from "@almomiaa/ui";

const meta = {
  title: "ERP/Data/JsonResponseViewer",
  component: JsonResponseViewer,
  tags: ["autodocs"],
  args: {
    method: "POST",
    endpoint: "/api/v1/orders",
    status: 201,
    durationMs: 42,
    timestamp: "2026-11-23T12:45:10.000Z",
    requestPayload: {
      orderChannel: "PreSales",
      customerId: "01933c90-9999-7000-8000-abcdef123456",
      warehouseId: 1,
    },
    payload: {
      id: "01933c91-0001-7000-8000-abcdef654321",
      orderNumber: "SO-20261123-8F21A90C",
      status: "PendingApproval",
      totalAmount: 1150.0,
    },
  },
} satisfies Meta<typeof JsonResponseViewer>;

export default meta;
type Story = StoryObj<typeof meta>;

export const Success201RTL: Story = {
  parameters: { direction: "rtl" },
};

export const ValidationError400Dark: Story = {
  args: {
    status: 400,
    payload: {
      status: 400,
      title: "Validation failed",
      errors: { "Request.VatNumber": ["Saudi VAT number must be exactly 15 digits."] },
    },
  },
  globals: { theme: "dark" },
  parameters: { direction: "ltr" },
};
