import type { HttpStatusCode } from "./StatusCode";

export class ApiError extends Error {
  statusCode: HttpStatusCode = "0";
  constructor(message: string) {
    super(message);
    this.name = "ApiError";
    this.statusCode = "100";
  }
}

export class UnauthorizedError extends ApiError {
  statusCode: HttpStatusCode = "0";
  constructor(message: string) {
    super(message);
    this.name = "UnauthorizedError";
    this.statusCode = "401";
  }
}

export class ForbiddenError extends ApiError {
  statusCode: HttpStatusCode = "0";
  constructor(message: string) {
    super(message);
    this.name = "ForbiddenError";
    this.statusCode = "403";
  }
}

export class NotFoundError extends ApiError {
  statusCode: HttpStatusCode = "0";
  constructor(message: string) {
    super(message);
    this.name = "NotFoundError";
    this.statusCode = "404";
  }
}

export class BadRequestError extends ApiError {
  statusCode: HttpStatusCode = "0";
  constructor(message: string) {
    super(message);
    this.name = "BadRequestError";
    this.statusCode = "400";
  }
}

export class InternalServerError extends ApiError {
  statusCode: HttpStatusCode = "0";
  constructor(message: string) {
    super(message);
    this.name = "InternalServerError";
    this.statusCode = "500";
  }
}
