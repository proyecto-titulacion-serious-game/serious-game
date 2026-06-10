/**
 * PT-A TITA — Generador automático de Google Forms
 *
 * INSTRUCCIONES:
 *  1. Abre https://script.google.com → Nuevo proyecto
 *  2. Pega TODO este archivo, reemplazando el código existente
 *  3. Clic en ▶ Ejecutar → función: crearTodosLosForms
 *  4. Acepta los permisos (Google Forms + Sheets)
 *  5. Al terminar, revisa el Log (Ver → Registros) para ver los links de cada form
 */

function crearTodosLosForms() {
  const links = {};

  links["B_Demografico"]   = crearFormDemografico();
  links["C_Pretest"]       = crearFormPretest();
  links["C_Postest"]       = crearFormPostest();
  links["E_SUS"]           = crearFormSUS();
  links["E_Likert"]        = crearFormLikert();
  links["F_SSQ_Inicio"]    = crearFormSSQ("SSQ Cybersickness — ANTES de la sesión (Anexo F - Inicio)");
  links["F_SSQ_Final"]     = crearFormSSQ("SSQ Cybersickness — DESPUÉS de la sesión (Anexo F - Final)");

  Logger.log("=== LINKS DE LOS FORMS ===");
  for (const [nombre, url] of Object.entries(links)) {
    Logger.log(`${nombre}: ${url}`);
  }

  // Crea una hoja con todos los links para fácil acceso
  const ss = SpreadsheetApp.create("PT-A TITA — Links de Forms");
  const hoja = ss.getActiveSheet();
  hoja.setName("Links");
  hoja.appendRow(["Form", "URL edición", "URL para participantes"]);
  for (const [nombre, url] of Object.entries(links)) {
    const form = FormApp.openByUrl(url);
    hoja.appendRow([nombre, url, form.getPublishedUrl()]);
  }
  Logger.log("Hoja de links creada: " + ss.getUrl());
}

// ─────────────────────────────────────────────────────────────
// ANEXO B — Datos demográficos
// ─────────────────────────────────────────────────────────────
function crearFormDemografico() {
  const form = FormApp.create("PT-A TITA — Datos Demográficos (Anexo B)");
  form.setDescription("Completa antes de iniciar la sesión de juego. Tus datos son anónimos y solo se usan con fines académicos.");
  form.setCollectEmail(false);

  form.addTextItem()
    .setTitle("Código de participante (el facilitador te lo entrega)")
    .setRequired(true);

  form.addMultipleChoiceItem()
    .setTitle("Rol asignado")
    .setChoiceValues(["Explorador (VR)", "Técnico (PC)"])
    .setRequired(true);

  form.addTextItem()
    .setTitle("Edad")
    .setRequired(true);

  form.addTextItem()
    .setTitle("Semestre / nivel actual")
    .setRequired(true);

  form.addMultipleChoiceItem()
    .setTitle("¿Cursaste o estás cursando Computación Ubicua?")
    .setChoiceValues(["Sí", "No"])
    .setRequired(true);

  form.addMultipleChoiceItem()
    .setTitle("Experiencia previa con realidad virtual (VR)")
    .setChoiceValues(["Ninguna", "Poca (1–3 veces)", "Frecuente (más de 3 veces)"])
    .setRequired(true);

  form.addMultipleChoiceItem()
    .setTitle("Experiencia con Unity / desarrollo de videojuegos")
    .setChoiceValues(["Ninguna", "Básica", "Intermedia o avanzada"])
    .setRequired(true);

  form.addScaleItem()
    .setTitle("Autoevaluación de conocimiento en electrónica básica")
    .setBounds(1, 5)
    .setLabels("1 = Nulo", "5 = Experto")
    .setRequired(true);

  Logger.log("Form B creado: " + form.getEditUrl());
  return form.getEditUrl();
}

// ─────────────────────────────────────────────────────────────
// ANEXO C — Pre-test
// ─────────────────────────────────────────────────────────────
function crearFormPretest() {
  const form = FormApp.create("PT-A TITA — Pre-test de Conocimiento (Anexo C)");
  form.setDescription("10 preguntas de opción múltiple sobre electrónica básica. Responde sin ayuda externa. 1 punto por pregunta.");
  form.setCollectEmail(false);

  form.addTextItem()
    .setTitle("Código de participante")
    .setRequired(true);

  const preguntas = [
    {
      titulo: "1. Ley de Ohm — Si una resistencia de 100 Ω tiene una caída de 5 V, ¿qué corriente circula?",
      opciones: ["a) 0,05 A", "b) 0,5 A", "c) 20 A", "d) 500 A"]
    },
    {
      titulo: "2. Circuito serie — En un circuito serie con dos resistencias (100 Ω y 200 Ω), la resistencia total es:",
      opciones: ["a) 66,7 Ω", "b) 150 Ω", "c) 300 Ω", "d) 20 000 Ω"]
    },
    {
      titulo: "3. Circuito paralelo — Dos resistencias iguales de 100 Ω en paralelo dan una resistencia equivalente de:",
      opciones: ["a) 50 Ω", "b) 100 Ω", "c) 200 Ω", "d) 0 Ω"]
    },
    {
      titulo: "4. Corriente en serie — En un circuito serie, la corriente que pasa por cada componente es:",
      opciones: ["a) Distinta en cada uno", "b) La misma en todos", "c) Cero", "d) Depende del color"]
    },
    {
      titulo: "5. Voltaje en paralelo — En ramas en paralelo, el voltaje en cada rama es:",
      opciones: ["a) El mismo en todas", "b) Se divide entre las ramas", "c) Siempre 0", "d) Siempre 9 V"]
    },
    {
      titulo: "6. Polaridad del LED — Un LED conectado con la polaridad invertida:",
      opciones: ["a) Se enciende más fuerte", "b) No enciende", "c) Explota siempre", "d) Cambia de color"]
    },
    {
      titulo: "7. Código de colores — Una resistencia con bandas marrón-negro-marrón vale aproximadamente:",
      opciones: ["a) 10 Ω", "b) 100 Ω", "c) 1 000 Ω", "d) 10 000 Ω"]
    },
    {
      titulo: "8. Capacitor electrolítico — Estos capacitores tienen polaridad; conectarlos al revés puede causar:",
      opciones: ["a) Nada", "b) Mayor capacitancia", "c) Falla/daño (calor, humo)", "d) Más brillo"]
    },
    {
      titulo: "9. Cortocircuito — Un cortocircuito se caracteriza por:",
      opciones: ["a) Resistencia muy alta", "b) Resistencia casi nula y corriente muy alta", "c) Voltaje infinito", "d) No circula corriente"]
    },
    {
      titulo: "10. Arduino — Para leer un sensor analógico en Arduino se usa típicamente:",
      opciones: ["a) Un pin digital de salida", "b) Un pin analógico de entrada (A0…)", "c) El pin de tierra (GND)", "d) El pin de 5 V"]
    }
  ];

  preguntas.forEach(p => {
    form.addMultipleChoiceItem()
      .setTitle(p.titulo)
      .setChoiceValues(p.opciones)
      .setRequired(true);
  });

  Logger.log("Form C Pre-test creado: " + form.getEditUrl());
  return form.getEditUrl();
}

// ─────────────────────────────────────────────────────────────
// ANEXO C' — Post-test
// ─────────────────────────────────────────────────────────────
function crearFormPostest() {
  const form = FormApp.create("PT-A TITA — Post-test de Conocimiento (Anexo C')");
  form.setDescription("10 preguntas equivalentes al pre-test, con valores distintos. Responde sin ayuda externa.");
  form.setCollectEmail(false);

  form.addTextItem()
    .setTitle("Código de participante")
    .setRequired(true);

  const preguntas = [
    {
      titulo: "1. Ley de Ohm — Una resistencia de 220 Ω tiene una caída de voltaje de 11 V. ¿Qué corriente circula?",
      opciones: ["a) 0,05 A", "b) 2 A", "c) 0,5 A", "d) 20 A"]
    },
    {
      titulo: "2. Circuito serie — En un circuito serie con tres resistencias (100 Ω, 150 Ω y 250 Ω), la resistencia total es:",
      opciones: ["a) 83,3 Ω", "b) 250 Ω", "c) 500 Ω", "d) 1 500 Ω"]
    },
    {
      titulo: "3. Circuito paralelo — Dos resistencias de 200 Ω y 200 Ω en paralelo dan una resistencia equivalente de:",
      opciones: ["a) 400 Ω", "b) 200 Ω", "c) 100 Ω", "d) 0 Ω"]
    },
    {
      titulo: "4. Corriente en paralelo — En un circuito paralelo, la corriente total que entrega la fuente es:",
      opciones: ["a) Menor que la corriente de cada rama", "b) Igual a la corriente de una sola rama", "c) La suma de las corrientes de todas las ramas", "d) Siempre cero"]
    },
    {
      titulo: "5. Voltaje en serie — En un circuito serie alimentado con 9 V y dos resistencias iguales, el voltaje en cada resistencia es:",
      opciones: ["a) 9 V", "b) 18 V", "c) 4,5 V", "d) 0 V"]
    },
    {
      titulo: "6. Polaridad del capacitor — Un capacitor electrolítico conectado con la polaridad invertida puede:",
      opciones: ["a) Funcionar normalmente", "b) Aumentar su capacitancia", "c) Sobrecalentarse y dañarse", "d) Actuar como una resistencia"]
    },
    {
      titulo: "7. Código de colores — Una resistencia con bandas rojo-rojo-rojo vale aproximadamente:",
      opciones: ["a) 22 Ω", "b) 220 Ω", "c) 2 200 Ω", "d) 22 000 Ω"]
    },
    {
      titulo: "8. LED y corriente — Un LED típico de señalización trabaja de forma segura con una corriente de:",
      opciones: ["a) 5 A", "b) 500 mA", "c) 10–20 mA", "d) 0,001 mA"]
    },
    {
      titulo: "9. Circuito abierto — En un circuito serie con un cable suelto (conexión abierta), la corriente que circula es:",
      opciones: ["a) Muy alta (riesgo de quema)", "b) La misma que sin el fallo", "c) Cero", "d) Depende del voltaje"]
    },
    {
      titulo: "10. Arduino — Para controlar el estado alto/bajo de un LED conectado al pin D13, la instrucción correcta es:",
      opciones: [
        "a) analogRead(13)",
        "b) pinMode(13, INPUT) → digitalRead(13)",
        "c) pinMode(13, OUTPUT) → digitalWrite(13, HIGH/LOW)",
        "d) analogWrite(13, 255)"
      ]
    }
  ];

  preguntas.forEach(p => {
    form.addMultipleChoiceItem()
      .setTitle(p.titulo)
      .setChoiceValues(p.opciones)
      .setRequired(true);
  });

  Logger.log("Form C' Post-test creado: " + form.getEditUrl());
  return form.getEditUrl();
}

// ─────────────────────────────────────────────────────────────
// ANEXO E — SUS (System Usability Scale)
// ─────────────────────────────────────────────────────────────
function crearFormSUS() {
  const form = FormApp.create("PT-A TITA — System Usability Scale / SUS (Anexo E)");
  form.setDescription(
    "10 afirmaciones sobre el sistema. Marca tu nivel de acuerdo.\n" +
    "1 = Totalmente en desacuerdo  |  5 = Totalmente de acuerdo"
  );
  form.setCollectEmail(false);

  form.addTextItem()
    .setTitle("Código de participante")
    .setRequired(true);

  const items = [
    "1. Creo que me gustaría usar este sistema con frecuencia.",
    "2. Encontré el sistema innecesariamente complejo.",
    "3. Pensé que el sistema era fácil de usar.",
    "4. Creo que necesitaría apoyo de un técnico para poder usar este sistema.",
    "5. Encontré que las distintas funciones del sistema estaban bien integradas.",
    "6. Pensé que había demasiada inconsistencia en el sistema.",
    "7. Imagino que la mayoría de la gente aprendería a usar este sistema muy rápido.",
    "8. Encontré el sistema muy incómodo / engorroso de usar.",
    "9. Me sentí muy seguro/a usando el sistema.",
    "10. Necesité aprender muchas cosas antes de poder empezar a usar el sistema."
  ];

  items.forEach(texto => {
    form.addScaleItem()
      .setTitle(texto)
      .setBounds(1, 5)
      .setLabels("1 Totalmente en desacuerdo", "5 Totalmente de acuerdo")
      .setRequired(true);
  });

  Logger.log("Form E SUS creado: " + form.getEditUrl());
  return form.getEditUrl();
}

// ─────────────────────────────────────────────────────────────
// ANEXO E' — Satisfacción Likert
// ─────────────────────────────────────────────────────────────
function crearFormLikert() {
  const form = FormApp.create("PT-A TITA — Satisfacción (Likert, Anexo E')");
  form.setDescription(
    "6 afirmaciones sobre tu experiencia con el juego.\n" +
    "1 = Totalmente en desacuerdo  |  5 = Totalmente de acuerdo"
  );
  form.setCollectEmail(false);

  form.addTextItem()
    .setTitle("Código de participante")
    .setRequired(true);

  const items = [
    "1. El juego me ayudó a entender mejor los conceptos de electrónica básica.",
    "2. La retroalimentación (luces, sonido, vibración) fue clara y útil.",
    "3. La colaboración con mi compañero/a fue necesaria y bien lograda.",
    "4. La narrativa (nave espacial) hizo la experiencia más motivadora.",
    "5. Recomendaría este juego como apoyo a la asignatura Computación Ubicua.",
    "6. Me sentí inmerso/a en el entorno virtual."
  ];

  items.forEach(texto => {
    form.addScaleItem()
      .setTitle(texto)
      .setBounds(1, 5)
      .setLabels("1 Totalmente en desacuerdo", "5 Totalmente de acuerdo")
      .setRequired(true);
  });

  Logger.log("Form E' Likert creado: " + form.getEditUrl());
  return form.getEditUrl();
}

// ─────────────────────────────────────────────────────────────
// ANEXO F — SSQ Cybersickness (reutilizable para antes y después)
// ─────────────────────────────────────────────────────────────
function crearFormSSQ(titulo) {
  const form = FormApp.create(titulo || "PT-A TITA — SSQ Cybersickness (Anexo F)");
  form.setDescription("Indica la intensidad de cada síntoma AHORA MISMO.\n0 = Ninguno  |  1 = Leve  |  2 = Moderado  |  3 = Severo");
  form.setCollectEmail(false);

  form.addTextItem()
    .setTitle("Código de participante")
    .setRequired(true);

  const sintomas = [
    "Malestar general",
    "Fatiga",
    "Dolor de cabeza",
    "Fatiga visual",
    "Dificultad para enfocar",
    "Náusea",
    "Mareo (con ojos abiertos)",
    "Sensación de vértigo"
  ];

  sintomas.forEach(s => {
    form.addMultipleChoiceItem()
      .setTitle(s)
      .setChoiceValues(["0 — Ninguno", "1 — Leve", "2 — Moderado", "3 — Severo"])
      .setRequired(true);
  });

  Logger.log("Form SSQ creado: " + form.getEditUrl());
  return form.getEditUrl();
}
