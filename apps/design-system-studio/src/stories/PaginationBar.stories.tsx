import { useState } from "react";
import type { Meta, StoryObj } from "@storybook/react";
import { PaginationBar } from "@almomiaa/ui";

const meta = {
  title: "ERP/Data/PaginationBar",
  component: PaginationBar,
  tags: ["autodocs"],
  args: {
    page: 1,
    pageSize: 25,
    totalCount: 142,
    onPageChange: () => undefined,
  },
} satisfies Meta<typeof PaginationBar>;

export default meta;
type Story = StoryObj<typeof meta>;

function InteractivePaginationDemo() {
  const [page, setPage] = useState(1);
  const [pageSize, setPageSize] = useState(25);
  return (
    <PaginationBar
      page={page}
      pageSize={pageSize}
      totalCount={142}
      onPageChange={setPage}
      onPageSizeChange={(n) => {
        setPageSize(n);
        setPage(1);
      }}
      labels={{ previous: "السابق", next: "التالي", showing: "عرض", of: "من", page: "صفحة", perPage: "الصفوف" }}
    />
  );
}

export const InteractiveRTL: Story = {
  render: () => <InteractivePaginationDemo />,
  parameters: { direction: "rtl" },
};

export const EnglishDark: Story = {
  globals: { theme: "dark" },
  parameters: { direction: "ltr" },
};
