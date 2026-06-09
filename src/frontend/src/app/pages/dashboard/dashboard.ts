import { Component, inject, OnDestroy, OnInit, signal } from '@angular/core';
import { FormBuilder, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { LanguageService } from '../../services/language.service';
import { CommonModule } from '@angular/common';
import { Subject, Subscription, switchMap, takeUntil, timer } from 'rxjs';
import { StockService } from '../../services/stock.service';
import { StockTick } from '../../models/stock.models';

@Component({
  selector: 'app-dashboard',
  imports: [ReactiveFormsModule, CommonModule],
  templateUrl: './dashboard.html',
  styleUrl: './dashboard.css',
})
export class Dashboard implements OnInit, OnDestroy {
  protected readonly langService = inject(LanguageService);
  private readonly fb = inject(FormBuilder);
  private readonly logService = inject(StockService);

  currentSimulatorSpeed = signal<string>('Stopped');
  isLoading = signal<boolean>(false);

  logStats = signal<Record<string, number>>({});
  stockTicksList = signal<StockTick[]>([]);

  controlForm!: FormGroup;

  private readonly destroy$ = new Subject<void>();
  private pollingSub?: Subscription;

  ngOnInit(): void {
    this.initForm();
    this.getInitialStatus();

    this.startPolling();

    this.loadRecentLogs();
    this.logService.startSignalRConnection();
    this.listenToLiveLogs();
  }

  ngOnDestroy(): void {
    this.stopPolling();
    this.logService.stopSignalRConnection();
    this.destroy$.next();
    this.destroy$.complete();
  }

  private loadRecentLogs(): void {
    const maxCount = this.controlForm?.value?.logCount || 100;
    this.logService.getRecentLogs(maxCount).subscribe({
      next: (recentLogs) => {
        this.stockTicksList.set(recentLogs);
      },
      error: (err) => console.error('Failed to load recent logs', err)
    });
  }

  private listenToLiveLogs(): void {
    this.logService.liveLog$
      .pipe(takeUntil(this.destroy$))
      .subscribe((newLog) => {
        const currentLogs = this.stockTicksList();
        const maxAllowed = this.controlForm.value.logCount || 100;

        const updatedLogs = [newLog, ...currentLogs];

        if (updatedLogs.length > maxAllowed) {
          updatedLogs.pop();
        }

        this.stockTicksList.set(updatedLogs);
      });
  }

  private initForm(): void {
    this.controlForm = this.fb.group({
      speed: [0, [Validators.required]],
      logCount: [100, [Validators.required, Validators.min(0), Validators.max(500)]]
    });
  }

  private getInitialStatus(): void {
    this.logService.getSimulatorStatus().subscribe({
      next: (res) => {
        this.currentSimulatorSpeed.set(res.currentSpeed);
        
        const speedValue = this.mapSpeedStringToEnum(res.currentSpeed);
        this.controlForm.patchValue({ speed: speedValue });
      },
      error: (err) => console.error('Failed to get simulator status via service', err)
    });
  }

  onSubmit(): void {
    if (this.controlForm.invalid) return;

    this.isLoading.set(true);
    const selectedSpeed = this.controlForm.value.speed;
    const count = this.controlForm.value.logCount;

    this.logService.setSimulatorSpeed(selectedSpeed).subscribe({
      next: (res) => {
        this.currentSimulatorSpeed.set(res.currentSpeed);
        this.isLoading.set(false);

        this.loadRecentLogs();

        if (res.currentSpeed !== 'Stopped') {
          this.startPolling();
        } else {
          this.stopPolling();
        }
      },
      error: (err) => {
        this.isLoading.set(false);
        console.error('Failed to update speed via service', err);
      }
    });
  }

  private startPolling(): void {
    this.stopPolling();

    this.pollingSub = timer(0, 1000)
      .pipe(
        switchMap(() => this.logService.getLogStats()),
        takeUntil(this.destroy$)
      )
      .subscribe({
        next: (stats) => {
          this.logStats.set(stats);
        },
        error: (err) => console.error('Stats polling error:', err)
      });
  }

  private stopPolling(): void {
    if (this.pollingSub) {
      this.pollingSub.unsubscribe();
    }
  }

  private mapSpeedStringToEnum(speedStr: string): number {
    switch (speedStr) {
      case 'Slow': return 1;
      case 'Medium': return 3;
      case 'Fast': return 5;
      default: return 0;
    }
  }
}
