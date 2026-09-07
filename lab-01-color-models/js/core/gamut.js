(function initializeGamutMapping(global) {
  const RGB_MIN = 0;
  const RGB_MAX = 255;
  const GAMUT_EPSILON = 1e-7;

  function assertRgb(rgb) {
    if (
      !rgb ||
      [rgb.r, rgb.g, rgb.b].some((component) => !Number.isFinite(component))
    ) {
      throw new TypeError("RGB components must be finite numbers");
    }
  }

  function isRgbInGamut(rgb) {
    assertRgb(rgb);

    return [rgb.r, rgb.g, rgb.b].every(
      (component) =>
        component >= RGB_MIN - GAMUT_EPSILON &&
        component <= RGB_MAX + GAMUT_EPSILON,
    );
  }

  function clipRgb(rgb) {
    assertRgb(rgb);

    return {
      r: Math.min(RGB_MAX, Math.max(RGB_MIN, rgb.r)),
      g: Math.min(RGB_MAX, Math.max(RGB_MIN, rgb.g)),
      b: Math.min(RGB_MAX, Math.max(RGB_MIN, rgb.b)),
    };
  }

  function scaleRgb(rgb) {
    assertRgb(rgb);

    if (isRgbInGamut(rgb)) {
      return clipRgb(rgb);
    }

    const components = [rgb.r, rgb.g, rgb.b];
    const lowerBound = Math.min(RGB_MIN, ...components);
    const upperBound = Math.max(RGB_MAX, ...components);
    const scale = RGB_MAX / (upperBound - lowerBound);
    const scaled = components.map(
      (component) => (component - lowerBound) * scale,
    );

    return { r: scaled[0], g: scaled[1], b: scaled[2] };
  }

  function mapRgbToGamut(rgb, strategy = "clipping") {
    const adjusted = !isRgbInGamut(rgb);

    if (strategy === "clipping") {
      return { rgb: clipRgb(rgb), adjusted, strategy };
    }

    if (strategy === "scaling") {
      return { rgb: scaleRgb(rgb), adjusted, strategy };
    }

    throw new RangeError(`unknown gamut mapping strategy: ${strategy}`);
  }

  global.ColorLab.gamut = Object.freeze({
    isRgbInGamut,
    clipRgb,
    scaleRgb,
    mapRgbToGamut,
  });
})(globalThis);
