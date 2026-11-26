import { JsonPipe } from '@angular/common';
import { HttpErrorResponse, HttpResponse } from '@angular/common/http';
import { Component, inject, signal } from '@angular/core';
import { AuthService } from '../../../core/services/auth.service';

interface EndpointResult { status: number; body: unknown; }
@Component({
  selector: 'app-endpoint-demo', imports: [JsonPipe],
  template: `<section class="card"><h1>API endpoint demo</h1>
    <p>Inspect public and authorized authentication endpoints.</p>
    <div class="actions"><button type="button" (click)="callPublic()">Call public status</button>
      <button type="button" (click)="callMe()">Call authorized profile</button></div>
    @if (result(); as result) { <h2>HTTP {{ result.status }}</h2><pre>{{ result.body | json }}</pre> }
  </section>`
})
export class EndpointDemoComponent {
  private readonly auth = inject(AuthService);
  readonly result = signal<EndpointResult | null>(null);
  callPublic(): void { this.auth.publicStatus().subscribe({ next: response => this.show(response), error: error => this.fail(error) }); }
  callMe(): void { this.auth.me().subscribe({ next: response => this.show(response), error: error => this.fail(error) }); }
  private show(response: HttpResponse<unknown>): void { this.result.set({ status: response.status, body: response.body }); }
  private fail(error: HttpErrorResponse): void { this.result.set({ status: error.status, body: error.error ?? { detail: 'The service is unavailable.' } }); }
}
