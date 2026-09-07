(function initializeGradients(global) {
  const { hsvToRgb, labToXyz, xyzToRgb } = global.ColorLab.conversions;
  const { mapRgbToGamut } = global.ColorLab.gamut;
  const DEFAULT_STOP_COUNT = 13;

  function modelColorToRgb(modelName, color, illuminant, gamutStrategy) {
    if (modelName === "hsv") {
      return hsvToRgb(color);
    }

    const xyz = modelName === "xyz" ? color : labToXyz(color, illuminant);
    return mapRgbToGamut(
      xyzToRgb(xyz, illuminant),
      gamutStrategy,
    ).rgb;
  }

  function formatRgb(rgb) {
    const channels = [rgb.r, rgb.g, rgb.b].map((component) =>
      Math.round(Math.min(255, Math.max(0, component))),
    );

    return `rgb(${channels.join(" ")})`;
  }

  function buildComponentGradient({
    modelName,
    componentName,
    color,
    minimum,
    maximum,
    illuminant = "D65",
    gamutStrategy = "clipping",
    stopCount = DEFAULT_STOP_COUNT,
  }) {
    if (!Number.isInteger(stopCount) || stopCount < 2) {
      throw new RangeError("a gradient must contain at least two stops");
    }

    const stops = [];

    for (let index = 0; index < stopCount; index += 1) {
      const position = index / (stopCount - 1);
      const sample = {
        ...color,
        [componentName]: minimum + (maximum - minimum) * position,
      };
      const rgb = modelColorToRgb(
        modelName,
        sample,
        illuminant,
        gamutStrategy,
      );

      stops.push(`${formatRgb(rgb)} ${(position * 100).toFixed(2)}%`);
    }

    return `linear-gradient(90deg, ${stops.join(", ")})`;
  }

  global.ColorLab.gradients = Object.freeze({
    modelColorToRgb,
    buildComponentGradient,
  });
})(globalThis);
