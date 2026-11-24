import {
  HubConnectionBuilder,
  HubConnectionState as SignalRHubState,
  LogLevel,
  type HubConnection,
} from "@microsoft/signalr";
import type { UserNotificationDto } from "../modules/notifications.service.js";

export type OperationsHubState =
  | "disconnected"
  | "connecting"
  | "connected"
  | "reconnecting";

export interface OrderSubmittedEvent {
  orderId: string;
  orderNumber: string;
  customerId: string;
  repId: string;
  repName: string;
  warehouseId: number;
  totalAmount: number;
  exceededCreditWarning: boolean;
  requiresFinanceCoApproval: boolean;
  hasStockShortage: boolean;
  hasPriceOverridePending: boolean;
  createdAtUtc: string;
}

export interface CreditCoApprovalRequiredEvent {
  orderId: string;
  orderNumber: string;
  customerId: string;
  customerBalanceAtOrder: number;
  customerCreditLimitAtOrder: number;
  totalAmount: number;
}

export interface OrderApprovedEvent {
  orderId: string;
  orderNumber: string;
  repId: string;
  warehouseId: number;
  totalAmount: number;
  approvedAtUtc: string;
}

export interface OrderRejectedEvent {
  orderId: string;
  orderNumber: string;
  repId: string;
  rejectionReason: string;
  rejectedAtUtc: string;
}

export interface OrderUpdatedEvent {
  orderId: string;
  orderNumber: string;
  repId: string;
  status: string;
  totalAmount: number;
  changeType: "ManagerEdit" | "PriceOverrideApproved" | "CreditCoApproved" | string;
}

export type OperationsHubEvent =
  | { type: "OrderSubmitted"; payload: OrderSubmittedEvent }
  | { type: "CreditCoApprovalRequired"; payload: CreditCoApprovalRequiredEvent }
  | { type: "OrderApproved"; payload: OrderApprovedEvent }
  | { type: "OrderRejected"; payload: OrderRejectedEvent }
  | { type: "OrderUpdated"; payload: OrderUpdatedEvent }
  | { type: "NotificationReceived"; payload: UserNotificationDto }
  | { type: "NotificationsMarkedAllRead"; payload: { recipientUserId: string } };

export interface OperationsHubClientOptions {
  hubUrl: string;
  getAccessToken: () => string | null | Promise<string | null>;
  onEvent?: (event: OperationsHubEvent) => void;
  onStateChange?: (state: OperationsHubState) => void;
}

export interface OperationsHubClient {
  start: () => Promise<void>;
  stop: () => Promise<void>;
  getState: () => OperationsHubState;
}

const BASE_RECONNECT_DELAYS_MS = [0, 2000, 5000, 10000, 30000] as const;

/**
 * Computes jittered exponential backoff delay in milliseconds (RISK-04) to prevent
 * reconnection thundering herds when mobile field clients regain connectivity simultaneously.
 */
export function computeJitteredReconnectDelay(
  previousRetryCount: number,
  randomSource: () => number = Math.random,
): number {
  const index = Math.min(Math.max(0, previousRetryCount), BASE_RECONNECT_DELAYS_MS.length - 1);
  const base = BASE_RECONNECT_DELAYS_MS[index];
  if (base === 0) {
    return Math.round(randomSource() * 400);
  }
  const jitter = Math.round(base * 0.25 * randomSource());
  return base + jitter;
}

function mapSignalRState(state: SignalRHubState): OperationsHubState {
  switch (state) {
    case SignalRHubState.Connected:
      return "connected";
    case SignalRHubState.Connecting:
      return "connecting";
    case SignalRHubState.Reconnecting:
      return "reconnecting";
    default:
      return "disconnected";
  }
}

export function createOperationsHubClient(
  options: OperationsHubClientOptions,
): OperationsHubClient {
  let connection: HubConnection | null = null;
  let currentState: OperationsHubState = "disconnected";

  const setState = (next: OperationsHubState) => {
    currentState = next;
    options.onStateChange?.(next);
  };

  const ensureConnection = (): HubConnection => {
    if (connection) {
      return connection;
    }

    const built = new HubConnectionBuilder()
      .withUrl(options.hubUrl, {
        accessTokenFactory: async () => (await options.getAccessToken()) ?? "",
        withCredentials: true,
      })
      .withAutomaticReconnect({
        nextRetryDelayInMilliseconds: (retryContext) =>
          computeJitteredReconnectDelay(retryContext.previousRetryCount),
      })
      .configureLogging(LogLevel.Warning)
      .build();

    built.on("OrderSubmitted", (payload: OrderSubmittedEvent) => {
      options.onEvent?.({ type: "OrderSubmitted", payload });
    });
    built.on("CreditCoApprovalRequired", (payload: CreditCoApprovalRequiredEvent) => {
      options.onEvent?.({ type: "CreditCoApprovalRequired", payload });
    });
    built.on("OrderApproved", (payload: OrderApprovedEvent) => {
      options.onEvent?.({ type: "OrderApproved", payload });
    });
    built.on("OrderRejected", (payload: OrderRejectedEvent) => {
      options.onEvent?.({ type: "OrderRejected", payload });
    });
    built.on("OrderUpdated", (payload: OrderUpdatedEvent) => {
      options.onEvent?.({ type: "OrderUpdated", payload });
    });
    built.on("NotificationReceived", (payload: UserNotificationDto) => {
      options.onEvent?.({ type: "NotificationReceived", payload });
    });
    built.on("NotificationsMarkedAllRead", (payload: { recipientUserId: string }) => {
      options.onEvent?.({ type: "NotificationsMarkedAllRead", payload });
    });

    built.onreconnecting(() => setState("reconnecting"));
    built.onreconnected(() => setState("connected"));
    built.onclose(() => setState("disconnected"));

    connection = built;
    return built;
  };

  return {
    async start(): Promise<void> {
      const conn = ensureConnection();
      if (conn.state !== SignalRHubState.Disconnected) {
        setState(mapSignalRState(conn.state));
        return;
      }

      setState("connecting");
      try {
        await conn.start();
        setState("connected");
      } catch {
        setState("disconnected");
      }
    },

    async stop(): Promise<void> {
      if (!connection) {
        setState("disconnected");
        return;
      }

      try {
        await connection.stop();
      } finally {
        setState("disconnected");
      }
    },

    getState(): OperationsHubState {
      return connection ? mapSignalRState(connection.state) : currentState;
    },
  };
}
