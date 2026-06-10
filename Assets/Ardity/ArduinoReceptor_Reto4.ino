/*
 * TITA Serious Game — Sketch receptor para Arduino Físico (Reto 4)
 *
 * Sube este sketch al Arduino UNO antes de conectarlo al PC del juego.
 * El sketch espera comandos de Unity via Ardity (puerto serial 9600 baud).
 *
 * PROTOCOLO DE ENTRADA (Unity → Arduino):
 *   "PIN:2,MODE:O,STATE:H,BLINK:500\n"
 *   - PIN   : número de pin digital (2, 4, 9, 10, 13…)
 *   - MODE  : O = OUTPUT  |  I = INPUT
 *   - STATE : H = HIGH    |  L = LOW
 *   - BLINK : 0 = sin blink | N = intervalo en ms
 *
 * PROTOCOLO DE SALIDA (Arduino → Unity):
 *   "V:4.97,I:15.2,ADC:890,PIN:2\n"
 *   - V   : voltaje en pin activo (V)
 *   - I   : corriente estimada (mA)
 *   - ADC : lectura analógica de A0 (0–1023)
 *   - PIN : pin activo actual
 *
 * CIRCUITO FÍSICO SUGERIDO (para el reto):
 *   - LED + resistor 330Ω entre pin D2 y GND
 *   - Sensor (LDR o potenciómetro) entre 5V y A0, con R a GND
 */

// ── Estado actual ──────────────────────────────────────
int   activePIN   = 4;      // Empieza con el pin incorrecto (la falla del reto)
bool  isOutput    = true;
bool  isHigh      = true;
bool  blinkMode   = true;
int   blinkMs     = 500;
bool  blinkState  = false;
unsigned long lastBlink = 0;

// ── Telemetría ─────────────────────────────────────────
unsigned long lastTelemetry = 0;
const int TELEMETRY_MS = 200;   // enviar telemetría cada 200ms

// ── Buffer serial ──────────────────────────────────────
String inputBuffer = "";

void setup() {
  Serial.begin(9600);
  pinMode(activePIN, OUTPUT);
  delay(200);
  Serial.println("TITA:READY");
}

void loop() {
  // ── Leer comandos de Unity ──────────────────────────
  while (Serial.available()) {
    char c = (char)Serial.read();
    if (c == '\n') {
      processCommand(inputBuffer);
      inputBuffer = "";
    } else {
      inputBuffer += c;
    }
  }

  // ── Ejecutar sketch activo ──────────────────────────
  if (isOutput) {
    if (blinkMode) {
      unsigned long now = millis();
      if (now - lastBlink >= (unsigned long)blinkMs) {
        lastBlink  = now;
        blinkState = !blinkState;
        digitalWrite(activePIN, blinkState ? HIGH : LOW);
      }
    } else {
      digitalWrite(activePIN, isHigh ? HIGH : LOW);
    }
  }

  // ── Enviar telemetría ───────────────────────────────
  if (millis() - lastTelemetry >= TELEMETRY_MS) {
    lastTelemetry = millis();
    sendTelemetry();
  }
}

// ── Parsear comando "PIN:2,MODE:O,STATE:H,BLINK:500" ──
void processCommand(String cmd) {
  cmd.trim();
  if (cmd.length() == 0) return;

  int newPin = extractInt(cmd, "PIN:");
  if (newPin > 0) {
    // Apagar pin anterior antes de cambiar
    if (isOutput) digitalWrite(activePIN, LOW);
    activePIN = newPin;
  }

  String modeStr = extractStr(cmd, "MODE:");
  if (modeStr == "O") {
    isOutput = true;
    pinMode(activePIN, OUTPUT);
  } else if (modeStr == "I") {
    isOutput = false;
    pinMode(activePIN, INPUT);
  }

  String stateStr = extractStr(cmd, "STATE:");
  if (stateStr == "H") isHigh = true;
  else if (stateStr == "L") isHigh = false;

  int blink = extractInt(cmd, "BLINK:");
  blinkMode = (blink > 0);
  if (blinkMode) blinkMs = blink;

  // Confirmación
  Serial.print("ACK:PIN:");
  Serial.println(activePIN);
}

// ── Enviar telemetría a Unity ──────────────────────────
void sendTelemetry() {
  // Voltaje en el pin activo (estimado desde estado)
  float voltage = 0.0f;
  if (isOutput) {
    bool pinHigh = blinkMode ? blinkState : isHigh;
    voltage = pinHigh ? 4.97f : 0.0f;
  } else {
    voltage = digitalRead(activePIN) == HIGH ? 4.97f : 0.0f;
  }

  // Corriente estimada (asumiendo LED + 330Ω)
  float currentMA = (voltage > 0) ? (voltage / 330.0f * 1000.0f) : 0.0f;

  // Lectura analógica
  int adcVal = analogRead(A0);

  Serial.print("V:");    Serial.print(voltage, 2);
  Serial.print(",I:");   Serial.print(currentMA, 1);
  Serial.print(",ADC:"); Serial.print(adcVal);
  Serial.print(",PIN:"); Serial.println(activePIN);
}

// ── Helpers de parseo ──────────────────────────────────
int extractInt(String s, String key) {
  int idx = s.indexOf(key);
  if (idx < 0) return -1;
  int start = idx + key.length();
  int end   = start;
  while (end < s.length() && (isDigit(s[end]) || s[end] == '-')) end++;
  return s.substring(start, end).toInt();
}

String extractStr(String s, String key) {
  int idx = s.indexOf(key);
  if (idx < 0) return "";
  int start = idx + key.length();
  int end   = start;
  while (end < s.length() && s[end] != ',' && s[end] != '\n') end++;
  return s.substring(start, end);
}
