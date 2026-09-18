import { describe, expect, it, vi } from "vitest";

import { useAuth } from "./useAuth";
import { useCan } from "./useCan";

vi.mock("./useAuth", () => ({
  useAuth: vi.fn(),
}));

const mockedUseAuth = vi.mocked(useAuth);

describe("useCan", () => {
  it("returns true when the user has the operation", () => {
    mockedUseAuth.mockReturnValue({
      allOperations: [{ name: "User.Create.POST" }],
    } as never);
    expect(useCan("User.Create.POST")).toBe(true);
  });

  it("returns false when the user lacks the operation", () => {
    mockedUseAuth.mockReturnValue({
      allOperations: [{ name: "User.Get.GET" }],
    } as never);
    expect(useCan("User.Create.POST")).toBe(false);
  });

  it("returns false for an empty operation key", () => {
    mockedUseAuth.mockReturnValue({ allOperations: [] } as never);
    expect(useCan("")).toBe(false);
  });

  it("returns false when the user has no operations", () => {
    mockedUseAuth.mockReturnValue({ allOperations: [] } as never);
    expect(useCan("Anything")).toBe(false);
  });
});
