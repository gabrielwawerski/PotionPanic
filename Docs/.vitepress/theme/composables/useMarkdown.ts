export function countCheckboxes(body: string): { done: number; total: number } {
  const matches = body.match(/- \[([ x])\]/g) || [];
  return {
    done: matches.filter((entry) => entry.includes("[x]")).length,
    total: matches.length,
  };
}

export function toggleCheckbox(body: string, index: number): string {
  let checkboxIndex = -1;

  return body
    .split("\n")
    .map((line) => {
      const match = line.match(/^(\s*- \[)([ x])(\] .+)$/);
      if (match) {
        checkboxIndex += 1;
        if (checkboxIndex === index) {
          return `${match[1]}${match[2] === "x" ? " " : "x"}${match[3]}`;
        }
      }

      return line;
    })
    .join("\n");
}
