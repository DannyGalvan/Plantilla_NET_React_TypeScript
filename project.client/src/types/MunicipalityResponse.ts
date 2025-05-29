import type { CountryResponse } from "./CountryResponse";

export interface MunicipalityResponse {
  code: string;
  name: string;
  countryCode: string;
  departmentCode: string;
  country?: CountryResponse;
}
