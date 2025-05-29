import type { MunicipalityResponse } from "./MunicipalityResponse";

export interface CountryResponse {
  code: string;
  name: string;
  municipalities: MunicipalityResponse[];
}
