import assert from "node:assert/strict";
import test from "node:test";

import "../js/core/matrix.js";
import "../js/core/illuminants.js";
import "../js/core/conversions.js";

const {
  hsvToLab,
  hsvToRgb,
  labToRgb,
  labToXyz,
  rgbToHsv,
  rgbToXyz,
  xyzToLab,
  xyzToRgb,
} = globalThis.ColorLab.conversions;
const {
  createRgbXyzMatrices,
  getWhitePoint,
} = globalThis.ColorLab.illuminants;
const { multiplyMatrices } = globalThis.ColorLab.matrix;

const ILLUMINANT_NAMES = ["D65", "D50", "E"];

function assertClose(actual, expected, tolerance = 1e-7) {
  assert.ok(
    Math.abs(actual - expected) <= tolerance,
    `expected ${actual} to be within ${tolerance} of ${expected}`,
  );
}

function assertColorClose(actual, expected, tolerance = 1e-7) {
  for (const component of Object.keys(expected)) {
    assertClose(actual[component], expected[component], tolerance);
  }
}

test("builds the standard sRGB D65 matrix from chromaticities", () => {
  const { rgbToXyz } = createRgbXyzMatrices("D65");
  const expected = [
    [0.4123907993, 0.3575843394, 0.1804807884],
    [0.2126390059, 0.7151686788, 0.0721923154],
    [0.0193308187, 0.1191947798, 0.9505321522],
  ];

  rgbToXyz.forEach((row, rowIndex) => {
    row.forEach((value, columnIndex) => {
      assertClose(value, expected[rowIndex][columnIndex], 1e-9);
    });
  });
});

test("rebuilds matrices for every supported illuminant", () => {
  for (const illuminantName of ILLUMINANT_NAMES) {
    const { rgbToXyz, xyzToRgb, whitePoint } =
      createRgbXyzMatrices(illuminantName);
    const inverseProduct = multiplyMatrices(rgbToXyz, xyzToRgb);

    rgbToXyz.forEach((row, rowIndex) => {
      assertClose(
        row.reduce((sum, value) => sum + value, 0),
        whitePoint[rowIndex],
      );
    });

    inverseProduct.forEach((row, rowIndex) => {
      row.forEach((value, columnIndex) => {
        assertClose(value, rowIndex === columnIndex ? 1 : 0);
      });
    });
  }

  assert.notDeepEqual(
    createRgbXyzMatrices("D65").rgbToXyz,
    createRgbXyzMatrices("D50").rgbToXyz,
  );
});

test("converts HSV primary colors to RGB", () => {
  assertColorClose(hsvToRgb({ h: 0, s: 100, v: 100 }), {
    r: 255,
    g: 0,
    b: 0,
  });
  assertColorClose(hsvToRgb({ h: 120, s: 100, v: 100 }), {
    r: 0,
    g: 255,
    b: 0,
  });
  assertColorClose(hsvToRgb({ h: 240, s: 100, v: 100 }), {
    r: 0,
    g: 0,
    b: 255,
  });
});

test("round-trips RGB through HSV", () => {
  const samples = [
    { r: 0, g: 0, b: 0 },
    { r: 255, g: 255, b: 255 },
    { r: 124, g: 92, b: 252 },
    { r: 17, g: 201, b: 83 },
  ];

  for (const rgb of samples) {
    assertColorClose(hsvToRgb(rgbToHsv(rgb)), rgb);
  }
});

test("maps RGB white to the selected XYZ white point", () => {
  for (const illuminantName of ILLUMINANT_NAMES) {
    const xyz = rgbToXyz({ r: 255, g: 255, b: 255 }, illuminantName);
    const whitePoint = getWhitePoint(illuminantName);

    assertColorClose(xyz, {
      x: whitePoint[0] * 100,
      y: 100,
      z: whitePoint[2] * 100,
    });
  }
});

test("round-trips RGB through XYZ for all illuminants", () => {
  const samples = [
    { r: 0, g: 0, b: 0 },
    { r: 255, g: 255, b: 255 },
    { r: 124, g: 92, b: 252 },
    { r: 240, g: 128, b: 32 },
  ];

  for (const illuminantName of ILLUMINANT_NAMES) {
    for (const rgb of samples) {
      assertColorClose(
        xyzToRgb(rgbToXyz(rgb, illuminantName), illuminantName),
        rgb,
        1e-6,
      );
    }
  }
});

test("converts the selected white point to neutral LAB", () => {
  for (const illuminantName of ILLUMINANT_NAMES) {
    const whitePoint = getWhitePoint(illuminantName);
    const lab = xyzToLab(
      {
        x: whitePoint[0] * 100,
        y: 100,
        z: whitePoint[2] * 100,
      },
      illuminantName,
    );

    assertColorClose(lab, { l: 100, a: 0, b: 0 });
  }
});

test("round-trips XYZ through LAB for all illuminants", () => {
  const samples = [
    { x: 0, y: 0, z: 0 },
    { x: 18, y: 20, z: 24 },
    { x: 41.24, y: 21.26, z: 1.93 },
  ];

  for (const illuminantName of ILLUMINANT_NAMES) {
    for (const xyz of samples) {
      assertColorClose(
        labToXyz(xyzToLab(xyz, illuminantName), illuminantName),
        xyz,
      );
    }
  }
});

test("round-trips an in-gamut color through the complete HSV–XYZ–LAB path", () => {
  const hsv = { h: 252, s: 63, v: 99 };

  for (const illuminantName of ILLUMINANT_NAMES) {
    const lab = hsvToLab(hsv, illuminantName);
    const restoredHsv = rgbToHsv(labToRgb(lab, illuminantName));

    assertColorClose(restoredHsv, hsv, 1e-6);
  }
});
