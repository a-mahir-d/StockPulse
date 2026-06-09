import { HttpClient } from "@angular/common/http";
import { inject, Injectable } from "@angular/core";
import { environment } from "../../environments/environment";
import { Observable, Subject } from "rxjs";
import { HubConnection, HubConnectionBuilder, HubConnectionState } from "@microsoft/signalr";
import { AuthService } from "./auth.service";
import { SimulatorStatus, SpeedUpdateResponse, StockTick } from "../models/stock.models";

@Injectable({
  providedIn: 'root'
})
export class StockService {
  private readonly http = inject(HttpClient);
  private readonly authService = inject(AuthService);
  private readonly baseUrl = `${environment.serverUrl}/api/stocks`;
  private readonly hubUrl = `${environment.serverUrl}/stockHub`;

  private hubConnection?: HubConnection;

  private readonly liveLogSource = new Subject<StockTick>();
  liveLog$ = this.liveLogSource.asObservable();

  getRecentLogs(count: number): Observable<StockTick[]> {
    return this.http.get<StockTick[]>(`${`${this.baseUrl}/recent`}?count=${count}`);
  }

  startSignalRConnection(): void {
    if (this.hubConnection?.state === HubConnectionState.Connected) return;

    this.hubConnection = new HubConnectionBuilder()
      .withUrl(this.hubUrl, {
        accessTokenFactory: () => this.authService.getToken() || ''
      })
      .withAutomaticReconnect()
      .build();

    this.hubConnection.start()
      .then(() => console.log('[SignalR] Connected to StockHub successfully using AuthService token.'))
      .catch(err => console.error('[SignalR] Error while starting connection:', err));

    this.hubConnection.on('ReceiveStockTick', (stockTick: StockTick) => {
      this.liveLogSource.next(stockTick);
    });
  }

  stopSignalRConnection(): void {
    if (this.hubConnection) {
      this.hubConnection.stop()
        .then(() => console.log('[SignalR] Connection stopped.'))
        .catch(err => console.error('[SignalR] Error while stopping connection:', err));
    }
  }

  getSimulatorStatus(): Observable<SimulatorStatus> {
    return this.http.get<SimulatorStatus>(`${this.baseUrl}/simulator/status`);
  }

  setSimulatorSpeed(speed: number): Observable<SpeedUpdateResponse> {
    return this.http.post<SpeedUpdateResponse>(
      `${this.baseUrl}/simulator/speed?speed=${speed}`, 
      {}
    );
  }

  getLogStats(): Observable<Record<string, number>> {
    return this.http.get<Record<string, number>>(`${this.baseUrl}/stats`);
  }
}