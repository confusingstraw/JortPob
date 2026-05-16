let mapImg, croppedImg;
let drawBuffer, gridBuffer;

let colorValue = 0;
let alphaValue = 127;
let brushSize = 35;

let showGrid = false;
let cellSize = 21;
let gridXOffset = 30;
let gridYOffset = 30;

async function setup() {
  // Load the map image and crop it to the desired area
  // Original size: 2317 x 1324px
  mapImg = await loadImage('/assets/map.png');
  croppedImg = mapImg.get(429, 78, mapImg.width - 1078, mapImg.height - 316); 

  let cnv = createCanvas(croppedImg.width, croppedImg.height);
  cnv.parent('map-container');

  drawBuffer = createGraphics(croppedImg.width, croppedImg.height);
  drawBuffer.clear();
  drawBuffer.colorMode(HSB, 255);

  gridBuffer = createGraphics(croppedImg.width, croppedImg.height);

  // Add event listeners to input fields
  const colorInput = document.getElementById('color-picker');
  colorInput.addEventListener('input', (event) => {
    colorValue = 175-parseInt(event.target.value);
    document.getElementById('color-label').textContent = `Hue: ${colorValue}`;
  });

  const alphaInput = document.getElementById('alpha-picker');
  alphaInput.addEventListener('input', (event) => {
    alphaValue = parseInt(event.target.value);
    document.getElementById('alpha-label').textContent = `Alpha: ${alphaValue}`;
  });
  
  document.getElementById('toggle-grid').addEventListener('click', () => {
    showGrid = !showGrid;
    if (showGrid) {
      createGridOverlay();
    } else {
      gridBuffer.clear();
    }
  });
}

function draw() {
  // Draw the image in the background
  image(croppedImg, 0, 0);

  // Draw the highlighter buffer on top
  image(drawBuffer, 0, 0);

  // Draw the grid buffer on top if the grid is visible
  image(gridBuffer, 0, 0);
}

function mouseDragged() {
  // Use erase mode to clear the area before drawing
  drawBuffer.strokeWeight(brushSize);
  drawBuffer.erase();
  drawBuffer.line(mouseX, mouseY, pmouseX, pmouseY);
  drawBuffer.noErase();

  // Draw on the buffer
  drawBuffer.strokeWeight(brushSize);
  drawBuffer.color(colorValue, 255, 255, alphaValue);
  drawBuffer.stroke(colorValue, 255, 255, alphaValue);
  drawBuffer.line(mouseX, mouseY, pmouseX, pmouseY); // Draw a line at the mouse position
}

function mousePressed() {
  // Same behavior as dragging
  mouseDragged();
}

function createGridOverlay() {
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

  for (let x = 0; x < width; x += cellSize * 5) {
    for (let y = 0; y < height; y += cellSize * 5) {
      gridBuffer.textSize(6);
      gridBuffer.fill(0, 255, 255, 240);
      gridBuffer.noStroke();
      gridBuffer.text(`${x/cellSize - gridXOffset}, ${y/cellSize - gridYOffset}`, x + 2, y - 2);
    }
  }
}
