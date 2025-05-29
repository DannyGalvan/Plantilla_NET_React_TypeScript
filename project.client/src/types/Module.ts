import type { Catalogue } from "./Catalogue";
import type { Operations } from "./Operations";

export interface Module extends Catalogue {
  image: string;
  path: string;

  operations?: Operations[];
}
