import { useState } from "react";
import type { Meta, StoryObj } from "@storybook/react";
import { BottomNavBar } from "@almomiaa/ui";

const meta: Meta<typeof BottomNavBar> = {
  title: "Navigation/BottomNavBar",
  component: BottomNavBar,
  tags: ["autodocs"],
  parameters: {
    layout: "fullscreen"
  }
};

export default meta;
type Story = StoryObj<typeof BottomNavBar>;

const HomeIcon = (
  <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth={2} className="h-5 w-5">
    <path strokeLinecap="round" strokeLinejoin="round" d="M3 10.5 12 3l9 7.5V20a1 1 0 0 1-1 1h-5v-6H9v6H4a1 1 0 0 1-1-1v-9.5Z" />
  </svg>
);

const PlusIcon = (
  <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth={2} className="h-5 w-5">
    <path strokeLinecap="round" strokeLinejoin="round" d="M12 5v14m-7-7h14" />
  </svg>
);

const ListIcon = (
  <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth={2} className="h-5 w-5">
    <path strokeLinecap="round" strokeLinejoin="round" d="M8 6h13M8 12h13M8 18h13M3 6h.01M3 12h.01M3 18h.01" />
  </svg>
);

function InteractiveBottomNavDemo() {
  const [active, setActive] = useState("home");
  return (
    <div className="min-h-[220px] bg-surface-canvas p-4 pb-24 text-text-primary">
      <p className="text-sm text-text-secondary">Active tab: {active}</p>
      <BottomNavBar
        onSelect={setActive}
        items={[
          { id: "home", label: "الرئيسية", icon: HomeIcon, active: active === "home" },
          { id: "new", label: "طلب جديد", icon: PlusIcon, active: active === "new" },
          { id: "requests", label: "طلباتي", icon: ListIcon, active: active === "requests", badgeCount: 3 }
        ]}
      />
    </div>
  );
}

export const Interactive: Story = {
  render: () => <InteractiveBottomNavDemo />
};
