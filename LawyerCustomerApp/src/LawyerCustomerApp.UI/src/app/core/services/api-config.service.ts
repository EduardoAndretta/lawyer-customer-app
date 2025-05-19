import { Injectable } from '@angular/core';
import { environment } from '../../../environments/environment';

@Injectable({
  providedIn: 'root'
})
export class ApiConfigService {
  private readonly apiUrl = environment.apiUrl;

  constructor() { }

  getBaseUrl(): string {
    return this.apiUrl;
  }
}