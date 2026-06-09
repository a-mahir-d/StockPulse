export interface SimulatorStatus {
  currentSpeed: string;
}

export interface SpeedUpdateResponse {
  message: string;
  currentSpeed: string;
}

export interface StockTick {
  symbol: string;
  price: number;
  lastUpdated: string;
}