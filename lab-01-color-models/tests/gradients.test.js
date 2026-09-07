import assert from "node:assert/strict";
import test from "node:test";

import "../js/core/matrix.js";
import "../js/core/illuminants.js";
import "../js/core/conversions.js";
import "../js/core/gamut.js";
import "../js/ui/gradients.js";

const { buildComponentGradient } = globalThis.ColorLab.gradients;

test("builds a gradient with the requested number of calculated stops", () => {
  const gradient = buildComponentGradient({
    modelName: "hsv",
    componentName: "h",
    color: { h: 252, s: 63, v: 99 },
    minimum: 0,
    maximum: 360,
    stopCount: 7,
  });

  assert.match(gradient, /^linear-gradient\(90deg,/);
  assert.equal((gradient.match(/rgb\(/g) ?? []).length, 7);
  assert.match(gradient, /0\.00%/);
  assert.match(gradient, /100\.00%/);
});

test("uses the current values of the other components", () => {
  const darkGradient = buildComponentGradient({
    modelName: "hsv",
    componentName: "s",
    color: { h: 30, s: 50, v: 20 },
    minimum: 0,
    maximum: 100,
  });
  const brightGradient = buildComponentGradient({
    modelName: "hsv",
    componentName: "s",
    color: { h: 30, s: 50, v: 100 },
    minimum: 0,
    maximum: 100,
  });

  assert.notEqual(darkGradient, brightGradient);
});

test("builds displayable XYZ and LAB gradients with gamut mapping", () => {
  const xyzGradient = buildComponentGradient({
    modelName: "xyz",
    componentName: "x",
    color: { x: 30, y: 20, z: 90 },
    minimum: 0,
    maximum: 100,
    illuminant: "D50",
    gamutStrategy: "scaling",
  });
  const labGradient = buildComponentGradient({
    modelName: "lab",
    componentName: "a",
    color: { l: 52, a: 49, b: -74 },
    minimum: -128,
    maximum: 127,
    illuminant: "E",
    gamutStrategy: "clipping",
  });

  assert.equal((xyzGradient.match(/rgb\(/g) ?? []).length, 13);
  assert.equal((labGradient.match(/rgb\(/g) ?? []).length, 13);
  assert.doesNotMatch(xyzGradient + labGradient, /NaN|Infinity/);
});
