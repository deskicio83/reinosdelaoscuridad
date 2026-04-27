# Ideas para Evolutivo — Reino de la Oscuridad
_Archivo de ideas descartadas o pospuestas para versiones futuras_
_Actualizar cuando surjan nuevas ideas durante el desarrollo_

---

## Gestión de equipos

- **Equipos guardados múltiples**: el jugador podría guardar varios equipos predefinidos
  con nombre y seleccionar entre ellos en el TeamSelectPanel antes de entrar a combate.
  Útil para jugadores con muchos esbirros que alternan equipos según el contenido.
  _Origen: sesión S18b — decisión de implementar solo equipo puntual en MVP_

---

## Tutorial de bienvenida

- **Tutorial inicial (S33)**: cuando se implemente TutorialScene, cambiar
  `tutorialCompleted = true` a `false` en `DataStorageSystem.CreateNewPlayerData()`
  para que el jugador nuevo pase por el tutorial antes de poder jugar.
  Actualmente se pone `true` para saltar al MainMenu directamente (MVP sin tutorial).
  _Origen: sesión S28_newplayer_

---

## Post-combate

- **Navegación directa a la siguiente fase**: desde la pantalla de recompensas
  post-combate, botón "Siguiente fase" que prepara el CombatContext de la fase
  siguiente y navega directamente sin volver al mapa de CampaignScene.
  _Origen: sesión S18b_

---

## Arena / PvP

_(pendiente)_

---

## Social

_(pendiente)_

---

## Economía

_(pendiente)_

---

## UX / UI

_(pendiente)_
