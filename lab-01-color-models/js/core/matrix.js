const MATRIX_SIZE = 3;
const SINGULAR_EPSILON = 1e-12;

function assertMatrix3x3(matrix, name = "matrix") {
  if (
    !Array.isArray(matrix) ||
    matrix.length !== MATRIX_SIZE ||
    matrix.some(
      (row) =>
        !Array.isArray(row) ||
        row.length !== MATRIX_SIZE ||
        row.some((value) => !Number.isFinite(value)),
    )
  ) {
    throw new TypeError(`${name} must be a 3 × 3 matrix of finite numbers`);
  }
}

function assertVector3(vector, name = "vector") {
  if (
    !Array.isArray(vector) ||
    vector.length !== MATRIX_SIZE ||
    vector.some((value) => !Number.isFinite(value))
  ) {
    throw new TypeError(`${name} must contain three finite numbers`);
  }
}

export function multiplyMatrixVector(matrix, vector) {
  assertMatrix3x3(matrix);
  assertVector3(vector);

  return matrix.map(
    (row) => row[0] * vector[0] + row[1] * vector[1] + row[2] * vector[2],
  );
}

export function multiplyMatrices(left, right) {
  assertMatrix3x3(left, "left matrix");
  assertMatrix3x3(right, "right matrix");

  return left.map((row, rowIndex) =>
    right[0].map(
      (_, columnIndex) =>
        row[0] * right[0][columnIndex] +
        row[1] * right[1][columnIndex] +
        row[2] * right[2][columnIndex],
    ),
  );
}

export function determinant3x3(matrix) {
  assertMatrix3x3(matrix);

  const [[a, b, c], [d, e, f], [g, h, i]] = matrix;

  return (
    a * (e * i - f * h) -
    b * (d * i - f * g) +
    c * (d * h - e * g)
  );
}

export function invertMatrix3x3(matrix) {
  assertMatrix3x3(matrix);

  const [[a, b, c], [d, e, f], [g, h, i]] = matrix;
  const determinant = determinant3x3(matrix);

  if (Math.abs(determinant) < SINGULAR_EPSILON) {
    throw new RangeError("matrix is singular and cannot be inverted");
  }

  const inverseDeterminant = 1 / determinant;

  return [
    [
      (e * i - f * h) * inverseDeterminant,
      (c * h - b * i) * inverseDeterminant,
      (b * f - c * e) * inverseDeterminant,
    ],
    [
      (f * g - d * i) * inverseDeterminant,
      (a * i - c * g) * inverseDeterminant,
      (c * d - a * f) * inverseDeterminant,
    ],
    [
      (d * h - e * g) * inverseDeterminant,
      (b * g - a * h) * inverseDeterminant,
      (a * e - b * d) * inverseDeterminant,
    ],
  ];
}

export function scaleMatrixColumns(matrix, scales) {
  assertMatrix3x3(matrix);
  assertVector3(scales, "column scales");

  return matrix.map((row) => row.map((value, index) => value * scales[index]));
}
