let mapImg, croppedImg;
let colorValue = 0;
let brushSize = 35;
let showGrid = true;
let gridSize = 25;
let alphaValue = 127;
let buffer;

async function setup() {
  // Load the map image and crop it to the desired area
  mapImg = await loadImage('/assets/map.png');
  croppedImg = mapImg.get(400, 100 , mapImg.width - 900, mapImg.height - 300); 

  let cnv = createCanvas(croppedImg.width, croppedImg.height);
  cnv.parent('map-container');

  buffer = createGraphics(croppedImg.width, croppedImg.height);
  buffer.clear();
  buffer.colorMode(HSB, 255);

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
  
  document.addEventListener('DOMContentLoaded', () => {
    const gridOverlay = document.getElementById('grid-overlay');
    gridOverlay.style.display = 'none';

    document.getElementById('toggle-grid').addEventListener('click', () => {
      if (gridOverlay.style.display === 'none' || gridOverlay.style.display === '') {
        gridOverlay.style.display = 'block';
        createGridOverlay();
      } else {
        gridOverlay.style.display = 'none';
      }
    });
  });
}

function draw() {
  // Draw the image in the background
  image(croppedImg, 0, 0);

  // Draw the highlighter buffer on top
  image(buffer, 0, 0);
}

function mouseDragged() {
  // Use erase mode to clear the area before drawing
  buffer.strokeWeight(brushSize);
  buffer.erase();
  buffer.line(mouseX, mouseY, pmouseX, pmouseY);
  buffer.noErase();

  // Draw on the buffer
  buffer.strokeWeight(brushSize);
  buffer.color(colorValue, 255, 255, alphaValue);
  buffer.stroke(colorValue, 255, 255, alphaValue);
  buffer.line(mouseX, mouseY, pmouseX, pmouseY); // Draw a line at the mouse position
}

function mousePressed() {
  // Same behavior as dragging
  mouseDragged();
}

function createGridOverlay() {
  const gridOverlay = document.getElementById('grid-overlay');
  const mapContainer = document.getElementById('map-container');

  // Align grid overlay with map container dimensions and position
  const { width, height, top, left } = mapContainer.getBoundingClientRect();
  gridOverlay.style.width = `${width}px`;
  gridOverlay.style.height = `${height}px`;
  gridOverlay.style.top = `${top}px`;
  gridOverlay.style.left = `${left}px`;

  gridOverlay.innerHTML = ''; // Clear existing grid lines

  for (let x = 0; x < width; x += gridSize) {
    const verticalLine = document.createElement('div');
    verticalLine.classList.add('grid-line', 'vertical');
    verticalLine.style.left = `${x}px`;
    gridOverlay.appendChild(verticalLine);
  }

  for (let y = 0; y < height; y += gridSize) {
    const horizontalLine = document.createElement('div');
    horizontalLine.classList.add('grid-line', 'horizontal');
    horizontalLine.style.top = `${y}px`;
    gridOverlay.appendChild(horizontalLine);
  }
}
