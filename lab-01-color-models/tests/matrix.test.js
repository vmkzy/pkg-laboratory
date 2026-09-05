import assert from "node:assert/strict";
import test from "node:test";

import {
  determinant3x3,
  invertMatrix3x3,
  multiplyMatrices,
  multiplyMatrixVector,
} from "../js/core/matrix.js";

const EPSILON = 1e-10;

function assertClose(actual, expected, tolerance = EPSILON) {
  assert.ok(
    Math.abs(actual - expected) <= tolerance,
    `expected ${actual} to be within ${tolerance} of ${expected}`,
  );
}

test("multiplies a 3 × 3 matrix by a vector", () => {
  const result = multiplyMatrixVector(
    [
      [1, 2, 3],
      [4, 5, 6],
      [7, 8, 9],
    ],
    [2, 0, -1],
  );

  assert.deepEqual(result, [-1, 2, 5]);
});

test("calculates a determinant", () => {
  const determinant = determinant3x3([
    [1, 2, 3],
    [0, 1, 4],
    [5, 6, 0],
  ]);

  assert.equal(determinant, 1);
});

test("calculates an inverse matrix", () => {
  const matrix = [
    [4, 7, 2],
    [3, 6, 1],
    [2, 5, 3],
  ];
  const product = multiplyMatrices(matrix, invertMatrix3x3(matrix));

  product.forEach((row, rowIndex) => {
    row.forEach((value, columnIndex) => {
      assertClose(value, rowIndex === columnIndex ? 1 : 0);
    });
  });
});

test("rejects a singular matrix", () => {
  assert.throws(
    () =>
      invertMatrix3x3([
        [1, 2, 3],
        [2, 4, 6],
        [3, 6, 9],
      ]),
    RangeError,
  );
});
