import type { Module } from "./Module";
import type { Operations } from "./Operations";

export interface Authorizations {
  module: Module;
  operations: Operations[];
}
