import { render, screen } from "@testing-library/react";
import { MemoryRouter } from "react-router";
import { afterEach, describe, expect, it, vi } from "vitest";

import { useAuth } from "../../hooks/useAuth";
import { useErrorsStore } from "../../stores/useErrorsStore";

vi.mock("../../hooks/useAuth", () => ({
  useAuth: vi.fn(),
}));
vi.mock("../../stores/useErrorsStore", () => ({
  useErrorsStore: vi.fn(),
}));

import ProtectedPublic from "./ProtectedPublic";

const mockedUseAuth = vi.mocked(useAuth);
const mockedUseErrorsStore = vi.mocked(useErrorsStore);

afterEach(() => {
  vi.clearAllMocks();
});

describe("ProtectedPublic", () => {
  it("renders children when the user is logged in and there is no error", () => {
    mockedUseAuth.mockReturnValue({ isLoggedIn: true } as never);
    mockedUseErrorsStore.mockReturnValue({ error: null } as never);

    render(
      <MemoryRouter>
        <ProtectedPublic>
          <div data-testid="child">OK</div>
        </ProtectedPublic>
      </MemoryRouter>,
    );

    expect(screen.getByTestId("child")).toHaveTextContent("OK");
  });

  it("hides children when the user is not logged in", () => {
    mockedUseAuth.mockReturnValue({ isLoggedIn: false } as never);
    mockedUseErrorsStore.mockReturnValue({ error: null } as never);

    render(
      <MemoryRouter>
        <ProtectedPublic>
          <div data-testid="child">OK</div>
        </ProtectedPublic>
      </MemoryRouter>,
    );

    expect(screen.queryByTestId("child")).toBeNull();
  });

  it("hides children when an error is in the store", () => {
    mockedUseAuth.mockReturnValue({ isLoggedIn: true } as never);
    mockedUseErrorsStore.mockReturnValue({
      error: { name: "ForbiddenError", statusCode: "403", message: "nope" },
    } as never);

    render(
      <MemoryRouter>
        <ProtectedPublic>
          <div data-testid="child">OK</div>
        </ProtectedPublic>
      </MemoryRouter>,
    );

    expect(screen.queryByTestId("child")).toBeNull();
  });
});
