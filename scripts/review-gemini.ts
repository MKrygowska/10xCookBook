import { review } from "./review-agent.js";

// Czytanie argumentów z stdin
async function readDiff(): Promise<string> {
  const chunks: Buffer[] = [];
  for await (const chunk of process.stdin) chunks.push(chunk as Buffer);
  return Buffer.concat(chunks).toString("utf8");
}

// Entry point całego procesu
const diff = await readDiff();
try {
  const result = await review(diff);
  console.log(JSON.stringify(result, null, 2));
} catch (error: any) {
  console.error("Błąd podczas uruchamiania agenta recenzującego:", error.message);
  process.exit(1);
}
