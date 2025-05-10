import { Injectable } from '@angular/core';

const TOKEN_KEY = 'auth-token';
const REFRESHTOKEN_KEY = 'auth-refreshtoken';

@Injectable({
  providedIn: 'root'
})
export class TokenStorageService {
  constructor() { }

  public saveToken(token: string): void {
    localStorage.removeItem(TOKEN_KEY);
    localStorage.setItem(TOKEN_KEY, token);
  }

  public getToken(): string | null {
    return localStorage.getItem(TOKEN_KEY);
  }

  public saveRefreshToken(token: string): void {
    localStorage.removeItem(REFRESHTOKEN_KEY);
    localStorage.setItem(REFRESHTOKEN_KEY, token);
  }

  public getRefreshToken(): string | null {
    return localStorage.getItem(REFRESHTOKEN_KEY);
  }

  public clearTokens(): void {
    localStorage.removeItem(TOKEN_KEY);
    localStorage.removeItem(REFRESHTOKEN_KEY);
  }
}