(function initializeApplication(global) {
  const {
    hsvToRgb,
    labToXyz,
    rgbToHsv,
    rgbToXyz,
    xyzToLab,
    xyzToRgb,
  } = global.ColorLab.conversions;
  const { mapRgbToGamut } = global.ColorLab.gamut;
  const { buildComponentGradient } = global.ColorLab.gradients;

  const MODEL_COMPONENTS = Object.freeze({
    hsv: ["h", "s", "v"],
    xyz: ["x", "y", "z"],
    lab: ["l", "a", "b"],
  });
  const MODEL_PRECISION = Object.freeze({ hsv: 1, xyz: 2, lab: 2 });
  const STRATEGY_LABELS = Object.freeze({
    clipping: "обрезание",
    scaling: "масштабирование",
  });

  const colorPicker = document.querySelector("#color-picker");
  const colorPreview = document.querySelector("#color-preview");
  const colorValue = document.querySelector("#current-color-value");
  const illuminantSelect = document.querySelector("#illuminant-select");
  const gamutSelect = document.querySelector("#gamut-select");
  const statusMessage = document.querySelector("#status-message");
  const modelControls = collectModelControls();

  let activeModel = "hsv";
  let state = createStateFromRgb(hexToRgb(colorPicker.value));
  let gradientFrameId = 0;

  function collectModelControls() {
    const controls = {};

    for (const [modelName, componentNames] of Object.entries(MODEL_COMPONENTS)) {
      const modelElement = document.querySelector(`[data-model="${modelName}"]`);
      controls[modelName] = {};

      for (const componentName of componentNames) {
        const componentElement = modelElement.querySelector(
          `[data-component="${componentName}"]`,
        );
        controls[modelName][componentName] = {
          number: componentElement.querySelector('[data-control="number"]'),
          range: componentElement.querySelector('[data-control="range"]'),
        };
      }
    }

    return controls;
  }

  function hexToRgb(hex) {
    const numericValue = Number.parseInt(hex.slice(1), 16);

    return {
      r: (numericValue >> 16) & 255,
      g: (numericValue >> 8) & 255,
      b: numericValue & 255,
    };
  }

  function rgbToHex(rgb) {
    const channels = [rgb.r, rgb.g, rgb.b].map((channel) =>
      Math.round(Math.min(255, Math.max(0, channel))),
    );

    return `#${channels
      .map((channel) => channel.toString(16).padStart(2, "0"))
      .join("")}`;
  }

  function createStateFromRgb(rgb) {
    const hsv = rgbToHsv(rgb);
    const xyz = rgbToXyz(rgb, illuminantSelect.value);

    return {
      rgb,
      hsv,
      xyz,
      lab: xyzToLab(xyz, illuminantSelect.value),
      gamutAdjusted: false,
    };
  }

  function createStateFromModel(modelName, color) {
    const illuminant = illuminantSelect.value;
    let hsv;
    let xyz;
    let lab;
    let rgb;
    let gamutAdjusted = false;

    if (modelName === "hsv") {
      hsv = color;
      rgb = hsvToRgb(hsv);
      xyz = rgbToXyz(rgb, illuminant);
      lab = xyzToLab(xyz, illuminant);
    } else if (modelName === "xyz") {
      xyz = color;
      lab = xyzToLab(xyz, illuminant);
      const mapped = mapRgbToGamut(xyzToRgb(xyz, illuminant), gamutSelect.value);
      rgb = mapped.rgb;
      hsv = rgbToHsv(rgb);
      gamutAdjusted = mapped.adjusted;
    } else if (modelName === "lab") {
      lab = color;
      xyz = labToXyz(lab, illuminant);
      const mapped = mapRgbToGamut(xyzToRgb(xyz, illuminant), gamutSelect.value);
      rgb = mapped.rgb;
      hsv = rgbToHsv(rgb);
      gamutAdjusted = mapped.adjusted;
    } else {
      throw new RangeError(`unknown source model: ${modelName}`);
    }

    return { rgb, hsv, xyz, lab, gamutAdjusted };
  }

  function readModel(modelName) {
    return Object.fromEntries(
      MODEL_COMPONENTS[modelName].map((componentName) => [
        componentName,
        Number(modelControls[modelName][componentName].number.value),
      ]),
    );
  }

  function formatComponent(value, precision) {
    const rounded = Number(value.toFixed(precision));
    return Object.is(rounded, -0) ? "0" : String(rounded);
  }

  function writeModel(modelName, color) {
    const precision = MODEL_PRECISION[modelName];

    for (const componentName of MODEL_COMPONENTS[modelName]) {
      const { number, range } = modelControls[modelName][componentName];
      const value = color[componentName];
      number.value = formatComponent(value, precision);

      const rangeMinimum = Number(range.min);
      const rangeMaximum = Number(range.max);
      range.value = String(Math.min(rangeMaximum, Math.max(rangeMinimum, value)));
    }
  }

  function renderColor() {
    const hex = rgbToHex(state.rgb);
    colorValue.textContent = hex.toUpperCase();
    colorPicker.value = hex;
    colorPreview.style.setProperty("--preview", hex);
    colorPreview.setAttribute("aria-label", `Образец цвета ${hex.toUpperCase()}`);
  }

  function renderStatus(correctionMessage = "") {
    statusMessage.classList.toggle(
      "status-message--warning",
      state.gamutAdjusted || Boolean(correctionMessage),
    );

    if (state.gamutAdjusted) {
      statusMessage.textContent =
        `Цвет выходит за диапазон sRGB. Применено: ` +
        `${STRATEGY_LABELS[gamutSelect.value]}.`;
    } else if (correctionMessage) {
      statusMessage.textContent = correctionMessage;
    } else {
      statusMessage.textContent = "Значения находятся в допустимом диапазоне.";
    }
  }

  function renderGradients() {
    for (const [modelName, components] of Object.entries(modelControls)) {
      for (const [componentName, { range }] of Object.entries(components)) {
        range.style.setProperty(
          "--range-background",
          buildComponentGradient({
            modelName,
            componentName,
            color: state[modelName],
            minimum: Number(range.min),
            maximum: Number(range.max),
            illuminant: illuminantSelect.value,
            gamutStrategy: gamutSelect.value,
          }),
        );
      }
    }
  }

  function scheduleGradientRender() {
    global.cancelAnimationFrame(gradientFrameId);
    gradientFrameId = global.requestAnimationFrame(() => {
      renderGradients();
      gradientFrameId = 0;
    });
  }

  function render(correctionMessage = "") {
    writeModel("hsv", state.hsv);
    writeModel("xyz", state.xyz);
    writeModel("lab", state.lab);
    renderColor();
    renderStatus(correctionMessage);
    scheduleGradientRender();
  }

  function updateFromModel(modelName, correctionMessage = "") {
    activeModel = modelName;
    state = createStateFromModel(modelName, readModel(modelName));
    render(correctionMessage);
  }

  function normalizeNumberInput(numberInput) {
    if (numberInput.value.trim() === "" || !Number.isFinite(numberInput.valueAsNumber)) {
      return { valid: false, corrected: false };
    }

    const minimum = Number(numberInput.min);
    const maximum = Number(numberInput.max);
    const original = numberInput.valueAsNumber;
    const normalized = Math.min(maximum, Math.max(minimum, original));

    numberInput.value = String(normalized);

    return { valid: true, corrected: normalized !== original };
  }

  function attachComponentHandlers() {
    for (const [modelName, components] of Object.entries(modelControls)) {
      for (const [componentName, { number, range }] of Object.entries(components)) {
        range.addEventListener("input", () => {
          number.value = range.value;
          updateFromModel(modelName);
        });

        number.addEventListener("change", () => {
          const normalization = normalizeNumberInput(number);

          if (!normalization.valid) {
            render("Введите числовое значение компоненты.");
            return;
          }

          range.value = number.value;
          const correctionMessage = normalization.corrected
            ? `Компонента ${componentName.toUpperCase()} приведена к допустимому диапазону.`
            : "";
          updateFromModel(modelName, correctionMessage);
        });
      }
    }
  }

  colorPicker.addEventListener("input", () => {
    activeModel = "hsv";
    state = createStateFromRgb(hexToRgb(colorPicker.value));
    render();
  });

  illuminantSelect.addEventListener("change", () => {
    state = createStateFromModel(activeModel, state[activeModel]);
    render();
  });

  gamutSelect.addEventListener("change", () => {
    state = createStateFromModel(activeModel, state[activeModel]);
    render();
  });

  attachComponentHandlers();
  render();
})(globalThis);
