# Figma Export (icons)

This folder contains a small Node script to export SVG icons from a Figma file into the project `src/assets/icons` folder.

Prerequisites
- Node.js installed
- A Figma Personal Access Token (set `FIGMA_TOKEN`)

Quick usage

```bash
# Linux / macOS
FIGMA_TOKEN=... npm run figma:export-icons

# Windows (PowerShell)
$env:FIGMA_TOKEN='...'; npm run figma:export-icons
```

Environment variables
- `FIGMA_TOKEN` — your Figma Personal Access Token (required)
- `FIGMA_FILE_KEY` — optional, defaults to the project file key used when the script was created
- `FIGMA_ICONS_NODE` — optional, the node id of the icons frame (defaults to the known frame)

Notes
- The script looks for nodes under the given frame and requests SVG images via the Figma Images API. It will save files to `src/assets/icons/{name}.svg`.
- After running the script, icons can be used via the `app-icon` component or the `app-iconify` adapter which falls back to PrimeIcons classes when SVG is missing.
