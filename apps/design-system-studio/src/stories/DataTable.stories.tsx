import type { Meta, StoryObj } from "@storybook/react";
import { Badge, DataTable, type DataTableColumn } from "@almomiaa/ui";

type SampleOrder = {
  id: string;
  orderNumber: string;
  customer: string;
  status: string;
  totalSar: string;
};

const sampleRows: SampleOrder[] = [
  { id: "1", orderNumber: "SO-20261123-A102", customer: "أسواق النخبة المركزية", status: "Approved", totalSar: "14,250.00 ر.س" },
  { id: "2", orderNumber: "SO-20261123-B209", customer: "تموينات الرياض الحديثة", status: "PendingApproval", totalSar: "3,890.50 ر.س" },
];

const columns: DataTableColumn<SampleOrder>[] = [
  { key: "orderNumber", header: "رقم الطلب", mono: true, render: (r) => r.orderNumber },
  { key: "customer", header: "العميل", render: (r) => r.customer },
  {
    key: "status",
    header: "الحالة",
    render: (r) => <Badge tone={r.status === "Approved" ? "success" : "warning"}>{r.status}</Badge>,
  },
  { key: "total", header: "الإجمالي", align: "end", numeric: true, render: (r) => r.totalSar },
];

const meta = {
  title: "ERP/Data/DataTable",
  component: DataTable<SampleOrder>,
  tags: ["autodocs"],
  args: {
    columns,
    rows: sampleRows,
    rowKey: (r) => r.id,
  },
} satisfies Meta<typeof DataTable<SampleOrder>>;

export default meta;
type Story = StoryObj<typeof meta>;

export const ArabicRTL: Story = {
  parameters: { direction: "rtl" },
};

export const LoadingSkeleton: Story = {
  args: { loading: true, rows: [] },
  parameters: { direction: "rtl" },
};

export const Empty: Story = {
  args: {
    rows: [],
    emptyTitle: "لا توجد طلبات مبيعات",
    emptyDescription: "ابدأ بإنشاء أول طلب مبيعات ليظهر هنا مباشرة.",
  },
  parameters: { direction: "rtl" },
};

export const DarkLTR: Story = {
  globals: { theme: "dark" },
  parameters: { direction: "ltr" },
};
