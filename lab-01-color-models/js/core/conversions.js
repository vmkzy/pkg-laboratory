import { createRgbXyzMatrices } from "./illuminants.js";
import { multiplyMatrixVector } from "./matrix.js";

const RGB_MAX = 255;
const PERCENT_MAX = 100;
const LAB_DELTA = 6 / 29;
const LAB_DELTA_CUBED = LAB_DELTA ** 3;

function assertFiniteComponents(color, componentNames, modelName) {
  if (
    !color ||
    componentNames.some((name) => !Number.isFinite(color[name]))
  ) {
    throw new TypeError(`${modelName} components must be finite numbers`);
  }
}

function normalizeHue(hue) {
  return ((hue % 360) + 360) % 360;
}

export function srgbChannelToLinear(channel) {
  return channel <= 0.04045
    ? channel / 12.92
    : ((channel + 0.055) / 1.055) ** 2.4;
}

export function linearChannelToSrgb(channel) {
  return channel <= 0.0031308
    ? 12.92 * channel
    : 1.055 * channel ** (1 / 2.4) - 0.055;
}

export function hsvToRgb({ h, s, v }) {
  assertFiniteComponents({ h, s, v }, ["h", "s", "v"], "HSV");

  if (s < 0 || s > PERCENT_MAX || v < 0 || v > PERCENT_MAX) {
    throw new RangeError("HSV saturation and value must be between 0 and 100");
  }

  const hue = normalizeHue(h);
  const saturation = s / PERCENT_MAX;
  const value = v / PERCENT_MAX;
  const chroma = value * saturation;
  const hueSector = hue / 60;
  const intermediate = chroma * (1 - Math.abs((hueSector % 2) - 1));

  let red = 0;
  let green = 0;
  let blue = 0;

  if (hueSector < 1) {
    [red, green] = [chroma, intermediate];
  } else if (hueSector < 2) {
    [red, green] = [intermediate, chroma];
  } else if (hueSector < 3) {
    [green, blue] = [chroma, intermediate];
  } else if (hueSector < 4) {
    [green, blue] = [intermediate, chroma];
  } else if (hueSector < 5) {
    [red, blue] = [intermediate, chroma];
  } else {
    [red, blue] = [chroma, intermediate];
  }

  const lightnessOffset = value - chroma;

  return {
    r: (red + lightnessOffset) * RGB_MAX,
    g: (green + lightnessOffset) * RGB_MAX,
    b: (blue + lightnessOffset) * RGB_MAX,
  };
}

export function rgbToHsv({ r, g, b }) {
  assertFiniteComponents({ r, g, b }, ["r", "g", "b"], "RGB");

  const red = r / RGB_MAX;
  const green = g / RGB_MAX;
  const blue = b / RGB_MAX;
  const maximum = Math.max(red, green, blue);
  const minimum = Math.min(red, green, blue);
  const delta = maximum - minimum;

  let hue = 0;

  if (delta !== 0) {
    if (maximum === red) {
      hue = 60 * (((green - blue) / delta) % 6);
    } else if (maximum === green) {
      hue = 60 * ((blue - red) / delta + 2);
    } else {
      hue = 60 * ((red - green) / delta + 4);
    }
  }

  return {
    h: normalizeHue(hue),
    s: maximum === 0 ? 0 : (delta / maximum) * PERCENT_MAX,
    v: maximum * PERCENT_MAX,
  };
}

export function rgbToXyz({ r, g, b }, illuminantName = "D65") {
  assertFiniteComponents({ r, g, b }, ["r", "g", "b"], "RGB");

  const linearRgb = [r, g, b].map((channel) =>
    srgbChannelToLinear(channel / RGB_MAX),
  );
  const { rgbToXyz } = createRgbXyzMatrices(illuminantName);
  const xyz = multiplyMatrixVector(rgbToXyz, linearRgb);

  return {
    x: xyz[0] * PERCENT_MAX,
    y: xyz[1] * PERCENT_MAX,
    z: xyz[2] * PERCENT_MAX,
  };
}

// The returned RGB values are intentionally not limited to 0…255.
// Clipping or scaling is a separate, user-selectable step.
export function xyzToRgb({ x, y, z }, illuminantName = "D65") {
  assertFiniteComponents({ x, y, z }, ["x", "y", "z"], "XYZ");

  const normalizedXyz = [x, y, z].map(
    (component) => component / PERCENT_MAX,
  );
  const { xyzToRgb } = createRgbXyzMatrices(illuminantName);
  const linearRgb = multiplyMatrixVector(xyzToRgb, normalizedXyz);
  const rgb = linearRgb.map(
    (channel) => linearChannelToSrgb(channel) * RGB_MAX,
  );

  return { r: rgb[0], g: rgb[1], b: rgb[2] };
}

function labForwardTransform(value) {
  return value > LAB_DELTA_CUBED
    ? Math.cbrt(value)
    : value / (3 * LAB_DELTA ** 2) + 4 / 29;
}

function labInverseTransform(value) {
  return value > LAB_DELTA
    ? value ** 3
    : 3 * LAB_DELTA ** 2 * (value - 4 / 29);
}

export function xyzToLab({ x, y, z }, illuminantName = "D65") {
  assertFiniteComponents({ x, y, z }, ["x", "y", "z"], "XYZ");

  const { whitePoint } = createRgbXyzMatrices(illuminantName);
  const fx = labForwardTransform(x / (whitePoint[0] * PERCENT_MAX));
  const fy = labForwardTransform(y / (whitePoint[1] * PERCENT_MAX));
  const fz = labForwardTransform(z / (whitePoint[2] * PERCENT_MAX));

  return {
    l: 116 * fy - 16,
    a: 500 * (fx - fy),
    b: 200 * (fy - fz),
  };
}

export function labToXyz({ l, a, b }, illuminantName = "D65") {
  assertFiniteComponents({ l, a, b }, ["l", "a", "b"], "LAB");

  const { whitePoint } = createRgbXyzMatrices(illuminantName);
  const fy = (l + 16) / 116;
  const fx = fy + a / 500;
  const fz = fy - b / 200;

  return {
    x: whitePoint[0] * labInverseTransform(fx) * PERCENT_MAX,
    y: whitePoint[1] * labInverseTransform(fy) * PERCENT_MAX,
    z: whitePoint[2] * labInverseTransform(fz) * PERCENT_MAX,
  };
}

export function hsvToXyz(hsv, illuminantName = "D65") {
  return rgbToXyz(hsvToRgb(hsv), illuminantName);
}

export function hsvToLab(hsv, illuminantName = "D65") {
  return xyzToLab(hsvToXyz(hsv, illuminantName), illuminantName);
}

export function labToRgb(lab, illuminantName = "D65") {
  return xyzToRgb(labToXyz(lab, illuminantName), illuminantName);
}
