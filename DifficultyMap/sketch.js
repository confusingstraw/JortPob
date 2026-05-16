let mapImg, croppedImg;
let drawBuffer, gridBuffer, exportBuffer;

const hueLimit = 250;
let colorValue = 0;
let alphaValue = 50;
let brushSize = 35;
let brushShape = 'circle'; // Default brush shape

let showGrid = true;
let showDifficulty = false;
// Coordinates of the top-left corner of the grid relative to the map image, in cell units
const cellOffsetX = 28;
const cellOffsetY = -27;
const cellSize = 21;

async function setup() {
  // Load the map image and crop it to the desired area
  // Original size: 2317 x 1324px
  mapImg = await loadImage('/assets/map.png');
  croppedImg = mapImg.get(471, 120, mapImg.width - 1246, mapImg.height - 400); 

  // Create canvas and buffers
  let cnv = createCanvas(croppedImg.width, croppedImg.height);
  cnv.parent('map-container');

  drawBuffer = createGraphics(croppedImg.width, croppedImg.height);
  drawBuffer.clear();
  drawBuffer.colorMode(HSB, 360, 100, 100, 100);

  gridBuffer = createGraphics(croppedImg.width, croppedImg.height);
  gridBuffer.colorMode(HSB, 360, 100, 100, 100);
  difficultyBuffer = createGraphics(croppedImg.width, croppedImg.height);
  difficultyBuffer.colorMode(HSB, 360, 100, 100, 100);
  drawGrid();

  exportBuffer = createGraphics(croppedImg.width, croppedImg.height);
  exportBuffer.colorMode(HSB, 360, 100, 100, 100);

  // Add event listeners to input fields
  const colorInput = document.getElementById('color-picker');
  setDrawColor(colorInput.value);
  colorInput.addEventListener('input', (event) => {
    setDrawColor(event.target.value);
  });

  const alphaInput = document.getElementById('alpha-picker');
  setAlpha(alphaInput.value);
  alphaInput.addEventListener('input', (event) => {
    setAlpha(event.target.value);
  });

  const brushSizeInput = document.getElementById('brush-size-picker');
  setBrushSize(brushSizeInput.value);
  brushSizeInput.addEventListener('input', (event) => {
    setBrushSize(event.target.value);
  });

  // Add event listeners for brush shape buttons
  document.getElementById('square-brush').addEventListener('click', () => {
    brushShape = 'square';
  });

  document.getElementById('circle-brush').addEventListener('click', () => {
    brushShape = 'circle';
  });

  document.getElementById('toggle-grid').addEventListener('click', () => {
    showGrid = !showGrid;
    drawGrid();
  });

  document.getElementById('toggle-difficulty').addEventListener('click', () => {
    showDifficulty = !showDifficulty;
    drawGrid();
  });

  document.getElementById('export-drawing-png').addEventListener('click', () => {
    exportDrawingPNG();
  });

  document.getElementById('export-canvas-png').addEventListener('click', () => {
    exportCanvasPNG();
  });

  document.getElementById('export-json').addEventListener('click', () => {
    exportJSON();
  });
}

function draw() {
  // Draw the image in the background
  image(croppedImg, 0, 0);

  // Draw the highlighter buffer on top
  image(drawBuffer, 0, 0);

  // Draw the grid buffer on top if the grid is visible
  if (showGrid) {
    image(gridBuffer, 0, 0);
  }
}

function setDrawColor(value) {
  colorValue = Math.round(hueLimit - (parseInt(value) * hueLimit / 100));
  document.getElementById('color-label').textContent = `Hue: ${colorValue}, Difficulty: ${getDifficultyFromHue(colorValue)}`;
}

function setAlpha(value) {
  alphaValue = parseInt(value);
  document.getElementById('alpha-label').textContent = `Alpha: ${alphaValue}`;
}

function setBrushSize(value) {
  brushSize = parseInt(value);
  document.getElementById('brush-size-label').textContent = `Brush Size: ${brushSize}px`;
}

function getDifficultyFromHue(col) {
  return Math.round(100 - (col / hueLimit * 100));
}

function mouseDragged() {
  drawBuffer.erase();
  if (brushShape === 'circle') {
    drawBuffer.strokeWeight(brushSize);
    drawBuffer.line(mouseX, mouseY, pmouseX, pmouseY);
  } else if (brushShape === 'square') {
    drawBuffer.noStroke();
    drawBuffer.rect(mouseX - brushSize / 2, mouseY - brushSize / 2, brushSize, brushSize);
  }
  drawBuffer.noErase();

  
  
  
  if (brushShape === 'circle') {
    drawBuffer.strokeWeight(brushSize);
    drawBuffer.stroke(colorValue, 100, 100, alphaValue);
    drawBuffer.noFill();
    drawBuffer.line(mouseX, mouseY, pmouseX, pmouseY);
  } else if (brushShape === 'square') {
    drawBuffer.noStroke();
    drawBuffer.fill(colorValue, 100, 100, alphaValue);
    drawBuffer.rect(mouseX - brushSize / 2, mouseY - brushSize / 2, brushSize, brushSize);
  }
}

function mousePressed() {
  // Same behavior as dragging
  mouseDragged();
}

function drawGrid() {
  gridBuffer.clear();

  if (!showGrid) {
    return;
  }

  let gridColor = color(215, 149, 39, 200);
  gridBuffer.stroke(gridColor);
  gridBuffer.strokeWeight(1);

  // Draw vertical lines
  for (let x = 0; x <= width; x += cellSize) {
    gridBuffer.line(x, 0, x, height);
  }

  // Draw horizontal lines
  for (let y = 0; y <= height; y += cellSize) {
    gridBuffer.line(0, y, width, y);
  }

  for (let x = (cellOffsetX % 5) * cellSize; x < width; x += cellSize * 5) {
    for (let y = (-cellOffsetY % 5) * cellSize; y < height; y += cellSize * 5) {
      gridBuffer.textSize(10);
      gridBuffer.fill(180, 100, 100, 240);
      gridBuffer.noStroke();
      gridBuffer.text(`${x/cellSize - cellOffsetX}, ${-(y/cellSize + cellOffsetY)}`, x + 2, y + 19);
    }
  }

  if (showDifficulty) {
    for (let x = 0; x < width; x += cellSize) {
      for (let y = 0; y < height; y += cellSize) {
        gridBuffer.textSize(10);
        gridBuffer.fill(0, 0, 100, 240);
        gridBuffer.noStroke();

        let difficulty = getAverageDifficulty(x, y, cellSize, cellSize);

        gridBuffer.text(difficulty, x + 2, y + 10);
      }
    }
  }
}

function exportDrawingPNG() {
  exportBuffer.clear();
  exportBuffer.image(drawBuffer, 0, 0);
  
  // Save the combined image
  exportBuffer.get().save('difficulty_map.png');
}

function exportCanvasPNG() {
  save('difficulty_map_full.png');
}

function exportJSON() {
  let gridData = [];

  for (let x = 0; x < width; x += cellSize) {
    for (let y = 0; y < height; y += cellSize) {
      let difficulty = getAverageDifficulty(x, y, cellSize, cellSize);
      gridData.push({
        x: x / cellSize - cellOffsetX,
        y: -(y / cellSize + cellOffsetY),
        d: (difficulty / 100)  // Convert to a number between 0 and 1
      });
    }
  }

  let jsonData = JSON.stringify(gridData, null, 2);
  let blob = new Blob([jsonData], { type: 'application/json' });
  let url = URL.createObjectURL(blob);

  let a = document.createElement('a');
  a.href = url;
  a.download = 'grid_data.json';
  a.click();
  URL.revokeObjectURL(url);
}

function getAverageDifficulty(x, y, w, h) {
  drawBuffer.loadPixels();
  let hueSum = 0;
  let count = 0;
  let d = pixelDensity(); // Account for high-density displays (e.g., Retina)

  // Loop through the rectangular area
  for (let i = x * d; i < (x + w) * d; i++) {
    for (let j = y * d; j < (y + h) * d; j++) {
      // Calculate index in the 1D pixels array [R, G, B, A, R, G, B, A...]
      let index = 4 * (j * width * d + i);

      let c = color(
        drawBuffer.pixels[index],
        drawBuffer.pixels[index + 1],
        drawBuffer.pixels[index + 2],
        drawBuffer.pixels[index + 3]
      );
      let h = hue(c);
      if (alpha(c) == 0) {
        hueSum += hueLimit; // Treat fully transparent pixels as max hue (lowest difficulty)
      } else {
        hueSum += h;
      }
      count++;
    }
  }

  return getDifficultyFromHue(hueSum / count);
}
