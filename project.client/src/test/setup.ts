import "@testing-library/jest-dom";

// Silence noisy React 19 act() warnings inside the test runner.
if (
  typeof globalThis !== "undefined" &&
  !("IS_REACT_ACT_ENVIRONMENT" in globalThis)
) {
  // @ts-expect-error — narrow environment flag for the React 19 runtime.
  globalThis.IS_REACT_ACT_ENVIRONMENT = true;
}
