import type { Env } from './env';

export class CoordinationObject {
  constructor(
    readonly state: DurableObjectState,
    readonly env: Env
  ) {}
}
