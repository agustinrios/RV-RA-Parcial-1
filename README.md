# Patrimonio AR

Aplicación de **realidad aumentada** para Android: apuntás la cámara del celular a una carta
impresa y el monumento se levanta en 3D sobre ella, con su ficha de datos.

Tres cartas de patrimonio argentino: el **Obelisco**, las **Cataratas del Iguazú** y el
**Glaciar Perito Moreno**.

Lo que ordena el proyecto es que los tres modelos están **a la misma escala, 1:400**. Eso permite
apoyar las cartas juntas y compararlas: el Perito Moreno, de 70 m, queda apenas más alto que el
Obelisco, de 67,5 m — algo que ninguna fotografía muestra.

## Cómo funciona

- **Image tracking** con AR Foundation + ARCore: cada carta es una *reference image* de la
  biblioteca de seguimiento.
- Al detectar una carta, `ImageTrackingSpawner` instancia el modelo correspondiente y lo ancla;
  `FichaBillboard` mantiene la ficha de datos orientada hacia la cámara.
- `PanelEstadoTracking` muestra el estado del seguimiento (buscando / detectado / perdido).
- Animaciones propias de cada pieza: `CascadaAnimada` para las Cataratas y `DesprendimientoHielo`
  para el Perito Moreno.
- `Assets/Scripts/Editor/` tiene las herramientas que armaron la escena y las cartas:
  `GeneradorPatrimonioAR`, `EntornoSimulacionPatrimonio`, `ConfiguradorProyectoAR` y
  `GeneradorPostales`.

## Probarlo

**En el celular:** instalar el APK (hace falta un dispositivo compatible con ARCore, Android 7.1+)
e imprimir las cartas desde `DOCUMENTACION/Cartas-15x17-Rios-Daniel-Agustin.pdf`, a tamaño real
(15 × 17 cm). La escala de impresión importa: el tracking usa el tamaño físico de la imagen.

**En la computadora, sin celular:** el proyecto incluye **XR Simulation**, así que la escena corre
en el editor sobre un entorno simulado sin necesidad de dispositivo.

## Documentación

La carpeta [`DOCUMENTACION/`](DOCUMENTACION/) tiene el **GDD** completo, el PDF de cartas
imprimibles y el guion del video de entrega, además de los scripts de PowerShell que se usaron
para generar y verificar las medidas de las cartas.

## Requisitos

- Unity **6000.3.17f1** con Android Build Support
- AR Foundation 6.3.5 / ARCore 6.3.5, URP
- Android 7.1+ (minSdk 25) con soporte ARCore

## Autor

**Rios Daniel Agustín** — Programación de Realidad Virtual y Aumentada, UTN.
Parcial 1 · `com.rios.patrimonioar`
