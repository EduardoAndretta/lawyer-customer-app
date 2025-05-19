import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { ApiConfigService } from './api-config.service';
import { KeyValueInformationDto, KeyValueParametersDto } from '../models/common.models';
// You might need specific DTOs if the KeyValueParametersDto differs for each endpoint
// For now, using the generic one.

@Injectable({
  providedIn: 'root'
})
export class ComboDataService {
  private baseUrl: string;

  constructor(
    private http: HttpClient,
    private apiConfig: ApiConfigService
  ) {
    this.baseUrl = `${this.apiConfig.getBaseUrl()}/combo`;
  }

  getPermissionsEnabledForGrantCase(params: KeyValueParametersDto): Observable<KeyValueInformationDto<number>> { // Assuming value is int64 (number)
    return this.http.post<KeyValueInformationDto<number>>(`${this.baseUrl}/permissions-enabled-for-grant-case`, params);
  }

  getPermissionsEnabledForRevokeCase(params: KeyValueParametersDto): Observable<KeyValueInformationDto<number>> {
    return this.http.post<KeyValueInformationDto<number>>(`${this.baseUrl}/permissions-enabled-for-revoke-case`, params);
  }

  getPermissionsEnabledForGrantUser(params: KeyValueParametersDto): Observable<KeyValueInformationDto<number>> {
    return this.http.post<KeyValueInformationDto<number>>(`${this.baseUrl}/permissions-enabled-for-grant-user`, params);
  }

  getPermissionsEnabledForRevokeUser(params: KeyValueParametersDto): Observable<KeyValueInformationDto<number>> {
    return this.http.post<KeyValueInformationDto<number>>(`${this.baseUrl}/permissions-enabled-for-revoke-user`, params);
  }

  getAttributes(params: KeyValueParametersDto): Observable<KeyValueInformationDto<number>> {
    return this.http.post<KeyValueInformationDto<number>>(`${this.baseUrl}/attributes`, params);
  }

  getRoles(params: KeyValueParametersDto): Observable<KeyValueInformationDto<number>> {
    return this.http.post<KeyValueInformationDto<number>>(`${this.baseUrl}/roles`, params);
  }
}