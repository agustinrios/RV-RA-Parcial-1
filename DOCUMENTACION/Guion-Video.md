# Guion del video — Patrimonio AR

**Rios Daniel Agustin** · Programación de Realidad Virtual y Aumentada (UTN)

El orden es el de la consigna: el High Concept sobre el GDD, después el proyecto de Unity en
detalle, y el prototipo funcionando recién al final.

Lo que está en letra normal se lee de corrido, como si le estuvieras explicando el proyecto a
alguien. Lo que está *en cursiva entre corchetes* es lo que mostrás en ese momento, no se dice.

---

## PARTE 1 · El High Concept — 0:00 a 0:35

*[El GDD abierto en la página 1.]*

Hola, soy Rios Daniel Agustín, de Programación de Realidad Virtual y Aumentada. Les presento
Patrimonio AR.

Son tres cartas de patrimonio argentino: el Obelisco, las Cataratas del Iguazú y el Glaciar
Perito Moreno. Apunto el celular a cualquiera de ellas y el lugar se levanta en tres dimensiones
sobre la carta, con su ficha de datos.

Lo que ordena todo el proyecto es que los tres están a la misma escala, uno a cuatrocientos. Así
puedo apoyarlas juntas y compararlas: el Perito Moreno, de 70 metros, queda apenas más alto que el
Obelisco, de 67 y medio. Ninguna fotografía muestra eso.

---

## PARTE 2 · El problema y el usuario — 0:35 a 1:05

*[Pasás a la página 2 del GDD.]*

¿Qué problema resuelve? Sobre estos lugares hay información de sobra, pero no hay manera de
dimensionarla. Si leo que el Obelisco mide 67 metros, ese número no me arma
ninguna imagen en la cabeza. El problema no es la falta de información, es la falta de escala
percibida.

Patrimonio AR ataca eso: pone los tres lugares sobre la mesa, uno al lado del otro. Lo pensé para estudiantes de secundaria, con las cartas repartidas por
grupo, y para visitantes de museos.

---

## PARTE 3 · Por qué estas imágenes — 1:05 a 1:40

*[Página 3 del GDD, donde están las tres cartas.]*

Estas son las marcadoras. Son fotos de los propios lugares, y las elegí por cómo las ve ARCore,
que no reconoce una imagen por lo que muestra sino por sus puntos característicos:
esquinas y cambios de contraste que pueda volver a encontrar desde otro ángulo.

Por eso busqué fotos con el detalle repartido por todo el cuadro, con contraste fuerte en blanco y
negro y sin patrones repetidos. Las medí con arcoreimg, que viene con el plugin de ARCore, y me
dieron 90, 100 y 95 sobre 100.

*[Opcional, solo si vas sobrado de tiempo:]* El formato salió de la misma prueba: una carta más
alargada obligaba a recortar las fotos por los costados y el Obelisco se caía de 100 a 25, así
que la dejé casi cuadrada.

---

## PARTE 4 · El proyecto en Unity — 1:40 a 2:55

Es la parte que más pesa. Mostrá cada cosa justo cuando la nombrás, sin apurarte.

*[Unity, con el Hierarchy a la vista.]*

Vamos al proyecto. Está hecho en Unity 6000.3.17f1 con AR Foundation 6.3.5 y el plugin de Google
ARCore. En la escena están los dos objetos base: el AR Session y el XR Origin, que lleva la
cámara.

*[Clic en `Assets/Recursos/PatrimonioLibrary.asset`.]*

Esta es la Reference Image Library. A cada carta le declaré su tamaño físico real, quince por
diecisiete centímetros, que es lo que miden impresas. Con ese dato el sistema calcula a qué distancia
está la carta: si no coincide con el papel, el modelo sale del tamaño equivocado.

*[Clic en el XR Origin, mostrando el AR Tracked Image Manager.]*

Acá está el AR Tracked Image Manager, con la library asignada y el Max Number Of Moving Images en
tres, que es lo que me deja rastrear las tres cartas al mismo tiempo.

*[Abrís `ImageTrackingSpawner.cs`.]*

Y este es el script que escribí. Me suscribo al evento trackablesChanged, que me avisa cuando
aparece una carta, cuando cambia el estado de una que ya estaba y cuando desaparece.

Tomé dos decisiones. Instancio el diorama como hijo del objeto que representa a la imagen, así la
sigue solo. Y cuando se pierde el seguimiento no lo destruyo, lo escondo: destruirlo obligaría a
rearmarlo cada vez, y eso se vería como un parpadeo.

*[Los tres prefabs en `Assets/Prefabs`.]*

Estos son los tres dioramas. Los modelé por código, cada uno en metros reales y escalado uno a
cuatrocientos.

---

## PARTE 5 · El prototipo funcionando — 2:55 a 3:30

*[Le das Play. Game view con XR Simulation.]*

Por último, lo ejecuto. Esto es XR Simulation, que me simula un entorno real dentro del editor:
las tres cartas apoyadas sobre una mesa.

Me acerco a la primera y el diorama se levanta con su ficha. Si me muevo alrededor, queda fijo
sobre la carta.

*[Abrís el plano hasta que entren las tres.]*

Abro el plano y entran las tres a la vez: el panel de abajo a la derecha confirma las tres
rastreadas. Esto mismo corre en el celular, con la APK ya compilada.

Gracias.

---

## Cómo moverte en XR Simulation

Con la Game view enfocada: **clic derecho sostenido** para mirar, y mientras lo tenés apretado,
**W A S D** para desplazarte y **Q / E** para bajar y subir. Movete despacio, que la cámara no dé
saltos.

## Antes de grabar

- Cerrá todo lo que no sea Unity. Que no entre ninguna notificación.
- Dejá abiertos Hierarchy, Project e Inspector, y agrandá la letra si hace falta.
- Grabá las partes por separado y después pegalas: es mucho más fácil que hacerlo de una.
- Leé el texto en voz alta una vez antes de grabar, para que no suene leído.
- El texto son unas 585 palabras: a ritmo normal da cerca de 3:50, dentro del limite de 4
  minutos. Cronometrate leyendolo una vez. Si te vas largo, lo primero que sale es el parrafo
  marcado como opcional en la parte 3, y despues la ultima frase de la parte 5.

## Al entregar

- Exportá en mp4.
- Subilo a YouTube como **Oculto** (oculto, no privado) o a Drive con acceso público.
- El archivo y el link tienen que llevar tu nombre completo.
