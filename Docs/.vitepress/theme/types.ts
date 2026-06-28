export interface Ticket {
  affectedFiles: string[];
  assignee: string;
  dependencies: string[];
  documentation: string[];
  id: number;
  milestone: string;
  title: string;
  status: string;
  priority: "critical" | "high" | "medium" | "low";
  tags: string[];
  body: string;
  url: string;
}

export interface Column {
  key: string;
  label: string;
  color: string;
}

export interface TicketValidationIssue {
  type: "identity" | "missing-sections";
  file: string;
  currentId: number;
  currentSlug: string;
  fixedId?: number;
  fixedSlug?: string;
  missingSections?: string[];
}
