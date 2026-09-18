import { render, screen } from "@testing-library/react";
import { MemoryRouter } from "react-router";
import { afterEach, describe, expect, it, vi } from "vitest";

import { useAuth } from "../../hooks/useAuth";

vi.mock("../../hooks/useAuth", () => ({
  useAuth: vi.fn(),
}));

import ProtectedRoute from "./ProtectedRoute";

const mockedUseAuth = vi.mocked(useAuth);

afterEach(() => {
  vi.clearAllMocks();
});

describe("ProtectedRoute", () => {
  it("redirects to /auth when the user is not logged in", () => {
    mockedUseAuth.mockReturnValue({
      isLoggedIn: false,
      redirect: false,
      allOperations: [{ name: "Anything" }],
    } as never);

    render(
      <MemoryRouter initialEntries={["/protected"]}>
        <ProtectedRoute operation="Protected.Operation.GET">
          <div data-testid="child">SECRET</div>
        </ProtectedRoute>
      </MemoryRouter>,
    );

    expect(screen.queryByTestId("child")).toBeNull();
  });

  it("redirects to /change-password when `redirect` is true", () => {
    mockedUseAuth.mockReturnValue({
      isLoggedIn: true,
      redirect: true,
      allOperations: [{ name: "Anything" }],
    } as never);

    render(
      <MemoryRouter initialEntries={["/protected"]}>
        <ProtectedRoute operation="Anything">
          <div data-testid="child">SECRET</div>
        </ProtectedRoute>
      </MemoryRouter>,
    );

    expect(screen.queryByTestId("child")).toBeNull();
  });

  it("redirects to /error when the user lacks the required operation", () => {
    mockedUseAuth.mockReturnValue({
      isLoggedIn: true,
      redirect: false,
      allOperations: [{ name: "Something.Else.GET" }],
    } as never);

    render(
      <MemoryRouter initialEntries={["/protected"]}>
        <ProtectedRoute operation="Protected.Operation.GET">
          <div data-testid="child">SECRET</div>
        </ProtectedRoute>
      </MemoryRouter>,
    );

    expect(screen.queryByTestId("child")).toBeNull();
  });

  it("renders children when the user is logged in, not redirected, and authorised", () => {
    mockedUseAuth.mockReturnValue({
      isLoggedIn: true,
      redirect: false,
      allOperations: [{ name: "Protected.Operation.GET" }],
    } as never);

    render(
      <MemoryRouter initialEntries={["/protected"]}>
        <ProtectedRoute operation="Protected.Operation.GET">
          <div data-testid="child">SECRET</div>
        </ProtectedRoute>
      </MemoryRouter>,
    );

    expect(screen.getByTestId("child")).toHaveTextContent("SECRET");
  });
});