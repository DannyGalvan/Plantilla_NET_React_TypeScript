export interface ValidationFailure {
  propertyName: string;
  errorMessage: string;
  attemptedValue: object;
  customerState: object;
  errorCode: string;
}
