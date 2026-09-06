import assert from "node:assert/strict";
import test from "node:test";

import "../js/core/matrix.js";
import "../js/core/illuminants.js";
import "../js/core/conversions.js";
import "../js/core/gamut.js";

const { clipRgb, isRgbInGamut, mapRgbToGamut, scaleRgb } =
  globalThis.ColorLab.gamut;

function assertClose(actual, expected, tolerance = 1e-9) {
  assert.ok(
    Math.abs(actual - expected) <= tolerance,
    `expected ${actual} to be within ${tolerance} of ${expected}`,
  );
}

test("detects whether RGB is inside the display gamut", () => {
  assert.equal(isRgbInGamut({ r: 0, g: 127.5, b: 255 }), true);
  assert.equal(isRgbInGamut({ r: -0.1, g: 128, b: 255 }), false);
  assert.equal(isRgbInGamut({ r: 0, g: 128, b: 255.1 }), false);
});

test("clips every out-of-range channel independently", () => {
  assert.deepEqual(clipRgb({ r: -12, g: 128, b: 270 }), {
    r: 0,
    g: 128,
    b: 255,
  });
});

test("scales all channels by one common range", () => {
  const scaled = scaleRgb({ r: -20, g: 100, b: 280 });

  assertClose(scaled.r, 0);
  assertClose(scaled.g, 102);
  assertClose(scaled.b, 255);
});

test("does not change valid RGB values", () => {
  const rgb = { r: 24, g: 130, b: 246 };

  assert.deepEqual(mapRgbToGamut(rgb, "clipping"), {
    rgb,
    adjusted: false,
    strategy: "clipping",
  });
  assert.deepEqual(mapRgbToGamut(rgb, "scaling"), {
    rgb,
    adjusted: false,
    strategy: "scaling",
  });
});
