# Elden Scrolls Difficulty Gradient Map Editor

This tool can be used to paint colors onto a map of Morrowind to assign difficulty to areas, which can be exported to an image or JSON.

## Running Locally

Run a local server then open `index.html` in your web browser.

```bash
# Using Python
python -m http.server 8000

# Using Node.js
npx http-server

# Using VS Code Live Server extension
# Right-click index.html -> "Open with Live Server"
```

## How to Use
### Drawing
1. Use the toolbar to select a color hue, which corresponds to a difficulty from 0-100. Blue is the easiest and red is the hardest.
1. Use the Alpha slider to adjust transparency so you can see the map behind what you draw. This does not affect the difficulty as long as it is above 0. If alpha is 0, it will be treated as 0 difficulty and act as an erase tool.
1. Adjust brush size/shape if desired.
1. Click and drag on the map to highlight areas with different colours to assign difficulties.

### Grid Display
- Click Toggle Grid to show/hide the cell grid and coordinates.
- Click Toggle Difficulty to show the calculated difficulty for each cell based on the average color drawn onto the cell (grid must also be enabled). This will take a moment to process, and will only update if you toggle it off then on again.


### Import and Export

#### Export Drawing PNG
Exports the drawing layer, which contains only the colors that you paint to the canvas so the map, grid, etc. are excluded. The file can be imported again to load the colors back onto the canvas, erasing anything previously drawn on it.

#### Export Canvas PNG
Exports everything on the canvas, so the map, grid, cell difficulty, etc. is all included. Files exported this way are not suitable for import.

#### Export JSON
Exports a JSON array of objects containing the coordinates and difficulty of each cell. The difficulty is converted to a decimal number in the 0-1 range instead of the 0-100 displayed in the UI. This will take a moment to process due to the difficulty calculation.

#### Import PNG
Import a PNG to the drawing layer.

## Resources

- [p5.js 2.0](https://beta.p5js.org/)
- [p5.js Reference](https://p5js.org/reference/)
