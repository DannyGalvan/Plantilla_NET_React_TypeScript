/**
 * RFC 9457 ProblemDetails envelope returned by the new \`Result.ToHttp()\`
 * extension on the API. \`traceId\` is added in \`Project.Web.ResultExtensions\`.
 */
export interface ProblemDetails {
  type?: string;
  title?: string;
  status: number;
  detail?: string;
  instance?: string;
  code?: string;
  traceId?: string;
  errors?: Record<string, string[]>;
}
