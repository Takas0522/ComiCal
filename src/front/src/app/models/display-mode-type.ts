export const displayStatus = {
    SmallDisplay: 0,
    CommonDisplay: 1,
  } as const;
export type DisplayStatusType = typeof displayStatus[keyof typeof displayStatus];