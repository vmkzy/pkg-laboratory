(function initializeIlluminants(global) {
const {
  invertMatrix3x3,
  multiplyMatrixVector,
  scaleMatrixColumns,
} = global.ColorLab.matrix;

// Chromaticity coordinates are source data, not ready-made conversion matrices.
const SRGB_PRIMARIES = Object.freeze({
  red: Object.freeze({ x: 0.64, y: 0.33 }),
  green: Object.freeze({ x: 0.3, y: 0.6 }),
  blue: Object.freeze({ x: 0.15, y: 0.06 }),
});

const ILLUMINANTS = Object.freeze({
  D65: Object.freeze({ x: 0.3127, y: 0.329 }),
  D50: Object.freeze({ x: 0.34567, y: 0.3585 }),
  E: Object.freeze({ x: 1 / 3, y: 1 / 3 }),
});

function xyToXyz({ x, y }, luminance = 1) {
  if (![x, y, luminance].every(Number.isFinite) || y === 0) {
    throw new TypeError("chromaticity and luminance must be finite, with y not equal to zero");
  }

  return [
    (x * luminance) / y,
    luminance,
    ((1 - x - y) * luminance) / y,
  ];
}

function createPrimaryMatrix(primaries) {
  const red = xyToXyz(primaries.red);
  const green = xyToXyz(primaries.green);
  const blue = xyToXyz(primaries.blue);

  return [
    [red[0], green[0], blue[0]],
    [red[1], green[1], blue[1]],
    [red[2], green[2], blue[2]],
  ];
}

function getWhitePoint(illuminantName) {
  const chromaticity = ILLUMINANTS[illuminantName];

  if (!chromaticity) {
    throw new RangeError(`unknown illuminant: ${illuminantName}`);
  }

  return xyToXyz(chromaticity);
}

function createRgbXyzMatrices(
  illuminantName,
  primaries = SRGB_PRIMARIES,
) {
  const primaryMatrix = createPrimaryMatrix(primaries);
  const whitePoint = getWhitePoint(illuminantName);

  // The scale factors force RGB white (1, 1, 1) to match the selected white point.
  const scales = multiplyMatrixVector(
    invertMatrix3x3(primaryMatrix),
    whitePoint,
  );
  const rgbToXyz = scaleMatrixColumns(primaryMatrix, scales);
  const xyzToRgb = invertMatrix3x3(rgbToXyz);

  return {
    whitePoint,
    rgbToXyz,
    xyzToRgb,
  };
}

global.ColorLab.illuminants = Object.freeze({
  SRGB_PRIMARIES,
  ILLUMINANTS,
  xyToXyz,
  getWhitePoint,
  createRgbXyzMatrices,
});
})(globalThis);
