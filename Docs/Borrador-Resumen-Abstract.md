# Borrador — Resumen y Abstract (Documento Capstone TITA)

> Redactado el 2026-06-02. El Resumen respeta el límite de 350 palabras y cierra con
> palabras clave (§2 del documento). Los resultados declarados son los efectivamente
> obtenidos (PT-C/PT-I/PT-S); la usabilidad (PT-A) se describe como fase de cierre, sin
> atribuir cifras aún no relevadas.

---

## 2. Resumen

La asignatura de Computación Ubicua de la Universidad de Las Américas (UDLA) presenta
dificultades en el aprendizaje de la electrónica básica: los estudiantes no logran visualizar
conceptos abstractos como el voltaje, la corriente y la resistencia, carecen de
retroalimentación inmediata ante el fallo de un circuito, temen dañar los componentes físicos
y disponen de horas limitadas de laboratorio. Para atender esta necesidad —diagnosticada
mediante una encuesta exploratoria a 20 estudiantes y un análisis FODA— se desarrolló un
Serious Game multijugador en realidad virtual orientado a reforzar la comprensión práctica y
teórica de la electrónica básica.

El proyecto se ejecutó bajo una metodología en cascada de cinco fases. A partir de una matriz
de evaluación basada en la norma ISO/IEC 25010 se seleccionó una solución de colaboración
asimétrica, en la que dos usuarios asumen roles diferenciados —el Explorador, que manipula
los circuitos en realidad virtual con un visor Meta Quest 3, y el Técnico, que diagnostica y
programa desde un computador— para resolver cuatro retos secuenciales de dificultad
creciente (Ley de Ohm, circuito en paralelo, circuito mixto con polaridad y sincronización
hardware/software con Arduino), ambientados en la narrativa de una nave espacial averiada. El
prototipo se implementó en Unity con comunicación multijugador mediante Photon Fusion 2.

La verificación se organizó conforme a la norma ISO/IEC/IEEE 29119 en cuatro niveles. Las
pruebas de componente validaron analíticamente el motor de simulación eléctrica (nueve
casos, 100 % de aprobación); las pruebas de integración y de sistema confirmaron la
comunicación entre módulos y el flujo completo de los cuatro retos en red local; y se
corrigieron siete defectos siguiendo ISO 29119-3. La evaluación de usabilidad con usuarios,
mediante la System Usability Scale y una escala Likert, constituye la fase de cierre, con su
protocolo e instrumentos ya elaborados.

Se concluye que la realidad virtual combinada con la colaboración asimétrica es un enfoque
viable para transformar una práctica de laboratorio limitada en una experiencia segura,
repetible y con retroalimentación inmediata.

**Palabras clave:** realidad virtual, serious game, electrónica básica, aprendizaje colaborativo,
multijugador asimétrico, gamificación, Computación Ubicua.

---

## 3. Abstract

The Ubiquitous Computing course at Universidad de Las Américas (UDLA) faces difficulties in
the teaching of basic electronics: students struggle to visualize abstract concepts such as
voltage, current and resistance, lack immediate feedback when a circuit fails, fear damaging
physical components, and have limited laboratory time. To address this need —diagnosed
through an exploratory survey of 20 students and a SWOT analysis— a multiplayer virtual
reality serious game was developed to strengthen both the practical and theoretical
understanding of basic electronics.

The project was carried out under a five-phase waterfall methodology. Based on an evaluation
matrix grounded in the ISO/IEC 25010 standard, an asymmetric collaboration solution was
selected, in which two users take on differentiated roles —the Explorer, who manipulates the
circuits in virtual reality using a Meta Quest 3 headset, and the Technician, who diagnoses and
programs from a computer— to solve four sequential challenges of increasing difficulty (Ohm's
law, parallel circuit, mixed circuit with polarity, and hardware/software synchronization with
Arduino), set within the narrative of a damaged spaceship. The prototype was implemented in
Unity with multiplayer communication through Photon Fusion 2.

Verification was organized according to the ISO/IEC/IEEE 29119 standard across four levels.
Component tests analytically validated the electrical simulation engine (nine cases, 100 % pass
rate); integration and system tests confirmed inter-module communication and the complete
flow of the four challenges over a local network; and seven defects were corrected following
ISO 29119-3. The usability evaluation with users, through the System Usability Scale and a
Likert scale, constitutes the closing phase, with its protocol and instruments already prepared.

It is concluded that virtual reality combined with asymmetric collaboration is a viable approach
to transform a constrained laboratory practice into a safe, repeatable experience with
immediate feedback.

**Keywords:** virtual reality, serious game, basic electronics, collaborative learning, asymmetric
multiplayer, gamification, Ubiquitous Computing.
